using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amry.Gst.Web.Properties;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;

namespace Amry.Gst.Web.Models
{
    class GstAzureStorage : IGstDataSource
    {
        static readonly CloudTable Table;
        static readonly CloudQueue DeletionQueue;

        readonly IGstDataSource _dataSource;

        static GstAzureStorage()
        {
            var account = CloudStorageAccount.Parse(Settings.Default.AzureStorage);
            var createTasks = new List<Task>();

            {
                var servicePoint = ServicePointManager.FindServicePoint(account.TableEndpoint);
                servicePoint.UseNagleAlgorithm = false;
                servicePoint.Expect100Continue = false;
                servicePoint.ConnectionLimit = 100;

                var client = account.CreateCloudTableClient();
                Table = client.GetTableReference("gst");
                createTasks.Add(Table.CreateIfNotExistsAsync());
            }

            {
                var servicePoint = ServicePointManager.FindServicePoint(account.QueueEndpoint);
                servicePoint.UseNagleAlgorithm = false;
                servicePoint.Expect100Continue = false;
                servicePoint.ConnectionLimit = 100;

                var client = account.CreateCloudQueueClient();
                DeletionQueue = client.GetQueueReference("gst-delete");
                createTasks.Add(DeletionQueue.CreateIfNotExistsAsync());
            }

            Task.WaitAll(createTasks.ToArray());
        }

        public GstAzureStorage(IGstDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IList<IGstLookupResult>> LookupGstDataAsync(GstLookupInputType inputType, string input, bool validateInput = false)
        {
            if (validateInput) {
                GstInputValidator.ValidateInput(inputType, input);
            }

            switch (inputType) {
                case GstLookupInputType.GstNumber: {
                    var result = await RetrieveOrLookupAsync(inputType, input);
                    if (result == null) {
                        return new IGstLookupResult[0];
                    }

                    InsertOrReplaceAsync(CachedGstEntity.CreateForGstNumberQuery(result));
                    return new[] {result};
                }

                case GstLookupInputType.BusinessRegNumber: {
                    var result = await RetrieveOrLookupAsync(inputType, input);
                    if (result == null) {
                        return new IGstLookupResult[0];
                    }

                    InsertOrReplaceAsync(CachedGstEntity.CreateForBusinessRegNumberQuery(result, input));
                    InsertOrReplaceAsync(CachedGstEntity.CreateForGstNumberQuery(result));
                    return new[] {result};
                }

                case GstLookupInputType.BusinessName: {
                    // Retrieve all entities of the given Partition Key.
                    var resultsQuery = Table.CreateQuery<CachedGstEntity>()
                        .Where(e => e.PartitionKey == CachedGstEntity.GetPartitionKeyForBusinessNameQuery(input));

                    var cachedResults = new List<IGstLookupResult>();
                    TableContinuationToken continuationToken = null;
                    do {
                        var segment = await resultsQuery.AsTableQuery().ExecuteSegmentedAsync(continuationToken);
                        cachedResults.AddRange(segment.Results);
                        continuationToken = segment.ContinuationToken;
                    } while (continuationToken != null);

                    // Return if there is any entity in cache.
                    if (cachedResults.Count > 0) {
                        return cachedResults;
                    }

                    // Lookup Customs' server and cache the results.
                    IList<IGstLookupResult> lookupResults;
                    try {
                        lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
                    } catch (CustomsGstException ex) {
                        if (ex.KnownErrorCode == KnownCustomsGstErrorCode.Over100Results) {
                            InsertOrReplaceAsync(CachedGstEntity.CreateForError(inputType, input, KnownCustomsGstErrorCode.Over100Results));
                        }
                        throw;
                    }

                    if (lookupResults.Count == 0) {
                        InsertOrReplaceAsync(CachedGstEntity.CreateForError(inputType, input, KnownCustomsGstErrorCode.NoResult));
                        return lookupResults;
                    }

                    BatchInsertOrReplaceAsync(lookupResults.SelectMany((result, i) => new[] {
                        CachedGstEntity.CreateForGstNumberQuery(result),
                        CachedGstEntity.CreateForBusinessNameQuery(result, input, i)
                    }));

                    // Mark for deletion at 3 AM two days later.
                    var twoDaysLater = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).AddDays(2);
                    var atThreeAm = twoDaysLater.AddHours(3 - twoDaysLater.Hour);
                    var timeDiffFromNow = atThreeAm - DateTime.Now;
                    DeletionQueue.AddMessageAsync(
                        new CloudQueueMessage(CachedGstEntity.GetPartitionKeyForBusinessNameQuery(input) + ":" + lookupResults.Count),
                        null, timeDiffFromNow, null, null);

                    return lookupResults;
                }
            }

            // Code should not be able to reach here
            throw new NotSupportedException();
        }

        async Task<IGstLookupResult> RetrieveOrLookupAsync(GstLookupInputType inputType, string input)
        {
            var cachedResult = await RetrieveAsync(inputType, input);
            if (cachedResult != null) {
                return cachedResult;
            }

            var lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
            if (lookupResults.Count == 0) {
                InsertOrReplaceAsync(CachedGstEntity.CreateForError(inputType, input, KnownCustomsGstErrorCode.NoResult));
            }

            return lookupResults.FirstOrDefault();
        }

        static async Task<CachedGstEntity> RetrieveAsync(GstLookupInputType inputType, string input)
        {
            TableOperation getOp;

            switch (inputType) {
                case GstLookupInputType.GstNumber:
                    getOp = TableOperation.Retrieve<CachedGstEntity>(CachedGstEntity.PartitionKeyForGstNumber, CachedGstEntity.GetRowKeyForGstNumber(input));
                    break;

                case GstLookupInputType.BusinessRegNumber:
                    getOp = TableOperation.Retrieve<CachedGstEntity>(CachedGstEntity.PartitionKeyForBusinessRegNumber, CachedGstEntity.GetRowKeyForBusinessRegNumber(input));
                    break;

                default:
                    throw new NotSupportedException();
            }

            var getResult = await Table.ExecuteAsync(getOp);
            return (CachedGstEntity) getResult.Result;
        }

        static Task InsertOrReplaceAsync(CachedGstEntity entity)
        {
            var insertOp = TableOperation.InsertOrReplace(entity);
            return Table.ExecuteAsync(insertOp);
        }

        static Task BatchInsertOrReplaceAsync(IEnumerable<CachedGstEntity> entities)
        {
            var entitiesLookup = entities.ToLookup(entity => entity.PartitionKey);
            return Task.WhenAll(entitiesLookup.Select(BatchInsertOrReplacePartitionAsync));
        }

        static Task BatchInsertOrReplacePartitionAsync(IEnumerable<CachedGstEntity> entities)
        {
            var batchTasks = new List<Task>();
            var batchOp = new TableBatchOperation();

            foreach (var entity in entities) {
                batchOp.InsertOrReplace(entity);
                if (batchOp.Count < 100) {
                    continue;
                }
                batchTasks.Add(Table.ExecuteBatchAsync(batchOp));
                batchOp = new TableBatchOperation();
            }

            if (batchOp.Count > 0) {
                batchTasks.Add(Table.ExecuteBatchAsync(batchOp));
            }

            return Task.WhenAll(batchTasks);
        }
    }
}
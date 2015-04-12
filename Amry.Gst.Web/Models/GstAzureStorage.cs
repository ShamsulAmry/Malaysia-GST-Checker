using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amry.Gst.Web.Properties;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;

namespace Amry.Gst.Web.Models
{
    class GstAzureStorage : IGstDataSource
    {
        static readonly CloudTable Table;

        readonly IGstDataSource _dataSource;

        static GstAzureStorage()
        {
            var account = CloudStorageAccount.Parse(Settings.Default.AzureStorage);
            var tableServicePoint = ServicePointManager.FindServicePoint(account.TableEndpoint);
            tableServicePoint.UseNagleAlgorithm = false;
            tableServicePoint.Expect100Continue = false;
            tableServicePoint.ConnectionLimit = 100;

            var client = account.CreateCloudTableClient();
            Table = client.GetTableReference("gst");
            Table.CreateIfNotExists();
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
                    var getOp = TableOperation.Retrieve<CachedGstEntity>(
                        CachedGstEntity.PartitionKeyForGstNumber,
                        CachedGstEntity.GetRowKeyForGstNumber(input));
                    var getResult = await Table.ExecuteAsync(getOp);
                    if (getResult.Result != null) {
                        return new[] {(IGstLookupResult) getResult.Result};
                    }

                    var lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
                    if (lookupResults.Count == 0) {
                        return lookupResults;
                    }

                    var cachedResult = CachedGstEntity.CreateForGstNumberQuery(lookupResults[0]);
                    var insertOp = TableOperation.Insert(cachedResult);
                    Table.ExecuteAsync(insertOp);
                    return lookupResults;
                }

                case GstLookupInputType.BusinessRegNumber: {
                    var getOp = TableOperation.Retrieve<CachedGstEntity>(
                        CachedGstEntity.PartitionKeyForBusinessRegNumber,
                        CachedGstEntity.GetRowKeyForBusinessRegNumber(input));
                    var getResult = await Table.ExecuteAsync(getOp);
                    if (getResult.Result != null) {
                        return new[] {(IGstLookupResult) getResult.Result};
                    }

                    var lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
                    if (lookupResults.Count == 0) {
                        return lookupResults;
                    }

                    {
                        // Insert business reg number partition key
                        var cachedResult = CachedGstEntity.CreateForBusinessRegNumberQuery(lookupResults[0], input);
                        var insertOp = TableOperation.Insert(cachedResult);
                        Table.ExecuteAsync(insertOp);
                    }
                    {
                        // Insert GST number partition key
                        var cachedResult = CachedGstEntity.CreateForGstNumberQuery(lookupResults[0]);
                        var insertOp = TableOperation.Insert(cachedResult, true);
                        Table.ExecuteAsync(insertOp);
                    }
                    return lookupResults;
                }

                case GstLookupInputType.BusinessName: {
                    var resultsQuery = Table.CreateQuery<CachedGstEntity>()
                        .Where(e => e.PartitionKey == CachedGstEntity.GetPartitionKeyForBusinessNameQuery(input));

                    var cachedResults = new List<IGstLookupResult>();
                    TableContinuationToken continuationToken = null;
                    do {
                        var segment = await resultsQuery.AsTableQuery().ExecuteSegmentedAsync(continuationToken);
                        cachedResults.AddRange(segment.Results);
                        continuationToken = segment.ContinuationToken;
                    } while (continuationToken != null);
                    if (cachedResults.Count > 0) {
                        return cachedResults;
                    }

                    var lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
                    if (lookupResults.Count == 0) {
                        return lookupResults;
                    }

                    BatchInsertOrReplaceAsync(lookupResults.SelectMany((result, i) => new[] {
                        CachedGstEntity.CreateForGstNumberQuery(result),
                        CachedGstEntity.CreateForBusinessNameQuery(result, input, i)
                    }));
                    return lookupResults;
                }
            }

            // Code should not be able to reach here
            throw new NotSupportedException();
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
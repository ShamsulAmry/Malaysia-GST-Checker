using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Retrieve all entities of the given Partition Key.
            var partitionKey = CachedGstEntity.GetPartitionKey(inputType, input);
            var resultsQuery = Table.CreateQuery<CachedGstEntity>().Where(e => e.PartitionKey == partitionKey);
            var cachedResults = new List<IGstLookupResult>();
            
            TableContinuationToken continuationToken = null;
            do {
                var segment = await resultsQuery.AsTableQuery().ExecuteSegmentedAsync(continuationToken);
                cachedResults.AddRange(segment.Results);
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            // Return if there is any entity in the cache.
            if (cachedResults.Count > 0) {
                return cachedResults;
            }

            // Lookup Customs' server and cache the results.
            IList<IGstLookupResult> lookupResults;
            try {
                lookupResults = await _dataSource.LookupGstDataAsync(inputType, input);
            } catch (CustomsGstException ex) {
                if (ex.KnownErrorCode == KnownCustomsGstErrorCode.Over100Results) {
#pragma warning disable 4014
                    InsertAsync(CachedGstEntity.CreateForError(
                        inputType, input, KnownCustomsGstErrorCode.Over100Results));
                }
#pragma warning restore 4014
                throw;
            }

            if (lookupResults.Count == 0) {
#pragma warning disable 4014
                InsertAndScheduleDeleteAsync(CachedGstEntity.CreateForError(
                    inputType, input, KnownCustomsGstErrorCode.NoResult), 6);
                return lookupResults;
#pragma warning restore 4014
            }

            if (lookupResults[0].IsLiveData) {
                CachedGstEntity lastCachedResult = null;

                var batchOp = new TableBatchOperation();
                for (var i = 0; i < lookupResults.Count; i++) {
                    lastCachedResult = CachedGstEntity.CreateForResult(inputType, input, lookupResults[i], i);
                    batchOp.InsertOrReplace(lastCachedResult);
                }
#pragma warning disable 4014
                Table.ExecuteBatchAsync(batchOp);
#pragma warning restore 4014

                if (inputType == GstLookupInputType.BusinessName) {
                    Debug.Assert(lastCachedResult != null, "cacheResult shouldn't be null here");
#pragma warning disable 4014
                    ScheduleDeleteAsync(lastCachedResult, 6);
#pragma warning restore 4014
                }
            }

            return lookupResults;
        }

        static Task InsertAsync(CachedGstEntity result)
        {
            return Table.ExecuteAsync(TableOperation.InsertOrReplace(result));
        }

        static Task ScheduleDeleteAsync(CachedGstEntity lastResult, int dayInterval)
        {
            var nDaysLater = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).AddDays(dayInterval);
            var atThreeAm = nDaysLater.AddHours(3 - nDaysLater.Hour);
            var timeDiffFromNow = atThreeAm - DateTime.Now;

            return DeletionQueue.AddMessageAsync(
                new CloudQueueMessage(lastResult.PartitionKey + ":" + lastResult.RowKey),
                null, timeDiffFromNow, null, null);
        }

        static Task InsertAndScheduleDeleteAsync(CachedGstEntity result, int dayInterval)
        {
            return Task.WhenAll(InsertAsync(result), ScheduleDeleteAsync(result, dayInterval));
        }
    }
}
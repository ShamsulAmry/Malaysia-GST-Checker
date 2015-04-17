using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amry.Gst.DeleteCache.Properties;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Table;

namespace Amry.Gst.DeleteCache
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    public static class Program
    {
        static readonly List<Task> Tasks = new List<Task>();

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            ExecuteJobHost().Wait();
        }

        static async Task ExecuteJobHost()
        {
            using (var host = new JobHost()) {
                await host.StartAsync();
                await Task.Delay(30000);
                await Task.WhenAll(Tasks);
                await host.StopAsync();
            }
        }

        public static Task ProcessDeleteCacheRequest(
            [QueueTrigger("gst-delete")] string keyInfo,
            [Table("gst")] CloudTable table,
            CancellationToken cancellationToken,
            TextWriter logger)
        {
            var task = DeleteCache(keyInfo, table, cancellationToken, logger);
            Tasks.Add(task);
            return task;
        }

        static async Task DeleteCache(string keyInfo, CloudTable table, CancellationToken cancellationToken, TextWriter logger)
        {
            var separatorIndex = keyInfo.LastIndexOf(':');
            var partitionKey = keyInfo.Substring(0, separatorIndex);
            var lastRowIndex = Convert.ToInt32(keyInfo.Substring(separatorIndex + 1));

#pragma warning disable 4014
            logger.LogAsync(Resources.DeletingPartitionLog, partitionKey);
#pragma warning restore 4014

            if (lastRowIndex == 0) {
                var deleteOp = TableOperation.Delete(new TableEntity(partitionKey, "000") {ETag = "*"});
                await table.ExecuteAsync(deleteOp, cancellationToken);
            } else {
                var batchTasks = new List<Task>();
                var batchOp = new TableBatchOperation();

                for (var i = 0; i <= lastRowIndex; i++) {
                    batchOp.Delete(new TableEntity(partitionKey, i.ToString("000")) {ETag = "*"});
                    if ((i + 1)%100 != 0) {
                        continue;
                    }
                    batchTasks.Add(table.ExecuteBatchAsync(batchOp, cancellationToken));
                    batchOp = new TableBatchOperation();
                }

                if (batchOp.Count > 0) {
                    batchTasks.Add(table.ExecuteBatchAsync(batchOp, cancellationToken));
                }

                await Task.WhenAll(batchTasks);
            }

#pragma warning disable 4014
            logger.LogAsync(Resources.PartitionDeletedLog, partitionKey);
#pragma warning restore 4014
        }
    }
}
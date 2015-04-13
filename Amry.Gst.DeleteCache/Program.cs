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
            var task = ProcessDeleteCacheRequestCore(keyInfo, table, cancellationToken, logger);
            Tasks.Add(task);
            return task;
        }

        static async Task ProcessDeleteCacheRequestCore(string keyInfo, CloudTable table, CancellationToken cancellationToken, TextWriter logger)
        {
            var separatorIndex = keyInfo.LastIndexOf(':');
            var partitionKey = keyInfo.Substring(0, separatorIndex);
            var rowCount = Convert.ToInt32(keyInfo.Substring(separatorIndex + 1));

            logger.LogAsync(Resources.DeletingPartitionLog, partitionKey);

            var batchTasks = new List<Task>();
            var batchOp = new TableBatchOperation();
            for (var i = 0; i < rowCount; i++) {
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

            logger.LogAsync(Resources.PartitionDeletedLog, partitionKey);
        }
    }
}
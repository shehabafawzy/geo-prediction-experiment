using GeoPrediction.Portable.Entities;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoPrediction.ProcessorWorkerRole.EventProcessing
{
    public class TrainingEventProcessor : IEventProcessor
    {
        #region Private Fields
        private string storageConnectionString;
        private string azureTableName;
        #endregion

        #region Public Constructors
        public TrainingEventProcessor(string storageConnectionString, string azureTableName)
        {
            this.storageConnectionString = storageConnectionString;
            this.azureTableName = azureTableName;
        }
        #endregion

        #region IEventProcessor Methods
        public Task OpenAsync(PartitionContext context)
        {
            try
            {
                Trace.TraceInformation("Opening partition {0}, {1}, {2}", context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId);
            }
            catch (Exception ex)
            {
                // Trace Exception 
                Trace.TraceError(ex.Message + ex.InnerException != null ? ex.InnerException.Message : string.Empty);
            }
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            try
            {
                if (events == null || (events != null && events.Count() == 0))
                {
                    Trace.TraceInformation("ProcessEventsAsync called with no events to process. Returning.");
                    return;
                }
                var eventDataList = events as IList<EventData> ?? events.ToList();

                Trace.TraceInformation("Process Events {0}, {1}, {2}, {3}", context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, eventDataList.Count);


                // Store the events in the Azure Table
                var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Get reference to the table
                var tableClient = storageAccount.CreateCloudTableClient();
                var readingsTable = tableClient.GetTableReference(azureTableName);
                await readingsTable.CreateIfNotExistsAsync();


                // Create entities out of the event data
                var tableBatchOperation = new TableBatchOperation();
                foreach (var ev in eventDataList)
                {
                    var reading = DeserializeEventData(ev);
                    var entity = new ReadingEntity(reading);
                    tableBatchOperation.Add(TableOperation.Insert(entity));
                }

                // Execute batch insert
                await readingsTable.ExecuteBatchAsync(tableBatchOperation);
                await context.CheckpointAsync();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            try
            {
                try
                {
                    Trace.TraceInformation("Closing partition {0}, {1}, {2}, {3}", context.EventHubPath, context.ConsumerGroupName, context.Lease.PartitionId, reason.ToString());
                }
                catch { /* No need to do anything about a tracing exception */}

                if (reason == CloseReason.Shutdown)
                {
                    await context.CheckpointAsync();
                }
            }
            catch (Exception ex)
            {
                // Trace Exception 
                Trace.TraceError(ex.Message + ex.InnerException != null ? ex.InnerException.Message : string.Empty);
            }
        }
        #endregion

        #region Private Static Methods
        private static Reading DeserializeEventData(EventData eventData)
        {
            return JsonConvert.DeserializeObject<Reading>(Encoding.UTF8.GetString(eventData.GetBytes()));
        }
        #endregion
    }

}

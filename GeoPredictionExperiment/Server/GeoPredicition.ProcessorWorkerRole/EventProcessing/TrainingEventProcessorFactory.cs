using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoPrediction.ProcessorWorkerRole.EventProcessing
{
    public class TrainingEventProcessorFactory<T> : IEventProcessorFactory where T : class, IEventProcessor
    {
        #region Private Fields
        private readonly T instance;
        private string storageConnectionString;
        private string azureTableName;
        #endregion

        #region Public Constructors
        public TrainingEventProcessorFactory()
        {
            storageConnectionString = null;
            azureTableName = null;
        }

        public TrainingEventProcessorFactory(string storageConnectionString, string azureTableName)
        {
            this.storageConnectionString = storageConnectionString;
            this.azureTableName = azureTableName;
        }

        public TrainingEventProcessorFactory(T instance)
        {
            this.instance = instance;
        }
        #endregion

        #region IEventProcessorFactory Methods
        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return instance ?? Activator.CreateInstance(typeof(T), storageConnectionString, azureTableName) as T;
        }
        #endregion
    } 

}

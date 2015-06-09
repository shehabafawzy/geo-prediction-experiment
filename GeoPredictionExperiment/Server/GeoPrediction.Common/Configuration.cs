using Microsoft.Azure;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoPrediction.Common
{
    public class Configuration
    {
        private const string configurationTableName = "GeoPredictionConfiguration";
        private const string geoPredictionStorageConnectionConfigurationKey = "GeoPrediction.ConfigurationStorageConnectionString";

        public static string GeoPredictionStorageConnectionConfigurationKey { get { return geoPredictionStorageConnectionConfigurationKey; } }

        /// <summary>
        /// If in training mode, data is sent to the Azure Table, otherwise, it should be further processed
        /// </summary>
        public static bool IsTrainingMode
        {
            get
            {
                bool isTrainingMode = true;
                try
                {
                    isTrainingMode = Convert.ToBoolean(Configuration.GetConfigurationValue("IsLiveMode", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load configuration. Using default IsTrainingMode({0}).",isTrainingMode);
                }
                return isTrainingMode;
            }
        }

        /// <summary>
        /// Name of the table where training data is dumped
        /// </summary>
        public static string TrainingTableName
        {
            get
            {
                string trainingTableName = "TrainingTable";
                try
                {
                    trainingTableName = Convert.ToString(Configuration.GetConfigurationValue("TrainingTable", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load configuration. Using default TrainingTableName({0}).", trainingTableName);
                }
                return trainingTableName;
            }
        }

        /// <summary>
        /// Name of the input event hub
        /// </summary>
        public static string InputEventHubName
        {
            get
            {
                string eventHubName = "InputEventHub";
                try
                {
                    eventHubName = Convert.ToString(Configuration.GetConfigurationValue("InputEventHubName", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load configuration. Using default InputEventHubName({0}).", eventHubName);
                }
                return eventHubName;
            }
        }

        /// <summary>
        /// Name of the output event hub
        /// </summary>
        public static string OutputEventHubName
        {
            get
            {
                string eventHubName = "OutputEventHub";
                try
                {
                    eventHubName = Convert.ToString(Configuration.GetConfigurationValue("OutputEventHubName", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load configuration. Using default OutputEventHubName({0}).", eventHubName);
                }
                return eventHubName;
            }
        }

        /// <summary>
        /// Name of the consumer group
        /// </summary>
        public static string ConsumerGroupName
        {
            get
            {
                string configurationValue = "ConsumerGroup";
                try
                {
                    configurationValue = Convert.ToString(Configuration.GetConfigurationValue("ConsumerGroupName", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceWarning("Could not load configuration. Using default ConsumerGroupName({0}).", configurationValue);
                }
                return configurationValue;
            }
        }


        /// <summary>
        /// ServiceBus namespace
        /// </summary>
        public static string ServiceBusNamespace
        {
            get
            {
                string configurationValue = "<namespace>.servicebus.windows.net";
                try
                {
                    configurationValue = Convert.ToString(Configuration.GetConfigurationValue("ServiceBusNamespace", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceError("Could not load configuration. Using default ServiceBusNamespace({0}).", configurationValue);
                }
                return configurationValue;
            }
        }

        /// <summary>
        /// Connection string of the ServiceBus used by the Event Hub
        /// </summary>
        public static string ServiceBusConnectionString
        {
            get
            {
                string configurationValue = "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=Manage;SharedAccessKey=<key>";
                try
                {
                    configurationValue = Convert.ToString(Configuration.GetConfigurationValue("ServiceBusConnectionString", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceError("Could not load configuration. Using default ServiceBusConnectionString({0}).", configurationValue);
                }
                return configurationValue;
            }
        }

        /// <summary>
        /// Connection string of the storage
        /// </summary>
        public static string StorageConnectionString
        {
            get
            {
                string configurationValue = "DefaultEndpointsProtocol=http;AccountName=<accountname>;AccountKey=<key>;";
                try
                {
                    configurationValue = Convert.ToString(Configuration.GetConfigurationValue("StorageConnectionString", "general"));
                }
                catch (Exception)
                {
                    // do nothing, use default value
                    Trace.TraceError("Could not load configuration. Using default StorageConnectionString({0}).", configurationValue);
                }
                return configurationValue;
            }
        }

        /// <summary>
        /// Get the configuration Value from the configuration Table. Response is a JSON format
        /// </summary>
        /// <param name="configKey">This is a rowkey in Azure Table</param>
        /// <param name="processKey">This is the partition key in Azure Table</param>
        /// <returns>JSON configuration</returns>
        public static string GetConfigurationValue(string configKey, string processKey)
        {
            string configurationValue = "";
            try
            {
                string storageAccountString = CloudConfigurationManager.GetSetting(geoPredictionStorageConnectionConfigurationKey);
                var account = CloudStorageAccount.Parse(storageAccountString);
                var tableClient = account.CreateCloudTableClient();
                var configTable = tableClient.GetTableReference(configurationTableName);

                var retrieveOperation = TableOperation.Retrieve<ConfigurationEntity>(processKey, configKey);

                // Execute the retrieve operation.
                TableResult retrievedResult = configTable.Execute(retrieveOperation);
                if (retrievedResult != null)
                {
                    ConfigurationEntity resultEntity = (ConfigurationEntity)retrievedResult.Result;
                    configurationValue = resultEntity.ConfigurationValue;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return configurationValue;
        }
    
        /// <summary>
        /// Sets the configuration Value from the configuration Table. Response is a JSON format
        /// </summary>
        /// <param name="configKey">This is a rowkey in Azure Table</param>
        /// <param name="processKey">This is the partition key in Azure Table</param>
        /// <param name="processKey">This is the value in Azure Table</param>
        /// <returns>Returns true if insert is successful</returns>
        public static bool SetConfigurationValue(string configKey, string processKey, string configurationValue)
        {
            bool result = true;
            try
            {
                string storageAccountString = CloudConfigurationManager.GetSetting(geoPredictionStorageConnectionConfigurationKey);
                CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                CloudTableClient tableClient = account.CreateCloudTableClient();
                CloudTable configTable = tableClient.GetTableReference(configurationTableName);

                var configEntity = new ConfigurationEntity(configKey, processKey);
                configEntity.ConfigurationValue = configurationValue;

                // Create the TableOperation that inserts the configuration entity.
                TableOperation insertOperation = TableOperation.Insert(configEntity);

                // Execute the insert operation.
                TableResult insertResult = configTable.Execute(insertOperation);
                if (insertResult == null)
                    result = false;

            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }


    }

    public class ConfigurationEntity : TableEntity
    {
        public ConfigurationEntity(string configKey, string processKey)
        {
            this.PartitionKey = processKey;
            this.RowKey = configKey;
        }

        public ConfigurationEntity() { }

        public string ConfigurationValue { get; set; }

    }
}

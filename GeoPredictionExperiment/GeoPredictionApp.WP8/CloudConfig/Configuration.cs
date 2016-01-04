using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GeoPredictionApp.WP8.CloudConfig
{
    public class Configuration
    {
        private const string configurationTableName = "GeoPredictionConfiguration";

        public static bool Configured { get; private set; }

        public static async Task PopulateConfiguration()
        {
            try
            {
                InputEventHubName = "carmonitoreh";
                ServiceBusNamespace = "carmonitorns";
                EventHubSharedAccessPolicyKey = "qzZ7vJtOctefhVZ0/gbFPXe5/SNXxgIUKuCeBxYOCIw=";
                //InputEventHubName = "temp";
                //ServiceBusNamespace = "newcarsb";
                //EventHubSharedAccessPolicyKey = "msDiD8y+woLwucn9VhI9hDYDmC2omD2nOROVbWLO7sk=";
                EventHubSharedAccessPolicyKeyName = "Allinone";
                EventHubSharedAccessPolicyTTL = 4320;
                Configured = true;
            }
            catch (Exception e)
            {
                App.TelemetryClient.TrackException(e);
                Configured = false;
            }
        }
        
        /// <summary>
        /// Get the configuration Value from the configuration Table. Response is a JSON format
        /// </summary>
        /// <param name="configKey">This is a rowkey in Azure Table</param>
        /// <param name="processKey">This is the partition key in Azure Table</param>
        /// <returns>JSON configuration</returns>
        public async static Task<string> GetConfigurationValue(string configKey, string processKey)
        {
            string configurationValue = "";
            try
            {
                string storageAccountString = App.ConfigurationStorageConnectionString;
                var account = CloudStorageAccount.Parse(storageAccountString);
                var tableClient = account.CreateCloudTableClient();
                var configTable = tableClient.GetTableReference(configurationTableName);

                var retrieveOperation = TableOperation.Retrieve<ConfigurationEntity>(processKey, configKey);

                // Execute the retrieve operation.
                TableResult retrievedResult = await configTable.ExecuteAsync(retrieveOperation);
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
        public async static Task<bool> SetConfigurationValue(string configKey, string processKey, string configurationValue)
        {
            bool result = true;
            try
            {
                string storageAccountString = App.ConfigurationStorageConnectionString;
                CloudStorageAccount account = CloudStorageAccount.Parse(storageAccountString);
                CloudTableClient tableClient = account.CreateCloudTableClient();
                CloudTable configTable = tableClient.GetTableReference(configurationTableName);

                var configEntity = new ConfigurationEntity(configKey, processKey);
                configEntity.ConfigurationValue = configurationValue;

                // Create the TableOperation that inserts the configuration entity.
                TableOperation insertOperation = TableOperation.Insert(configEntity);

                // Execute the insert operation.
                TableResult insertResult = await configTable.ExecuteAsync(insertOperation);
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

        public static string InputEventHubName { get; set; }

        public static string ServiceBusNamespace { get; set; }

        public static string EventHubSharedAccessPolicyKeyName { get; set; }
        public static string EventHubSharedAccessPolicyKey { get; set; }

        public static int EventHubSharedAccessPolicyTTL { get; set; }

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

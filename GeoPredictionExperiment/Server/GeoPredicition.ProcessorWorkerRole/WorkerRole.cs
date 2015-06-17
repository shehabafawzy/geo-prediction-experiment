using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

using Microsoft.ServiceBus.Messaging;
using GeoPrediction.ProcessorWorkerRole.EventProcessing;


namespace ProcessorWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private bool IsTraining;
        private string ServiceBusConnectionString;
        public string StorageConnectionString;
        public string TrainingTableName;
        public string ConsumerGroupName;
        public string InputEventHubName;
        public string OutputEventHubName;

        public override void Run()
        {
            Trace.TraceInformation("ProcessorWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // Start Event Processing
            //lock (this)
            //{
            //    lock (this)
            //    {
                    StartEventProcessorAsync().Wait();
            //    }
            //}

            bool result = base.OnStart();

            Trace.TraceInformation("ProcessorWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("ProcessorWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("ProcessorWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read the IsTraining configuration every 10 seconds.For other configuraiton, you'll have to restart the role
                IsTraining = GeoPrediction.Common.Configuration.IsTrainingMode;
                await Task.Delay(10000);
            }
        }

        private async Task StartEventProcessorAsync()
        {
            Trace.TraceInformation("Starting event processor");
            Trace.TraceInformation("Fetching onfiguration from Azure configuration table");

            // Get configuration. This is done once per role start. If you change the configuration on the table, you'll need to restart the role            
            IsTraining = GeoPrediction.Common.Configuration.IsTrainingMode;
            ServiceBusConnectionString = GeoPrediction.Common.Configuration.ServiceBusConnectionString;
            StorageConnectionString = GeoPrediction.Common.Configuration.StorageConnectionString;
            TrainingTableName = GeoPrediction.Common.Configuration.TrainingTableName;
            ConsumerGroupName = GeoPrediction.Common.Configuration.ConsumerGroupName;
            InputEventHubName = GeoPrediction.Common.Configuration.InputEventHubName;
            OutputEventHubName = GeoPrediction.Common.Configuration.OutputEventHubName;

            // Get references to the input and output event hubs
            var inputEventHubClient = EventHubClient.CreateFromConnectionString(ServiceBusConnectionString, InputEventHubName);
            var outputEventHubClient = EventHubClient.CreateFromConnectionString(ServiceBusConnectionString, OutputEventHubName);



            if (IsTraining)
            {
                Trace.TraceInformation("Event processor in training mode");

                // Get the default Consumer Group 
                var inputEventProcessorHost = new EventProcessorHost(RoleEnvironment.CurrentRoleInstance.Id,
                                                               inputEventHubClient.Path.ToLower(),
                                                               ConsumerGroupName.ToLower(),
                                                               ServiceBusConnectionString,
                                                               StorageConnectionString)
                {
                    PartitionManagerOptions = new PartitionManagerOptions
                    {
                        AcquireInterval = TimeSpan.FromSeconds(10), // Default is 10 seconds 
                        RenewInterval = TimeSpan.FromSeconds(10), // Default is 10 seconds 
                        LeaseInterval = TimeSpan.FromSeconds(30) // Default value is 30 seconds 
                    }
                };
                var eventProcessorOptions = new EventProcessorOptions
                {
                    InvokeProcessorAfterReceiveTimeout = true,
                    MaxBatchSize = 100,
                    PrefetchCount = 100,
                    ReceiveTimeOut = TimeSpan.FromSeconds(30),
                };
                eventProcessorOptions.ExceptionReceived += eventProcessorOptions_ExceptionReceived;


                Trace.TraceInformation("Registering event processor");
                await inputEventProcessorHost.RegisterEventProcessorFactoryAsync(
                    new TrainingEventProcessorFactory<TrainingEventProcessor>(StorageConnectionString, TrainingTableName), eventProcessorOptions);
            }
            else
            {
                Trace.TraceInformation("Event processor in live mode");

                //await inputEventProcessorHost.RegisterEventProcessorFactoryAsync(
                //    new ScoringEventProcessorFactory<ScoringEventProcessor>(storageConnectionString, trainingTableName), eventProcessorOptions); 
            }

        }

        void eventProcessorOptions_ExceptionReceived(object sender, ExceptionReceivedEventArgs e)
        {
            if (e == null || e.Exception == null)
            {
                return;
            }

            // Trace Exception 
            Trace.TraceError(e.Exception.ToString());
        }
    }


}

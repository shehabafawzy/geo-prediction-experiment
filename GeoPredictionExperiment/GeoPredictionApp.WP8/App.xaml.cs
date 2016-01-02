﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights;
using GeoPredictionApp.WP8.Event;
using GeoPredictionApp.WP8.CloudConfig;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.System.Display;
using GeoPredictionApp.WP8.Views;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Phone.UI.Input;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace GeoPredictionApp.WP8
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Allows tracking page views, exceptions and other telemetry through the Microsoft Application Insights service.
        /// </summary>
        public static TelemetryClient TelemetryClient;
        public static string start = "";
        public static string End = "";

        private TransitionCollection transitions;

        public static string ConfigurationStorageConnectionString { get; set; }

        public static string DeviceUniqueIdentifier { get; set; }

        public static EventHubWrapper EventHubWrapper { get; set; }
        public static DisplayRequest dispRequest = null;
        /// <summary>
        /// Computes a unique identifier for the device
        /// </summary>
        /// <returns></returns>
        private static string GetDeviceID()
        {
            Windows.System.Profile.HardwareToken token = Windows.System.Profile.HardwareIdentification.GetPackageSpecificToken(null);
            Windows.Storage.Streams.IBuffer hardwareId = token.Id;

            Windows.Security.Cryptography.Core.HashAlgorithmProvider hasher = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm(Windows.Security.Cryptography.Core.HashAlgorithmNames.Md5);
            Windows.Storage.Streams.IBuffer hashed = hasher.HashData(hardwareId);

            string hashedString = Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(hashed);
            return hashedString;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            TelemetryClient = new TelemetryClient();
            DeviceUniqueIdentifier = GetDeviceID();

            // Connect to Azure Configuration Table to retrieve settings
            ConfigurationStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=cariotstorage;AccountKey=9xtO8YO7QYqBN6M2g8wDcvBJXEW1vGTOqkI7+B37cp1SfxIhO6hFj/BcFI8OKDeTETGWVq7bHjtx+GMk12cWMw==";


            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

            // ensure general app exceptions are handled
            this.UnhandledException += App_UnhandledException;

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame != null && rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        private void ApplicationWindow_VisiblityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (!e.Visible)
            {
                // Application went to background
                ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");

                // Set the text on the toast. 
                // The first line of text in the ToastText02 template is treated as header text, and will be bold.
                toastTextElements[0].AppendChild(toastXml.CreateTextNode("Warning!"));
                toastTextElements[1].AppendChild(toastXml.CreateTextNode("Tracking may stop in the background."));

                // Create the actual toast object using this toast specification.
                ToastNotification toast = new ToastNotification(toastXml);

                // Send the toast
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            else
            {
                // Application is FullScreen again
            }
        }

        async void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            // Track the unhandled exception
            App.TelemetryClient.TrackException(e.Exception);
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync
            (Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {

                var dialog = new MessageDialog(string.Format("{0}\n{1}", e.Message, e.Exception.StackTrace), "Unhandled exception");
                await dialog.ShowAsync();
            });
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            await Configuration.PopulateConfiguration();
            if (Configuration.Configured)
            {
                // Initialize event hub wrapper with retrieved settings
                EventHubWrapper = new EventHubWrapper
                {
                    EventHubName = Configuration.InputEventHubName,
                    PublisherName = App.DeviceUniqueIdentifier,
                    SenderKey = Configuration.EventHubSharedAccessPolicyKey,
                    SenderKeyName = Configuration.EventHubSharedAccessPolicyKeyName,
                    ServicebusNamespace = Configuration.ServiceBusNamespace,
                    TTLMinutes = Configuration.EventHubSharedAccessPolicyTTL
                };
                EventHubWrapper.InitEventHubConnection();
            }

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
                if (dispRequest == null)
                {

                    // Activate a display-required request. If successful, the screen is 
                    // guaranteed not to turn off automatically due to user inactivity.
                    dispRequest = new DisplayRequest();
                    dispRequest.RequestActive();
                }
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("Username"))
                {
                    if (!rootFrame.Navigate(typeof(LoginPage), e.Arguments))
                    {
                        throw new Exception("Failed to create initial page");
                    } 
                }
                else
                {
                    if (!rootFrame.Navigate(typeof(LocationDestination), e.Arguments))
                    {
                        throw new Exception("Failed to create initial page");
                    } 
                }
            }


            Window.Current.VisibilityChanged += ApplicationWindow_VisiblityChanged;

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        
        

    }
}
using GeoPrediction.Portable.Entities;
using GeoPredictionApp.WP8.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace GeoPredictionApp.WP8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        static int ErrorCount = 0;
        static int SuccessCount = 0;

        public static Geolocator GeoLocator = new Geolocator();
        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;


            GeoLocator.DesiredAccuracy = PositionAccuracy.High;
            GeoLocator.MovementThreshold = 5; // Track every 5 meters
            GeoLocator.ReportInterval = 1000; // Track every 1 second
            GeoLocator.PositionChanged += GeoLocator_PositionChanged;

            this.DataContext = DefaultViewModel;
        }

        void GeoLocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            try
            {
                var altitude = args.Position.Coordinate.Point.Position.Altitude;
                var latitude = args.Position.Coordinate.Point.Position.Latitude;
                var longitude = args.Position.Coordinate.Point.Position.Longitude;
                var heading = args.Position.Coordinate.Heading;
                var speed = args.Position.Coordinate.Speed * 3.6; // convert m/s to km/h
                var timestamp = args.Position.Coordinate.Timestamp;

                // Create a reading
                var reading = new Reading
                {
                    Altitude = altitude,
                    Latitude = latitude,
                    Longitude = longitude,
                    Heading = heading,
                    Speed = speed,
                    TimestampUTC = timestamp.ToUniversalTime().DateTime,
                    Ticks = timestamp.ToUniversalTime().Ticks,
                    HHMMSSUTC = timestamp.ToUniversalTime().TimeOfDay.ToString(),
                    UniqueDeviceId = App.DeviceUniqueIdentifier,
                    Name = "Anonymous",
                    ReverseGeocode = "N/A"
                };

                App.EventHubWrapper.SendMessageAsync(JsonConvert.SerializeObject(reading));
                SuccessCount++;
                DefaultViewModel["SuccessCounter"] = SuccessCount;
            }
            catch (Exception e)
            {
                ErrorCount++;
                DefaultViewModel["ErrorCounter"] = ErrorCount;
                DefaultViewModel["LastError"] = e.Message;
            }
            finally
            {
                DefaultViewModel["LastAttempt"] = DateTime.Now;
            }

        }


        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void stopAndQuitButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Exit();
        }

        private async void backgroundTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            Geoposition currentPosition = await GeoLocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));
            Geocircle fenceCircle = new Geocircle(currentPosition.Coordinate.Point.Position, 25);
            Geofence newFence = new Geofence("Dummy", fenceCircle, MonitoredGeofenceStates.Exited, false, TimeSpan.FromSeconds(1), DateTimeOffset.Now, TimeSpan.FromDays(30));
            GeofenceMonitor.Current.Geofences.Add(newFence);
            var LocationTaskBuilder = new BackgroundTaskBuilder
            {
                Name = "GeoPredictionLocationBackgroundTask",
                TaskEntryPoint = "GeoPrediction.BackgroundTasks.LocationBackgroundTask"
            };

            // Add GeoFence Trigger
            var trigger = new LocationTrigger(LocationTriggerType.Geofence);
            LocationTaskBuilder.SetTrigger(trigger);
            // Ensure there is an internet connection before the background task is launched. 
            var condition = new SystemCondition(SystemConditionType.InternetAvailable);
            LocationTaskBuilder.AddCondition(condition); 

            var geofenceTask = LocationTaskBuilder.Register();
            geofenceTask.Completed += (s, args) =>
            {
                var geoReports = GeofenceMonitor.Current.ReadReports();
                //foreach (var geofenceStateChangeReport in geoReports)
                //{
                //    var id = geofenceStateChangeReport.Geofence.Id;
                //    var newState = geofenceStateChangeReport.NewState.ToString();
                //    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                //        new MessageDialog(newState + " : " + id)
                //        .ShowAsync());
                //}
            };

        }
    }
}

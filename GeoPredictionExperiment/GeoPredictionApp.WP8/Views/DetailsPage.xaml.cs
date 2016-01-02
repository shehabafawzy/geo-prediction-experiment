using GeoPrediction.Portable.Entities;
using GeoPredictionApp.WP8.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace GeoPredictionApp.WP8.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetailsPage : Page
    {
        static int ErrorCount = 0;
        static int SuccessCount = 0;
        public static Geolocator GeoLocator = new Geolocator();
        bool StartTrip = false;
        string Username = "";
        DateTime StartDateTime;
        double TotalDistance = 0;
        Geocoordinate LastCoordinate;
        DispatcherTimer dispatcherTimer;
        private int count = 0;

        public DetailsPage()
        {
            this.InitializeComponent();

            GeoLocator.DesiredAccuracy = PositionAccuracy.High;
            GeoLocator.MovementThreshold = 5; // Track every 5 meters
            GeoLocator.ReportInterval = 1000; // Track every 1 second
            GeoLocator.PositionChanged += GeoLocator_PositionChanged;
            Username = ApplicationData.Current.LocalSettings.Values["Username"].ToString();
            UserNameTextBlock.Text = Username;
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += (s,e) => {
                if (StartTrip)
                {
                    TimeTextBlock.Text = (System.DateTime.Now.TimeOfDay - StartDateTime.TimeOfDay).ToString("hh\\:mm\\:ss");
                }
            };
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }
        async void GeoLocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (StartTrip)
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
                        Speed = speed == null ? 0:Convert.ToInt32(speed),
                        TimestampUTC = timestamp.ToUniversalTime().DateTime,
                        Ticks = timestamp.ToUniversalTime().Ticks,
                        HHMMSSUTC = timestamp.ToUniversalTime().TimeOfDay.ToString(),
                        UniqueDeviceId = App.DeviceUniqueIdentifier,
                        Name = Username,
                        ReverseGeocode = "N/A",
                        DurationInMinutes = timestamp.ToUniversalTime().DateTime.Hour * 60 + timestamp.ToUniversalTime().DateTime.Minute,
                        Start = App.start,
                        End = App.End
                    };

                    //update ui
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SpeedTextBlock.Text = speed.ToString().Split('.')[0];
                    });
                    if (LastCoordinate != null)
                    {
                        TotalDistance += Coordinates.Distance(LastCoordinate.Point.Position.Latitude, LastCoordinate.Point.Position.Longitude, latitude, longitude, 'K');
                        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            DistanceTextBlock.Text = Math.Round(TotalDistance, 2).ToString();
                        });
                    }
                    LastCoordinate = args.Position.Coordinate;

                    await App.EventHubWrapper.SendMessageAsync(JsonConvert.SerializeObject(reading));
                    SuccessCount++;
                    //["SuccessCounter"] = SuccessCount;
                }
                catch (Exception e)
                {
                    ErrorCount++;
                    //DefaultViewModel["ErrorCounter"] = ErrorCount;
                    //DefaultViewModel["LastError"] = e.Message + "\n" + e.StackTrace;
                }
                finally
                {
                    //DefaultViewModel["LastAttempt"] = DateTime.Now;
                } 
            }

        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await Windows.UI.ViewManagement.StatusBar.GetForCurrentView().HideAsync(); 
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            GeoLocator.PositionChanged -= GeoLocator_PositionChanged;
        }

        private void StartTripButton_Click(object sender, RoutedEventArgs e)
        {
            if (!StartTrip)
            {
                StartDateTime = DateTime.Now;
                TotalDistance = 0;
                StartTrip = true;
                DistanceTextBlock.Text = "0";
                SpeedTextBlock.Text = "0";
                TimeTextBlock.Text = "00:00:00";
                dispatcherTimer.Start();
                StartTripButton.IsEnabled = false;
                StopTripButton.IsEnabled = true;
            }
        }

        private void StopTripButton_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            StartTrip = false;
            StartTripButton.IsEnabled = true;
            StopTripButton.IsEnabled = false;
        }

        private void ChangeNameAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
        }
    }
}

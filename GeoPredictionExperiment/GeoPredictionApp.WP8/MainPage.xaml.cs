using GeoPrediction.Portable.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace GeoPredictionApp.WP8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public static Geolocator GeoLocator = new Geolocator();

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
            GeoLocator.DesiredAccuracy = PositionAccuracy.High;
            GeoLocator.MovementThreshold = 5; // Track every 5 meters
            GeoLocator.ReportInterval = 1000; // Track every 1 second
            GeoLocator.PositionChanged += GeoLocator_PositionChanged;
        }

        void GeoLocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var altitude = args.Position.Coordinate.Point.Position.Altitude;
            var latitude = args.Position.Coordinate.Point.Position.Latitude;
            var longitude = args.Position.Coordinate.Point.Position.Longitude;
            var heading = args.Position.Coordinate.Heading;
            var speed = args.Position.Coordinate.Speed;
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

            // Publish it on the event hub
            // TO DO
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }
    }
}

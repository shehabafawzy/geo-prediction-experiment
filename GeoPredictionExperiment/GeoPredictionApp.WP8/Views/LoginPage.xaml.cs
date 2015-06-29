using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
            LoginStoryboard.Begin();
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

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                await new Windows.UI.Popups.MessageDialog("Please enter a username or choose anonymous.").ShowAsync();
                return;
            }
            ApplicationData.Current.LocalSettings.Values["Username"] = UsernameTextBox.Text;
            Frame.Navigate(typeof(DetailsPage));
        }

        private void AnonymousTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["Username"] = "Anonymous";
            Frame.Navigate(typeof(DetailsPage));
        }
    }
}

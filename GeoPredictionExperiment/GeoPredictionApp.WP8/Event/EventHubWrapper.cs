using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Web.Http;

namespace GeoPredictionApp.WP8.Event
{
    public class EventHubWrapper
    {
        // Http connection string, SAS tokem and client
        Uri uri;
        private string sas;
        HttpClient httpClient = new HttpClient();
        bool EventHubConnectionInitialized = false;

        /// <summary>
        /// Service Bus namespace
        /// </summary>
        public string ServicebusNamespace  { get; set; }

        /// <summary>
        /// The name of the event hub
        /// </summary>
        public string EventHubName { get; set; }

        /// <summary>
        /// Unique device identifier
        /// </summary>
        public string PublisherName { get; set; }

        /// <summary>
        /// Name of the Shared Access Policy
        /// </summary>
        public string SenderKeyName { get; set; }

        /// <summary>
        /// Key of the Shared Access Policy
        /// </summary>
        public string SenderKey { get; set; }

        /// <summary>
        /// TTL for token expiry
        /// </summary>
        public int TTLMinutes { get; set; }


        string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value).Replace("%20", "+");
        }

        /// <summary>
        /// Send message to Azure Event Hub using HTTP/REST API
        /// </summary>
        /// <param name="message"></param>
        public async void SendMessageAsync(string message)
        {
            if (this.EventHubConnectionInitialized)
            {
                try
                {
                    HttpStringContent content = new HttpStringContent(message, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    HttpResponseMessage postResult = await httpClient.PostAsync(uri, content);

                    if (postResult.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("Message Sent: {0}", content);
                        App.TelemetryClient.TrackEvent("MessageSent");
                    }
                    else
                    {
                        App.TelemetryClient.TrackEvent("MessageSNotent");
                        Debug.WriteLine("Failed sending message: {0}", postResult.ReasonPhrase);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception when sending message:" + e.Message);
                    App.TelemetryClient.TrackException(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Helper function to get SAS token for connecting to Azure Event Hub
        /// </summary>
        /// <returns></returns>
        private string SASTokenHelper()
        {
            int expiry = (int)DateTime.UtcNow.AddMinutes(this.TTLMinutes).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            string stringToSign = UrlEncode(this.uri.ToString()) + "\n" + expiry.ToString();
            string signature = HmacSha256(this.SenderKey, stringToSign);
            string token = String.Format("sr={0}&sig={1}&se={2}&skn={3}", UrlEncode(this.uri.ToString()), UrlEncode(signature), expiry, this.SenderKeyName);

            return token;
        }

        /// <summary>
        /// Because Windows.Security.Cryptography.Core.MacAlgorithmNames.HmacSha256 doesn't
        /// exist in WP8.1 context we need to do another implementation
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string HmacSha256(string key, string kvalue)
        {
            var keyStrm = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            var valueStrm = CryptographicBuffer.ConvertStringToBinary(kvalue, BinaryStringEncoding.Utf8);

            var objMacProv = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            var hash = objMacProv.CreateHash(keyStrm);
            hash.Append(valueStrm);

            return CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset());
        }

        /// <summary>
        /// Initialize Event Hub connection
        /// </summary>
        public bool InitEventHubConnection()
        {
            try
            {
                this.uri = new Uri("https://" + this.ServicebusNamespace +
                              ".servicebus.windows.net/" + this.EventHubName +
                              "/publishers/" + this.PublisherName + "/messages");
                this.sas = SASTokenHelper();

                this.httpClient.DefaultRequestHeaders.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("SharedAccessSignature", sas);
                this.EventHubConnectionInitialized = true;
                return true;
            }
            catch (Exception e)
            {
                App.TelemetryClient.TrackException(e);
                return false;
            }
        }

    }
}

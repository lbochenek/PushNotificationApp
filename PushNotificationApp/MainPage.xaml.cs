using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Net.Http;
using Windows.Networking.PushNotifications;
using QuickType;
using System.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PushNotificationApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Boolean isAuthenticated;
        String token;
        private HttpClient client;
        public MainPage()
        {
            this.InitializeComponent();
            this.token = null;
            this.client = new HttpClient();

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[PushNotifications] Button Clicked.");
            await this.SendPushNotification(0);
        }

        private async System.Threading.Tasks.Task SendPushNotification(int attempts)
        {
            if (attempts >= 2)
            {
                Debug.WriteLine($"[PushNotifications] Tried to authenticate more than two times");
                return;
            }

            if (this.token == null)
            {
                await this.AuthenticateWithWNS();
            }

            StringContent xmlContent = new StringContent("<toast>< visual >< binding template = \"ToastGeneric\" >< text > Hello~</ text >< text > This is a push notification </ text ></ binding ></ visual ></ toast > ", Encoding.UTF8, "text/xml");
            var appRef = App.Current as App;
            string channel = appRef.Channel.Uri;

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, channel)
            {
                Content = xmlContent
            };

            httpRequestMessage.Headers.Add("Authorization", "Bearer " + this.token);
            httpRequestMessage.Headers.Add("X-WNS-Type", "wns/toast");
            httpRequestMessage.Headers.Add("X-WNS-RequestForStatus", "true");

            var response = await this.client.SendAsync(httpRequestMessage);
            Debug.WriteLine($"[PushNotifications] sent post req");
            //https://msdn.microsoft.com/library/windows/apps/xaml/hh868252
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) //need new auth token
            {
                this.token = null;
                await this.SendPushNotification(attempts++);

            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Gone || response.StatusCode == System.Net.HttpStatusCode.NotFound) //need a new channel
            {
                await appRef.InitNotificationsAsync();
                await this.SendPushNotification(attempts++);

            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotAcceptable) //wns throtled
            {
                await this.SendPushNotification(attempts++);
            }
            else
            {
                Debug.WriteLine($"[PushNotifications] could not send push request!");
            }
        }

        private async System.Threading.Tasks.Task AuthenticateWithWNS()
        {
            Dictionary<String, String> creds = GetCredentials();

            Debug.WriteLine($"[PushNotifications] creds fetched bout to dictionary!");
            Dictionary<String, String> values = new Dictionary<String, String>
            {
                { "grant_type", "client_credentials" },
                { "client_id", creds["AppSID"] },
                { "client_secret", creds["AppSecret"] },
                { "scope", "notify.windows.com" }
            };
            Debug.WriteLine($"[PushNotifications] values created");
            var content = new FormUrlEncodedContent(values);
            Debug.WriteLine($"[PushNotifications] formurlencodedcontent");
            var response = await client.PostAsync("https://login.live.com/accesstoken.srf", content);
            Debug.WriteLine($"[PushNotifications] post post req");
            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[PushNotifications] post req success");
                var responseString = await response.Content.ReadAsStringAsync();
                var data = Welcome.FromJson(responseString);
                this.token = data.AccessToken;
                this.isAuthenticated = true;
                Debug.WriteLine($"[PushNotifications] " + this.token);
            } else
            {
                Debug.WriteLine($"[PushNotifications] could not get access token!");
            }


        }

        private Dictionary<String, String> GetCredentials()
        {
            var resources = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            var gid = resources.GetString("AppSID");
            var secret = resources.GetString("AppSecret");

            if (gid == null || gid == "")
            {
                throw new Exception("No AppGID found in Resources.resw");
            }
            else if (secret == null || secret == "")
            {
                throw new Exception("No AppSecret found in Resources.resw");
            }

            Dictionary<String, String> creds = new Dictionary<String, String>
            {
                { "AppSID", gid },
                { "AppSecret", secret }
            };

            Debug.WriteLine($"[PushNotifications] GID!");
            Debug.WriteLine($"[PushNotifications] " + gid);
            Debug.WriteLine($"[PushNotifications] SECRET!" + secret);

            return creds;
        }
    }
}

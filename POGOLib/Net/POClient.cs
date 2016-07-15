using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using POGOLib.Net.Data;
using POGOLib.Net.Data.Login;
using POGOLib.Pokemon;
using POGOLib.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace POGOLib.Net
{
    public class POClient
    {
        public POClient(string username, LoginProvider loginProvider)
        {
            UID = HashUtil.HashMD5(username + loginProvider).ToLower();
            ClientData = new ClientData
            {
                Username = username,
                LoginProvider = loginProvider
            };

            if(ClientData.LoginProvider == LoginProvider.GoogleAuth)
                throw new Exception("Google Authentication is not supported.");

            Authenticated += OnAuthenticated;
        }

        public string UID { get; }
        public ClientData ClientData { get; private set; }
        public RPCClient RPCClient { get; private set; }

        public bool LoadClientData()
        {
            var saveDataPath = Path.Combine(Environment.CurrentDirectory, "savedata", $"{UID}.json");

            if (!File.Exists(saveDataPath))
                return false;

            ClientData = JsonConvert.DeserializeObject<ClientData>(File.ReadAllText(saveDataPath));

            if (!(ClientData.AuthData.ExpireDateTime > DateTime.Now))
                return false;
            
            OnAuthenticated(EventArgs.Empty);

            return true;
        }

        public void SaveClientData()
        {
            Console.WriteLine(JsonConvert.SerializeObject(ClientData));

            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "savedata", $"{UID}.json"), JsonConvert.SerializeObject(ClientData, Formatting.Indented));
        }

        public async Task<bool> Authenticate(string password)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;

                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Configuration.LoginUserAgent);

                    var loginData = await GetLoginData(httpClient);
                    var ticket = await PostLogin(httpClient, loginData, password);

                    if (ticket == null)
                        return false;

                    ClientData.AuthData = await PostLoginOauth(httpClient, ticket);
                    OnAuthenticated(EventArgs.Empty);

                    return true;
                }
            }
        }

        private async Task<LoginData> GetLoginData(HttpClient httpClient)
        {
            var loginDataResponse = await httpClient.GetAsync(Configuration.LoginUrl);
            var loginData = JsonConvert.DeserializeObject<LoginData>(await loginDataResponse.Content.ReadAsStringAsync());

            return loginData;
        }

        private async Task<string> PostLogin(HttpClient httpClient, LoginData loginData, string password)
        {
            var loginResponse = await httpClient.PostAsync(Configuration.LoginUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"lt", loginData.Lt},
                {"execution", loginData.Execution},
                {"_eventId", "submit"},
                {"username", ClientData.Username},
                {"password", password}
            }));

            var loginResponseDataRaw = await loginResponse.Content.ReadAsStringAsync();
            if (!loginResponseDataRaw.Contains("{"))
            {
                var locationQuery = loginResponse.Headers.Location.Query;
                var ticketStartPosition = locationQuery.IndexOf("=", StringComparison.Ordinal) + 1;
                return locationQuery.Substring(ticketStartPosition, locationQuery.Length - ticketStartPosition);
            }

            var loginResponseData = JObject.Parse(loginResponseDataRaw);
            var loginResponseErrors = (JArray)loginResponseData["errors"];

            foreach (var loginResponseError in loginResponseErrors)
            {
                Console.WriteLine($"Login error: '{loginResponseError}'");
            }

            return null;
        }

        private async Task<AuthData> PostLoginOauth(HttpClient httpClient, string ticket)
        {
            var loginResponse = await httpClient.PostAsync(Configuration.LoginOauthUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"client_id", "mobile-app_pokemon-go"},
                {"redirect_uri", "https://www.nianticlabs.com/pokemongo/error"},
                {"client_secret", "w8ScCUXJQc6kXKw8FiOhd8Fixzht18Dq3PEVkUCP5ZPxtgyWsbTvWHFLm2wNY0JR"},
                {"grant_type", "refresh_token"},
                {"code", ticket}
            }));

            var loginResponseDataRaw = await loginResponse.Content.ReadAsStringAsync();

            var oAuthData = Regex.Match(loginResponseDataRaw, "access_token=(?<accessToken>.*?)&expires=(?<expires>\\d+)");
            if (!oAuthData.Success)
                throw new Exception("Couldn't verify the OAuth login response data.");

            return new AuthData
            {
                AccessToken = oAuthData.Groups["accessToken"].Value,
                ExpireDateTime = DateTime.Now.AddSeconds(int.Parse(oAuthData.Groups["expires"].Value))
            };
        }

        public bool HasGpsData()
        {
            return ClientData.GpsData != null;
        }

        public void SetGpsData(GpsData gpsData)
        {
            ClientData.GpsData = gpsData;
        }

        public void SetGpsData(double latitude, double longitude, double altitude = 50.0)
        {
            if (!HasGpsData())
            {
                ClientData.GpsData = new GpsData
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    Altitude = altitude
                };
            }
            else
            {
                ClientData.GpsData.Latitude = latitude;
                ClientData.GpsData.Longitude = longitude;
                ClientData.GpsData.Altitude = altitude;
            }
        }

        private void OnAuthenticated(object sender, EventArgs eventArgs)
        {
            RPCClient = new RPCClient(this);
        }

        private void OnAuthenticated(EventArgs e)
        {
            Authenticated?.Invoke(this, e);
        }

        private event EventHandler Authenticated;
    }
}

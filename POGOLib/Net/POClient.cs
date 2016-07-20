using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DankMemes.GPSOAuthSharp;
using log4net;
using POGOLib.Net.Data;
using POGOLib.Net.Data.Login;
using POGOLib.Pokemon;
using POGOLib.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOProtos.Inventory;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings;

namespace POGOLib.Net
{
    public class PoClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoClient));

        /// <summary>
        /// Initializes an instance of the <see cref="PoClient"/> class.
        /// </summary>
        /// <param name="username">The username to authenticate with.</param>
        /// <param name="loginProvider">The login provider to authenticate with.</param>
        public PoClient(string username, LoginProvider loginProvider)
        {
            Uid = HashUtil.HashMD5(username + loginProvider).ToLower();
            ClientData = new ClientData
            {
                Username = username,
                LoginProvider = loginProvider
            };
            
            Authenticated += OnAuthenticated;
        }

        /// <summary>
        /// Gets a unique user identifier for the <see cref="PoClient"/>.
        /// </summary>
        public string Uid { get; }

        /// <summary>
        /// Gets the client-side data of the <see cref="PoClient"/>.
        /// </summary>
        public ClientData ClientData { get; private set; }

        /// <summary>
        /// Gets the underlying RPC client of the <see cref="PoClient"/>.
        /// </summary>
        public RpcClient RpcClient { get; private set; }

        /// <summary>
        /// Gets whether the <see cref="PoClient"/> is current dispatching heartbeats.
        /// </summary>
        public bool IsHeartbeating { get; private set; }

        /// <summary>
        /// <see cref="GlobalSettings"/> is automatically updated and only accessible after authenticating.
        /// </summary>
        public GlobalSettings GlobalSettings { get; internal set; }

        /// <summary>
        /// Gets the map objects of the latest response in the <see cref="PoClient"/>.
        /// </summary>
        public GetMapObjectsResponse MapObjects { get; internal set; }

        /// <summary>
        /// Gets the inventory, according to the latest response in the <see cref="PoClient"/>.
        /// </summary>
        public InventoryDelta Inventory { get; internal set; }

        /// <summary>
        /// Loads the client data from a local destination.
        /// </summary>
        /// <returns></returns>
        public bool LoadClientData()
        {
            var saveDataPath = Path.Combine(Environment.CurrentDirectory, "savedata", $"{Uid}.json");

            if (!File.Exists(saveDataPath))
                return false;

            ClientData = JsonConvert.DeserializeObject<ClientData>(File.ReadAllText(saveDataPath));

            if (!(ClientData.AuthData.ExpireDateTime > DateTime.UtcNow))
                return false;
            
            OnAuthenticated(EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Saves the client to a local destination.
        /// </summary>
        public void SaveClientData()
        {
            File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "savedata", $"{Uid}.json"), JsonConvert.SerializeObject(ClientData, Formatting.Indented));
        }

        /// <summary>
        /// Authenticates using the <see cref="ClientData"/> along with a specific password.
        /// </summary>
        /// <param name="password">The password to authenticate with.</param>
        /// <returns>True if the authentication was successful; otherwise false.</returns>
        public bool Authenticate(string password)
        {
            switch (ClientData.LoginProvider)
            {
                case LoginProvider.PokemonTrainerClub:
                {
                        using (var httpClientHandler = new HttpClientHandler())
                        {
                            httpClientHandler.AllowAutoRedirect = false;

                            using (var httpClient = new HttpClient(httpClientHandler))
                            {
                                httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.LoginUserAgent);

                                var loginData = GetLoginDataAsync(httpClient).Result;
                                var ticket = PostLoginAsync(httpClient, loginData, password).Result;

                                if (ticket == null)
                                    return false;

                                ClientData.AuthData = PostLoginOauthAsync(httpClient, ticket).Result;
                                OnAuthenticated(EventArgs.Empty);

                                return true;
                            }
                        }
                }

                case LoginProvider.GoogleAuth:
                {
                        var googleClient = new GPSOAuthClient(ClientData.Username, password);
                        var masterLoginResponse = googleClient.PerformMasterLogin();

                        if (masterLoginResponse.ContainsKey("Error") && masterLoginResponse["Error"] == "BadAuthentication")
                            return false;

                        if (!masterLoginResponse.ContainsKey("Token"))
                            throw new Exception("Token was missing from master login response.");

                        var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], Constants.GoogleAuthService, Constants.GoogleAuthApp, Constants.GoogleAuthClientSig);

                        if (!oauthResponse.ContainsKey("Auth"))
                            throw new Exception("Auth token was missing from oauth login response.");

                        ClientData.AuthData = new AuthData
                        {
                            AccessToken = oauthResponse["Auth"],
                            ExpireDateTime = TimeUtil.GetDateTimeFromS(int.Parse(oauthResponse["Expiry"]))
                        };
                        OnAuthenticated(EventArgs.Empty);

                        return true;
                    }
            }

            throw new Exception("Unknown login provider.");
        }

        private async Task<LoginData> GetLoginDataAsync(HttpClient httpClient)
        {
            var loginDataResponse = await httpClient.GetAsync(Constants.LoginUrl);
            var loginData = JsonConvert.DeserializeObject<LoginData>(await loginDataResponse.Content.ReadAsStringAsync());

            return loginData;
        }

        private async Task<string> PostLoginAsync(HttpClient httpClient, LoginData loginData, string password)
        {
            var loginResponse = await httpClient.PostAsync(Constants.LoginUrl, new FormUrlEncodedContent(new Dictionary<string, string>
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
                Log.Debug($"Login error: '{loginResponseError}'");
            }

            return null;
        }

        private async Task<AuthData> PostLoginOauthAsync(HttpClient httpClient, string ticket)
        {
            var loginResponse = await httpClient.PostAsync(Constants.LoginOauthUrl, new FormUrlEncodedContent(new Dictionary<string, string>
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
                ExpireDateTime = DateTime.UtcNow.AddSeconds(int.Parse(oAuthData.Groups["expires"].Value))
            };
        }

        /// <summary>
        /// Determines whether there is GPS data.
        /// </summary>
        /// <returns></returns>
        public bool HasGpsData()
        {
            return GpsData != null;
        }

        /// <summary>
        /// Gets or sets the GPS data of the <see cref="PoClient"/>.
        /// </summary>
        public GpsData GpsData
        {
            get { return ClientData.GpsData; }
            set { ClientData.GpsData = value; }
        }
        
        /// <summary>
        /// Sets the GPS data to a specific latitude, longitude and altitude.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="altitude">The altitude.</param>
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
            RpcClient = new RpcClient(this);
            StartHeartbeats();
        }

        private void OnAuthenticated(EventArgs e)
        {
            Authenticated?.Invoke(this, e);
        }

        private event EventHandler Authenticated;
        
        /// <summary>
        /// Starts sending heartbeats through the underlying <see cref="RpcClient"/>.
        /// </summary>
        public void StartHeartbeats()
        {
            if (IsHeartbeating)
            {
                Log.Debug("Heartbeating has already been started.");
                return;
            }

            if (GlobalSettings == null)
            {
                RpcClient.Heartbeat(); // Forcekick

                if(GlobalSettings == null)
                    throw new Exception("Couldn't fetch settings.");
            }

            IsHeartbeating = true;

            new Thread(() =>
            {
                var sleepTime = Convert.ToInt32(GlobalSettings.Map.GetMapObjectsMinRefreshSeconds) * 1000;

                while (IsHeartbeating)
                {
                    RpcClient.Heartbeat();

                    Thread.Sleep(sleepTime);
                }
            }) { IsBackground = true }.Start();
        }

        /// <summary>
        /// Stops sending heartbeats through the underlying <see cref="RpcClient"/>.
        /// </summary>
        public void StopHeartbeats()
        {
            if (!IsHeartbeating)
            {
                Log.Debug("Heartbeating has already been stopped.");
                return;
            }

            IsHeartbeating = false;
        }
    }
}

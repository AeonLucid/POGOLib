using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using DankMemes.GPSOAuthSharp;
using GeoCoordinatePortable;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon.Data;
using POGOLib.Util;

namespace POGOLib.Net.Authentication
{
    /// <summary>
    ///     Responsible for Authenticating and Re-authenticating the user.
    /// </summary>
    public static class Login
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Login));

        /// <summary>
        ///     Login with a stored <see cref="AccessToken" />.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="password">The password is needed for reauthentication.</param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static Session GetSession(AccessToken accessToken, string password, double initialLatitude,
            double initialLongitude)
        {
            if (accessToken.IsExpired)
            {
                throw new Exception("AccessToken is expired.");
            }
            Log.Debug("Authenticated from cache.");
            return new Session(accessToken, password, new GeoCoordinate(initialLatitude, initialLongitude));
        }

        /// <summary>
        ///     Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="username">Your username.</param>
        /// <param name="password">Your password.</param>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static Session GetSession(string username, string password, LoginProvider loginProvider,
            double initialLatitude, double initialLongitude)
        {
            AccessToken accessToken;

            switch (loginProvider)
            {
                case LoginProvider.GoogleAuth:
                    accessToken = WithGoogle(username, password);
                    break;
                case LoginProvider.PokemonTrainerClub:
                    accessToken = WithPokemonTrainerClub(username, password);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loginProvider), loginProvider, null);
            }

            return new Session(accessToken, password, new GeoCoordinate(initialLatitude, initialLongitude));
        }

        /// Authenticate the user through Google.
        internal static AccessToken WithGoogle(string email, string password)
        {
            var googleClient = new GPSOAuthClient(email, password);
            var masterLoginResponse = googleClient.PerformMasterLogin();

            if (masterLoginResponse.ContainsKey("Error"))
            {
                throw new Exception($"Google returned an error message: '{masterLoginResponse["Error"]}'");
            }
            if (!masterLoginResponse.ContainsKey("Token"))
            {
                throw new Exception("Token was missing from master login response.");
            }
            var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], Constants.GoogleAuthService,
                Constants.GoogleAuthApp, Constants.GoogleAuthClientSig);
            if (!oauthResponse.ContainsKey("Auth"))
            {
                throw new Exception("Auth token was missing from oauth login response.");
            }
            Log.Debug("Authenticated through Google.");
            return new AccessToken
            {
                Username = email,
                Token = oauthResponse["Auth"],
                Expiry = TimeUtil.GetDateTimeFromSeconds(int.Parse(oauthResponse["Expiry"])),
                LoginProvider = LoginProvider.GoogleAuth
            };
        }

        /// Authenticate the user through PTC.
        internal static AccessToken WithPokemonTrainerClub(string username, string password)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.LoginUserAgent);
                    var loginData = GetLoginData(httpClient);
                    var ticket = PostLogin(httpClient, username, password, loginData);
                    var accessToken = PostLoginOauth(httpClient, ticket);
                    accessToken.Username = username;
                    Log.Debug("Authenticated through PTC.");
                    return accessToken;
                }
            }
        }

        /// <summary>
        ///     Responsible for retrieving login parameters for <see cref="PostLogin" />.
        /// </summary>
        /// <param name="httpClient">An initialized <see cref="HttpClient" /></param>
        /// <returns><see cref="LoginData" /> for <see cref="PostLogin" />.</returns>
        private static LoginData GetLoginData(HttpClient httpClient)
        {
            var loginDataResponse = httpClient.GetAsync(Constants.LoginUrl).Result;
            var loginData =
                JsonConvert.DeserializeObject<LoginData>(loginDataResponse.Content.ReadAsStringAsync().Result);
            return loginData;
        }

        /// <summary>
        ///     Responsible for submitting the login request.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="username">The user's PTC username.</param>
        /// <param name="password">The user's PTC password.</param>
        /// <param name="loginData"><see cref="LoginData" /> taken from PTC website using <see cref="GetLoginData" />.</param>
        /// <returns></returns>
        private static string PostLogin(HttpClient httpClient, string username, string password, LoginData loginData)
        {
            var loginResponse =
                httpClient.PostAsync(Constants.LoginUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"lt", loginData.Lt},
                    {"execution", loginData.Execution},
                    {"_eventId", "submit"},
                    {"username", username},
                    {"password", password}
                })).Result;

            var loginResponseDataRaw = loginResponse.Content.ReadAsStringAsync().Result;
            if (!loginResponseDataRaw.Contains("{"))
            {
                var locationQuery = loginResponse.Headers.Location.Query;
                var ticketStartPosition = locationQuery.IndexOf("=", StringComparison.Ordinal) + 1;
                return locationQuery.Substring(ticketStartPosition, locationQuery.Length - ticketStartPosition);
            }

            var loginResponseData = JObject.Parse(loginResponseDataRaw);
            var loginResponseErrors = (JArray) loginResponseData["errors"];

            throw new Exception($"Pokemon Trainer Club gave error(s): '{string.Join(",", loginResponseErrors)}'");
        }

        /// <summary>
        ///     Responsible for finishing the oauth login request.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        private static AccessToken PostLoginOauth(HttpClient httpClient, string ticket)
        {
            var loginResponse =
                httpClient.PostAsync(Constants.LoginOauthUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", "mobile-app_pokemon-go"},
                    {"redirect_uri", "https://www.nianticlabs.com/pokemongo/error"},
                    {"client_secret", "w8ScCUXJQc6kXKw8FiOhd8Fixzht18Dq3PEVkUCP5ZPxtgyWsbTvWHFLm2wNY0JR"},
                    {"grant_type", "refresh_token"},
                    {"code", ticket}
                })).Result;

            var loginResponseDataRaw = loginResponse.Content.ReadAsStringAsync().Result;

            var oAuthData = Regex.Match(loginResponseDataRaw,
                "access_token=(?<accessToken>.*?)&expires=(?<expires>\\d+)");
            if (!oAuthData.Success)
            {
                throw new Exception($"Couldn't verify the OAuth login response data '{loginResponseDataRaw}'.");
            }
            return new AccessToken
            {
                Token = oAuthData.Groups["accessToken"].Value,
                Expiry = DateTime.UtcNow.AddSeconds(int.Parse(oAuthData.Groups["expires"].Value)),
                LoginProvider = LoginProvider.PokemonTrainerClub
            };
        }
    }
}
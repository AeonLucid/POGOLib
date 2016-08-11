using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using GeoCoordinatePortable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOLib.Logging;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon.Data;
using POGOLib.Util;
using System.Threading.Tasks;
using DankMemes.GPSOAuthSharp;

namespace POGOLib.Net.Authentication
{
    /// <summary>
    ///     Responsible for Authenticating and Re-authenticating the user.
    /// </summary>
    public static class Login
    {

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
            Logger.Debug("Authenticated from cache.");
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
        public static async Task<Session> GetSession(string username, string password, LoginProvider loginProvider,
            double initialLatitude, double initialLongitude)
        {
            AccessToken accessToken;

            switch (loginProvider)
            {
                case LoginProvider.GoogleAuth:
                    accessToken = await WithGoogle(username, password);
                    break;
                case LoginProvider.PokemonTrainerClub:
                    accessToken = await WithPokemonTrainerClub(username, password);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loginProvider), loginProvider, null);
            }

            return new Session(accessToken, password, new GeoCoordinate(initialLatitude, initialLongitude));
        }

        /// Authenticate the user through Google.
        internal static async Task<AccessToken> WithGoogle(string email, string password)
        {
            var googleClient = new GPSOAuthClient(email, password);
            var masterLoginResponse = await googleClient.PerformMasterLogin();

            if (masterLoginResponse.ContainsKey("Error"))
            {
                throw new Exception($"Google returned an error message: '{masterLoginResponse["Error"]}'");
            }
            if (!masterLoginResponse.ContainsKey("Token"))
            {
                throw new Exception("Token was missing from master login response.");
            }
            var oauthResponse = await googleClient.PerformOAuth(masterLoginResponse["Token"], Constants.GoogleAuthService,
                Constants.GoogleAuthApp, Constants.GoogleAuthClientSig);
            if (!oauthResponse.ContainsKey("Auth"))
            {
                throw new Exception("Auth token was missing from oauth login response.");
            }
            Logger.Debug("Authenticated through Google.");
            return new AccessToken
            {
                Username = email,
                Token = oauthResponse["Auth"],
                Expiry = TimeUtil.GetDateTimeFromSeconds(int.Parse(oauthResponse["Expiry"])),
                LoginProvider = LoginProvider.GoogleAuth
            };
        }

        /// Authenticate the user through PTC.
        internal static async Task<AccessToken> WithPokemonTrainerClub(string username, string password)
        {
            using (var httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.LoginUserAgent);
                    var loginData = await GetLoginData(httpClient);
                    var ticket = await PostLogin(httpClient, username, password, loginData);
                    var accessToken = await PostLoginOauth(httpClient, ticket);
                    accessToken.Username = username;
                    Logger.Debug("Authenticated through PTC.");
                    return accessToken;
                }
            }
        }

        /// <summary>
        ///     Responsible for retrieving login parameters for <see cref="PostLogin" />.
        /// </summary>
        /// <param name="httpClient">An initialized <see cref="HttpClient" /></param>
        /// <returns><see cref="LoginData" /> for <see cref="PostLogin" />.</returns>
        private static async Task<LoginData> GetLoginData(HttpClient httpClient)
        {
            var loginDataResponse = await httpClient.GetAsync(Constants.LoginUrl);
            var loginData =
                JsonConvert.DeserializeObject<LoginData>(await loginDataResponse.Content.ReadAsStringAsync());
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
        private static async Task<string> PostLogin(HttpClient httpClient, string username, string password, LoginData loginData)
        {
            var loginResponse =
                await httpClient.PostAsync(Constants.LoginUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"lt", loginData.Lt},
                    {"execution", loginData.Execution},
                    {"_eventId", "submit"},
                    {"username", username},
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
            var loginResponseErrors = (JArray) loginResponseData["errors"];

            throw new Exception($"Pokemon Trainer Club gave error(s): '{string.Join(",", loginResponseErrors)}'");
        }

        /// <summary>
        ///     Responsible for finishing the oauth login request.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        private static async Task<AccessToken> PostLoginOauth(HttpClient httpClient, string ticket)
        {
            var loginResponse =
                await httpClient.PostAsync(Constants.LoginOauthUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"client_id", "mobile-app_pokemon-go"},
                    {"redirect_uri", "https://www.nianticlabs.com/pokemongo/error"},
                    {"client_secret", "w8ScCUXJQc6kXKw8FiOhd8Fixzht18Dq3PEVkUCP5ZPxtgyWsbTvWHFLm2wNY0JR"},
                    {"grant_type", "refresh_token"},
                    {"code", ticket}
                }));

            var loginResponseDataRaw = await loginResponse.Content.ReadAsStringAsync();

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
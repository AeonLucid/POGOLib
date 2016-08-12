using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POGOLib.Logging;
using POGOLib.Net.Authentication.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace POGOLib.Pokemon
{
    public class PtcLoginProvider : ILoginProvider
    {
        public string ProviderID => "ptc";

        public async Task<AccessToken> GetAccessToken(string username, string password)
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
        private async Task<LoginData> GetLoginData(HttpClient httpClient)
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
        private async Task<string> PostLogin(HttpClient httpClient, string username, string password, LoginData loginData)
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
            var loginResponseErrors = (JArray)loginResponseData["errors"];

            throw new Exception($"Pokemon Trainer Club gave error(s): '{string.Join(",", loginResponseErrors)}'");
        }

        /// <summary>
        ///     Responsible for finishing the oauth login request.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        private async Task<AccessToken> PostLoginOauth(HttpClient httpClient, string ticket)
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
                ProviderID = ProviderID
            };
        }
    }
}

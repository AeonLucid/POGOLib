using System.Threading.Tasks;
using GPSOAuthSharp;
using POGOLib.Official.Logging;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Util;

namespace POGOLib.Official.LoginProviders
{
    /// <summary>
    /// The <see cref="ILoginProvider"/> for Google.
    /// Use this if you want to authenticate to PokemonGo using a Google account.
    /// </summary>
    public class GoogleLoginProvider : ILoginProvider
    {
        private readonly string _username;
        private readonly string _password;

        public GoogleLoginProvider(string username, string password)
        {
            _username = username;
            _password = password;
        }
        
        /// <summary>
        /// The unique identifier of the <see cref="GoogleLoginProvider"/>.
        /// </summary>
        public string ProviderId => "google";

        /// <summary>
        /// The unique identifier of the user trying to authenticate using the <see cref="GoogleLoginProvider"/>.
        /// </summary>
        public string UserId => _username;

        /// <summary>
        /// Retrieves an <see cref="AccessToken"/> by logging into through the Google Play Services OAuth.
        /// </summary>
        /// <returns>Returns an <see cref="AccessToken"/>.</returns>
        public async Task<AccessToken> GetAccessToken()
        {
            var googleClient = new GPSOAuthClient(_username, _password);
            var masterLoginResponse = await googleClient.PerformMasterLogin();

            if (masterLoginResponse.ContainsKey("Error"))
            {
                if (masterLoginResponse["Error"].Equals("NeedsBrowser"))
                    throw new GoogleLoginException($"You have to log into an browser with the email '{_username}'.");

                throw new GoogleLoginException($"Google returned an error message: '{masterLoginResponse["Error"]}'");
            }
            if (!masterLoginResponse.ContainsKey("Token"))
            {
                throw new GoogleLoginException("Token was missing from master login response.");
            }
            var oauthResponse = await googleClient.PerformOAuth(masterLoginResponse["Token"], Constants.GoogleAuthService,
                Constants.GoogleAuthApp, Constants.GoogleAuthClientSig);
            if (!oauthResponse.ContainsKey("Auth"))
            {
                throw new GoogleLoginException("Auth token was missing from oauth login response.");
            }
            Logger.Debug("Authenticated through Google.");
            return new AccessToken
            {
                Username = _username,
                Token = oauthResponse["Auth"],
                Expiry = TimeUtil.GetDateTimeFromSeconds(int.Parse(oauthResponse["Expiry"])),
                ProviderID = ProviderId
            };
        }
    }
}

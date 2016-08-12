using DankMemes.GPSOAuthSharp;
using POGOLib;
using POGOLib.Logging;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon;
using POGOLib.Util;
using System;
using System.Threading.Tasks;

namespace GoogleLoginProviderLib
{
    public class GoogleLoginProvider : ILoginProvider
    {
        public string ProviderID => "google";

        public async Task<AccessToken> GetAccessToken(string username, string password)
        {
            var googleClient = new GPSOAuthClient(username, password);
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
                Username = username,
                Token = oauthResponse["Auth"],
                Expiry = TimeUtil.GetDateTimeFromSeconds(int.Parse(oauthResponse["Expiry"])),
                ProviderID = ProviderID
            };
        }
    }
}

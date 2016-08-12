using System;
using GeoCoordinatePortable;
using POGOLib.Logging;
using POGOLib.Net.Authentication.Data;
using System.Threading.Tasks;
using POGOLib.Pokemon;

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
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, string password, double initialLatitude,
            double initialLongitude)
        {
            if (accessToken.IsExpired)
            {
                throw new Exception("AccessToken is expired.");
            }
            Logger.Debug("Authenticated from cache.");
            return new Session(loginProvider, accessToken, password, new GeoCoordinate(initialLatitude, initialLongitude));
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
        public static async Task<Session> GetSession(ILoginProvider loginProvider, string username, string password,
            double initialLatitude, double initialLongitude)
        {
            AccessToken accessToken = await loginProvider.GetAccessToken(username, password);
            return new Session(loginProvider, accessToken, password, new GeoCoordinate(initialLatitude, initialLongitude));
        }


    }
}
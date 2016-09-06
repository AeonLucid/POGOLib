using System;
using GeoCoordinatePortable;
using POGOLib.Logging;
using POGOLib.Net.Authentication.Data;
using System.Threading.Tasks;
using POGOLib.Net.Authentication.Providers;

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
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, double initialLatitude, double initialLongitude)
        {
            if (accessToken.IsExpired)
                throw new Exception("AccessToken is expired.");

            Logger.Debug("Authenticated from cache.");

            return new Session(loginProvider, accessToken, new GeoCoordinate(initialLatitude, initialLongitude));
        }

        /// <summary>
        ///     Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, double initialLatitude, double initialLongitude)
        {
            return new Session(loginProvider, await loginProvider.GetAccessToken(), new GeoCoordinate(initialLatitude, initialLongitude));
        }

        /// <summary>
        ///     Login with a stored <see cref="AccessToken" />.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, GeoCoordinate coordinate)
        {
            if (accessToken.IsExpired)
            {
                throw new Exception("AccessToken is expired.");
            }
            Logger.Debug("Authenticated from cache.");
            return new Session(loginProvider, accessToken, coordinate);
        }

        /// <summary>
        ///     Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, GeoCoordinate coordinate)
        {
            AccessToken accessToken = await loginProvider.GetAccessToken();
            return new Session(loginProvider, accessToken, coordinate);
        }
        
    }
}

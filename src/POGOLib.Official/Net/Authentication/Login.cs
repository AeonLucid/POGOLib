using System;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using POGOLib.Official.Logging;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net.Authentication.Data;
using static POGOProtos.Networking.Envelopes.Signature.Types;

namespace POGOLib.Official.Net.Authentication
{
    /// <summary>
    /// Responsible for Authenticating and Re-authenticating the user.
    /// </summary>
    public static class Login
    {
        /// <summary>
        /// Login with a stored <see cref="AccessToken" />.
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="accessToken"></param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceInfo">The <see cref="DeviceInfo"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceInfo"/>.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, double initialLatitude, double initialLongitude, DeviceInfo deviceInfo = null)
        {
            if (accessToken.IsExpired)
                throw new Exception("AccessToken is expired.");

            Logger.Debug("Authenticated from cache.");

            return new Session(loginProvider, accessToken, new GeoCoordinate(initialLatitude, initialLongitude), deviceInfo);
        }

        /// <summary>
        /// Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceInfo">The <see cref="DeviceInfo"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceInfo"/>.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, double initialLatitude, double initialLongitude, DeviceInfo deviceInfo = null)
        {
            return new Session(loginProvider, await loginProvider.GetAccessToken(), new GeoCoordinate(initialLatitude, initialLongitude), deviceInfo);
        }

        /// <summary>
        /// Login with a stored <see cref="AccessToken" />.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="accessToken">The <see cref="AccessToken"/> you want to re-use.</param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceInfo">The <see cref="DeviceInfo"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceInfo"/>.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, GeoCoordinate coordinate, DeviceInfo deviceInfo = null)
        {
            if (accessToken.IsExpired)
            {
                throw new ArgumentException($"{nameof(accessToken)} is expired.");
            }

            Logger.Debug("Authenticated from cache.");
            return new Session(loginProvider, accessToken, coordinate, deviceInfo);
        }

        /// <summary>
        /// Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceInfo">The <see cref="DeviceInfo"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceInfo"/>.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, GeoCoordinate coordinate, DeviceInfo deviceInfo = null)
        {
            return new Session(loginProvider, await loginProvider.GetAccessToken(), coordinate, deviceInfo);
        }
        
    }
}

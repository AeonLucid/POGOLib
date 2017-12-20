﻿using System;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using POGOLib.Official.Logging;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Util.Device;
using POGOProtos.Networking.Requests.Messages;

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
        /// <param name="deviceWrapper">The <see cref="DeviceWrapper"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceWrapper"/>.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, double initialLatitude, double initialLongitude, DeviceWrapper deviceWrapper = null, GetPlayerMessage.Types.PlayerLocale playerLocale = null)
        {
            if (accessToken.IsExpired)
                throw new Exception("AccessToken is expired.");

            Logger.Debug("Authenticated from cache.");

            return new Session(loginProvider, accessToken, new GeoCoordinate(initialLatitude, initialLongitude), deviceWrapper, playerLocale);
        }

        /// <summary>
        /// Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="initialLatitude">The initial latitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="initialLongitude">The initial longitude you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceWrapper">The <see cref="DeviceWrapper"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceWrapper"/>.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, double initialLatitude, double initialLongitude, DeviceWrapper deviceWrapper = null, GetPlayerMessage.Types.PlayerLocale playerLocale = null)
        {
            return new Session(loginProvider, await loginProvider.GetAccessToken(), new GeoCoordinate(initialLatitude, initialLongitude), deviceWrapper, playerLocale);
        }

        /// <summary>
        /// Login with a stored <see cref="AccessToken" />.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="accessToken">The <see cref="AccessToken"/> you want to re-use.</param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceWrapper">The <see cref="DeviceWrapper"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceWrapper"/>.</param>
        /// <returns></returns>
        public static Session GetSession(ILoginProvider loginProvider, AccessToken accessToken, GeoCoordinate coordinate, DeviceWrapper deviceWrapper = null, GetPlayerMessage.Types.PlayerLocale playerLocale = null)
        {
            if (accessToken.IsExpired)
            {
                throw new ArgumentException($"{nameof(accessToken)} is expired.");
            }

            Logger.Debug("Authenticated from cache.");
            return new Session(loginProvider, accessToken, coordinate, deviceWrapper, playerLocale);
        }

        /// <summary>
        /// Login through OAuth with PTC / Google.
        /// </summary>
        /// <param name="loginProvider">The OAuth provider you use to authenticate.</param>
        /// <param name="coordinate">The initial coordinate you will spawn at after logging into PokémonGo.</param>
        /// <param name="deviceWrapper">The <see cref="DeviceWrapper"/> used by the <see cref="Session"/>, keep null if you want a randomly generated <see cref="DeviceWrapper"/>.</param>
        /// <returns></returns>
        public static async Task<Session> GetSession(ILoginProvider loginProvider, GeoCoordinate coordinate, DeviceWrapper deviceWrapper = null, GetPlayerMessage.Types.PlayerLocale playerLocale = null)
        {
            return new Session(loginProvider, await loginProvider.GetAccessToken(), coordinate, deviceWrapper, playerLocale);
        }
    }
}

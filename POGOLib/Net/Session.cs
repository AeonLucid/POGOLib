﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using POGOLib.Net.Authentication;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon;
using POGOLib.Pokemon.Data;
using POGOProtos.Settings;
using Splat;

namespace POGOLib.Net
{
    /// <summary>
    ///     This is an authenticated <see cref="Session" /> with PokémonGo that handles everything between the developer and
    ///     PokémonGo.
    /// </summary>
    public class Session : IDisposable
    {
        private static readonly IFullLogger Log = LogHost.Default;

        /// <summary>
        ///     This is the <see cref="HeartbeatDispatcher" /> which is responsible for retrieving events and updating gps
        ///     location.
        /// </summary>
        private readonly HeartbeatDispatcher _heartbeat;

        /// <summary>
        ///     This is the <see cref="RpcClient" /> which is responsible for all communication between us and PokémonGo.
        ///     Only use this if you know what you are doing.
        /// </summary>
        public readonly RpcClient RpcClient;

        internal Session(AccessToken accessToken, string password, GeoCoordinate geoCoordinate)
        {
            AccessToken = accessToken;
            Password = password;
            Player = new Player(geoCoordinate);
            Map = new Map();
            Templates = new Templates();
            RpcClient = new RpcClient(this);
            _heartbeat = new HeartbeatDispatcher(this);
        }

        public Templates Templates { get; }

        /// <summary>
        ///     Gets the <see cref="AccessToken" /> of the <see cref="Session" />.
        /// </summary>
        public AccessToken AccessToken { get; private set; }

        /// <summary>
        ///     Gets the <see cref="Password" /> of the <see cref="Session" />.
        /// </summary>
        internal string Password { get; }

        /// <summary>
        ///     Gets the <see cref="Player" /> of the <see cref="Session" />.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        ///     Gets the <see cref="Map" /> of the <see cref="Session" />.
        /// </summary>
        public Map Map { get; }

        /// <summary>
        ///     Gets the <see cref="GlobalSettings" /> of the <see cref="Session" />.
        /// </summary>
        public GlobalSettings GlobalSettings { get; internal set; }

        /// <summary>
        ///     Gets the hash of the <see cref="GlobalSettings" />.
        /// </summary>
        internal string GlobalSettingsHash { get; set; } = string.Empty;

        private Mutex ReauthenticateMutex { get; } = new Mutex();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<bool> Startup()
        {
            if (!await RpcClient.Startup())
            {
                return false;
            }
            _heartbeat.StartDispatcher();
            return true;
        }

        public void Shutdown()
        {
            _heartbeat.StopDispatcher();
        }

        /// <summary>
        ///     Ensures the <see cref="Session" /> gets reauthenticated, no matter how long it takes.
        /// </summary>
        internal async void Reauthenticate()
        {
            ReauthenticateMutex.WaitOne();
            if (AccessToken.IsExpired)
            {
                AccessToken accessToken = null;
                var tries = 0;
                while (accessToken == null)
                {
                    try
                    {
                        switch (AccessToken.LoginProvider)
                        {
                            case LoginProvider.PokemonTrainerClub:
                                accessToken = await Login.WithPokemonTrainerClub(AccessToken.Username, Password);
                                break;
                            case LoginProvider.GoogleAuth:
                                accessToken = Login.WithGoogle(AccessToken.Username, Password);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Reauthenticate exception was catched: ", exception);
                    }
                    finally
                    {
                        if (accessToken == null)
                        {
                            var sleepSeconds = Math.Min(60, ++tries*5);
                            Log.Error($"Reauthentication failed, trying again in {sleepSeconds} seconds.");
                            await Task.Delay(sleepSeconds * 1000);
                        }
                    }
                }
                AccessToken = accessToken;
                OnAccessTokenUpdated();
            }
            ReauthenticateMutex.ReleaseMutex();
        }

        private void OnAccessTokenUpdated()
        {
            AccessTokenUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> AccessTokenUpdated;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReauthenticateMutex?.Dispose();
                RpcClient?.Dispose();
            }
        }
    }
}
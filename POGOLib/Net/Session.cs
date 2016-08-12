using System;
using System.Threading;
using GeoCoordinatePortable;
using POGOLib.Logging;
using POGOLib.Net.Authentication;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon;
using POGOLib.Util.Devices;
using POGOProtos.Settings;
using System.Threading.Tasks;
using System.Linq;
using POGOLib.Util;

namespace POGOLib.Net
{
    /// <summary>
    ///     This is an authenticated <see cref="Session" /> with PokémonGo that handles everything between the developer and
    ///     PokémonGo.
    /// </summary>
    public class Session : IDisposable
    {
        
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

        readonly static string[] VALID_LOGIN_PROVIDERS = new [] { "ptc", "google" };

        /// <summary>
        ///     Stores data like assets and item templates. Defaults to an in-memory cache, but can be implemented as writing to disk by the platform
        /// </summary>
        public IDataCache DataCache { get; set; } = new MemoryDataCache();

        internal Session(ILoginProvider loginProvider, AccessToken accessToken, string password, GeoCoordinate geoCoordinate, Device device = null)
        {
            if (!VALID_LOGIN_PROVIDERS.Contains(loginProvider.ProviderID))
            {
                throw new ArgumentException("LoginProvider ID must be one of the following: " + string.Join(", ", VALID_LOGIN_PROVIDERS));
            }

            if (device == null) device = DeviceInfo.GetDeviceByName("nexus5");
            LoginProvider = loginProvider;
            AccessToken = accessToken;
            Password = password;
            Device = device;
            Player = new Player(geoCoordinate);
            Map = new Map(this);
            RpcClient = new RpcClient(this);
            _heartbeat = new HeartbeatDispatcher(this);
        }

        //public Templates Templates { get; private set; }

        // throw new Exception($"{nameof(TemplateDataCache)} must be set before {nameof(Startup)} is called to use {nameof(Templates)}");

        /// <summary>
        ///     Gets the <see cref="AccessToken" /> of the <see cref="Session" />.
        /// </summary>
        public AccessToken AccessToken { get; private set; }

        public ILoginProvider LoginProvider { get; private set; }

        /// <summary>
        ///     Gets the <see cref="Password" /> of the <see cref="Session" />.
        /// </summary>
        internal string Password { get; }

        /// <summary>
        ///     Gets the <see cref="Device"/> of the <see cref="Session"/>. The <see cref="RpcClient"/> will try to act like this <see cref="Device"/>.
        /// </summary>
        public Device Device { get; private set; }

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
            if (!(await RpcClient.Startup()))
            {
                return false;
            }
            await _heartbeat.StartDispatcher();
            return true;
        }

        public void Shutdown()
        {
            _heartbeat.StopDispatcher();
        }

        /// <summary>
        ///     Ensures the <see cref="Session" /> gets reauthenticated, no matter how long it takes.
        /// </summary>
        internal async Task Reauthenticate()
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
                        accessToken = await LoginProvider.GetAccessToken(AccessToken.Username, Password);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"Reauthenticate exception was catched: {exception}");
                    }
                    finally
                    {
                        if (accessToken == null)
                        {
                            var sleepSeconds = Math.Min(60, ++tries*5);
                            Logger.Error($"Reauthentication failed, trying again in {sleepSeconds} seconds.");
                            await Task.Delay(TimeSpan.FromMilliseconds(sleepSeconds * 1000));
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
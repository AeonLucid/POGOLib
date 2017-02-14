using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using POGOLib.Official.Logging;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Pokemon;
using POGOLib.Official.Util.Device;
using POGOLib.Official.Util.Hash;
using POGOProtos.Settings;
using static POGOProtos.Networking.Envelopes.Signature.Types;

namespace POGOLib.Official.Net
{
    /// <summary>
    /// This is an authenticated <see cref="Session" /> with PokémonGo that handles everything between the developer and PokémonGo.
    /// </summary>
    public class Session : IDisposable
    {
        
        /// <summary>
        /// This is the <see cref="HeartbeatDispatcher" /> which is responsible for retrieving events and updating gps location.
        /// </summary>
        private readonly HeartbeatDispatcher _heartbeat;

        /// <summary>
        /// This is the <see cref="RpcClient" /> which is responsible for all communication between us and PokémonGo.
        /// Only use this if you know what you are doing.
        /// </summary>
        public readonly RpcClient RpcClient;

        private static readonly string[] ValidLoginProviders = { "ptc", "google" };

        /// <summary>
        /// Stores data like assets and item templates. Defaults to an in-memory cache, but can be implemented as writing to disk by the platform
        /// </summary>
        // public IDataCache DataCache { get; set; } = new MemoryDataCache();
        // public Templates Templates { get; private set; }

        internal Session(ILoginProvider loginProvider, AccessToken accessToken, GeoCoordinate geoCoordinate, DeviceInfo deviceInfo = null)
        {
            if (!ValidLoginProviders.Contains(loginProvider.ProviderId))
            {
                throw new ArgumentException($"LoginProvider ID must be one of the following: {string.Join(", ", ValidLoginProviders)}");
            }

            HttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Niantic App");
            HttpClient.DefaultRequestHeaders.ExpectContinue = false;
            
            DeviceInfo = deviceInfo ?? DeviceInfoUtil.GetRandomDevice(this);
            AccessToken = accessToken;
            LoginProvider = loginProvider;
            Player = new Player(geoCoordinate);
            Map = new Map(this);
            RpcClient = new RpcClient(this);
            _heartbeat = new HeartbeatDispatcher(this);
        }

        /// <summary>
        /// Gets the <see cref="Random"/> of the <see cref="Session"/>.
        /// </summary>
        internal Random Random { get; private set; } = new Random();

        /// <summary>
        /// Gets the <see cref="HttpClient"/> of the <see cref="Session"/>.
        /// </summary>
        internal HttpClient HttpClient { get; }

        /// <summary>
        /// Gets the <see cref="DeviceInfo"/> used by <see cref="RpcEncryption"/>.
        /// </summary>
        public DeviceInfo DeviceInfo { get; private set; }

        /// <summary>
        /// Gets the <see cref="ILoginProvider"/> used to obtain an <see cref="AccessToken"/>.
        /// </summary>
        private ILoginProvider LoginProvider { get; }

        /// <summary>
        ///  Gets the <see cref="AccessToken"/> of the <see cref="Session" />.
        /// </summary>
        public AccessToken AccessToken { get; private set; }

        /// <summary>
        /// Gets the <see cref="Player"/> of the <see cref="Session" />.
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// Gets the <see cref="Map"/> of the <see cref="Session" />.
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// Gets the <see cref="GlobalSettings"/> of the <see cref="Session" />.
        /// </summary>
        public GlobalSettings GlobalSettings { get; internal set; }

        /// <summary>
        /// Gets the hash of the <see cref="GlobalSettings"/>.
        /// </summary>
        internal string GlobalSettingsHash { get; set; } = string.Empty;

        private Semaphore ReauthenticateMutex { get; } = new Semaphore(1, 1);

        public async Task<bool> StartupAsync()
        {
            if (!Configuration.IgnoreHashVersion && !await CheckHasherVersion())
            {
                throw new HashVersionMismatchException($"The version of the {nameof(Configuration.Hasher)} ({Configuration.Hasher.PokemonVersion}) does not match the minimal API version of PokemonGo. Set 'Configuration.IgnoreHashVersion' to true if you want to disable the version check.");
            }

            if (!await RpcClient.StartupAsync().ConfigureAwait(false))
            {
                return false;
            }

            await _heartbeat.StartDispatcher().ConfigureAwait(false);

            return true;
        }

        public void Shutdown()
        {
            _heartbeat.StopDispatcher();
        }

        /// <summary>
        /// Checks if the current minimal version of PokemonGo matches the version of the <see cref="Configuration.Hasher"/>.
        /// </summary>
        /// <returns>Returns true if the version matches.</returns>
        public async Task<bool> CheckHasherVersion()
        {
            var pogoVersionRaw = await HttpClient.GetStringAsync(Constants.VersionUrl);
            pogoVersionRaw = pogoVersionRaw.Replace("\n", "");
            pogoVersionRaw = pogoVersionRaw.Replace("\u0006", "");

            var pogoVersion = new Version(pogoVersionRaw);
            var result = Configuration.Hasher.PokemonVersion.CompareTo(pogoVersion);
            return result != -1;
        }

        /// <summary>
        /// Ensures the <see cref="Session" /> gets reauthenticated, no matter how long it takes.
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
                        accessToken = await LoginProvider.GetAccessToken();
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
            ReauthenticateMutex.Release();
        }

        private void OnAccessTokenUpdated()
        {
            AccessTokenUpdated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<EventArgs> AccessTokenUpdated;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            ReauthenticateMutex?.Dispose();
            RpcClient?.Dispose();
            HttpClient?.Dispose();
        }
    }
}
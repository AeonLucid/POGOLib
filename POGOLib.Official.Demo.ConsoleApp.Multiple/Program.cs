using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using POGOLib.Official.Extensions;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net;
using POGOLib.Official.Net.Authentication;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Util.Hash;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using LogLevel = POGOLib.Official.Logging.LogLevel;

namespace POGOLib.Official.Demo.ConsoleApp.Multiple
{
    public class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This is just a demo application to test out the library and hit all
        /// rate limits. If you want to see basic usage, please look at
        /// the project "POGOLib.Official.Demo.ConsoleApp".
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            // Configure Logger
            LogManager.Configuration = new XmlLoggingConfiguration(Path.Combine(Directory.GetCurrentDirectory(), "nlog.config"));

            Logging.Logger.RegisterLogOutput((level, message) =>
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        Logger.Debug(message);
                        break;
                    case LogLevel.Info:
                        Logger.Info(message);
                        break;
                    case LogLevel.Notice:
                    case LogLevel.Warn:
                        Logger.Warn(message);
                        break;
                    case LogLevel.Error:
                        Logger.Error(message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(level), level, null);
                }
            });

            // Initiate console
            Logger.Info("Booting up.");
            Logger.Info("Type 'q', 'quit' or 'exit' to exit.");
            Console.Title = "POGO Multiple Demo";

            // Configure hasher
            var pokeHashAuthKey = Environment.GetEnvironmentVariable("POKEHASH_AUTHKEY") ?? "";

            Configuration.Hasher = new PokeHashHasher(pokeHashAuthKey);

            // Settings
            var accounts = JsonConvert.DeserializeObject<List<Account>>(File.ReadAllText("accounts.json"));

            Run(accounts);

            // Handle quit commands.
            HandleCommands();
        }

        private static void Run(IEnumerable<Account> accounts)
        {
            foreach (var account in accounts)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        // Login
                        ILoginProvider loginProvider;

                        switch (account.LoginProvider)
                        {
                            case "google":
                                loginProvider = new GoogleLoginProvider(account.Username, account.Password);
                                break;
                            case "ptc":
                                loginProvider = new PtcLoginProvider(account.Username, account.Password);
                                break;
                            default:
                                throw new ArgumentException("Login provider must be either \"google\" or \"ptc\".");
                        }

                        var locRandom = new Random();
                        var latitude = 51.507352 + locRandom.NextDouble(-0.000030, 0.000030); // Somewhere in London
                        var longitude = -0.127758 + locRandom.NextDouble(-0.000030, 0.000030);
                        var session = await GetSession(loginProvider, latitude, longitude, true);

                        SaveAccessToken(session.AccessToken);

                        session.AccessTokenUpdated += SessionOnAccessTokenUpdated;
                        session.Player.Inventory.Update += InventoryOnUpdate;
                        session.Map.Update += MapOnUpdate;

                        // Send initial requests and start HeartbeatDispatcher.
                        // This makes sure that the initial heartbeat request finishes and the "session.Map.Cells" contains stuff.
                        if (!await session.StartupAsync())
                        {
                            throw new Exception("Session couldn't start up.");
                        }

                        // Retrieve the closest fort to your current player coordinates.
                        var closestFort = session.Map.GetFortsSortedByDistance().FirstOrDefault();
                        if (closestFort != null)
                        {
                            for (var i = 0; i < 50; i++)
                            {
                                Task.Run(async () =>
                                {
                                    var request = new Request
                                    {
                                        RequestType = RequestType.FortDetails,
                                        RequestMessage = new FortDetailsMessage
                                        {
                                            FortId = closestFort.Id,
                                            Latitude = closestFort.Latitude,
                                            Longitude = closestFort.Longitude
                                        }.ToByteString()
                                    };

                                    await session.RpcClient.GetRequestEnvelopeAsync(new[] { request }, true);
                                });
                            }
                        }
                        else
                        {
                            Logger.Info("No fort found nearby.");
                        }

                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Username '{account.Username}' had exception: {e.Message}");
                    }
                }, CancellationTokenSource.Token);
            }
        }

        private static void SessionOnAccessTokenUpdated(object sender, EventArgs eventArgs)
        {
            var session = (Session)sender;

            SaveAccessToken(session.AccessToken);

            Logger.Info("Saved access token to file.");
        }

        private static void InventoryOnUpdate(object sender, EventArgs eventArgs)
        {
            Logger.Info("Inventory was updated.");
        }

        private static void MapOnUpdate(object sender, EventArgs eventArgs)
        {
            Logger.Info("Map was updated.");
        }

        private static void SaveAccessToken(AccessToken accessToken)
        {
            var fileName = Path.Combine(Directory.GetCurrentDirectory(), "Cache", $"{accessToken.Uid}.json");

            File.WriteAllText(fileName, JsonConvert.SerializeObject(accessToken, Formatting.Indented));
        }

        private static void HandleCommands()
        {
            var keepRunning = true;

            while (keepRunning)
            {
                var command = Console.ReadLine();

                switch (command)
                {
                    case "q":
                    case "quit":
                    case "exit":
                        CancellationTokenSource.Cancel(false);
                        keepRunning = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Login to PokémonGo and return an authenticated <see cref="Session" />.
        /// </summary>
        /// <param name="loginProvider">Provider ID must be 'PTC' or 'Google'.</param>
        /// <param name="initLat">The initial latitude.</param>
        /// <param name="initLong">The initial longitude.</param>
        /// <param name="mayCache">Can we cache the <see cref="AccessToken" /> to a local file?</param>
        private static async Task<Session> GetSession(ILoginProvider loginProvider, double initLat, double initLong, bool mayCache = false)
        {
            var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "Cache");
            var fileName = Path.Combine(cacheDir, $"{loginProvider.UserId}-{loginProvider.ProviderId}.json");

            if (mayCache)
            {
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                if (File.Exists(fileName))
                {
                    var accessToken = JsonConvert.DeserializeObject<AccessToken>(File.ReadAllText(fileName));

                    if (!accessToken.IsExpired)
                        return Login.GetSession(loginProvider, accessToken, initLat, initLong);
                }
            }

            var session = await Login.GetSession(loginProvider, initLat, initLong);

            if (mayCache)
                SaveAccessToken(session.AccessToken);

            return session;
        }
    }
}
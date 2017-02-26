using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using POGOLib.Official.LoginProviders;
using POGOLib.Official.Net;
using POGOLib.Official.Net.Authentication;
using POGOLib.Official.Net.Authentication.Data;
using POGOLib.Official.Util.Hash;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using LogLevel = POGOLib.Official.Logging.LogLevel;
using POGOLib.Official.Extensions;

namespace POGOLib.Official.Demo.ConsoleApp
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            Run(args).GetAwaiter().GetResult();
        }

        private static async Task Run(string[] args)
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
            Console.Title = "POGO Demo";

            // Configure hasher - DO THIS BEFORE ANYTHING ELSE!!
            //
            //  If you want to use the latest POGO version, you have
            //  to use the PokeHashHasher. For more information:
            //  https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer
            //
            //  You may also not use the PokeHashHasher, it will then use
            //  the built-in hasher which was made for POGO 0.45.0. 
            //  Don't forget to use "Configuration.IgnoreHashVersion = true;" too.
            //
            //  Expect some captchas in that case..

            var pokeHashAuthKey = Environment.GetEnvironmentVariable("POKEHASH_AUTHKEY") ?? "";

            Configuration.Hasher = new PokeHashHasher(pokeHashAuthKey);
            // Configuration.IgnoreHashVersion = true;

            // Settings
            var loginProviderStr = "ptc";
            var usernameStr = Environment.GetEnvironmentVariable("PTC_USERNAME") ?? ""; // Your PTC username
            var passwordStr = Environment.GetEnvironmentVariable("PTC_PASSWORD") ?? ""; // Your PTC password

            // Login
            ILoginProvider loginProvider;

            switch (loginProviderStr)
            {
                case "google":
                    loginProvider = new GoogleLoginProvider(usernameStr, passwordStr);
                    break;
                case "ptc":
                    loginProvider = new PtcLoginProvider(usernameStr, passwordStr);
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
                var fortDetailsBytes = await session.RpcClient.SendRemoteProcedureCallAsync(new Request
                {
                    RequestType = RequestType.FortDetails,
                    RequestMessage = new FortDetailsMessage
                    {
                        FortId = closestFort.Id,
                        Latitude = closestFort.Latitude,
                        Longitude = closestFort.Longitude
                    }.ToByteString()
                });
                var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

                Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));
            }
            else
            {
                Logger.Info("No fort found nearby.");
            }

            // Handle quit commands.
            HandleCommands();
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
                        keepRunning = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Login to PokémonGo and return an authenticated <see cref="Session" />.
        /// </summary>
        /// <param name="loginProvider">Provider must be PTC or Google.</param>
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
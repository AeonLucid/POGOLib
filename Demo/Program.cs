using CommandLine;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using POGOLib.Net;
using POGOLib.Net.Authentication;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon.Data;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System;
using System.IO;
using System.Linq;

namespace Demo
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        ///     This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Log.Info("Booting up.");
            Log.Info("Type 'q', 'quit' or 'exit' to exit.");
            Console.Title = "POGO Demo";

            var user = "user";
            var password = "password";
            var provider = "Google"; //"PTC"

            //var arguments = new Arguments();
            //if (Parser.Default.ParseArguments(args, arguments))
            //{
            var latitude = 37.808673;
            var longitude = -122.409950;
            var session = GetSession(user, password, provider, latitude,
                longitude, true, DeviceSettings.FromPresets("galaxy6"));

            SaveAccessToken(session.AccessToken);

            session.AccessTokenUpdated += SessionOnAccessTokenUpdated;
            session.Player.Inventory.Update += InventoryOnUpdate;
            session.Map.Update += MapOnUpdate;

            // Send initial requests and start HeartbeatDispatcher
            session.Startup();

            //var fortDetailsBytes = session.RpcClient.SendRemoteProcedureCall(new Request
            //{
            //    RequestType = RequestType.FortDetails,
            //    RequestMessage = new FortDetailsMessage
            //    {
            //        FortId = "e4a5b5a63cf34100bd620c598597f21c.12",
            //        Latitude = 51.507335,
            //        Longitude = -0.127689
            //    }.ToByteString()
            //});
            //var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

            //Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));
            //}

            HandleCommands();
        }

        private static void SessionOnAccessTokenUpdated(object sender, EventArgs eventArgs)
        {
            var session = (Session)sender;

            SaveAccessToken(session.AccessToken);

            Log.Info("Saved access token to file.");
        }

        private static void InventoryOnUpdate(object sender, EventArgs eventArgs)
        {
            Log.Info("Inventory was updated.");
        }

        private static void MapOnUpdate(object sender, EventArgs eventArgs)
        {
            Log.Info("Map was updated.");
        }

        private static void SaveAccessToken(AccessToken accessToken)
        {
            var fileName = Path.Combine(Environment.CurrentDirectory, "cache", $"{accessToken.Uid}.json");

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
        ///     Login to PokémonGo and return an authenticated <see cref="Session" />.
        /// </summary>
        /// <param name="username">The username of your PTC / Google account.</param>
        /// <param name="password">The password of your PTC / Google account.</param>
        /// <param name="loginProviderStr">Must be 'PTC' or 'Google'.</param>
        /// <param name="initLat">The initial latitude.</param>
        /// <param name="initLong">The initial longitude.</param>
        /// <param name="mayCache">Can we cache the <see cref="AccessToken" /> to a local file?</param>
        private static Session GetSession(string username, string password, string loginProviderStr, double initLat,
            double initLong, bool mayCache = false, DeviceSettings deviceSettings = null)
        {
            var loginProvider = ResolveLoginProvider(loginProviderStr);
            var cacheDir = Path.Combine(Environment.CurrentDirectory, "cache");
            var fileName = Path.Combine(cacheDir, $"{username}-{loginProvider}.json");

            deviceSettings = deviceSettings ?? DeviceSettings.FromPresets(DeviceInfoHelper.DeviceInfoSets.Keys.First());

            if (mayCache)
            {
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                if (File.Exists(fileName))
                {
                    var accessToken = JsonConvert.DeserializeObject<AccessToken>(File.ReadAllText(fileName));

                    if (!accessToken.IsExpired)
                        return Login.GetSession(accessToken, password, initLat, initLong, deviceSettings);
                }
            }

            var session = Login.GetSession(username, password, loginProvider, initLat, initLong, deviceSettings);

            if (mayCache)
                SaveAccessToken(session.AccessToken);

            return session;
        }

        private static LoginProvider ResolveLoginProvider(string loginProvider)
        {
            switch (loginProvider)
            {
                case "PTC":
                    return LoginProvider.PokemonTrainerClub;

                case "Google":
                    return LoginProvider.GoogleAuth;

                default:
                    throw new Exception($"The login method '{loginProvider}' is not supported.");
            }
        }
    }
}
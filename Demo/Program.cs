﻿using System;
using System.IO;
using CommandLine;
using Google.Protobuf;
using Newtonsoft.Json;
using POGOLib.Logging;
using POGOLib.Net;
using POGOLib.Net.Authentication;
using POGOLib.Net.Authentication.Data;
using POGOLib.Pokemon.Data;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System.Threading.Tasks;

namespace Demo
{
    public class Program
    {
        
        /// <summary>
        ///     This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args"></param>
        public static  void Main(string[] args)
        {
            Run(args).GetAwaiter().GetResult();
        }

        private static async Task Run(string[] args)
        {
            Logger.RegisterLogOutput((logLevel, message) => {
                if (logLevel < LoggerConfiguration.MinimumLogLevel) return;

                var foregroundColor = LoggerConfiguration.DefaultForegroundColor;
                var backgroundColor = LoggerConfiguration.DefaultBackgroundColor;
                var timestamp = DateTime.Now.ToString("HH:mm:ss");

                if (LoggerConfiguration.LogLevelColors.ContainsKey(logLevel))
                {
                    var colors = LoggerConfiguration.LogLevelColors[logLevel];

                    foregroundColor = colors.ForegroundColor;
                    backgroundColor = colors.BackgroundColor;
                }

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
                Console.WriteLine($"{timestamp,-10}{logLevel,-8}{message}");
                Console.ResetColor();
            });

            Logger.Info("Booting up.");
            Logger.Info("Type 'q', 'quit' or 'exit' to exit.");
            Console.Title = "POGO Demo";

            var arguments = new Arguments();
            if (Parser.Default.ParseArguments(args, arguments))
            {
                var latitude = 51.507351; // Somewhere in London
                var longitude = -0.127758;
                var session = await GetSession(arguments.Username, arguments.Password, arguments.LoginProvider, latitude,
                    longitude, true);

                SaveAccessToken(session.AccessToken);

                session.AccessTokenUpdated += SessionOnAccessTokenUpdated;
                session.Player.Inventory.Update += InventoryOnUpdate;
                session.Map.Update += MapOnUpdate;

                // Send initial requests and start HeartbeatDispatcher
                await session.Startup();

                var fortDetailsBytes = await session.RpcClient.SendRemoteProcedureCall(new Request
                {
                    RequestType = RequestType.FortDetails,
                    RequestMessage = new FortDetailsMessage
                    {
                        FortId = "e4a5b5a63cf34100bd620c598597f21c.12",
                        Latitude = 51.507335,
                        Longitude = -0.127689
                    }.ToByteString()
                });
                var fortDetailsResponse = FortDetailsResponse.Parser.ParseFrom(fortDetailsBytes);

                Console.WriteLine(JsonConvert.SerializeObject(fortDetailsResponse, Formatting.Indented));
            }

            HandleCommands();
        }

        private static void SessionOnAccessTokenUpdated(object sender, EventArgs eventArgs)
        {
            var session = (Session) sender;

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
        private static async Task<Session> GetSession(string username, string password, string loginProviderStr, double initLat,
            double initLong, bool mayCache = false)
        {
            var loginProvider = ResolveLoginProvider(loginProviderStr);
            var cacheDir = Path.Combine(Environment.CurrentDirectory, "cache");
            var fileName = Path.Combine(cacheDir, $"{username}-{loginProvider}.json");

            if (mayCache)
            {
                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);

                if (File.Exists(fileName))
                {
                    var accessToken = JsonConvert.DeserializeObject<AccessToken>(File.ReadAllText(fileName));

                    if (!accessToken.IsExpired)
                        return Login.GetSession(accessToken, password, initLat, initLong);
                }
            }

            var session = await Login.GetSession(username, password, loginProvider, initLat, initLong);

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
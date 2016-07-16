using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using POGOLib.Net;
using POGOLib.Pokemon;
using POGOLib.Util;
using ConsoleUtil = Demo.Util.ConsoleUtil;

namespace Demo
{
    public class Program
    {
        public static string SaveDataPath { get; private set; }
        
        private static readonly ILog Log = LogManager.GetLogger(typeof(POClient));

        /// <summary>
        /// This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.Title = "POGO Demo";

            var arguments = ParseArguments(args);

            SaveDataPath = Path.Combine(Environment.CurrentDirectory, "savedata");
            if (!Directory.Exists(SaveDataPath)) Directory.CreateDirectory(SaveDataPath);
            
            string username;

            if (!arguments.ContainsKey("username"))
            {
                Log.Info("Hi, please enter your PTC details.");

                Console.Write("Username: ");
                username = Console.ReadLine();
            }
            else
            {
                username = arguments["username"];
            }

            var client = new POClient(username, LoginProvider.PokemonTrainerClub);
            // Load previous data.
            if (!client.LoadClientData())
            {
                Console.Write("Password: ");
                var password = ConsoleUtil.ReadLineMasked();
                
                // Need to set initial gps data before authenticating!
                if (!client.HasGpsData())
                {
                    Console.Write("First time lat: ");
                    var latitude = Console.ReadLine()?.Replace(".", ",");
                    Console.Write("First time long: ");
                    var longitude = Console.ReadLine()?.Replace(".", ",");

                    client.SetGpsData(double.Parse(latitude), double.Parse(longitude));
                }

                if(!client.Authenticate(password).Result)
                    throw new Exception("Wrong password.");

                client.SaveClientData();
            }

//            var profile = client.RPCClient.GetProfile();
//            Log.Info($"Username: {profile.Username}");

            var mapObjects = client.RPCClient.GetMapObjects();

            Log.Info($"Cells: {mapObjects.MapCells.Count}");
            foreach (var mapCell in mapObjects.MapCells)
            {
//                foreach (var fortData in mapCell.Forts)
//                {
//                    Log.Info($"FortId: {fortData.Id}");
//                    Log.Info($"\tFortType: {fortData.Type}");
//                }

//                foreach (var fortSummary in mapCell.FortSummaries)
//                {
//                    Log.Info($"FortSummaryId: {fortSummary.FortSummaryId}");
//                    Log.Info($"\tLatitude: {fortSummary.Latitude}");
//                    Log.Info($"\tLongitude: {fortSummary.Longitude}");
//                }

                foreach (var pokemon in mapCell.CatchablePokemons)
                {
                    Log.Info($"CatchablePokemon: {pokemon.PokemonType}");
//                    Log.Info($"\tEncounterId: {pokemon.EncounterId}");
                    Log.Info($"\tSpawnpointId: {pokemon.SpawnpointId}");
//                    Log.Info($"\tLatitude: {pokemon.Latitude}");
//                    Log.Info($"\tLongitude: {pokemon.Longitude}");
                    Log.Info($"\tExpiration: {TimeUtil.GetDateTimeFromMs(pokemon.ExpirationTimestampMs).AddHours(2)}"); // I'm in GMT+2 so I add two hours.
                }

                foreach (var pokemon in mapCell.NearbyPokemons)
                {
                    Log.Info($"NearbyPokemon: {pokemon.PokemonType}");
//                    Log.Info($"\tEncounterId: {pokemon.EncounterId}");
                    Log.Info($"\tDistanceInMeters: {pokemon.DistanceInMeters}");
                }

                foreach (var pokemon in mapCell.WildPokemons)
                {
                    Log.Info($"WildPokemon: {pokemon.PokemonType}");
                    Log.Info($"\tEncounterId: {pokemon.EncounterId}");
                    Log.Info($"\tSpawnpointId: {pokemon.SpawnpointId}");
//                    Log.Info($"\tLatitude: {pokemon.Latitude}");
//                    Log.Info($"\tLongitude: {pokemon.Longitude}");
                }
            }

            // Make sure to save if you want to use save / loading.
            client.SaveClientData();

            Console.ReadKey();
        }

        private static Dictionary<string, string> ParseArguments(IEnumerable<string> args)
        {
            var arguments = new Dictionary<string, string>();

            foreach (var s in args)
            {
                if (!s.StartsWith("--") || !s.Contains("="))
                {
                    Log.Error($"Invalid argument: '{s}'");
                    throw new ArgumentException(nameof(s));
                }

                var argument = s.Substring(2, s.Length - 2);
                var equalPos = s.IndexOf("=", StringComparison.Ordinal) - 2;
                var key = argument.Substring(0, equalPos);
                var value = argument.Substring(equalPos + 1, argument.Length - key.Length - 1);

                arguments.Add(key, value);
            }

            return arguments;
        }
    }
}

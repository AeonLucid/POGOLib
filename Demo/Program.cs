using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
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
        
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoClient));

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

            var loginProvider = LoginProvider.PokemonTrainerClub;

            if (arguments.ContainsKey("auth"))
            {
                if(arguments["auth"] == "google") loginProvider = LoginProvider.GoogleAuth;
            }

            var client = new PoClient(username, loginProvider);
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

                if(!client.Authenticate(password))
                    throw new Exception("Wrong password.");

                client.SaveClientData();
            }
            
//            var profile = client.RpcClient.GetProfile();
//            Log.Info($"Username: {profile.Username}");

            foreach (var mapCell in client.RpcClient.MapObjects.MapCells)
            {
                Log.Info($"CellId: {mapCell.S2CellId}");

                foreach (var pokemon in mapCell.CatchablePokemons)
                {
                    Log.Info($"\tCatchablePokemon: {pokemon.PokemonType}");
                    Log.Info($"\t\tExpiration: {TimeUtil.GetDateTimeFromMs(pokemon.ExpirationTimestampMs).AddHours(2)}"); // I'm in GMT+2 so I add two hours.
                }

                foreach (var pokemon in mapCell.NearbyPokemons)
                {
                    Log.Info($"\tNearbyPokemon: {pokemon.PokemonType}");
                    Log.Info($"\t\tDistanceInMeters: {pokemon.DistanceInMeters}");
                }

                foreach (var wildPokemon in mapCell.WildPokemons)
                {
                    Log.Info($"\tWildPokemon: {wildPokemon.Pokemon.PokemonId}");
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

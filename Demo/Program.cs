using System;
using System.IO;
using Demo.Util;
using POGOLib.Net;
using POGOLib.Net.Data;
using POGOLib.Pokemon;

namespace Demo
{
    public class Program
    {
        public static string SaveDataPath { get; private set; }

        /// <summary>
        /// This is just a demo application to test out the library / show a bit how it works.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.Title = "POGO Demo";

            SaveDataPath = Path.Combine(Environment.CurrentDirectory, "savedata");
            if (!Directory.Exists(SaveDataPath)) Directory.CreateDirectory(SaveDataPath);
            
            Console.WriteLine("Hi, please enter your PTC details.");
            Console.Write("Username: ");
            var username = Console.ReadLine();

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
            }

            // Make sure to save if you want to use save / loading.
            client.SaveClientData();

            Console.ReadKey();
        }
    }
}

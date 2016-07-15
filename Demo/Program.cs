using System;
using System.IO;
using Demo.Util;
using POGOLib.Net;
using POGOLib.Pokemon;

namespace Demo
{
    public class Program
    {
        public static string SaveDataPath { get; private set; }

        public static void Main(string[] args)
        {
            Console.Title = "POGO Demo";

            SaveDataPath = Path.Combine(Environment.CurrentDirectory, "savedata");
            if (!Directory.Exists(SaveDataPath)) Directory.CreateDirectory(SaveDataPath);

            Console.WriteLine("Hi, please enter your PTC details.");
            Console.Write("Username: ");
            var username = Console.ReadLine();

            var client = new POClient(username, LoginProvider.PokemonTrainerClub);
            if (!client.LoadAuthData())
            {
                Console.Write("Password: ");
                var password = ConsoleUtil.ReadLineMasked();

                if(!client.Authenticate(password).Result)
                    throw new Exception("Wrong password.");
            }

            Console.ReadKey();
        }
    }
}

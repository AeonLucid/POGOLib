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
            
            Console.WriteLine($"AccessToken: {client.AuthData.AccessToken}");
            Console.WriteLine($"ExpireDateTime: {client.AuthData.ExpireDateTime}");

//            var req = new RequestEnvelop
//            {
//                Unknown1 = 2,
//                RpcId = 8145806132888207460,
//                Latitude = BitConverter.ToUInt64(BitConverter.GetBytes(52.0953791), 0),
//                Longitude = BitConverter.ToUInt64(BitConverter.GetBytes(5.1164692), 0),
//                Altitude = BitConverter.ToUInt64(BitConverter.GetBytes(3.0), 0),
//                Unknown12 = 989,
//                Auth = new AuthInfo
//                {
//                    Provider = "ptc",
//                    Token = new JWT
//                    {
//                        Contents = client.AuthData.AccessToken,
//                        Unknown13 = 59
//                    }
//                },
//                Requests =
//                {
//                    new Requests
//                    {
//                        Type = 2
//                    },
//                    new Requests
//                    {
//                        Type = 126
//                    },
//                    new Requests
//                    {
//                        Type = 4
//                    },
//                    new Requests
//                    {
//                        Type = 129
//                    },
//                    new Requests
//                    {
//                        Type = 5,
//                        Message = new Unknown3
//                        {
//                            Unknown4 = "4a2e9bc330dae60e7b74fc85b98868ab4700802e"
//                        }
//                    }
//                }
//            };
//
//            using (var memoryStream = new MemoryStream())
//            {
//                req.WriteTo(memoryStream);
//
//                using (var httpClient = new HttpClient())
//                {
//                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Configuration.ApiUserAgent);
//
//                    var response = httpClient.PostAsync(Configuration.ApiUrl, new ByteArrayContent(memoryStream.ToArray())).Result;
//
//                    var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
//                    var responseEnvelope = ResponseEnvelop.Parser.ParseFrom(responseBytes);
//
//                    Console.WriteLine(responseBytes.Length);
//                    Console.WriteLine(responseEnvelope.ApiUrl);
//                }
//            }

            Console.ReadKey();
        }
    }
}

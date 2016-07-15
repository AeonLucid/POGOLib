using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOLib.Pokemon.Proto;
using static POGOLib.Pokemon.Proto.RequestEnvelop.Types;
using static POGOLib.Pokemon.Proto.RequestEnvelop.Types.AuthInfo.Types;

namespace POGOLib.Net
{
    public class RPCClient
    {
        private readonly POClient _poClient;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public RPCClient(POClient poClient)
        {
            _poClient = poClient;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Configuration.ApiUserAgent);
            _apiUrl = GetApiEndpoint();

            Console.WriteLine($"Using ApiUrl {_apiUrl}");
        }

        private string GetApiEndpoint()
        {
            var response = SendRemoteProtocolCall(new[]
            {
                new Requests
                {
                    Type = 2
                },
                new Requests
                {
                    Type = 126
                },
                new Requests
                {
                    Type = 4
                },
                new Requests
                {
                    Type = 129
                },
                new Requests
                {
                    Type = 5,
                    Message = new Unknown3
                    {
                        Unknown4 = "4a2e9bc330dae60e7b74fc85b98868ab4700802e"
                    }
                }
            });

            return response.ApiUrl;
        }

        private ResponseEnvelop SendRemoteProtocolCall(IEnumerable<Requests> requests)
        {
            var req = new RequestEnvelop
            {
                Unknown1 = 2,
                RpcId = 8145806132888207460,
                Latitude = BitConverter.ToUInt64(BitConverter.GetBytes(52.0953791), 0),
                Longitude = BitConverter.ToUInt64(BitConverter.GetBytes(5.1164692), 0),
                Altitude = BitConverter.ToUInt64(BitConverter.GetBytes(3.0), 0),
                Unknown12 = 989,
                Auth = new AuthInfo
                {
                    Provider = "ptc",
                    Token = new JWT
                    {
                        Contents = _poClient.AuthData.AccessToken,
                        Unknown13 = 59
                    }
                }
            };

            req.Requests.Add(requests);

            using (var memoryStream = new MemoryStream())
            {
                req.WriteTo(memoryStream);

                using (var response = _httpClient.PostAsync(Configuration.ApiUrl, new ByteArrayContent(memoryStream.ToArray())).Result)
                {
                    var responseBytes = response.Content.ReadAsByteArrayAsync().Result;

                    return ResponseEnvelop.Parser.ParseFrom(responseBytes);
                }
            }
        }
    }
}

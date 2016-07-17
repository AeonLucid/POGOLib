using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Google.Protobuf;
using log4net;
using POGOLib.Pokemon;
using POGOLib.Util;
using POGOProtos;
using POGOProtos.Requests;
using POGOProtos.Requests.Messages;
using POGOProtos.Responses;
using POGOProtos.Settings;
using static POGOProtos.Envelopes.Types;
using static POGOProtos.Envelopes.Types.RequestEnvelope.Types;
using static POGOProtos.Envelopes.Types.RequestEnvelope.Types.AuthInfo.Types;

namespace POGOLib.Net
{
    public class RpcClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RpcClient));

        private readonly PoClient _poClient;
        private readonly HttpClient _httpClient;
        private ulong _requestId;
        private readonly string _apiUrl;
        private AuthTicket _authTicket;

        private string _settingsHash;

        public RpcClient(PoClient poClient)
        {
            _poClient = poClient;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Configuration.ApiUserAgent);
            _requestId = (ulong) new Random().Next(100000000, 999999999);
            _apiUrl = $"https://{GetApiEndpoint()}/rpc";
        }

        public GlobalSettings GlobalSettings { get; private set; }
        public MapObjects MapObjects { get; private set; }

        private ulong RequestId
        {
            get {
                _requestId = _requestId + 1;
                return _requestId;
            }
        }

        private string GetApiEndpoint()
        {
            var response = SendRemoteProtocolCall(Configuration.ApiUrl, new Request
            {
                RequestType = RequestType.GetPlayer
            });

            return response.ApiUrl;
        }

        public MapObjects GetMapObjects()
        {
            var cellIds = MapUtil.GetCellIdsForLatLong(_poClient.ClientData.GpsData.Latitude, _poClient.ClientData.GpsData.Longitude);
            var sinceTimeMs = new List<long>(cellIds.Length);

            for (var i = 0; i < cellIds.Length; i++)
                sinceTimeMs.Add(0);

            var response = SendRemoteProtocolCall(_apiUrl, new Request
            {
                RequestType = RequestType.GetMapObjects,
                RequestMessage = new GetMapObjectsMessage
                {
                    CellId = {cellIds},
                    SinceTimeMs ={ sinceTimeMs.ToArray() },
                    Latitude = _poClient.ClientData.GpsData.Latitude,
                    Longitude = _poClient.ClientData.GpsData.Longitude
                }.ToByteString()
            });

            return MapObjects.Parser.ParseFrom(response.Returns[0]);
        }

        public LocalPlayer GetProfile()
        {
            var response = SendRemoteProtocolCall(_apiUrl, new Request
            {
                RequestType = RequestType.GetPlayer
            });

            return GetPlayerResponse.Parser.ParseFrom(response.Returns[0]).LocalPlayer;
        }

        private IEnumerable<Request> GetDefaultRequests()
        {
            return new[]
            {
                new Request
                {
                    RequestType = RequestType.GetHatchedEggs
                },
                new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = new GetInventoryMessage
                    {
                        TimestampMs = TimeUtil.GetCurrentTimestampInMs()
                    }.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.CheckAwardedBadges
                },
                new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = new GetDownloadSettingsMessage
                    {
                        Hash = "4a2e9bc330dae60e7b74fc85b98868ab4700802e"
                    }.ToByteString()
                }
            };
        }

        private ResponseEnvelope SendRemoteProtocolCall(string apiUrl, Request request)
        {
            if (!_poClient.HasGpsData())
                throw new Exception("No gps data has been set, can't send a rpc call.");

            var requestEnvelope = new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = RequestId,
                Latitude = _poClient.ClientData.GpsData.Latitude,
                Longitude = _poClient.ClientData.GpsData.Longitude,
                Altitude = _poClient.ClientData.GpsData.Altitude,
                Unknown12 = 123, // TODO: Figure this out.
                Requests = { GetDefaultRequests() }
            };

            if (_authTicket == null)
            {
                requestEnvelope.AuthInfo = new AuthInfo
                {
                    Provider = _poClient.ClientData.LoginProvider == LoginProvider.PokemonTrainerClub ? "ptc" : "google",
                    Token = new JWT
                    {
                        Contents = _poClient.ClientData.AuthData.AccessToken,
                        Unknown2 = 59
                    }
                };
            }
            else
            {
                requestEnvelope.AuthTicket = _authTicket;
            }

            requestEnvelope.Requests.Insert(0, request);

            using (var memoryStream = new MemoryStream())
            {
                requestEnvelope.WriteTo(memoryStream);

                using (var response = _httpClient.PostAsync(apiUrl, new ByteArrayContent(memoryStream.ToArray())).Result)
                {
                    var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                    var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);

                    if (_authTicket == null && responseEnvelope.AuthTicket != null)
                        _authTicket = responseEnvelope.AuthTicket;

                    Log.Debug($"Received {responseBytes.Length} bytes.");

                    // Problems:
                    // 5 Payloads are received but only the first one (request) is made available.
                    // Fix the other 4.

                    // 0 = request
                    // 1 = GetHatchedEggs
                    // 2 = GetInventory
                    // 3 = CheckAwardedBadges
                    // 4 = DownloadSettings

                    if (responseEnvelope.Returns.Count == 5)
                    {
                        var hatchedEggs = GetHatchedEggsResponse.Parser.ParseFrom(responseEnvelope.Returns[1]);

                        var checkAwardedBadges = CheckAwardedBadgesResponse.Parser.ParseFrom(responseEnvelope.Returns[3]);
                        var downloadSettingsResponse = DownloadSettingsResponse.Parser.ParseFrom(responseEnvelope.Returns[4]);

                        if (downloadSettingsResponse.Settings != null)
                        {
                            if (GlobalSettings == null || _settingsHash != downloadSettingsResponse.Hash)
                            {
                                _settingsHash = downloadSettingsResponse.Hash;
                                GlobalSettings = downloadSettingsResponse.Settings;
                            }
                            else
                            {
                                GlobalSettings = downloadSettingsResponse.Settings;
                            }
                        }
                    }

//                    var count = 0;
//                    foreach (var @return in responseEnvelope.Returns)
//                    {
//                        Log.Debug($"\tIndex: {count}");
//                        Log.Debug($"\t\tLength: {@return.Length}");
//
//                        count++;
//                    }

                    return responseEnvelope;
                }
            }
        }

        internal void Heartbeat()
        {
            MapObjects = GetMapObjects();
        }
    }
}

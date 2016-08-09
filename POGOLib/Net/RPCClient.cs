using GeoCoordinatePortable;
using Google.Protobuf;
using Google.Protobuf.Collections;
using log4net;
using POGOLib.Pokemon;
using POGOLib.Pokemon.Data;
using POGOLib.Util;
using POGOProtos.Enums;
using POGOProtos.Map;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace POGOLib.Net
{
    public class RpcClient : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RpcClient));

        static private readonly Stopwatch _internalWatch = new Stopwatch();

        /// <summary>
        ///     The <see cref="HttpClient" /> used for communication with PokémonGo.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        /// <summary>
        ///     The current 'unique' request id we are at.
        /// </summary>
        private ulong _requestId;

        /// <summary>
        ///     The rpc url we have to call.
        /// </summary>
        private string _requestUrl;

        private DeviceSettings _deviceSettings;

        internal RpcClient(Session session, DeviceSettings deviceSettings)
        {
            _session = session;
            _deviceSettings = deviceSettings;

            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.ApiUserAgent);
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            _requestId = (ulong)new Random().Next(100000000, 999999999);
        }

        internal DateTime LastRpcRequest { get; private set; }

        internal DateTime LastRpcMapObjectsRequest { get; private set; }

        internal GeoCoordinate LastGeoCoordinateMapObjectsRequest { get; private set; } = new GeoCoordinate();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Sends all requests which the (android-)client sends on startup
        /// </summary>
        internal bool Startup()
        {
            try
            {
                // Send GetPlayer to check if we're connected and authenticated
                GetPlayerResponse playerResponse;
                do
                {
                    var response = SendRemoteProcedureCall(new Request
                    {
                        RequestType = RequestType.GetPlayer
                    });
                    playerResponse = GetPlayerResponse.Parser.ParseFrom(response);
                    if (!playerResponse.Success)
                    {
                        Thread.Sleep(1000);
                    }
                } while (!playerResponse.Success);

                _session.Player.Data = playerResponse.PlayerData;

                // Get DownloadRemoteConfig
                var remoteConfigResponse = SendRemoteProcedureCall(new Request
                {
                    RequestType = RequestType.DownloadRemoteConfigVersion,
                    RequestMessage = new DownloadRemoteConfigVersionMessage
                    {
                        Platform = Platform.Android,
                        AppVersion = 2903
                    }.ToByteString()
                });
                var remoteConfigParsed = DownloadRemoteConfigVersionResponse.Parser.ParseFrom(remoteConfigResponse);

                var timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds();
                if (_session.Templates.AssetDigests == null || remoteConfigParsed.AssetDigestTimestampMs > timestamp)
                {
                    // GetAssetDigest
                    var assetDigestResponse = SendRemoteProcedureCall(new Request
                    {
                        RequestType = RequestType.GetAssetDigest,
                        RequestMessage = new GetAssetDigestMessage
                        {
                            Platform = Platform.Android,
                            AppVersion = 2903
                        }.ToByteString()
                    });
                    _session.Templates.SetAssetDigests(GetAssetDigestResponse.Parser.ParseFrom(assetDigestResponse));
                }

                if (_session.Templates.ItemTemplates == null || remoteConfigParsed.ItemTemplatesTimestampMs > timestamp)
                {
                    // DownloadItemTemplates
                    var itemTemplateResponse = SendRemoteProcedureCall(new Request
                    {
                        RequestType = RequestType.DownloadItemTemplates
                    });
                    _session.Templates.SetItemTemplates(
                        DownloadItemTemplatesResponse.Parser.ParseFrom(itemTemplateResponse));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     It is not recommended to call this. Map objects will update automatically and fire the <see cref="Map.Update" />
        ///     event.
        /// </summary>
        public void RefreshMapObjects()
        {
            var cellIds = MapUtil.GetCellIdsForLatLong(_session.Player.Coordinate.Latitude,
                _session.Player.Coordinate.Longitude);
            var sinceTimeMs = new List<long>(cellIds.Length);

            for (var i = 0; i < cellIds.Length; i++)
            {
                sinceTimeMs.Add(0);
            }

            var response = SendRemoteProcedureCall(new Request
            {
                RequestType = RequestType.GetMapObjects,
                RequestMessage = new GetMapObjectsMessage
                {
                    CellId =
                    {
                        cellIds
                    },
                    SinceTimestampMs =
                    {
                        sinceTimeMs.ToArray()
                    },
                    Latitude = _session.Player.Coordinate.Latitude,
                    Longitude = _session.Player.Coordinate.Longitude
                }.ToByteString()
            });

            var mapObjects = GetMapObjectsResponse.Parser.ParseFrom(response);

            if (mapObjects.Status == MapObjectsStatus.Success)
            {
                Log.Debug($"Received '{mapObjects.MapCells.Count}' map cells.");
                Log.Debug($"Received '{mapObjects.MapCells.SelectMany(c => c.CatchablePokemons).Count()}' pokemons.");
                Log.Debug($"Received '{mapObjects.MapCells.SelectMany(c => c.Forts).Count()}' forts.");
                if (mapObjects.MapCells.Count == 0)
                {
                    Log.Error("We received 0 map cells, are your GPS coordinates correct?");
                    return;
                }
                _session.Map.Cells = mapObjects.MapCells;
            }
            else
            {
                Log.Error($"GetMapObjects status is: '{mapObjects.Status}'.");
            }
        }

        /// <summary>
        ///     Gets the next <see cref="_requestId" /> for the <see cref="RequestEnvelope" />.
        /// </summary>
        /// <returns></returns>
        private ulong GetNextRequestId()
        {
            return _requestId++;
        }

        /// <summary>
        ///     Gets a collection of requests that should be sent in every request to PokémonGo along with your own
        ///     <see cref="Request" />.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Request> GetDefaultRequests()
        {
            var request = new List<Request>
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
                        LastTimestampMs = _session.Player.Inventory.LastInventoryTimestampMs
                    }.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.CheckAwardedBadges
                }
            };

            if (string.IsNullOrEmpty(_session.GlobalSettingsHash))
            {
                request.Add(new Request
                {
                    RequestType = RequestType.DownloadSettings
                });
            }
            else
            {
                request.Add(new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = new DownloadSettingsMessage
                    {
                        Hash = _session.GlobalSettingsHash
                    }.ToByteString()
                });
            }

            //If Incense is active we add this:
            //request.Add(new Request
            //{
            //    RequestType = RequestType.GetIncensePokemon,
            //    RequestMessage = new GetIncensePokemonMessage
            //    {
            //        PlayerLatitude = _session.Player.Coordinate.Latitude,
            //        PlayerLongitude = _session.Player.Coordinate.Longitude
            //    }.ToByteString()
            //});

            return request;
        }

        /// <summary>
        ///     Gets a <see cref="RequestEnvelope" /> with the default requests and authentication data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private RequestEnvelope GetRequestEnvelope(Request request)
        {
            var requestEnvelope = new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = GetNextRequestId(),
                Latitude = _session.Player.Coordinate.Latitude,
                Longitude = _session.Player.Coordinate.Longitude,
                Altitude = _session.Player.Coordinate.Altitude,
                Unknown12 = 123, // TODO: Figure this out.
                Requests = { GetDefaultRequests() }
            };

			requestEnvelope.Requests.Insert(0, request);

			if (_session.AccessToken.AuthTicket == null || _session.AccessToken.IsExpired)
            {
                if (_session.AccessToken.IsExpired)
                {
                    _session.Reauthenticate();
                }
                requestEnvelope.AuthInfo = new RequestEnvelope.Types.AuthInfo
                {
                    Provider = _session.AccessToken.LoginProvider == LoginProvider.PokemonTrainerClub ? "ptc" : "google",
                    Token = new RequestEnvelope.Types.AuthInfo.Types.JWT
                    {
                        Contents = _session.AccessToken.Token,
                        Unknown2 = 59
                    }
                };
            }
            else
            {
                requestEnvelope.AuthTicket = _session.AccessToken.AuthTicket;
                requestEnvelope.Unknown6 = GenerateSignature(requestEnvelope.Requests);
            }

            //requestEnvelope.Requests.Insert(0, request);

            return requestEnvelope;
        }

        /// <summary>
        ///     Prepares the <see cref="RequestEnvelope" /> to be sent with <see cref="_httpClient" />.
        /// </summary>
        /// <param name="requestEnvelope">The <see cref="RequestEnvelope" /> that will be send.</param>
        /// <returns><see cref="StreamContent" /> to be sent with <see cref="_httpClient" />.</returns>
        private ByteArrayContent PrepareRequestEnvelope(RequestEnvelope requestEnvelope)
        {
            var messageBytes = requestEnvelope.ToByteArray();

            // TODO: Compression?

            return new ByteArrayContent(messageBytes);
        }

        public ByteString SendRemoteProcedureCall(RequestType requestType)
        {
            return SendRemoteProcedureCall(new Request
            {
                RequestType = requestType
            });
        }

        public ByteString SendRemoteProcedureCall(Request request)
        {
            var requestEnvelope = GetRequestEnvelope(request);

            using (var requestData = PrepareRequestEnvelope(requestEnvelope))
            {
                using (var response = _httpClient.PostAsync(_requestUrl ?? Constants.ApiUrl, requestData).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Debug(response.Content.ReadAsStringAsync().Result);
                        throw new Exception(
                            "Received a non-success HTTP status code from the RPC server, see the console for the response.");
                    }

                    var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                    var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);

                    switch (responseEnvelope.StatusCode)
                    {
                        case 52: // Rate limit?
                            Log.Info(
                                $"We are sending requests too fast, sleeping for {Configuration.RateLimitTimeout} milliseconds.");
                            Thread.Sleep(Configuration.RateLimitTimeout);
                            return SendRemoteProcedureCall(request);

                        case 53: // New RPC url
                            if (Regex.IsMatch(responseEnvelope.ApiUrl, "pgorelease\\.nianticlabs\\.com\\/plfe\\/\\d+"))
                            {
                                _requestUrl = $"https://{responseEnvelope.ApiUrl}/rpc";
                                return SendRemoteProcedureCall(request);
                            }
                            throw new Exception(
                                $"Received an incorrect API url: '{responseEnvelope.ApiUrl}', status code was: '{responseEnvelope.StatusCode}'.");

                        case 102: // Invalid auth
                            Log.Debug("Received StatusCode 102, reauthenticating.");
                            _session.AccessToken.Expire();
                            _session.Reauthenticate();
                            return SendRemoteProcedureCall(request);

                        default:
                            Log.Info($"Unknown status code: {responseEnvelope.StatusCode}");
                            break;
                    }

                    LastRpcRequest = DateTime.UtcNow;
                    Log.Debug($"Sent RPC Request: '{request.RequestType}'");
                    if (request.RequestType == RequestType.GetMapObjects)
                    {
                        LastRpcMapObjectsRequest = LastRpcRequest;
                        LastGeoCoordinateMapObjectsRequest = _session.Player.Coordinate;
                    }
                    if (responseEnvelope.AuthTicket != null)
                    {
                        _session.AccessToken.AuthTicket = responseEnvelope.AuthTicket;
                        Log.Debug("Received a new AuthTicket from Pokémon!");
                    }
                    return HandleResponseEnvelope(request, responseEnvelope);
                }
            }
        }

        /// <summary>
        ///     Responsible for handling the received <see cref="ResponseEnvelope" />.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseEnvelope">
        ///     The <see cref="ResponseEnvelope" /> received from
        ///     <see cref="SendRemoteProcedureCall(Request)" />.
        /// </param>
        /// <returns>Returns the <see cref="ByteString" /> response of the <see cref="Request" />.</returns>
        private ByteString HandleResponseEnvelope(Request request, ResponseEnvelope responseEnvelope)
        {
            if (responseEnvelope.Returns.Count != 5)
            {
                throw new Exception($"There were only {responseEnvelope.Returns.Count} responses, we expected 5.");
            }

            // Take requested response and remove from returns.
            var requestResponse = responseEnvelope.Returns[0];
            responseEnvelope.Returns.RemoveAt(0);

            // Handle the default responses.
            HandleDefaultResponses(responseEnvelope.Returns);

            // Handle responses which affect the inventory
            HandleInventoryResponses(request, requestResponse);

            return requestResponse;
        }

        private void HandleInventoryResponses(Request request, ByteString requestResponse)
        {
            ulong pokemonId = 0;
            switch (request.RequestType)
            {
                case RequestType.ReleasePokemon:
                    var releaseResponse = ReleasePokemonResponse.Parser.ParseFrom(requestResponse);
                    if (releaseResponse.Result == ReleasePokemonResponse.Types.Result.Success ||
                        releaseResponse.Result == ReleasePokemonResponse.Types.Result.Failed)
                    {
                        var releaseMessage = ReleasePokemonMessage.Parser.ParseFrom(request.RequestMessage);
                        pokemonId = releaseMessage.PokemonId;
                    }
                    break;

                case RequestType.EvolvePokemon:
                    var evolveResponse = EvolvePokemonResponse.Parser.ParseFrom(requestResponse);
                    if (evolveResponse.Result == EvolvePokemonResponse.Types.Result.Success ||
                        evolveResponse.Result == EvolvePokemonResponse.Types.Result.FailedPokemonMissing)
                    {
                        var releaseMessage = ReleasePokemonMessage.Parser.ParseFrom(request.RequestMessage);
                        pokemonId = releaseMessage.PokemonId;
                    }
                    break;
            }
            if (pokemonId > 0)
            {
                var pokemons = _session.Player.Inventory.InventoryItems.Where(
                    i =>
                        i?.InventoryItemData?.PokemonData != null &&
                        i.InventoryItemData.PokemonData.Id.Equals(pokemonId));
                _session.Player.Inventory.RemoveInventoryItems(pokemons);
            }
        }

        /// <summary>
        ///     Handles the default heartbeat responses.
        /// </summary>
        /// <param name="returns">The payload of the <see cref="ResponseEnvelope" />.</param>
        private void HandleDefaultResponses(RepeatedField<ByteString> returns)
        {
            var responseCount = 0;
            foreach (var bytes in returns)
            {
                switch (responseCount)
                {
                    case 0: // Get_Hatched_Eggs
                        var hatchedEggs = GetHatchedEggsResponse.Parser.ParseFrom(bytes);
                        if (hatchedEggs.Success)
                        {
                            // TODO: Throw event, wrap in an object.
                        }
                        break;

                    case 1: // Get_Inventory
                        var inventory = GetInventoryResponse.Parser.ParseFrom(bytes);
                        if (inventory.Success)
                        {
                            if (inventory.InventoryDelta.NewTimestampMs >=
                                _session.Player.Inventory.LastInventoryTimestampMs)
                            {
                                _session.Player.Inventory.LastInventoryTimestampMs =
                                    inventory.InventoryDelta.NewTimestampMs;
                                if (inventory.InventoryDelta != null &&
                                    inventory.InventoryDelta.InventoryItems.Count > 0)
                                {
                                    _session.Player.Inventory.UpdateInventoryItems(inventory.InventoryDelta);
                                }
                            }
                        }
                        break;

                    case 2: // Check_Awarded_Badges
                        var awardedBadges = CheckAwardedBadgesResponse.Parser.ParseFrom(bytes);
                        if (awardedBadges.Success)
                        {
                            // TODO: Throw event, wrap in an object.
                        }
                        break;

                    case 3: // Download_Settings
                        var downloadSettings = DownloadSettingsResponse.Parser.ParseFrom(bytes);
                        if (string.IsNullOrEmpty(downloadSettings.Error))
                        {
                            if (downloadSettings.Settings == null)
                            {
                                continue;
                            }
                            if (_session.GlobalSettings == null || _session.GlobalSettingsHash != downloadSettings.Hash)
                            {
                                _session.GlobalSettingsHash = downloadSettings.Hash;
                                _session.GlobalSettings = downloadSettings.Settings;
                            }
                            else
                            {
                                _session.GlobalSettings = downloadSettings.Settings;
                            }
                        }
                        else
                        {
                            Log.Debug($"DownloadSettingsResponse.Error: '{downloadSettings.Error}'");
                        }
                        break;

                    default:
                        throw new Exception($"Unknown response appeared..? {responseCount}");
                }

                responseCount++;
            }
        }

        public POGOProtos.Networking.Envelopes.Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var sig = new POGOProtos.Networking.Signature();
            sig.TimestampSinceStart = (ulong)_internalWatch.ElapsedMilliseconds;
            sig.Timestamp = (ulong)TimeUtil.GetCurrentTimestampInMilliseconds();
            sig.SensorInfo = new POGOProtos.Networking.Signature.Types.SensorInfo()
            {
                AccelNormalizedZ = GenRandom(9.8),
                AccelNormalizedX = GenRandom(0.02),
                AccelNormalizedY = GenRandom(0.3),
                TimestampSnapshot = (ulong)_internalWatch.ElapsedMilliseconds - 230,
                MagnetometerX = GenRandom(012271042913198471),
                MagnetometerY = GenRandom(-0.015570580959320068),
                MagnetometerZ = GenRandom(0.010850906372070313),
                AngleNormalizedX = GenRandom(17.950439453125),
                AngleNormalizedY = GenRandom(-23.36273193359375),
                AngleNormalizedZ = GenRandom(-48.8250732421875),
                AccelRawX = GenRandom(-0.0120010357350111),
                AccelRawY = GenRandom(-0.04214850440621376),
                AccelRawZ = GenRandom(0.94571763277053833),
                GyroscopeRawX = GenRandom(7.62939453125e-005),
                GyroscopeRawY = GenRandom(-0.00054931640625),
                GyroscopeRawZ = GenRandom(0.0024566650390625),
                AccelerometerAxes = 3
            };
            sig.DeviceInfo = new POGOProtos.Networking.Signature.Types.DeviceInfo()
            {
                DeviceId = _deviceSettings.DeviceId,
                AndroidBoardName = _deviceSettings.AndroidBoardName,
                AndroidBootloader = _deviceSettings.AndroidBootloader,
                DeviceBrand = _deviceSettings.DeviceBrand,
                DeviceModel = _deviceSettings.DeviceModel,
                DeviceModelIdentifier = _deviceSettings.DeviceModelIdentifier,
                DeviceModelBoot = _deviceSettings.DeviceModelBoot,
                HardwareManufacturer = _deviceSettings.HardwareManufacturer,
                HardwareModel = _deviceSettings.HardwareModel,
                FirmwareBrand = _deviceSettings.FirmwareBrand,
                FirmwareTags = _deviceSettings.FirmwareTags,
                FirmwareType = _deviceSettings.FirmwareType,
                FirmwareFingerprint = _deviceSettings.FirmwareFingerprint
            };
            sig.LocationFix.Add(new POGOProtos.Networking.Signature.Types.LocationFix()
            {
                Provider = "network",

                //Unk4 = 120,
                Latitude = (float)_session.Player.Coordinate.Latitude,
                Longitude = (float)_session.Player.Coordinate.Longitude,
                Altitude = (float)_session.Player.Coordinate.Altitude,
                TimestampSinceStart = (ulong)_internalWatch.ElapsedMilliseconds - 200,
                Floor = 3,
                LocationType = 1
            });

            //Compute 10
            var x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_session.AccessToken.AuthTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(32, firstHash);
            var locationBytes = BitConverter.GetBytes(_session.Player.Coordinate.Latitude).Reverse()
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Longitude).Reverse())
                .Concat(BitConverter.GetBytes(_session.Player.Coordinate.Altitude).Reverse()).ToArray();
            sig.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 20
            x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            sig.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 24
            x = new System.Data.HashFunction.xxHash(64, 0x1B845238);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_session.AccessToken.AuthTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(64, seed);
            foreach (var req in requests)
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));

            //static for now
            sig.Unk22 = ByteString.CopyFrom(new byte[16] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F });

            Unknown6 val = new Unknown6();
            val.RequestType = 6;
            val.Unknown2 = new Unknown6.Types.Unknown2();
            val.Unknown2.Unknown1 = ByteString.CopyFrom(Encrypt(sig.ToByteArray()));
            return val;
        }

        private byte[] Encrypt(byte[] bytes)
        {
            var outputLength = 32 + bytes.Length + (256 - (bytes.Length % 256));
            var ptr = Marshal.AllocHGlobal(outputLength);
            var ptrOutput = Marshal.AllocHGlobal(outputLength);
            FillMemory(ptr, (uint)outputLength, 0);
            FillMemory(ptrOutput, (uint)outputLength, 0);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            try
            {
                int outputSize = outputLength;
                EncryptNative(ptr, bytes.Length, new byte[32], 32, ptrOutput, out outputSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var output = new byte[outputLength];
            Marshal.Copy(ptrOutput, output, 0, outputLength);
            return output;
        }

        [DllImport("binaries/encrypt.dll", EntryPoint = "encrypt", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        static extern private void EncryptNative(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        private static extern void FillMemory(IntPtr destination, uint length, byte fill);

        public static double GenRandom(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = (num * (1 - randomFactor));
            var randomMax = (num * (1 + randomFactor));
            var randomizedDelay = new Random().NextDouble() * (randomMax - randomMin) + randomMin; ;
            return randomizedDelay; ;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }
    }
}
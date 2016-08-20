using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using GeoCoordinatePortable;
using Google.Protobuf;
using Google.Protobuf.Collections;
using POGOLib.Logging;
using POGOLib.Util;
using POGOProtos.Enums;
using POGOProtos.Map;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace POGOLib.Net
{
    public class RpcClient : IDisposable
    {

        /// <summary>
        ///     The <see cref="HttpClient" /> used for communication with PokémonGo.
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        /// <summary>
        ///     The class responsible for all encryption / signing regarding <see cref="RpcClient"/>.
        /// </summary>
        private readonly RpcEncryption _rpcEncryption;

        /// <summary>
        ///     The current 'unique' request id we are at.
        /// </summary>
        private ulong _requestId;

        /// <summary>
        ///     The rpc url we have to call.
        /// </summary>
        private string _requestUrl;

        internal RpcClient(Session session)
        {
            _session = session;
            _rpcEncryption = new RpcEncryption(session);

            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(_session.Device.UserAgent);
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            _requestId = (ulong) new Random().Next(100000000, 999999999);
        }

        internal DateTime LastRpcRequest { get; private set; }

        internal DateTime LastRpcMapObjectsRequest { get; private set; }

        internal GeoCoordinate LastGeoCoordinateMapObjectsRequest { get; private set; } = new GeoCoordinate();

        /// <summary>
        ///     Sends all requests which the (android-)client sends on startup
        /// </summary>
        internal async Task<bool> Startup()
        {
            try
            {
                // Send GetPlayer to check if we're connected and authenticated
                GetPlayerResponse playerResponse;
                do
                {
                    var response = await SendRemoteProcedureCall(new Request
                    {
                        RequestType = RequestType.GetPlayer
                    });
                    playerResponse = GetPlayerResponse.Parser.ParseFrom(response);
                    if (!playerResponse.Success)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1000));
                    }
                } while (!playerResponse.Success);

				_session.Player.Data = playerResponse.PlayerData;

            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public async Task<GetAssetDigestResponse> GetAssets()
        {
            // check if template cache has been set

            // Get DownloadRemoteConfig
            var remoteConfigResponse = await SendRemoteProcedureCall(new Request
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

            // TODO: the timestamp comparisons seem to be used for determining if the stored data is invalid and needs refreshed,
            //       however, looking at this code I'm not sure it's implemented correctly - or if these refactors still match the behavior of
            //       the previous code... same concern with the next method GetItemTemplates()..

            var cachedMsg = await _session.DataCache.GetCachedAssetDigest();
            if (cachedMsg != null && remoteConfigParsed.AssetDigestTimestampMs <= timestamp)
            {
                return cachedMsg;
            }
            else
            {
                // GetAssetDigest
                var assetDigestResponse = await SendRemoteProcedureCall(new Request
                {
                    RequestType = RequestType.GetAssetDigest,
                    RequestMessage = new GetAssetDigestMessage
                    {
                        Platform = Platform.Android,
                        AppVersion = 2903
                    }.ToByteString()
                });
                var msg = GetAssetDigestResponse.Parser.ParseFrom(assetDigestResponse);
                _session.DataCache.SaveData(DataCacheExtensions.AssetDigestFile, msg);
                return msg;
            }
        }
        
        public async Task<DownloadItemTemplatesResponse> GetItemTemplates()
        {
            // Get DownloadRemoteConfig
            var remoteConfigResponse = await SendRemoteProcedureCall(new Request
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

            var cachedMsg = await _session.DataCache.GetCachedItemTemplates();
            if (cachedMsg != null && remoteConfigParsed.AssetDigestTimestampMs <= timestamp)
            {
                return cachedMsg;
            }
            else
            {
                // GetAssetDigest
                var itemTemplateResponse = await SendRemoteProcedureCall(new Request
                {
                    RequestType = RequestType.DownloadItemTemplates
                });
                var msg = DownloadItemTemplatesResponse.Parser.ParseFrom(itemTemplateResponse);
                _session.DataCache.SaveData(DataCacheExtensions.ItemTemplatesFile, msg);
                return msg;
            }
        }

        /// <summary>
        ///     It is not recommended to call this. Map objects will update automatically and fire the map update event.
        /// </summary>
        public async Task RefreshMapObjects()
        {
            var cellIds = MapUtil.GetCellIdsForLatLong(_session.Player.Coordinate.Latitude,
                _session.Player.Coordinate.Longitude);
            var sinceTimeMs = new List<long>(cellIds.Length);

            for (var i = 0; i < cellIds.Length; i++)
            {
                sinceTimeMs.Add(0);
            }

            var response = await SendRemoteProcedureCall(new Request
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
                Logger.Debug($"Received '{mapObjects.MapCells.Count}' map cells.");
                Logger.Debug($"Received '{mapObjects.MapCells.SelectMany(c => c.CatchablePokemons).Count()}' pokemons.");
                Logger.Debug($"Received '{mapObjects.MapCells.SelectMany(c => c.Forts).Count()}' forts.");
                if (mapObjects.MapCells.Count == 0)
                {
                    Logger.Error("We received 0 map cells, are your GPS coordinates correct?");
                    return;
                }
                _session.Map.Cells = mapObjects.MapCells;
            }
            else
            {
                Logger.Error($"GetMapObjects status is: '{mapObjects.Status}'.");
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
        private async Task<RequestEnvelope> GetRequestEnvelope(Request request)
        {
            var requestEnvelope = new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = GetNextRequestId(),
                Latitude = _session.Player.Coordinate.Latitude,
                Longitude = _session.Player.Coordinate.Longitude,
                Altitude = _session.Player.Coordinate.Altitude,
                Unknown12 = 123, // TODO: Figure this out.
                Requests = {GetDefaultRequests()}
            };
            requestEnvelope.Requests.Insert(0, request);

            if (_session.AccessToken.AuthTicket == null || _session.AccessToken.IsExpired)
            {
                if (_session.AccessToken.IsExpired)
                {
                    await _session.Reauthenticate();
                }

                requestEnvelope.AuthInfo = new RequestEnvelope.Types.AuthInfo
                {
                    Provider = _session.AccessToken.ProviderID,
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
            }

            requestEnvelope.Unknown6 = _rpcEncryption.GenerateSignature(requestEnvelope);

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

        public async Task<ByteString> SendRemoteProcedureCall(RequestType requestType)
        {
            return await SendRemoteProcedureCall(new Request
            {
                RequestType = requestType
            });
        }

		public Action<Request> OnStartRPC;
		public Action<Request> OnEndRPC;

		private static long lastRpc = 0;
		private const int minDiff = 800;
		private ConcurrentQueue<Request> queue = new ConcurrentQueue<Request>();
		private ConcurrentDictionary<Request, ByteString> dict = new ConcurrentDictionary<Request, ByteString>();
		private static Mutex m = new Mutex();
		public async Task<ByteString> SendRemoteProcedureCall(Request request)
		{
			queue.Enqueue(request);
			m.WaitOne();
			if (OnStartRPC != null)
			{
				OnStartRPC(request);
			}
			Request r;
			while (queue.TryDequeue(out r))
			{
				var diff = Math.Max(0,DateTime.Now.Millisecond - lastRpc);
				if (diff < minDiff)
				{
					var delay = (minDiff - diff) + (int)(new Random().NextDouble() * 0); // Add some randomness
					await Task.Delay((int)(delay));
				}
				lastRpc = DateTime.Now.Millisecond;
				var response = await PerformRemoteProcedureCall(r);
				dict.GetOrAdd(r, response);
			}
			if (OnEndRPC != null)
			{
				OnEndRPC(request);
			}
			m.ReleaseMutex();
			ByteString ret;
			dict.TryRemove(request, out ret);
			return ret;
		}

		private async Task<ByteString> PerformRemoteProcedureCall(Request request)
		{
			var requestEnvelope = await GetRequestEnvelope(request);

            using (var requestData = PrepareRequestEnvelope(requestEnvelope))
            {
                using (var response = await _httpClient.PostAsync(_requestUrl ?? Constants.ApiUrl, requestData))
                {
					if (!response.IsSuccessStatusCode)
                    {
                        Logger.Debug(await response.Content.ReadAsStringAsync());
                        throw new Exception(
                            "Received a non-success HTTP status code from the RPC server, see the console for the response.");
                    }

                    var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                    var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);

					switch (responseEnvelope.StatusCode)
                    {
                        case 1:
                            // Success!?
                            break;

                        case 52: // Slow servers? TODO: Throttling (?)
                            Logger.Warn(
                                $"We are sending requests too fast, sleeping for {Configuration.SlowServerTimeout} milliseconds.");
                            await Task.Delay(TimeSpan.FromMilliseconds(Configuration.SlowServerTimeout));
                            return await SendRemoteProcedureCall(request);

                        case 53: // New RPC url
                            if (Regex.IsMatch(responseEnvelope.ApiUrl, "pgorelease\\.nianticlabs\\.com\\/plfe\\/\\d+"))
                            {
                                _requestUrl = $"https://{responseEnvelope.ApiUrl}/rpc";
                                return await SendRemoteProcedureCall(request);
                            }
                            throw new Exception(
                                $"Received an incorrect API url: '{responseEnvelope.ApiUrl}', status code was: '{responseEnvelope.StatusCode}'.");

                        case 102: // Invalid auth
                            Logger.Debug("Received StatusCode 102, reauthenticating.");
                            _session.AccessToken.Expire();
                            await _session.Reauthenticate();
                            return await SendRemoteProcedureCall(request);

                        default:
                            Logger.Info($"Unknown status code: {responseEnvelope.StatusCode}");
                            break;
                    }

                    LastRpcRequest = DateTime.UtcNow;
                    Logger.Debug($"Sent RPC Request: '{request.RequestType}'");
                    if (request.RequestType == RequestType.GetMapObjects)
                    {
                        LastRpcMapObjectsRequest = LastRpcRequest;
                        LastGeoCoordinateMapObjectsRequest = _session.Player.Coordinate;
                    }
                    if (responseEnvelope.AuthTicket != null)
                    {
                        _session.AccessToken.AuthTicket = responseEnvelope.AuthTicket;
                        Logger.Debug("Received a new AuthTicket from Pokémon!");
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
                            Logger.Debug($"DownloadSettingsResponse.Error: '{downloadSettings.Error}'");
                        }
                        break;

                    default:
                        throw new Exception($"Unknown response appeared..? {responseCount}");
                }

                responseCount++;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
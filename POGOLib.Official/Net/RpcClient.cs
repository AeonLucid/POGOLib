using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using Google.Protobuf;
using Newtonsoft.Json;
using POGOLib.Official.Logging;
using POGOLib.Official.Util;
using POGOProtos.Map;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using POGOProtos.Enums;
using POGOProtos.Networking.Platform;
using POGOProtos.Networking.Platform.Requests;
using POGOProtos.Networking.Platform.Responses;

namespace POGOLib.Official.Net
{
    public class RpcClient : IDisposable
    {

        /// <summary>
        ///     The authenticated <see cref="Session" />.
        /// </summary>
        private readonly Session _session;

        /// <summary>
        ///     The class responsible for all encryption / signing regarding <see cref="RpcClient"/>.
        /// </summary>
        private readonly RpcEncryption _rpcEncryption;

        /// <summary>
        ///     The current request count we are at.
        /// </summary>
        private ulong _requestCount;

        /// <summary>
        ///     The rpc url we have to call.
        /// </summary>
        private string _requestUrl;

        private string _mapKey;

        private readonly List<RequestType> _defaultRequests = new List<RequestType>
        {
            RequestType.CheckChallenge,
            RequestType.GetHatchedEggs,
            RequestType.GetInventory,
            RequestType.CheckAwardedBadges,
            RequestType.DownloadSettings
        };

        private readonly ConcurrentQueue<RequestEnvelope> _rpcQueue = new ConcurrentQueue<RequestEnvelope>();

        private readonly ConcurrentDictionary<RequestEnvelope, ByteString> _rpcResponses = new ConcurrentDictionary<RequestEnvelope, ByteString>();

        private static readonly Semaphore RpcQueueMutex = new Semaphore(1, 1);

        internal RpcClient(Session session)
        {
            _session = session;
            _rpcEncryption = new RpcEncryption(session);
            _mapKey = string.Empty;
        }

        internal DateTime LastRpcRequest { get; private set; }

        internal DateTime LastRpcMapObjectsRequest { get; private set; }

        internal GeoCoordinate LastGeoCoordinateMapObjectsRequest { get; private set; } = new GeoCoordinate();
        
        internal Platform GetPlatform()
        {
            return _session.DeviceInfo.DeviceBrand == "Apple" ? Platform.Ios : Platform.Android;
        }

        private long PositiveRandom()
        {
            long ret = _session.Random.Next() | (_session.Random.Next() << 32);
            // lrand48 ensures it's never < 0
            // So do the same
            if (ret < 0)
                ret = -ret;
            return ret;
        }

        private void IncrementRequestCount()
        {
            // Request counts on android jump more than 1 at a time according to logs
            // They are fully sequential on iOS though
            // So mimic that same behavior here.
            switch (GetPlatform())
            {
                case Platform.Android:
                    _requestCount += (uint)_session.Random.Next(2, 15);
                    break;
                case Platform.Ios:
                    _requestCount++;
                    break;
            }
        }
        
        /// <summary>
        /// Sends all requests which the (ios-)client sends on startup
        /// </summary>
        internal async Task<bool> StartupAsync()
        {
            // Send GetPlayer to check if we're connected and authenticated
            GetPlayerResponse playerResponse;
            do
            {
                var response = await SendRemoteProcedureCallAsync(new[]
                {
                    new Request
                    {
                        RequestType = RequestType.GetPlayer
                    },
                    new Request
                    {
                        RequestType = RequestType.CheckChallenge,
                        RequestMessage = new CheckChallengeMessage
                        {
                            DebugRequest = false
                        }.ToByteString()
                    }
                });
                playerResponse = GetPlayerResponse.Parser.ParseFrom(response);
                if (!playerResponse.Success)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                }
            } while (!playerResponse.Success);

            _session.Player.Data = playerResponse.PlayerData;

            return true;
        }

        // TODO: Reimplement
        #region Unused assets code
        //        public async Task<GetAssetDigestResponse> GetAssetsAsync()
        //        {
        //            // check if template cache has been set
        //
        //            // Get DownloadRemoteConfig
        //            var remoteConfigResponse = await SendRemoteProcedureCallAsync(new Request
        //            {
        //                RequestType = RequestType.DownloadRemoteConfigVersion,
        //                RequestMessage = new DownloadRemoteConfigVersionMessage
        //                {
        //                    Platform = Platform.Android,
        //                    AppVersion = 2903
        //                }.ToByteString()
        //            });
        //
        //            var remoteConfigParsed = DownloadRemoteConfigVersionResponse.Parser.ParseFrom(remoteConfigResponse);
        //            var timestamp = (ulong) TimeUtil.GetCurrentTimestampInMilliseconds();
        //
        //            // TODO: the timestamp comparisons seem to be used for determining if the stored data is invalid and needs refreshed,
        //            //       however, looking at this code I'm not sure it's implemented correctly - or if these refactors still match the behavior of
        //            //       the previous code... same concern with the next method GetItemTemplates()..
        //
        //            var cachedMsg = _session.DataCache.GetCachedAssetDigest();
        //            if (cachedMsg != null && remoteConfigParsed.AssetDigestTimestampMs <= timestamp)
        //            {
        //                return cachedMsg;
        //            }
        //            else
        //            {
        //                // GetAssetDigest
        //                var assetDigestResponse = await SendRemoteProcedureCallAsync(new Request
        //                {
        //                    RequestType = RequestType.GetAssetDigest,
        //                    RequestMessage = new GetAssetDigestMessage
        //                    {
        //                        Platform = Platform.Android,
        //                        AppVersion = 2903
        //                    }.ToByteString()
        //                });
        //                var msg = GetAssetDigestResponse.Parser.ParseFrom(assetDigestResponse);
        //                _session.DataCache.SaveData(DataCacheExtensions.AssetDigestFile, msg);
        //                return msg;
        //            }
        //        }
        //
        //        public async Task<DownloadItemTemplatesResponse> GetItemTemplatesAsync()
        //        {
        //            // Get DownloadRemoteConfig
        //            var remoteConfigResponse = await SendRemoteProcedureCallAsync(new Request
        //            {
        //                RequestType = RequestType.DownloadRemoteConfigVersion,
        //                RequestMessage = new DownloadRemoteConfigVersionMessage
        //                {
        //                    Platform = Platform.Android,
        //                    AppVersion = 2903
        //                }.ToByteString()
        //            });
        //
        //            var remoteConfigParsed = DownloadRemoteConfigVersionResponse.Parser.ParseFrom(remoteConfigResponse);
        //            var timestamp = (ulong) TimeUtil.GetCurrentTimestampInMilliseconds();
        //
        //            var cachedMsg = _session.DataCache.GetCachedItemTemplates();
        //            if (cachedMsg != null && remoteConfigParsed.AssetDigestTimestampMs <= timestamp)
        //            {
        //                return cachedMsg;
        //            }
        //            else
        //            {
        //                // GetAssetDigest
        //                var itemTemplateResponse = await SendRemoteProcedureCallAsync(new Request
        //                {
        //                    RequestType = RequestType.DownloadItemTemplates
        //                });
        //                var msg = DownloadItemTemplatesResponse.Parser.ParseFrom(itemTemplateResponse);
        //                _session.DataCache.SaveData(DataCacheExtensions.ItemTemplatesFile, msg);
        //                return msg;
        //            }
        //        }
        #endregion

        /// <summary>
        ///     It is not recommended to call this. Map objects will update automatically and fire the map update event.
        /// </summary>
        public async Task RefreshMapObjectsAsync()
        {
            var cellIds = MapUtil.GetCellIdsForLatLong(_session.Player.Coordinate.Latitude, _session.Player.Coordinate.Longitude);
            var sinceTimeMs = new List<long>(cellIds.Length);

            foreach (var cellId in cellIds)
            {
                var cell = _session.Map.Cells.FirstOrDefault(x => x.S2CellId == cellId);

                sinceTimeMs.Add(cell?.CurrentTimestampMs ?? 0);
            }

            var response = await SendRemoteProcedureCallAsync(new Request
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
                // TODO: Cleaner?
                var pokemonCatchable = mapObjects.MapCells.SelectMany(c => c.CatchablePokemons).Count();
                var pokemonWild = mapObjects.MapCells.SelectMany(c => c.WildPokemons).Count();
                var pokemonNearby = mapObjects.MapCells.SelectMany(c => c.NearbyPokemons).Count();
                var pokemonCount = pokemonCatchable + pokemonWild + pokemonNearby;

                Logger.Debug($"Received '{mapObjects.MapCells.Count}' map cells.");
                Logger.Debug($"Received '{pokemonCount}' pokemons. Catchable({pokemonCatchable}) Wild({pokemonWild}) Nearby({pokemonNearby})");
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
        ///     Gets the next request id for the <see cref="RequestEnvelope" />.
        /// </summary>
        /// <returns></returns>
        private ulong GetNextRequestId()
        {
            if (_requestCount == 1)
            {
                IncrementRequestCount();

                switch (GetPlatform())
                {
                    case Platform.Android:
                        // lrand48 is "broken" in that the first run of it will return a static value.
                        // So the first time we send a request, we need to match that initial value. 
                        // Note: On android srand(4) is called in .init_array which seeds the initial value.
                        return 0x53B77E48000000B0;
                    case Platform.Ios:
                        // Same as lrand48, iOS uses "rand()" without a pre-seed which always gives the same first value.
                        return 0x41A700000002;
                }
            }

            // Note that the API expects a "positive" random value here. (At least on Android it does due to lrand48 implementation details)
            // So we'll just use the same for iOS since it doesn't hurt, and means less code required.
            ulong r = (((ulong)PositiveRandom() | ((_requestCount + 1) >> 31)) << 32) | (_requestCount + 1);
            IncrementRequestCount();
            return r;
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
                    RequestType = RequestType.CheckChallenge,
                    RequestMessage = new CheckChallengeMessage
                    {
                        DebugRequest = false
                    }.ToByteString()
                },
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
        ///     Gets a <see cref="RequestEnvelope" /> with authentication data.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="addDefaultRequests"></param>
        /// <returns></returns>
        public async Task<RequestEnvelope> GetRequestEnvelopeAsync(IEnumerable<Request> request, bool addDefaultRequests)
        {
            var requestEnvelope = new RequestEnvelope
            {
                StatusCode = 2,
                RequestId = GetNextRequestId(),
                Latitude = _session.Player.Coordinate.Latitude,
                Longitude = _session.Player.Coordinate.Longitude
            };

            requestEnvelope.Requests.AddRange(request);

            if (addDefaultRequests)
                requestEnvelope.Requests.AddRange(GetDefaultRequests());

            if (_session.AccessToken.AuthTicket != null && _session.AccessToken.AuthTicket.ExpireTimestampMs < ((ulong)TimeUtil.GetCurrentTimestampInMilliseconds() - (60000 * 2)))
            {
                // Check for almost expired AuthTicket (2 minute buffer). Null out the AuthTicket so that AccessToken is used.
                _session.AccessToken.AuthTicket = null;
            }

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

            requestEnvelope.PlatformRequests.Add(await _rpcEncryption.GenerateSignatureAsync(requestEnvelope));

            if (requestEnvelope.Requests.Count > 0 && (
                    requestEnvelope.Requests[0].RequestType == RequestType.GetMapObjects ||
                    requestEnvelope.Requests[0].RequestType == RequestType.GetPlayer))
            {
                requestEnvelope.PlatformRequests.Add(new RequestEnvelope.Types.PlatformRequest
                {
                    Type = PlatformRequestType.UnknownPtr8,
                    RequestMessage = new UnknownPtr8Request
                    {
                        Message = _mapKey
                    }.ToByteString()
                });
            }

            return requestEnvelope;
        }

        public async Task<ByteString> SendRemoteProcedureCallAsync(RequestType requestType)
        {
            return await SendRemoteProcedureCallAsync(new Request
            {
                RequestType = requestType
            });
        }

        public async Task<ByteString> SendRemoteProcedureCallAsync(Request request)
        {
            return await SendRemoteProcedureCall(await GetRequestEnvelopeAsync(new[] {request}, true));
        }

        public async Task<ByteString> SendRemoteProcedureCallAsync(Request[] request)
        {
            return await SendRemoteProcedureCall(await GetRequestEnvelopeAsync(request, false));
        }

        private Task<ByteString> SendRemoteProcedureCall(RequestEnvelope requestEnvelope)
        {
            return Task.Run(async () =>
            {
                _rpcQueue.Enqueue(requestEnvelope);

                try
                {
                    RpcQueueMutex.WaitOne();

                    RequestEnvelope processRequestEnvelope;
                    while (_rpcQueue.TryDequeue(out processRequestEnvelope))
                    {
                        var diff = Math.Max(0, DateTime.Now.Millisecond - LastRpcRequest.Millisecond);
                        if (diff < Configuration.ThrottleDifference)
                        {
                            var delay = Configuration.ThrottleDifference - diff + (int)(_session.Random.NextDouble() * 0);

                            await Task.Delay(delay);
                        }

                        _rpcResponses.GetOrAdd(processRequestEnvelope, await PerformRemoteProcedureCallAsync(processRequestEnvelope));
                    }

                    ByteString ret;
                    _rpcResponses.TryRemove(requestEnvelope, out ret);
                    return ret;
                }
                finally
                {
                    RpcQueueMutex.Release();
                }
            });
        }

        private async Task<ByteString> PerformRemoteProcedureCallAsync(RequestEnvelope requestEnvelope)
        {
            try
            {
                using (var requestData = new ByteArrayContent(requestEnvelope.ToByteArray()))
                {
                    Logger.Debug($"Sending RPC Request: '{string.Join(", ", requestEnvelope.Requests.Select(x => x.RequestType))}'");
                    Logger.Debug($"=> Platform Request: '{string.Join(", ", requestEnvelope.PlatformRequests.Select(x => x.Type))}'");

                    using (var response = await _session.HttpClient.PostAsync(_requestUrl ?? Constants.ApiUrl, requestData))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Logger.Debug(await response.Content.ReadAsStringAsync());

                            throw new Exception("Received a non-success HTTP status code from the RPC server, see the console for the response.");
                        }

                        var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                        var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);

                        switch (responseEnvelope.StatusCode)
                        {
                            // Valid response.
                            case ResponseEnvelope.Types.StatusCode.Ok: 
                                // Success!?
                                break;
                        
                            // Valid response and new rpc url.
                            case ResponseEnvelope.Types.StatusCode.OkRpcUrlInResponse:
                                if (Regex.IsMatch(responseEnvelope.ApiUrl, "pgorelease\\.nianticlabs\\.com\\/plfe\\/\\d+"))
                                {
                                    _requestUrl = $"https://{responseEnvelope.ApiUrl}/rpc";
                                }
                                else
                                {
                                    throw new Exception($"Received an incorrect API url: '{responseEnvelope.ApiUrl}', status code was: '{responseEnvelope.StatusCode}'.");
                                }
                                break;

                            // A new rpc endpoint is available.
                            case ResponseEnvelope.Types.StatusCode.Redirect: 
                                if (Regex.IsMatch(responseEnvelope.ApiUrl, "pgorelease\\.nianticlabs\\.com\\/plfe\\/\\d+"))
                                {
                                    _requestUrl = $"https://{responseEnvelope.ApiUrl}/rpc";

                                    return await PerformRemoteProcedureCallAsync(requestEnvelope);
                                }
                                throw new Exception($"Received an incorrect API url: '{responseEnvelope.ApiUrl}', status code was: '{responseEnvelope.StatusCode}'.");

                            // The login token is invalid.
                            // TODO: Make cleaner to reduce duplicate code with the GetRequestEnvelopeAsync method.
                            case ResponseEnvelope.Types.StatusCode.InvalidAuthToken:
                                Logger.Debug("Received StatusCode 102, reauthenticating.");

                                _session.AccessToken.Expire();
                                await _session.Reauthenticate();

                                // Apply new token.
                                requestEnvelope.AuthInfo = new RequestEnvelope.Types.AuthInfo
                                {
                                    Provider = _session.AccessToken.ProviderID,
                                    Token = new RequestEnvelope.Types.AuthInfo.Types.JWT
                                    {
                                        Contents = _session.AccessToken.Token,
                                        Unknown2 = 59
                                    }
                                };

                                // Re-sign envelope.
                                var signature = requestEnvelope.PlatformRequests.FirstOrDefault(x => x.Type == PlatformRequestType.SendEncryptedSignature);
                                if (signature != null)
                                {
                                    requestEnvelope.PlatformRequests.Remove(signature);
                                }

                                requestEnvelope.PlatformRequests.Insert(0, await _rpcEncryption.GenerateSignatureAsync(requestEnvelope));

                                // Re-send envelope.
                                return await PerformRemoteProcedureCallAsync(requestEnvelope);

                            default:
                                Logger.Info($"Unknown status code: {responseEnvelope.StatusCode}");
                                break;
                        }

                        LastRpcRequest = DateTime.UtcNow;

                        if (requestEnvelope.Requests[0].RequestType == RequestType.GetMapObjects)
                        {
                            LastRpcMapObjectsRequest = LastRpcRequest;
                            LastGeoCoordinateMapObjectsRequest = _session.Player.Coordinate;
                        }

                        if (responseEnvelope.AuthTicket != null)
                        {
                            _session.AccessToken.AuthTicket = responseEnvelope.AuthTicket;
                            Logger.Debug("Received a new AuthTicket from Pokemon!");
                        }

                        var mapPlatform = responseEnvelope.PlatformReturns.FirstOrDefault(x => x.Type == PlatformRequestType.UnknownPtr8);
                        if (mapPlatform != null)
                        {
                            var unknownPtr8Response = UnknownPtr8Response.Parser.ParseFrom(mapPlatform.Response);
                            _mapKey = unknownPtr8Response.Message;
                        }
                        
                        return HandleResponseEnvelope(requestEnvelope, responseEnvelope);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"SendRemoteProcedureCall exception: {e}");
                return null;
            }
        }

        /// <summary>
        /// Responsible for handling the received <see cref="ResponseEnvelope" />.
        /// </summary>
        /// <param name="requestEnvelope">The <see cref="RequestEnvelope"/> prepared by <see cref="PerformRemoteProcedureCallAsync"/>.</param>
        /// <param name="responseEnvelope">The <see cref="ResponseEnvelope"/> received from <see cref="SendRemoteProcedureCallAsync(POGOProtos.Networking.Requests.Request)" />.</param>
        /// <returns>Returns the <see cref="ByteString" />response of the <see cref="Request"/>.</returns>
        private ByteString HandleResponseEnvelope(RequestEnvelope requestEnvelope, ResponseEnvelope responseEnvelope)
        {
            if (responseEnvelope.Returns.Count == 0)
            {
                throw new Exception("There were 0 responses.");
            }

            // Take requested response and remove from returns.
            var requestResponse = responseEnvelope.Returns[0];

            // Handle the default responses.
            HandleDefaultResponses(requestEnvelope, responseEnvelope.Returns);

            // Handle responses which affect the inventory
            HandleInventoryResponses(requestEnvelope.Requests[0], requestResponse);

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
        /// <param name="requestEnvelope"></param>
        /// <param name="returns">The payload of the <see cref="ResponseEnvelope" />.</param>
        private void HandleDefaultResponses(RequestEnvelope requestEnvelope, IList<ByteString> returns)
        {
            var responseIndexes = new Dictionary<int, RequestType>();
            
            for (var i = 0; i < requestEnvelope.Requests.Count; i++)
            {
                var request = requestEnvelope.Requests[i];
                if (_defaultRequests.Contains(request.RequestType))
                    responseIndexes.Add(i, request.RequestType);
            }

            foreach (var responseIndex in responseIndexes)
            {
                var bytes = returns[responseIndex.Key];

                switch (responseIndex.Value)
                {
                    case RequestType.GetHatchedEggs: // Get_Hatched_Eggs
                        var hatchedEggs = GetHatchedEggsResponse.Parser.ParseFrom(bytes);
                        if (hatchedEggs.Success)
                        {
                            // TODO: Throw event, wrap in an object.
                        }
                        break;

                    case RequestType.GetInventory: // Get_Inventory
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

                    case RequestType.CheckAwardedBadges: // Check_Awarded_Badges
                        var awardedBadges = CheckAwardedBadgesResponse.Parser.ParseFrom(bytes);
                        if (awardedBadges.Success)
                        {
                            // TODO: Throw event, wrap in an object.
                        }
                        break;

                    case RequestType.DownloadSettings: // Download_Settings
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

                    // TODO: Let the developer know about this somehow.
                    case RequestType.CheckChallenge:
                        var checkChallenge = CheckChallengeResponse.Parser.ParseFrom(bytes);
                        if (checkChallenge.ShowChallenge)
                        {
                            Logger.Warn($"Received Captcha on {_session.AccessToken.Username}");
                            Logger.Warn(JsonConvert.SerializeObject(checkChallenge, Formatting.Indented));
                        }
                        break;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }
    }
}
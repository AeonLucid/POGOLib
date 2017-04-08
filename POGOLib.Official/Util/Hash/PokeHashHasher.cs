using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using POGOLib.Official.Logging;
using POGOLib.Official.Util.Encryption.PokeHash;
using POGOLib.Official.Util.Hash.PokeHash;
using POGOProtos.Networking.Envelopes;

namespace POGOLib.Official.Util.Hash
{
    /// <summary>
    ///     This is the <see cref="IHasher"/> which uses the API
    ///     provided by https://www.pokefarmer.com/. If you want
    ///     to buy an API key, go to this url.
    ///     https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer
    /// 
    ///     Android version: 0.61.0
    ///     IOS version: 1.31.0
    /// </summary>
    public class PokeHashHasher : IHasher
    {
        public static string PokeHashUrl = "https://pokehash.buddyauth.com/";

        /* Obsoleted APIs */
        public static Tuple <string,string,long>  v57_4  = new Tuple<string, string, long>(
            "api/v127_4/hash","0.57.4", -816976800928766045
           );
        /* *** */

        public static Tuple <string,string,long>  v59_1   = new Tuple<string, string, long>(
            "api/v129_1/hash","0.59.1", -3226782243204485589
           );
        public static Tuple <string,string,long>  v61_0   = new Tuple<string, string, long>(
            "api/v131_0/hash","0.61.0", 0x11fdf018c941ef22
           );

        public static string PokeHashEndpoint = v61_0.Item1;

        public Version PokemonVersion { get; set;} = new Version (v61_0.Item2);

        public long Unknown25 { get; set;} = v61_0.Item3;

        private readonly List<PokeHashAuthKey> _authKeys;

        private readonly HttpClient _httpClient;

        private readonly Semaphore _keySelection;
        
        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey) : this(new []{ authKey })
        {

        }

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKeys">The PokeHash authkeys obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string[] authKeys)
        {
            if (authKeys.Length == 0)
                throw new ArgumentException($"{nameof(authKeys)} may not be empty.");

            _authKeys = new List<PokeHashAuthKey>();
            
            // We don't want any duplicate keys.
            foreach (var authKey in authKeys)
            {
                var pokeHashAuthKey = new PokeHashAuthKey(authKey);
                if (_authKeys.Contains(pokeHashAuthKey))
                    throw new Exception($"The auth key '{authKey}' is a duplicate.");

                _authKeys.Add(pokeHashAuthKey);
            }

            // Initialize HttpClient.
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(PokeHashUrl)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("POGOLib (https://github.com/AeonLucid/POGOLib)");

            _keySelection = new Semaphore(1, 1);
        }


        public async Task<HashData> GetHashDataAsync(RequestEnvelope requestEnvelope, Signature signature, byte[] locationBytes, byte[][] requestsBytes, byte[] serializedTicket)
        {
            var requestData = new PokeHashRequest
            {
                Timestamp = signature.Timestamp,
                Latitude = requestEnvelope.Latitude,
                Longitude = requestEnvelope.Longitude,
                Altitude = requestEnvelope.Accuracy, // Accuracy actually is altitude
                AuthTicket = serializedTicket,
                SessionData = signature.SessionHash.ToByteArray(),
                Requests = new List<byte[]>(requestsBytes)
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            
            using (var response = await PerformRequest(requestContent))
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                string message;

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var responseData = JsonConvert.DeserializeObject<PokeHashResponse>(responseContent);

                        return new HashData
                        {
                            LocationAuthHash = responseData.LocationAuthHash,
                            LocationHash = responseData.LocationHash,
                            RequestHashes = responseData.RequestHashes
                                .Select(x => (ulong) x)
                                .ToArray()
                        };

                    case HttpStatusCode.BadRequest:
                        message = $"Bad request sent to the hashing server! {responseContent}";
                        break;
                    
                    case HttpStatusCode.Unauthorized:
                        message = "The auth key supplied for PokeHash was invalid.";
                        break;
                    
                    case (HttpStatusCode) 429:
                        message = $"Your request has been limited. {responseContent}";
                        break;

                    default:
                        message = $"We received an unknown HttpStatusCode ({response.StatusCode})..";
                        break;
                }

                // TODO: Find a better way to let the developer know of these issues.
                message = $"[PokeHash]: {message}";

                Logger.Error(message);
                throw new Exception(message);
            }
        }
        
        private Task<HttpResponseMessage> PerformRequest(HttpContent requestContent)
        {
            return Task.Run(async () =>
            {
                PokeHashAuthKey authKey;
                var extendedSelection = false;

                // Key selection
                try
                {
                    _keySelection.WaitOne();

//                    Logger.Warn(">>> Entering key selection.");
                    
                    var availableKeys = _authKeys.Where(x => x.Requests < x.MaxRequestCount).ToArray();
                    if (availableKeys.Length > 0)
                    {
                        authKey = availableKeys.First();
                        authKey.Requests += 1;

//                        Logger.Warn("Found available auth key.");

                        // If the auth key has not been initialize yet, we need to have control a bit longer
                        // to configure it properly.
                        extendedSelection |= !authKey.IsInitialized;
                    }
                    else
                    {
//                        Logger.Warn("No available auth keys found.");

                        authKey = _authKeys
                            .OrderBy(x => x.RatePeriodEnd)
                            .First();

                        var sleepTime = (int) Math.Ceiling(authKey.RatePeriodEnd.Subtract(DateTime.UtcNow).TotalMilliseconds);

                        if (sleepTime < 0) 
                            sleepTime = 1000;
                        if (sleepTime > 60000) 
                            sleepTime = 60000;

                        Logger.Debug("sleepTime: "+sleepTime );
                         try {
                            await Task.Delay(sleepTime);
                        } catch (Exception ex1) {
                            Logger.Debug(ex1.ToString());
                            await Task.Delay(60000);
                        }

                        // Rate limit is over, so reset requests.
                        authKey.Requests = 0;
                        // We have to receive the new rate period end.
                        extendedSelection = true;

//                        Logger.Warn("Key selection is done with sleeping.");
                    }
                }
                finally
                {
                    if (!extendedSelection)
                    {
//                        Logger.Warn("<<< Exiting key selection.");

                        _keySelection.Release();
                    }
                    else
                    {
//                        Logger.Warn("=== Holding key selection.");
                    }
                }
                
                requestContent.Headers.Add("X-AuthToken", authKey.AuthKey);

                var response = await _httpClient.PostAsync(PokeHashEndpoint, requestContent);

                // Handle response
                try
                {
                    // Parse headers
                    int maxRequestCount;
                    int rateRequestsRemaining;
                    int ratePeriodEndSeconds;

                    IEnumerable<string> maxRequestsValue;
                    IEnumerable<string> requestsRemainingValue;
                    IEnumerable<string> ratePeriodEndValue;
                    if (response.Headers.TryGetValues("X-MaxRequestCount", out  maxRequestsValue) &&
                        response.Headers.TryGetValues("X-RateRequestsRemaining", out  requestsRemainingValue) &&
                        response.Headers.TryGetValues("X-RatePeriodEnd", out  ratePeriodEndValue))
                    {
                        if (!int.TryParse(maxRequestsValue.First(), out maxRequestCount) ||
                            !int.TryParse(requestsRemainingValue.First(), out rateRequestsRemaining) ||
                            !int.TryParse(ratePeriodEndValue.FirstOrDefault(), out ratePeriodEndSeconds))
                        {
                            throw new Exception("Failed parsing pokehash response header values.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed parsing pokehash response headers.");
                    }

                    // Use parsed headers
                    if (!authKey.IsInitialized)
                    {
                        authKey.MaxRequestCount = maxRequestCount;
                        authKey.Requests = authKey.MaxRequestCount - rateRequestsRemaining;
                        authKey.IsInitialized = true;
                    }
                    
                    var ratePeriodEnd = TimeUtil.GetDateTimeFromSeconds(ratePeriodEndSeconds);
                    if (ratePeriodEnd > authKey.RatePeriodEnd)
                    {
//                        Logger.Warn($"[AuthKey: {authKey.AuthKey}] {authKey.RatePeriodEnd} increased to {ratePeriodEnd}.");

                        authKey.RatePeriodEnd = ratePeriodEnd;
                    }

                    return response;
                }
                finally
                {
                    if (extendedSelection)
                    {
//                        Logger.Warn("<<< Exiting extended key selection.");

                        _keySelection.Release();
                    }
                }
            });
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}

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
    ///     Android version: 0.87.5
    ///     IOS version: 1.57.5
    /// </summary>
    public class PokeHashHasher : IHasher
    {
        private readonly List<PokeHashAuthKey> _authKeys;
        private readonly List<PokeHashAuthHasher> _authHashers 
            = new List<PokeHashAuthHasher>{new PokeHashAuthHasher("PH", new Uri("http://hash.goman.io/"))};

        private readonly Semaphore _keySelection;

		public Version PokemonVersion { get; } = new Version("0.87.5");
        
		// NOTE: Warning: Sotmetimes, this value is not the same that API version, so we need know it for each new API version.
        public uint AppVersion { get; } = 8700;

        public long Unknown25 { get; } = unchecked((long)0x3E75FB078C4573FA);
    
        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey) : this(new []{ authKey })
        {

        }

        /// <summary>
        ///     Get Hash server of current key the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="key">The PokeHash authkeys obtained from Hash Server. </param>
        private Uri GetHashUri(string key)
        {
            foreach (var hasher in _authHashers) {
                if (key.StartsWith(hasher.pattern, StringComparison.Ordinal))
                    return hasher.uri;
            }
            return Configuration.HasherUrl;;
        }

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKeys">The PokeHash authkeys obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string[] authKeys)
        {
            if (authKeys.Length < 1)
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

            int retryCount = 0;
            while (retryCount <= 10)
            {
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
                                    .Select(x => (ulong)x)
                                    .ToArray()
                            };

                        case HttpStatusCode.NotFound:
                            message = $"Hashing endpoint not found!";
                            break;

                        case HttpStatusCode.BadRequest:
                            message = $"Bad request sent to the hashing server! {responseContent}";
                            break;

                        case HttpStatusCode.Unauthorized:
                            message = "The auth key supplied for PokeHash was invalid.";
                            break;

                        case (HttpStatusCode)429:
                            message = $"Your request has been limited. {response}";
                            break;

                        default:
                            message = $"We received an unknown HttpStatusCode ({response.StatusCode})..";
                            break;
                    }

                    // TODO: Find a better way to let the developer know of these issues.
                    message = $"[PokeHash]: {message}";

                    Logger.Error(message);

                    if (retryCount == 10)
                        throw new Exception(message);

                    retryCount++;
                }
            }
            return null;
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

                    var availableKeys = _authKeys.Where(x => x.Requests < x.MaxRequestCount).ToArray();
                    if (availableKeys.Length > 0)
                    {
                        authKey = availableKeys.First();
                        authKey.Requests += 1;

                        // If the auth key has not been initialized yet, we need to have control a bit longer
                        // to configure it properly.
                        if (!authKey.IsInitialized)
                            extendedSelection = true;
                    }
                    else
                    {
                        authKey = _authKeys
                            .OrderBy(x => x.RatePeriodEnd)
                            .First();

                        var sleepTime = (int)Math.Ceiling(authKey.RatePeriodEnd.Subtract(DateTime.UtcNow).TotalMilliseconds);

                        PokehashSleeping?.Invoke(this, sleepTime);

                        await Task.Delay(sleepTime);

                        // Rate limit is over, so reset requests.
                        authKey.Requests = 0;
                        // We have to receive the new rate period end.
                        extendedSelection = true;
                    }
                }
                finally
                {
                    if (!extendedSelection)
                    {
                        _keySelection.Release();
                    }
                }

                requestContent.Headers.Add("X-AuthToken", authKey.AuthKey);

                HttpResponseMessage response = null;
                try
                {
                    // Initialize HttpClient.
                    using (var _httpClient = new HttpClient
                    {
                        BaseAddress = GetHashUri(authKey.AuthKey)
                    })
                    {
                        _httpClient.DefaultRequestHeaders.Clear();
                        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("POGOLib.Core (https://github.com/Furtif/POGOLib)");
                        response = await _httpClient.PostAsync(Configuration.HashEndpoint, requestContent);
                        _httpClient.Dispose();
                    };
                }
                catch (Exception ex)
                {
                    throw new PokeHashException(ex.Message);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new Exception("Pokehash key seems invalid");
                }

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
                    if (response.Headers.TryGetValues("X-MaxRequestCount", out maxRequestsValue) &&
                        response.Headers.TryGetValues("X-RateRequestsRemaining", out requestsRemainingValue) &&
                        response.Headers.TryGetValues("X-RatePeriodEnd", out ratePeriodEndValue))
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
                        authKey.RatePeriodEnd = ratePeriodEnd;
                    }

                    return response;
                }
                finally
                {
                    if (extendedSelection)
                    {
                        _keySelection.Release();
                    }
                }
            });
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }

        public event EventHandler<int> PokehashSleeping;
    }
}

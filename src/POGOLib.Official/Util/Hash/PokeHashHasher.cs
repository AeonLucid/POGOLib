using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    ///     Android version: 0.51.0
    ///     IOS version: 1.21.0
    /// </summary>
    public class PokeHashHasher : IHasher
    {
        private const string PokeHashUrl = "https://pokehash.buddyauth.com/";

        private const string PokeHashEndpoint = "api/v121/hash";

        private readonly HttpClient _httpClient;

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(PokeHashUrl)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("POGOLib (https://github.com/AeonLucid/POGOLib)");
            _httpClient.DefaultRequestHeaders.Add("X-AuthToken", authKey);
        }

        public Version PokemonVersion { get; } = new Version("0.51.0");

        public long Unknown25 { get; } = -8832040574896607694;

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
            
            using (var response = await _httpClient.PostAsync(PokeHashEndpoint, requestContent))
            {
                var responseContent = await response.Content.ReadAsStringAsync();

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
                        throw new Exception($"Bad request sent to the hashing server! {responseContent}");
                    
                    case HttpStatusCode.Unauthorized:
                        throw new Exception("The auth key supplied for PokeHash was invalid.");
                    
                    case (HttpStatusCode) 429:
                        throw new Exception($"Your request has been limited. {responseContent}");

                    default:
                        throw new Exception($"We received an unknown HttpStatusCode ({response.StatusCode})..");
                }
            }
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}

using System;
using POGOLib.Official.Util.Encryption.PokeHash;
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
        public Version PokemonVersion { get; } = new Version("0.51.0");

        public long Unknown25 { get; } = -8832040574896607694;

        private readonly string _authKey;

        /// <summary>
        ///     Initializes the <see cref="PokeHashHasher"/>.
        /// </summary>
        /// <param name="authKey">The PokeHash authkey obtained from https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer. </param>
        public PokeHashHasher(string authKey)
        {
            _authKey = authKey;
        }

        public HashData GetHashData(RequestEnvelope requestEnvelope, byte[] locationBytes, byte[][] requestsBytes, byte[] serializedTicket)
        {
            throw new NotImplementedException();
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptPokeHash.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}

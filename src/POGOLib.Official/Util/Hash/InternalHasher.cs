using POGOLib.Official.Util.Encryption.Legacy;

namespace POGOLib.Official.Util.Hash
{
    /// <summary>
    ///     This is the default <see cref="IHasher"/> used by POGOLib.
    /// 
    ///     Android version: 0.45.0
    ///     IOS version: 1.15.0
    /// </summary>
    internal class InternalHasher : IHasher
    {
        public string PokemonVersion { get; } = "0.45.0";

        public long Unknown25 { get; } = -1553869577012279119;

        public int GetLocationHash1(byte[] locationBytes, byte[] serializedTicket)
        {
            return (int) NiaHashLegacy.Hash32Salt(locationBytes, NiaHashLegacy.Hash32(serializedTicket));
        }

        public int GetLocationHash2(byte[] locationBytes)
        {
            return (int) NiaHashLegacy.Hash32(locationBytes);
        }

        public ulong GetRequestHash(byte[] requestBytes, byte[] serializedTicket)
        {
            return NiaHashLegacy.Hash64Salt64(requestBytes, NiaHashLegacy.Hash64(serializedTicket));
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCryptLegacy.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}

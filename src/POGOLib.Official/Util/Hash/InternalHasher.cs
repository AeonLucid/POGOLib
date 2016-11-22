using POGOLib.Official.Util.Encryption;

namespace POGOLib.Official.Util.Hash
{
    internal class InternalHasher : IHasher
    {
        public string PokemonVersion { get; } = "0.45.0";

        public long Unknown25 { get; } = -1553869577012279119;

        public int GetLocationHash1(byte[] locationBytes, byte[] serializedTicket)
        {
            return (int) NiaHash.Hash32Salt(locationBytes, NiaHash.Hash32(serializedTicket));
        }

        public int GetLocationHash2(byte[] locationBytes)
        {
            return (int) NiaHash.Hash32(locationBytes);
        }

        public ulong GetRequestHash(byte[] requestBytes, byte[] serializedTicket)
        {
            return NiaHash.Hash64Salt64(requestBytes, NiaHash.Hash64(serializedTicket));
        }

        public byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs)
        {
            return PCrypt.Encrypt(signatureBytes, timestampSinceStartMs);
        }
    }
}

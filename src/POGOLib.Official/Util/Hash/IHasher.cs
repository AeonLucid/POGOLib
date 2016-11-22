namespace POGOLib.Official.Util.Hash
{
    public interface IHasher
    {

        /// <summary>
        /// The PokémonVersion this <see cref="IHasher"/> is made for.
        /// Please use API versioning of PokemonGo only (https://pgorelease.nianticlabs.com/plfe/version).
        /// </summary>
        string PokemonVersion { get; }

        long Unknown25 { get; }

        int GetLocationHash1(byte[] locationBytes, byte[] serializedTicket);

        int GetLocationHash2(byte[] locationBytes);

        ulong GetRequestHash(byte[] requestBytes, byte[] serializedTicket);

        byte[] GetEncryptedSignature(byte[] signatureBytes, uint timestampSinceStartMs);

    }
}

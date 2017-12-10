using System;

namespace POGOLib.Official.Util.Hash.PokeHash
{
    internal class PokeHashAuthKey : IEquatable<PokeHashAuthKey>
    {
        public PokeHashAuthKey(string authKey, Uri hashurl)
        {
            AuthKey = authKey;
            HashUrl = hashurl;
        }

        public Uri HashUrl { get; set; }

        public string AuthKey { get; }

        public bool IsInitialized { get; set; } = false;

        public int MaxRequestCount { get; set; } = 150;

        public int Requests { get; set; } = 0;

        public DateTime RatePeriodEnd { get; set; } = DateTime.UtcNow;

        public bool Equals(PokeHashAuthKey other)
        {
            return AuthKey.Equals(other.AuthKey);
        }
    }
}

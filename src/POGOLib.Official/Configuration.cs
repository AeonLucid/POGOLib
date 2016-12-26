using POGOLib.Official.Util.Hash;

namespace POGOLib.Official
{
    public static class Configuration
    {

        /// <summary>
        /// Gets or sets the amount of milliseconds between HTTP requests to PokemonGo.
        /// </summary>
        public static int ThrottleDifference { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the <see cref="IgnoreHashVersion"/> boolean. If set to true, HashVersion checking will be disabled.
        /// </summary>
        public static bool IgnoreHashVersion { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="IHasher"/> used in <see cref="POGOLib.Official.Net.RpcEncryption"/>.
        /// </summary>
        public static IHasher Hasher { get; set; } = new LegacyHasher();

    }
}
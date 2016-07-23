namespace POGOLib
{
    public static class Configuration
    {

        /// <summary>
        /// Gets or sets whether debugging is enabled.
        /// </summary>
        public static bool Debug { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RateLimitTimeout"/> in milliseconds for when StatusCode 52 is received from PokémonGo.
        /// </summary>
        public static int RateLimitTimeout { get; set; } = 30000;

    }
}

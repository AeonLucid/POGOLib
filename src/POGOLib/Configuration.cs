namespace POGOLib
{
    public static class Configuration
    {
        /// <summary>
        ///     Gets or sets the <see cref="SlowServerTimeout" /> in milliseconds for when StatusCode 52 is received from PokémonGo.
        /// </summary>
        public static int SlowServerTimeout { get; set; } = 10000;
    }
}
namespace POGOLib
{
    public static class Configuration
    {
		private static int _slowServerTimeout = 10000;
		private static int _startupTimeout = 5000;
        /// <summary>
        ///     Gets or sets the <see cref="SlowServerTimeout" /> in milliseconds for when StatusCode 52 is received from PokémonGo.
        /// </summary>
		public static int SlowServerTimeout {
			get {
				return _slowServerTimeout;
			}

			set { 
				_slowServerTimeout = value;
			}
		}

		public static int StartupTimeout {
			get {
				return _startupTimeout;
			}

			set { 
				_startupTimeout = value;
			}
		}
	}
}
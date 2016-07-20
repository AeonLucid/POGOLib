using Newtonsoft.Json;
using POGOLib.Pokemon;

namespace POGOLib.Net.Data
{
    /// <summary>
    /// Represents client-side data, used to authenticate through a <see cref="PoClient"/>.
    /// </summary>
    public class ClientData
    {
        /// <summary>
        /// Gets or sets the username of the <see cref="ClientData"/>.
        /// </summary>
        [JsonProperty("username", Required = Required.Always)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the login provider of the <see cref="ClientData"/>.
        /// </summary>
        [JsonProperty("login_provider", Required = Required.Always)]
        public LoginProvider LoginProvider { get; set; }

        /// <summary>
        /// Gets or sets the GPS data of the <see cref="ClientData"/>.
        /// </summary>
        [JsonProperty("gps_data", Required = Required.Always)]
        public GpsData GpsData { get; set; }

        /// <summary>
        /// Gets or sets the authentication data of the <see cref="ClientData"/>.
        /// </summary>
        [JsonProperty("auth_data")]
        public AuthData AuthData { get; set; }
    }
}
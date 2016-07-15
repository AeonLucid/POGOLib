using Newtonsoft.Json;
using POGOLib.Pokemon;

namespace POGOLib.Net.Data
{
    public class ClientData
    {

        [JsonProperty("username", Required = Required.Always)]
        public string Username { get; set; }

        [JsonProperty("login_provider", Required = Required.Always)]
        public LoginProvider LoginProvider { get; set; }

        [JsonProperty("gps_data", Required = Required.Always)]
        public GpsData GpsData { get; set; }

        [JsonProperty("auth_data")]
        public AuthData AuthData { get; set; }

    }
}
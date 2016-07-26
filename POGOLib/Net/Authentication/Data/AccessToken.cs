using System;
using Newtonsoft.Json;
using POGOLib.Pokemon.Data;
using POGOProtos.Networking.Envelopes;

namespace POGOLib.Net.Authentication.Data
{
    public class AccessToken
    {
        [JsonIgnore]
        public string Uid => $"{Username}-{LoginProvider}";

        [JsonProperty("username", Required = Required.Always)]
        public string Username { get; internal set; }

        [JsonProperty("token", Required = Required.Always)]
        public string Token { get; internal set; }

        [JsonProperty("expiry", Required = Required.Always)]
        public DateTime Expiry { get; internal set; }

        [JsonProperty("login_provider", Required = Required.Always)]
        public LoginProvider LoginProvider { get; internal set; }

        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow > Expiry;

        [JsonIgnore]
        public AuthTicket AuthTicket { get; internal set; }

        public void Expire()
        {
            Expiry = DateTime.UtcNow;
            AuthTicket = null;
        }
    }
}
using System;
using Newtonsoft.Json;

namespace POGOLib.Net.Data
{
    public class AuthData
    {

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expire_datetime")]
        public DateTime ExpireDateTime { get; set; }

    }
}

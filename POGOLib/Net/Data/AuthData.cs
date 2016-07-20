using System;
using Newtonsoft.Json;

namespace POGOLib.Net.Data
{
    /// <summary>
    /// Represents the authentication response data.
    /// </summary>
    public class AuthData
    {
        /// <summary>
        /// Gets or sets the access token of the <see cref="AuthData"/>.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the expire date of the <see cref="AuthData"/>.
        /// </summary>
        [JsonProperty("expire_datetime")]
        public DateTime ExpireDateTime { get; set; }
    }
}

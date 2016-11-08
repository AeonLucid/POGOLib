using Newtonsoft.Json;

namespace POGOLib.Official.Net.Authentication.Data
{
    internal struct LoginData
    {
        [JsonProperty("lt", Required = Required.Always)]
        public string Lt { get; set; }

        [JsonProperty("execution", Required = Required.Always)]
        public string Execution { get; set; }
    }
}
using Newtonsoft.Json;

namespace POGOLib.Net.Data
{
    public class GpsData
    {

        [JsonProperty("latitude", Required = Required.Always)]
        public double Latitude { get; set; }

        [JsonProperty("longitude", Required = Required.Always)]
        public double Longitude { get; set; }

        [JsonProperty("altitude", Required = Required.Always)]
        public double Altitude { get; set; }

    }
}
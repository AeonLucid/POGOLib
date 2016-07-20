using Newtonsoft.Json;

namespace POGOLib.Net.Data
{
    /// <summary>
    /// Represents a GPS position.
    /// </summary>
    public class GpsData
    {
        /// <summary>
        /// Gets or sets the latitude of the <see cref="GpsData"/>.
        /// </summary>
        [JsonProperty("latitude", Required = Required.Always)]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the <see cref="GpsData"/>.
        /// </summary>
        [JsonProperty("longitude", Required = Required.Always)]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the altitude of the <see cref="GpsData"/>.
        /// </summary>
        [JsonProperty("altitude", Required = Required.Always)]
        public double Altitude { get; set; }
    }
}
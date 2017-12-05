using POGOProtos.Networking.Requests.Messages;
using System;

namespace POGOLib.Official.Util
{
    internal class LocationUtil
    {

        public static float OffsetLatitudeLongitude(double lat, double ran)
        {
            const int round = 6378137;
            var dl = ran / (round * Math.Cos(Math.PI * lat / 180));

            return (float)(lat + dl * 180 / Math.PI);
        }

    }

    /// <summary>
    /// Description of LocaleInfo.
    /// </summary>
    public class ILocaleInfo
    {
        public string Country = "US";
        public string Language = "en";
        public string TimeZone = "America/New_York";

        public void SetValues(string country = "US", string language = "en", string timezone = "America/New_York")
        {
            Country = country;
            Language = language;
            TimeZone = timezone;
        }

        public GetPlayerMessage.Types.PlayerLocale PlayerLocale()
        {
            var locale = new GetPlayerMessage.Types.PlayerLocale
            {
                Country = Country,
                Language = Language,
                Timezone = TimeZone
            };
            return locale;
        }
    }
}

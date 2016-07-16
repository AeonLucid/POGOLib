using System;

namespace POGOLib.Util
{
    public static class TimeUtil
    {

        private static DateTime _posixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetCurrentTimestampInMs()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static DateTime GetDateTimeFromMs(long timestampMs)
        {
            return _posixTime.AddMilliseconds(timestampMs);
        }

        public static DateTime GetDateTimeFromS(int timestampS)
        {
            return _posixTime.AddSeconds(timestampS);
        }
    }
}

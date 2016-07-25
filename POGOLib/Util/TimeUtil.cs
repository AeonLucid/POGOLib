using System;

namespace POGOLib.Util
{
    public static class TimeUtil
    {
        private static DateTime _posixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        ///     Returns the current unix timestamp in milliseconds (UTC).
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentTimestampInMilliseconds()
        {
            return (long) (DateTime.UtcNow - _posixTime).TotalMilliseconds;
        }

        /// <summary>
        ///     Returns the current unix timestamp in seconds (UTC).
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentTimestampInSeconds()
        {
            return (long) (DateTime.UtcNow - _posixTime).TotalSeconds;
        }

        public static DateTime GetDateTimeFromMilliseconds(long timestampMilliseconds)
        {
            return _posixTime.AddMilliseconds(timestampMilliseconds);
        }

        public static DateTime GetDateTimeFromSeconds(int timestampSeconds)
        {
            return _posixTime.AddSeconds(timestampSeconds);
        }
    }
}
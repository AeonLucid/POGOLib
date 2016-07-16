using System;

namespace POGOLib.Util
{
    internal static class TimeUtil
    {

        public static long GetCurrentTimestampInMs()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

    }
}

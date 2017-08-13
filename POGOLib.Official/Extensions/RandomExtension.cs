using System;
using System.Linq;

namespace POGOLib.Official.Extensions
{
    public static class RandomExtension
    {

        public static double NextDouble(this Random random, double minimum, double maximum)
        {
            return random.NextDouble() * (maximum - minimum) + minimum;
        }

        public static string NextHexNumber(this Random random, int length)
        {
            var buffer = new byte[length / 2];
            random.NextBytes(buffer);

            var result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (length % 2 == 0)
                return result;

            return result + random.Next(16).ToString("X");
        }

    }

    public class RequestIdGenerator
    {
        private static long MULTIPLIER = 16807;
        private static long MODULUS = 0x7FFFFFFF;

        private long rpcIdHigh = 1;
        private long rpcId = 2;

        /**
         * Generates next request id and increments count
         * @return the next request id
         */
        public long Next()
        {
            rpcIdHigh = MULTIPLIER * rpcIdHigh % MODULUS;
            return rpcId++ | (rpcIdHigh << 32);
        }
    }
}

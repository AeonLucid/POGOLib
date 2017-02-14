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
}

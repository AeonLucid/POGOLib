using System;
using System.Linq;

namespace POGOLib
{
    internal class HexUtil
    {

        private static readonly Random Random = new Random();

        public static string GetRandomHexNumber(int length)
        {
            var buffer = new byte[length / 2];
            Random.NextBytes(buffer);

            var result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (length % 2 == 0)
                return result;

            return result + Random.Next(16).ToString("X");
        }

    }
}

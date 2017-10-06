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

    public class RandomIdGenerator
    {
        public static ulong LastRequestID { get; private set; }

        // Thanks to Noctem and Xelwon
        // Lehmer random number generator - https://en.wikipedia.org/wiki/Lehmer_random_number_generator

        ulong MersenePrime = 0x7FFFFFFF;           // M = 2^31 -1 = 2,147,483,647 (Mersenne prime M31)
        ulong PrimeRoot = 0x41A7;                  // A = 16807 (a primitive root modulo M31)
        ulong Quotient = 0x1F31D;                  // Q = 127773 = M / A (to avoid overflow on A * seed)
        ulong Rest = 0xB14;                        // R = 2836 = M % A (to avoid overflow on A * seed)
        public static ulong Hi = 1;
        public static ulong Lo = 2;

        public RandomIdGenerator()
        {
            LastRequestID = (LastRequestID == 0) ? 1 : LastRequestID;
        }

        public ulong Last()
        {
            return LastRequestID;
        }

        // Old method to obtain the request ID
        public ulong NextLehmerRandom()
        {
            Hi = 0;
            Lo = 0;
            ulong NewRequestID;

            Hi = LastRequestID / Quotient;
            Lo = LastRequestID % Quotient;

            NewRequestID = PrimeRoot * Lo - Rest * Hi;
            if (NewRequestID <= 0)
                NewRequestID = NewRequestID + MersenePrime;

            //Logger.Debug($"[OLD LEHMER] {NewRequestID.ToString("X")} [{Hi.ToString("X")},{Lo.ToString("X")}]");

            NewRequestID = NewRequestID % 0x80000000;
            LastRequestID = NewRequestID;

            return NewRequestID;
        }

        // New method to obtain the request ID (extracted from pgoapi)
        // TODO: Check this with pgoapi. This has  not sense for me (Xelwon) .
        // https://github.com/pogodevorg/pgoapi/blob/develop/pgoapi/rpc_api.py
        // Line 82
        public ulong NextSinceAPI0691()
        {
            Hi = PrimeRoot * Hi % MersenePrime;
            ulong NewRequestID = Lo++ | (Hi << 32);
            LastRequestID = NewRequestID;
            //Logger.Debug($"[NEW METHOD] {NewRequestID.ToString("X")} [{Hi.ToString("X")},{Lo.ToString("X")}]");

            return NewRequestID;
        }

        public ulong Next()
        {
            return NextSinceAPI0691();
        }
    }

    public class Uk27IdGenerator
    {
        private static int min = 1000;
        private static int max = 60000;
        private static readonly Random _random = new Random();

        public void Init(int _min, int _max)
        {
            min = _min;
            max = _max;
        }

        public int Next()
        {
            return _random.Next(min, max);
        }
    }
}

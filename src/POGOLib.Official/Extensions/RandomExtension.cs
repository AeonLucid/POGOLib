using System;

namespace POGOLib.Extensions
{
    public static class RandomExtension
    {

        public static double NextDouble(this Random random, double minimum, double maximum)
        {
            return random.NextDouble() * (maximum - minimum) + minimum;
        }

    }
}

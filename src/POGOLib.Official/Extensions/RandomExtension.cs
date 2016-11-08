using System;

namespace POGOLib.Official.Extensions
{
    public static class RandomExtension
    {

        public static double NextDouble(this Random random, double minimum, double maximum)
        {
            return random.NextDouble() * (maximum - minimum) + minimum;
        }

    }
}

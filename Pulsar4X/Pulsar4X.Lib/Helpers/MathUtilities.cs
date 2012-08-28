using System;

namespace Pulsar4X
{
    public static class MathUtilities
    {
        // TODO: This should be OUT of this class
        public static Random Random;

        static MathUtilities()
        {
            Random = new Random();
        }

        public static double About(double value, double variation)
        {
            return (value + (value * Random.NextDouble(-variation, variation)));
        }

        /* function for 'soft limiting' temperatures */
        public static double Limit(double x)
        {
            return x / Math.Sqrt(Math.Sqrt(1 + x * x * x * x));
        }

        public static double Soft(double v, double max, double min)
        {
            double dv = v - min;
            double dm = max - min;
            return (Limit(2 * dv / dm - 1) + 1) / 2 * dm + min;
        }

    }
}

using System;

namespace Pulsar4X
{
    public static class ExtensionMethods
    {
        public static double NextDouble(this Random rnd, double min, double max)
        {
            return rnd.NextDouble() * (max - min) + min;
        }

        public static double About(this Random rnd, double value, double variation)
        {
            return (value + (value * rnd.NextDouble(-variation, variation)));
        }

        public static double RandomEccentricity(this Random rnd)
        {
            return 1.0 - Math.Pow(rnd.NextDouble(0.0, 1.0), Constants.Units.ECCENTRICITY_COEFF);
        }

        public static double randomNormal(this Random rnd)
        {
            double u1 = rnd.NextDouble();
            double u2 = rnd.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return randStdNormal;
        }
    }
}

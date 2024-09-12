using System;

namespace Pulsar4X.Engine
{
    public class GaussianRandom
    {
        public double NextGaussian(Random rng, double mean, double standardDeviation)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();

            double standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            return mean + standardDeviation * standardNormal;
        }

        /// <summary>
        /// Get a random value on a bell curve
        /// </summary>
        /// <param name="rng"></param>
        /// <param name="min">The minimum value that can be returned</param>
        /// <param name="max">The maximum value that can be returned</param>
        /// <param name="mean">The mean of the bell curve</param>
        /// <param name="standardDeviation">The standard deviation of the bell curve</param>
        /// <returns></returns>
        public int NextBellCurve(Random rng, int min, int max, double mean, double standardDeviation)
        {
            int result;
            do
            {
                result = (int)NextGaussian(rng, mean, standardDeviation);
            } while(result < min || result > max);

            return result;
        }
    }
}
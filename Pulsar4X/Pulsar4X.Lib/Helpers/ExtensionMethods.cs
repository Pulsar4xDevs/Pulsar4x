using System;

namespace Pulsar4X
{
    public static class ExtensionMethods
    {
        public static double NextDouble(this Random rnd, double min, double max)
        {
            return rnd.NextDouble() * (max - min) + min;
        }
    }
}

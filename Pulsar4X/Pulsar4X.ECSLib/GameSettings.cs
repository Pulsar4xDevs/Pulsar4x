using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public static class GameSettings
    {
        /// <summary>
        /// Scientific Constants
        /// </summary>
        public static class Science
        {
            // Gravitation Constant
            public const double GravitationalConstant = 6.67384E-11;
        }


        /// <summary>
        /// Constants dealing with units and measurements
        /// </summary>
        public static class Units
        {
            public const double SolarMassInKG = 1.98855E30;

            public const double EarthMassInKG = 5.97219E24;

            public const double SolMassInEarthMasses = 332946;

            public const double KmPerLightYear = 9460730472580.8;

            public const double AuPerLightYear = KmPerLightYear / KmPerAu;

            public const double KmPerAu = MetersPerAu / 1000;

            public const double MetersPerAu = 149597870700;  // this is exact, see: http://en.wikipedia.org/wiki/Astronomical_unit

            /// <summary>
            /// Plus or Minus 65Km
            /// </summary>
            public const double SolarRadiusInKm = 696342.0;

            public const double SolarRadiusInAu = SolarRadiusInKm / KmPerAu;

            /// <summary>
            /// Earth's gravity in m/s^2. Aka 1g.
            /// </summary>
            public const double EarthGravity = 9.81;

            /// <summary>
            /// Note that this is = to 1 ATM.
            /// </summary>
            public const double EarthAtmosphereInKpa = 101.325;

            /// <summary>
            /// Add to Kelvin to get degrees c.
            /// </summary>
            public const double DegreesCToKelvin = 273.15;

            /// <summary>
            /// Add to degrees c to get kelvin.
            /// </summary>
            public const double KelvinToDegreesC = -273.15;
        }

        public static class GameConstants
        {
            public const int MinimumTimestep = 5;
        }
    }
}

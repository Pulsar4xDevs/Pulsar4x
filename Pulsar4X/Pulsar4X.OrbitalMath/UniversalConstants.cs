using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital
{
    public static class UniversalConstants
    {
        public const int MinimumTimestep = 5;

        /// <summary>
        /// Scientific Constants
        /// </summary>
        public static class Science
        {
            // Gravitation Constant
            public const double GravitationalConstant = 6.67408E-11;

            //h in m2 kg/s (or j/s)
            public const double PlankConstant = 6.62607015E-34;
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

            public const double MetersPerKm = 1000;

            public const double KmPerAu = MetersPerAu / MetersPerKm;

            public const double MetersPerAu = 149597870700;  // this is exact, see: http://en.wikipedia.org/wiki/Astronomical_unit

            #region Pressure - https://en.wikipedia.org/wiki/Standard_atmosphere_(unit)
            public const float PascalsPerATM = 101325;

            public const float ATMPerPascal = 1 / PascalsPerATM;

            public const float BarPerATM = 1.01325f;

            public const float ATMPerBar = 1 / BarPerATM;

            public const float TorrPerATM = 760;

            public const float ATMPerTorr = 1 / TorrPerATM;
            #endregion

            /// <summary>
            /// Speed of Sound in Air ion Metres per Second
            /// </summary>
            public const double SpeedOfSoundInMetresPerSecond = 343;

            /// <summary>
            /// Speed of Light in Metres per Second
            /// </summary>
            public const double SpeedOfLightInMetresPerSecond = 299792458;  // see https://en.wikipedia.org/wiki/Speed_of_light

            /// <summary>
            /// Plus or Minus 65Km
            /// </summary>
            public const double SolarRadiusInKm = 696342.0;

            public const double SolarRadiusInAu = SolarRadiusInKm / KmPerAu;

            public const double SolarRadius = SolarRadiusInKm * MetersPerKm;

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
    }

}

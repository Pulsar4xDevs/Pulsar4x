using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X
{
    /// <summary>
    /// Container class for all the constants used elsewhere in the game
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Constants dealing with units and measurements
        /// </summary>
        public class Units
        {
            /// <summary>
            /// Mass in grams of one solar mass
            /// </summary>
            public const double SOLAR_MASS_IN_GRAMS = Constants.Sol.Sun.MASS_IN_GRAMS;

            public const double EARTH_MASS_IN_GRAMS = Constants.Sol.Earth.MASS_IN_GRAMS;

            public const double SUN_MASS_IN_EARTH_MASSES = SOLAR_MASS_IN_GRAMS / EARTH_MASS_IN_GRAMS;

            public const double CM_PER_AU = 1.495978707E13;

            public const double CM_PER_KM = 1.0E5;

            public const double KM_PER_AU = CM_PER_AU / CM_PER_KM;

            /// <summary>
            /// Number of radians in 360 degrees
            /// </summary>
            public const double RADIANS_PER_ROTATION = 2.0 * Math.PI;
        }

        /// <summary>
        /// Constants detailing our solar system
        /// </summary>
        public class Sol
        {
            /// <summary>
            /// Constants involving our Sun
            /// </summary>
            public class Sun
            {
                public const double MASS_IN_GRAMS = 1.989E33;
            }

            /// <summary>
            /// Constants involving Earth
            /// </summary>
            public class Earth
            {
                public const double MASS_IN_GRAMS = 5.977E27;

                /// <summary>
                /// Density in grams per cubic centimetre
                /// </summary>
                public const double DENSITY = 5.52;

                /// <summary>
                /// Radius in centimeters
                /// </summary>
                public const double RADIUS = 6.378E8;
            }
        }

        public class Gasses
        {
        }

        /// <summary>
        /// Constants specifically used in the stargen module
        /// </summary>
        public class Stargen
        {
            /// <summary>
            /// Initial mass of injected protoplanet
            /// </summary>
            public const double PROTOPLANET_MASS = 1.0E-15;

            public const double ASTEROID_MASS_LIMIT = 0.001;

            /// <summary>
            /// Constant representing the gas/dust ratio
            /// </summary>
            public const double K = 50.0;

            /// <summary>
            /// Constant used in Crit_mass calc
            /// </summary>
            public const double B = 1.2E-5;

            /// <summary>
            /// Coefficient of dust density (A in Dole's paper)
            /// </summary>
            public const double DUST_DENSITY_COEFF = 2.0E-3;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double ALPHA = 5.0;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double N = 3.0;

            /// <summary>
            /// Constant used in density calcs
            /// </summary>
            public const double J = 1.46E-19;
        }
    }
}

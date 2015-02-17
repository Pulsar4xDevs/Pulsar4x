using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Lib;

namespace Pulsar4X
{
    /// <summary>
    /// Galaxy Gen holds some information used by the SystemGen Class when generating Stars.
    /// It does not actually Generate anything on its own, but rahter guids the generation of each star system to make a consistand Galaxy.
    /// </summary>
    public static class GalaxyGen
    {
        /// <summary>
        /// RNG used to generate seeds for a star system if none are provided.
        /// </summary>
        public  static Random SeedRNG = new Random();

        /// <summary>
        /// Indicates weither We shoudl generate a Real Star System or a more gamey one.
        /// </summary>
        public static bool RealStarSystems = true;

        /// <summary>
        /// The chance of a Non-player Race being generated on a suitable planet.
        /// </summary>
        public static double NPRGenerationChance = 0.3333;

        /// <summary>
        /// Minium number of comets each system will have. All systems will be guaranteed to have a least this many comets.
        /// </summary>
        public static int MiniumCometsPerSystem = 0;


        
        #region Advanced Star Generation Parameters
        // Note that the data is this section is largly based on scientific fact
        // See: http://en.wikipedia.org/wiki/Stellar_classification
        // these values SHOULD NOT be Modified if you weant sane star generation.
        // Also note that thile these are constants they were not added to the 
        // constants file because they are only used for star gen.


        /// <summary>
        /// Small helper struct to make all these min/max dicts. nicer.
        /// </summary>
        public struct MinMaxStruct { public double _min, _max; }

        /// <summary>
        /// This Dictionary holds the minium and maximum radius values (in AU) for a Star given its spectral type.
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarRadiusBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 6.6 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 250 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.B, new MinMaxStruct() { _min = 1.8 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 6.6 * Constants.Units.SOLAR_RADIUS_IN_AU }  },
                { SpectralType.A, new MinMaxStruct() { _min = 1.4 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.8 * Constants.Units.SOLAR_RADIUS_IN_AU }  },
                { SpectralType.F, new MinMaxStruct() { _min = 1.15 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.4 * Constants.Units.SOLAR_RADIUS_IN_AU }  },
                { SpectralType.G, new MinMaxStruct() { _min = 0.96 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.15 * Constants.Units.SOLAR_RADIUS_IN_AU }  },
                { SpectralType.K, new MinMaxStruct() { _min = 0.7 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 0.96 * Constants.Units.SOLAR_RADIUS_IN_AU }  },
                { SpectralType.M, new MinMaxStruct() { _min = 0.12 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 0.7 * Constants.Units.SOLAR_RADIUS_IN_AU}  },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum Temperature (in degrees celsius) values for a Star given its spectral type.
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarTemperatureBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 30000, _max = 60000 } },
                { SpectralType.B, new MinMaxStruct() { _min = 10000, _max = 30000 }  },
                { SpectralType.A, new MinMaxStruct() { _min = 7500, _max = 10000 }  },
                { SpectralType.F, new MinMaxStruct() { _min = 6000, _max = 7500 }  },
                { SpectralType.G, new MinMaxStruct() { _min = 5200, _max = 6000 }  },
                { SpectralType.K, new MinMaxStruct() { _min = 3700, _max = 5200 }  },
                { SpectralType.M, new MinMaxStruct() { _min = 2400, _max = 3700 }  },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum Luminosity (in Solar luminosity, i.e. Sol = 1). values for a Star given its spectral type.
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarLuminosityBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 30000, _max = 1000000 } },
                { SpectralType.B, new MinMaxStruct() { _min = 25, _max = 30000 }  },
                { SpectralType.A, new MinMaxStruct() { _min = 5, _max = 25 }  },
                { SpectralType.F, new MinMaxStruct() { _min = 1.5, _max = 5 }  },
                { SpectralType.G, new MinMaxStruct() { _min = 0.6, _max = 1.5 }  },
                { SpectralType.K, new MinMaxStruct() { _min = 0.08, _max = 0.6 }  },
                { SpectralType.M, new MinMaxStruct() { _min = 0.0001, _max = 0.08 }  },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum mass values (in Kg) for a Star given its spectral type.
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarMassBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 16 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 265 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.B, new MinMaxStruct() { _min = 2.1 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 16 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
                { SpectralType.A, new MinMaxStruct() { _min = 1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 2.1 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
                { SpectralType.F, new MinMaxStruct() { _min = 1.04 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
                { SpectralType.G, new MinMaxStruct() { _min = 0.8 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 1.04 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
                { SpectralType.K, new MinMaxStruct() { _min = 0.45 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 0.8 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
                { SpectralType.M, new MinMaxStruct() { _min = 0.08 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 0.45 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS }  },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum Age values (in years) for a Star given its spectral type.
        /// @note Max age of a star in the Milky Way is 13.2 billion years, the age of the milky way. A star could be older 
        /// (like 100 billion years older if not for the fact that the universion is only about 14 billion years old) but then it wouldn't be in the milky way.
        /// This is used for both K and M type stars both of which can easly be older than the milky way).
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarAgeBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 0, _max = 6000000 } },        // after 6 million years O types eiother go nova or become B type stars.
                { SpectralType.B, new MinMaxStruct() { _min = 0, _max = 100000000 }  },     // could not find any info on B type ages, so i made it between O and A (100 million).
                { SpectralType.A, new MinMaxStruct() { _min = 0, _max = 350000000 }  },     // A type stars are always young, typicall a few hundred million years..
                { SpectralType.F, new MinMaxStruct() { _min = 0, _max = 3000000000 }  },    // Could not find any info again, chose a number between B and G stars (3 billion)
                { SpectralType.G, new MinMaxStruct() { _min = 0, _max = 10000000000 }  },   // The life of a G class star is about 10 billion years.
                { SpectralType.K, new MinMaxStruct() { _min = 0, _max = 13200000000 }  },
                { SpectralType.M, new MinMaxStruct() { _min = 0, _max = 13200000000 }  },
            };

        #endregion


        #region Advanced Planet Generation Parameters

        /// <summary>
        /// The chance Planets will be generated around a given star. A number between 0 and 1 (e.g. a 33% chance would be 0.33).
        /// </summary>
        public static const double PlanetGenerationChance = 0.8;

        public static const int MaxNoOfPlanets = 20;

        /// <summary>
        /// Controls how much the type of a star affects the generation of planets.
        /// </summary>
        public static Dictionary<SpectralType, double> StarSpecralTypePlanetGenerationRatio = new Dictionary<SpectralType, double>()
            {
                { SpectralType.O, 0.2 },
                { SpectralType.B, 0.25 },
                { SpectralType.A, 0.4 },
                { SpectralType.F, 0.8 },
                { SpectralType.G, 0.9 },
                { SpectralType.K, 0.75 },
                { SpectralType.M, 0.25 },
            };

       

        #endregion
    }
}

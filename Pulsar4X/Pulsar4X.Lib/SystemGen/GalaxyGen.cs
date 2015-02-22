using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Helpers.GameMath;

namespace Pulsar4X
{
    /// <summary>
    /// Galaxy Gen holds some information used by the SystemGen Class when generating Stars.
    /// It does not actually Generate anything on its own, but rahter guides the generation of each star system to make a consistent Galaxy.
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
        public static bool RealStarSystems = false;

        /// <summary>
        /// The chance of a Non-player Race being generated on a suitable planet.
        /// </summary>
        public static double NPRGenerationChance = 0.3333;

        /// <summary>
        /// Minium number of comets each system will have. All systems will be guaranteed to have a least this many comets.
        /// </summary>
        public static int MiniumCometsPerSystem = 0;

        /// <summary>
        /// Small helper struct to make all these min/max dicts. nicer.
        /// </summary>
        public struct MinMaxStruct { public double _min, _max; }

        
        #region Advanced Star Generation Parameters
        // Note that the data is this section is largly based on scientific fact
        // See: http://en.wikipedia.org/wiki/Stellar_classification
        // these values SHOULD NOT be Modified if you weant sane star generation.
        // Also note that thile these are constants they were not added to the 
        // constants file because they are only used for star gen.

        /// <summary>
        /// This Dictionary holds the minium and maximum radius values (in AU) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarRadiusBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 6.6 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 250 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.B, new MinMaxStruct() { _min = 1.8 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 6.6 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.A, new MinMaxStruct() { _min = 1.4 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.8 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.F, new MinMaxStruct() { _min = 1.15 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.4 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.G, new MinMaxStruct() { _min = 0.96 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 1.15 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.K, new MinMaxStruct() { _min = 0.7 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 0.96 * Constants.Units.SOLAR_RADIUS_IN_AU } },
                { SpectralType.M, new MinMaxStruct() { _min = 0.12 * Constants.Units.SOLAR_RADIUS_IN_AU, _max = 0.7 * Constants.Units.SOLAR_RADIUS_IN_AU} },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum Temperature (in degrees celsius) values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarTemperatureBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 30000, _max = 60000 } },
                { SpectralType.B, new MinMaxStruct() { _min = 10000, _max = 30000 } },
                { SpectralType.A, new MinMaxStruct() { _min = 7500, _max = 10000 } },
                { SpectralType.F, new MinMaxStruct() { _min = 6000, _max = 7500 } },
                { SpectralType.G, new MinMaxStruct() { _min = 5200, _max = 6000 } },
                { SpectralType.K, new MinMaxStruct() { _min = 3700, _max = 5200 } },
                { SpectralType.M, new MinMaxStruct() { _min = 2400, _max = 3700 } },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum Luminosity (in Solar luminosity, i.e. Sol = 1). values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarLuminosityBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 30000, _max = 1000000 } },
                { SpectralType.B, new MinMaxStruct() { _min = 25, _max = 30000 } },
                { SpectralType.A, new MinMaxStruct() { _min = 5, _max = 25 } },
                { SpectralType.F, new MinMaxStruct() { _min = 1.5, _max = 5 } },
                { SpectralType.G, new MinMaxStruct() { _min = 0.6, _max = 1.5 } },
                { SpectralType.K, new MinMaxStruct() { _min = 0.08, _max = 0.6 } },
                { SpectralType.M, new MinMaxStruct() { _min = 0.0001, _max = 0.08 } },
            };

        /// <summary>
        /// This Dictionary holds the minium and maximum mass values (in Kg) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> StarMassBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 16 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 265 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.B, new MinMaxStruct() { _min = 2.1 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 16 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.A, new MinMaxStruct() { _min = 1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 2.1 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.F, new MinMaxStruct() { _min = 1.04 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.G, new MinMaxStruct() { _min = 0.8 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 1.04 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.K, new MinMaxStruct() { _min = 0.45 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 0.8 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
                { SpectralType.M, new MinMaxStruct() { _min = 0.08 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS, _max = 0.45 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS } },
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
                { SpectralType.B, new MinMaxStruct() { _min = 0, _max = 100000000 } },     // could not find any info on B type ages, so i made it between O and A (100 million).
                { SpectralType.A, new MinMaxStruct() { _min = 0, _max = 350000000 } },     // A type stars are always young, typicall a few hundred million years..
                { SpectralType.F, new MinMaxStruct() { _min = 0, _max = 3000000000 } },    // Could not find any info again, chose a number between B and G stars (3 billion)
                { SpectralType.G, new MinMaxStruct() { _min = 0, _max = 10000000000 } },   // The life of a G class star is about 10 billion years.
                { SpectralType.K, new MinMaxStruct() { _min = 0, _max = 13200000000 } },
                { SpectralType.M, new MinMaxStruct() { _min = 0, _max = 13200000000 } },
            };

        #endregion


        #region Advanced SystemBody and other Body Generation Parameters

        /// <summary>
        /// The chance Planets will be generated around a given star. A number between 0 and 1 (e.g. a 33% chance would be 0.33).
        /// </summary>
        public const double PlanetGenerationChance = 0.8;

        /// <summary>
        /// The maximum number -1 of planets which will be generated.
        /// </summary>
        public const int MaxNoOfPlanets = 25;

        /// <summary>
        /// The maximum number of asteriods a system can have. Period.
        /// </summary>
        public const int MaxNoOfAsteroids = 300;

        /// <summary>
        /// Asteriods are generate in belts, this controls the max number per belt. It cannot be larger than MaxNoOfAsteroids.
        /// </summary>
        public const int MaxNoOfAsteroidsPerBelt = 200;

        /// <summary>
        /// Asteriods are generated in belts. this controls the maximum number of belts.
        /// </summary>
        public const int MaxNoOfAsteroidBelts = 3;

        /// <summary>
        /// Used to compute the number of dwarf planets in a given steriod belt.
        /// The formular used is: NoOfAsteriodsInBelt / NumberOfAsteroidsPerDwarfPlanet = NoOfDwarfPlanets;
        /// Dwarf planets are always generated along with their asteriod belt. its the whole "hasn't cleard its orbit" thing.
        /// </summary>
        public const int NumberOfAsteroidsPerDwarfPlanet = 20;

        /// <summary>
        /// The Maximum number of comets per system. note that if MiniumCometsPerSystem > MaxNoOfComets then MiniumCometsPerSystem = MaxNoOfComets.
        /// </summary>
        public const int MaxNoOfComets = 50;

        /// <summary>
        /// Asteroids and Dwarf planets are generated in belts. To do this a single orbit is first generate as the 
        /// basis for the whole belt. Asteroids then apply a small gitter of + or - a percentage of the original orbit 
        /// (except MeanAnomaly, which is the starting point on the orbit. that is random). 
        /// The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
        /// </summary>
        public const double MaxAsteroidOrbitDeviation = 0.05;

        /// <summary>
        /// The maximum SystemBody orbit Inclination. Also used as the maximum orbital tilt.
        /// Angle in degrees.
        /// </summary>
        public const double MaxPlanetInclination = 45; // degrees. used for orbits and axial tilt.

        /// <summary>
        /// This controls the maximum moon mass relative to the parent body.
        /// </summary>
        public const double MaxMoonMassRelativeToParentBody = 0.4;

        /// <summary>
        /// We want StarOrbitGravityFactor times less gravitational attraction from childStar to parentStar's furthest planet then parentStar to parentStar's furthest planet.
        /// <? todo: Is this comment completely confusing?
        /// </summary>
        public const double StarOrbitGravityFactor = 10;

        /// <summary>
        /// The chance a Terrestrial body will have some form of Tectonic activity.
        /// Note that very small/low mass bodies will still end up dead.
        /// </summary>
        public const double TerrestrialBodyTectonicActiviyChance = 0.5;

        /// <summary>
        /// Epoch used when generating orbits. There should be no reason to change this.
        /// </summary>
        public static DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

        /// <summary>
        /// The maximum temperture of a planet which can have Ice Moons, in Kelvin
        /// This is used to work out the change of an Ice moon around a planet, the 
        /// lower the plante's temp below this the moor likly an ice moon is.
        /// </summary>
        public const double IceMoonMaximumParentTemperature = 150;

        /// <summary>
        /// The minium possible length of a day for any system body.
        /// </summary>
        public const int MiniumPossibleDayLength = 6;

        /// <summary>
        /// Is timesd by the total radius of the moon and its parent to come up with a minium orbit distance for that body.
        /// </summary>
        public const double MinMoonOrbitMultiplier = 2;

        /// <summary>
        /// This is the Absolute maximum orbit of moons, in AU.
        /// @note The maximum may be RelativeMaxMoonOrbitDistance instead, but will never be more then this.
        /// </summary>
        public const double AbsoluteMaxMoonOrbitDistance = 60581692 / Constants.Units.KM_PER_AU;

        /// <summary>
        /// This is the relative maximum orbit of moons, in AU. it is times by the parents semiMajorAxis.
        /// @note The maximum may be AbsoluteMaxMoonOrbitDistance instead, but will never be more then this.
        /// </summary>
        public const double RelativeMaxMoonOrbitDistance = 0.25; 

        /// <summary>
        /// Controls how much the type of a star affects the generation of planets.
        /// @note These numbers can be tweaked as desired for gameplay. They affect the number of planets generated for a given star type.
        /// @note Other factors such as the stars lumosoty and mass are also taken into account. So these numbers may not make a whole lot of sense on the surface.
        /// </summary>
        public static Dictionary<SpectralType, double> StarSpecralTypePlanetGenerationRatio = new Dictionary<SpectralType, double>()
            {
                { SpectralType.O, 0.6 },
                { SpectralType.B, 0.7 },
                { SpectralType.A, 0.9 },
                { SpectralType.F, 0.9 },
                { SpectralType.G, 2.1 },
                { SpectralType.K, 2.4 },
                { SpectralType.M, 1.8 },
            };

        /// <summary>
        /// Values which determin the distrubution of planet types in a star system.
        /// these are largly arbritary based on the planets in our solar system.
        /// Dwarf planets are excluded because they are generated with asteroids
        /// rather then with planets (on account of not having cleared their orbits).
        /// </summary>
        /// 
        public static WeightedList<SystemBody.PlanetType> PlanetTypeDisrubution = new WeightedList<SystemBody.PlanetType>()
        {
            { 0.2, SystemBody.PlanetType.GasGiant },
            { 0.2, SystemBody.PlanetType.IceGiant },
            { 0.1, SystemBody.PlanetType.GasDwarf },
            { 0.5, SystemBody.PlanetType.Terrestrial }
        };

        /// <summary>
        /// Limits of SystemBody masses based on type. Units are Kg.
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, MinMaxStruct> PlanetMassByType = new Dictionary<SystemBody.PlanetType, MinMaxStruct>()
            {
                { SystemBody.PlanetType.GasGiant, new MinMaxStruct() { _min = 15 * Constants.Units.EARTH_MASS_IN_KILOGRAMS, _max = 500 * Constants.Units.EARTH_MASS_IN_KILOGRAMS } },
                { SystemBody.PlanetType.IceGiant, new MinMaxStruct() { _min = 5 * Constants.Units.EARTH_MASS_IN_KILOGRAMS, _max = 30 * Constants.Units.EARTH_MASS_IN_KILOGRAMS} },
                { SystemBody.PlanetType.GasDwarf, new MinMaxStruct() { _min = 1 * Constants.Units.EARTH_MASS_IN_KILOGRAMS, _max = 15 * Constants.Units.EARTH_MASS_IN_KILOGRAMS } },
                { SystemBody.PlanetType.Terrestrial, new MinMaxStruct() { _min = 0.05 * Constants.Units.EARTH_MASS_IN_KILOGRAMS, _max = 5 * Constants.Units.EARTH_MASS_IN_KILOGRAMS }  },
                { SystemBody.PlanetType.Moon, new MinMaxStruct() { _min = 1E16, _max = 1 * Constants.Units.EARTH_MASS_IN_KILOGRAMS } },
                { SystemBody.PlanetType.IceMoon, new MinMaxStruct() { _min = 1E16, _max = 5E22 } }, // note 1E16 is 1 nano earth mass.
                { SystemBody.PlanetType.DwarfPlanet, new MinMaxStruct() { _min = 2E20 , _max = 5E23 } },
                { SystemBody.PlanetType.Asteroid, new MinMaxStruct() { _min = 1E15, _max = 9E19 } },
                { SystemBody.PlanetType.Comet, new MinMaxStruct() { _min = 1E13, _max = 9E14 } },
            };

        /// <summary>
        /// Limits of a Planets density based on its type, in g/cm3
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, MinMaxStruct> PlanetDensityByType = new Dictionary<SystemBody.PlanetType, MinMaxStruct>()
            {
                { SystemBody.PlanetType.GasGiant, new MinMaxStruct() { _min = 0.5, _max = 10 } },
                { SystemBody.PlanetType.IceGiant, new MinMaxStruct() { _min = 1, _max = 5 } },
                { SystemBody.PlanetType.GasDwarf, new MinMaxStruct() { _min = 1, _max = 8 } },
                { SystemBody.PlanetType.Terrestrial, new MinMaxStruct() { _min = 3, _max = 8 } },
                { SystemBody.PlanetType.Moon, new MinMaxStruct() { _min = 1.4, _max = 5 } },
                { SystemBody.PlanetType.IceMoon, new MinMaxStruct() { _min = 1, _max = 3 } },
                { SystemBody.PlanetType.DwarfPlanet, new MinMaxStruct() { _min = 1, _max = 6 } },
                { SystemBody.PlanetType.Asteroid, new MinMaxStruct() { _min = 1, _max = 6 } },
                { SystemBody.PlanetType.Comet, new MinMaxStruct() { _min = 0.25, _max = 0.7 } },
            };

        /// <summary>
        /// Orbital distance restrictions (i.e. SemiMajorAxis restrictions) for a planet based upon the type of star it is orbiting.
        /// Units are AU.
        /// @note These numbers, with the exception of G class stars, are based on habital zone calculations. They could be tweaked for gameplay.
        /// </summary>
        public static Dictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                { SpectralType.O, new MinMaxStruct() { _min = 1, _max = 200 } },
                { SpectralType.B, new MinMaxStruct() { _min = 0.5, _max = 100 } },
                { SpectralType.A, new MinMaxStruct() { _min = 0.3, _max = 80 } },
                { SpectralType.F, new MinMaxStruct() { _min = 0.2, _max = 60 } },
                { SpectralType.G, new MinMaxStruct() { _min = 0.1, _max = 40 } },
                { SpectralType.K, new MinMaxStruct() { _min = 0.01, _max = 20 } },
                { SpectralType.M, new MinMaxStruct() { _min = 0.005, _max = 8 } },
            };

        /// <summary>
        /// This is used to adjust the orbital distances in the range of OrbitalDistanceByStarSpectralType for a given planet type.
        /// This is done by raising the random number generated to select from the range to the power of this distribution number.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, double> OrbitalDistanceDistributionByPlanetType = new Dictionary<SystemBody.PlanetType, double>()
            {
                { SystemBody.PlanetType.GasGiant, 1.8 },
                { SystemBody.PlanetType.IceGiant, 1.5 },
                { SystemBody.PlanetType.GasDwarf, 2 },
                { SystemBody.PlanetType.Terrestrial, 3 },
                { SystemBody.PlanetType.DwarfPlanet, 1.2 },
                { SystemBody.PlanetType.Asteroid, 1.2 },
                { SystemBody.PlanetType.Comet, 0.5 },
            };

        /// <summary>
        /// The possible ranges for albedo for various planet types.
        /// @note These are WAGs roughly based on the albedo of bodies in our solar system. They couild be tweak for gameplay.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, MinMaxStruct> PlanetAlbedoByType = new Dictionary<SystemBody.PlanetType, MinMaxStruct>()
            {
                { SystemBody.PlanetType.GasGiant, new MinMaxStruct() { _min = 0.5, _max = 0.7 } },
                { SystemBody.PlanetType.IceGiant, new MinMaxStruct() { _min = 0.5, _max = 0.7 } },
                { SystemBody.PlanetType.GasDwarf, new MinMaxStruct() { _min = 0.3, _max = 0.7 } },
                { SystemBody.PlanetType.Terrestrial, new MinMaxStruct() { _min = 0.05, _max = 0.5 } },
                { SystemBody.PlanetType.Moon, new MinMaxStruct() { _min = 0.05, _max = 0.5 } },
                { SystemBody.PlanetType.IceMoon, new MinMaxStruct() { _min = 0.4, _max = 0.7 } },
                { SystemBody.PlanetType.DwarfPlanet, new MinMaxStruct() { _min = 0.05, _max = 0.95 } },
                { SystemBody.PlanetType.Asteroid, new MinMaxStruct() { _min = 0.05, _max = 0.15 } },
                { SystemBody.PlanetType.Comet, new MinMaxStruct() { _min = 0.95, _max = 0.99 } },
            };

        /// <summary>
        /// The possible range of values for different the magnetic field (aka Magnetosphere) of different planet types.
        /// In microtesla (uT).
        /// @note @note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They couild be tweak for gameplay.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, MinMaxStruct> PlanetMagneticFieldByType = new Dictionary<SystemBody.PlanetType, MinMaxStruct>()
            {
                { SystemBody.PlanetType.GasGiant, new MinMaxStruct() { _min = 10, _max = 2000 } },
                { SystemBody.PlanetType.IceGiant, new MinMaxStruct() { _min = 5, _max = 50 } },
                { SystemBody.PlanetType.GasDwarf, new MinMaxStruct() { _min = 0.1, _max = 20 } },
                { SystemBody.PlanetType.Terrestrial, new MinMaxStruct() { _min = 0.0001, _max = 45 } },
                { SystemBody.PlanetType.Moon, new MinMaxStruct() { _min = 0.0001, _max = 1 } },
                { SystemBody.PlanetType.IceMoon, new MinMaxStruct() { _min = 0.0001, _max = 0.001 } },
                { SystemBody.PlanetType.DwarfPlanet, new MinMaxStruct() { _min = 0.00001, _max = 0.0001 } },
                { SystemBody.PlanetType.Asteroid, new MinMaxStruct() { _min = 0.000001, _max = 0.00001 } },
                { SystemBody.PlanetType.Comet, new MinMaxStruct() { _min = 0.0000001, _max = 0.000001 } },
            };

        /// <summary>
        /// This value is multiplied by (SystemBody Mass / Max Mass for SystemBody Type) i.e. a mass ratio, to get the chance of an atmosphere for this planet.
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, double> AtmosphereGenerationModifier = new Dictionary<SystemBody.PlanetType, double>()
            {
                { SystemBody.PlanetType.GasGiant, 100000000 },
                { SystemBody.PlanetType.IceGiant, 100000000 },
                { SystemBody.PlanetType.GasDwarf, 100000000 },
                { SystemBody.PlanetType.Terrestrial, 1.4 },
                { SystemBody.PlanetType.Moon, 0.3 },
                { SystemBody.PlanetType.IceMoon, 0 },
                { SystemBody.PlanetType.DwarfPlanet, 0 },
                { SystemBody.PlanetType.Asteroid, 0 },
                { SystemBody.PlanetType.Comet, 0 },
            };

        /// <summary>
        /// This value is used to determin if a planet gets moons. if a random number between 0 and 1 is less then this number then the planet geets moons.
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, double> MoonGenerationChanceByPlanetType = new Dictionary<SystemBody.PlanetType, double>()
            {
                { SystemBody.PlanetType.GasGiant, 0.99999999 },
                { SystemBody.PlanetType.IceGiant, 0.99999999 },
                { SystemBody.PlanetType.GasDwarf, 0.99 },
                { SystemBody.PlanetType.Terrestrial, 0.5 },
                { SystemBody.PlanetType.DwarfPlanet, 0.0001 },
            };

        /// <summary>
        /// The maximum number of moons a planet of a given type can have. 
        /// The bigger the planets the more moons it can have and the closer it will get to having the maximum number.
        /// @note Given the way the calculation for max moons is done it is unlikly that any planet will ever have the maximum number of moon, so pad as desired.
        /// </summary>
        public static Dictionary<SystemBody.PlanetType, double> MaxNoOfMoonsByPlanetType = new Dictionary<SystemBody.PlanetType, double>()
        {
                { SystemBody.PlanetType.GasGiant, 20 },
                { SystemBody.PlanetType.IceGiant, 15 },
                { SystemBody.PlanetType.GasDwarf, 8 },
                { SystemBody.PlanetType.Terrestrial, 4 },
                { SystemBody.PlanetType.DwarfPlanet, 1 },
        };

        /// <summary>
        /// These are the maxinum thresholds fore each type of tectonic activity a planet can have.
        /// Tectonic activity is calculated by Mass (in earth masses) / Star Age. 
        /// Earth has a tectonic activity of 0.217 by this calculation.
        /// So if the tectonic activing number is < the threshold of Earth like but greater than Minor then it will be Earth like.
        /// </summary>
        public static Dictionary<SystemBody.TectonicActivity, double> BodyTectonicsThresholds = new Dictionary<SystemBody.TectonicActivity, double>()
        {
                { SystemBody.TectonicActivity.Dead , 0.01 },
                { SystemBody.TectonicActivity.Minor , 0.2 },
                { SystemBody.TectonicActivity.EarthLike , 0.4 },
                { SystemBody.TectonicActivity.Major , 1 }          // Not used, just here for completness.
        };

        #endregion

        #region Ruins Generation

        /// <summary>
        /// The chance that ruins will be generated on a suitable planet or moon.
        /// @note A suitable planet/moon includes an atmosphere between 2.5 and 0.01 atm. 
        /// </summary>
        public const double RuinsGenerationChance = 0.5;

        /// <summary>
        /// The chance of any given ruins size being generated.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public static WeightedList<Ruins.RSize> RuinsSizeDisrubution = new WeightedList<Ruins.RSize>()
        {
            { 40, Ruins.RSize.Outpost },
            { 30, Ruins.RSize.Settlement },
            { 20, Ruins.RSize.Colony },
            { 10, Ruins.RSize.City }
        };

        /// <summary>
        /// The chance of any given ruins quility being generated. 
        /// @note There is some special adiyional logic for Ruins.RQuality.MultipleIntact.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public static WeightedList<Ruins.RQuality> RuinsQuilityDisrubution = new WeightedList<Ruins.RQuality>()
        {
            { 40, Ruins.RQuality.Destroyed },
            { 30, Ruins.RQuality.Ruined },
            { 15, Ruins.RQuality.PartiallyIntact },
            { 15, Ruins.RQuality.Intact } 
        };

        /// <summary>
        /// The ranges for the Ruins Count, by Ruins Size.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public static Dictionary<Ruins.RSize, MinMaxStruct> RuinsCountRangeBySize = new Dictionary<Ruins.RSize, MinMaxStruct>()
        {
            { Ruins.RSize.Outpost, new MinMaxStruct() { _min = 15, _max = 50 } },
            { Ruins.RSize.Settlement, new MinMaxStruct() { _min = 50, _max = 100 } },
            { Ruins.RSize.Colony, new MinMaxStruct() { _min = 100, _max = 200 } },
            { Ruins.RSize.City, new MinMaxStruct() { _min = 500, _max = 1000 } },
        };

        /// <summary>
        /// The Quility modifiers. Final Ruins count is determined by RuinsCount * QuilityModifier.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public static Dictionary<Ruins.RQuality, double> RuinsQuilityAdjustment = new Dictionary<Ruins.RQuality, double>()
        {
            { Ruins.RQuality.Destroyed, 1.25 },
            { Ruins.RQuality.Ruined, 1.5 },
            { Ruins.RQuality.PartiallyIntact, 1.75 },
            { Ruins.RQuality.Intact, 2.0 },
            { Ruins.RQuality.MultipleIntact, 3.0 }
        };

        #endregion
    }
}

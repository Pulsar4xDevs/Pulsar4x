using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public enum SystemBand
    {
        InnerBand,
        HabitableBand,
        OuterBand,
    };

    public static class GalaxyFactory
    {
        public static Random SeedRNG;

        public static class Settings
        {
            /// <summary>
            /// Indicates weither We shoudl generate a Real Star System or a more gamey one.
            /// </summary>
            public static bool RealStarSystems = false;

            /// <summary>
            /// The chance of a Non-player Race being generated on a suitable planet.
            /// </summary>
            public static double NPRGenerationChance = 0.3333;

            #region Advanced Star Generation Parameters

            // Note that the data is this section is largly based on scientific fact
            // See: http://en.wikipedia.org/wiki/Stellar_classification
            // these values SHOULD NOT be Modified if you weant sane star generation.
            // Also note that thile these are constants they were not added to the 
            // constants file because they are only used for star gen.

            /// <summary>
            /// Distribution of differnt stra spectral types. This is based on actuall numbers in real life.
            /// See: http://en.wikipedia.org/wiki/Stellar_classification
            /// </summary>
            public static WeightedList<SpectralType> StarTypeDistributionForRealStars = new WeightedList<SpectralType>
            {
                {0.00003, SpectralType.O},
                {0.13, SpectralType.B},
                {0.6, SpectralType.A},
                {3, SpectralType.F},
                {7.6, SpectralType.G},
                {12.1, SpectralType.K},
                {76.45, SpectralType.M},
                {0.11997, SpectralType.M} // reserved for more exotic star types
            };

            /// <summary>
            /// Distribution of differnt star spectral types. These numbers are made up and can be tweaked for game balance.
            /// </summary>
            public static WeightedList<SpectralType> StarTypeDistributionForFakeStars = new WeightedList<SpectralType>
            {
                {0, SpectralType.O},
                {0, SpectralType.B},
                {5, SpectralType.A},
                {15, SpectralType.F},
                {50, SpectralType.G},
                {15, SpectralType.K},
                {10, SpectralType.M},
                {5, SpectralType.M} // reserved for more exotic star types
            };

            /// <summary>
            /// This Dictionary holds the minium and maximum radius values (in AU) for a Star given its spectral type.
            /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> StarRadiusBySpectralType = new Dictionary<SpectralType, MinMaxStruct>
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 6.6 * GameSettings.Units.SolarRadiusInAu,
                    Max = 250 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 1.8 * GameSettings.Units.SolarRadiusInAu,
                    Max = 6.6 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 1.4 * GameSettings.Units.SolarRadiusInAu,
                    Max = 1.8 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 1.15 * GameSettings.Units.SolarRadiusInAu,
                    Max = 1.4 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.96 * GameSettings.Units.SolarRadiusInAu,
                    Max = 1.15 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.7 * GameSettings.Units.SolarRadiusInAu,
                    Max = 0.96 * GameSettings.Units.SolarRadiusInAu
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.12 * GameSettings.Units.SolarRadiusInAu,
                    Max = 0.7 * GameSettings.Units.SolarRadiusInAu
                }},
            };

            /// <summary>
            /// This Dictionary holds the minium and maximum Temperature (in degrees celsius) values for a Star given its spectral type.
            /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> StarTemperatureBySpectralType = new Dictionary<SpectralType, MinMaxStruct>
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 30000,
                    Max = 60000
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 10000,
                    Max = 30000
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 7500,
                    Max = 10000
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 6000,
                    Max = 7500
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 5200,
                    Max = 6000
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 3700,
                    Max = 5200
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 2400,
                    Max = 3700
                }},
            };

            /// <summary>
            /// This Dictionary holds the minium and maximum Luminosity (in Solar luminosity, i.e. Sol = 1). values for a Star given its spectral type.
            /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> StarLuminosityBySpectralType = new Dictionary<SpectralType, MinMaxStruct>
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 30000,
                    Max = 1000000
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 25,
                    Max = 30000
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 5,
                    Max = 25
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 1.5,
                    Max = 5
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.6,
                    Max = 1.5
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.08,
                    Max = 0.6
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.0001,
                    Max = 0.08
                }},
            };

            /// <summary>
            /// This Dictionary holds the minium and maximum mass values (in Kg) for a Star given its spectral type.
            /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> StarMassBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 16 * GameSettings.Units.SolarMassInKG,
                    Max = 265 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 2.1 * GameSettings.Units.SolarMassInKG,
                    Max = 16 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 1.4 * GameSettings.Units.SolarMassInKG,
                    Max = 2.1 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 1.04 * GameSettings.Units.SolarMassInKG,
                    Max = 1.4 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.8 * GameSettings.Units.SolarMassInKG,
                    Max = 1.04 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.45 * GameSettings.Units.SolarMassInKG,
                    Max = 0.8 * GameSettings.Units.SolarMassInKG
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.08 * GameSettings.Units.SolarMassInKG,
                    Max = 0.45 * GameSettings.Units.SolarMassInKG
                }},
            };

            /// <summary>
            /// This Dictionary holds the minium and maximum Age values (in years) for a Star given its spectral type.
            /// @note Max age of a star in the Milky Way is 13.2 billion years, the age of the milky way. A star could be older 
            /// (like 100 billion years older if not for the fact that the universion is only about 14 billion years old) but then it wouldn't be in the milky way.
            /// This is used for both K and M type stars both of which can easly be older than the milky way).
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> StarAgeBySpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 0,
                    Max = 6000000
                }}, // after 6 million years O types eiother go nova or become B type stars.
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 0,
                    Max = 100000000
                }}, // could not find any info on B type ages, so i made it between O and A (100 million).
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 0,
                    Max = 350000000
                }}, // A type stars are always young, typicall a few hundred million years..
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 0,
                    Max = 3000000000
                }}, // Could not find any info again, chose a number between B and G stars (3 billion)
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0,
                    Max = 10000000000
                }}, // The life of a G class star is about 10 billion years.
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0,
                    Max = 13200000000
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0,
                    Max = 13200000000
                }},
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
            /// Asteriods are generate in belts, this controls the max number per belt. It cannot be larger than MaxNoOfAsteroids.
            /// </summary>
            public const int MaxNoOfAsteroidsPerBelt = 150;

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
            /// Minium number of comets each system will have. All systems will be guaranteed to have a least this many comets.
            /// </summary>
            public static int MiniumCometsPerSystem = 0;

            /// <summary>
            /// The Maximum number of comets per system. note that if MiniumCometsPerSystem > MaxNoOfComets then MiniumCometsPerSystem = MaxNoOfComets.
            /// </summary>
            public const int MaxNoOfComets = 25;

            /// <summary>
            /// Asteroids and Dwarf planets are generated in belts. To do this a single orbit is first generate as the 
            /// basis for the whole belt. Asteroids then apply a small gitter of + or - a percentage of the original orbit 
            /// (except MeanAnomaly, which is the starting point on the orbit. that is random). 
            /// The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
            /// </summary>
            public const double MaxAsteroidOrbitDeviation = 0.03;

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
            /// We must be OrbitGravityFactor less attracted to any other object ot be "cleared".
            /// <@ todo: Is this comment completely confusing?
            /// </summary>
            public const double OrbitGravityFactor = 20;

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
            public const double MinMoonOrbitMultiplier = 2.5;

            /// <summary>
            /// This is the Absolute maximum orbit of moons, in AU.
            /// @note The maximum may be RelativeMaxMoonOrbitDistance instead, but will never be more then this.
            /// </summary>
            public const double AbsoluteMaxMoonOrbitDistance = 60581692 / GameSettings.Units.KmPerAu;

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
                {SpectralType.O, 0.6},
                {SpectralType.B, 0.7},
                {SpectralType.A, 0.9},
                {SpectralType.F, 0.9},
                {SpectralType.G, 2.1},
                {SpectralType.K, 2.4},
                {SpectralType.M, 1.8},
            };

            /// <summary>
            /// Limits of SystemBody masses based on type. Units are Kg.
            /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
            /// </summary>
            public static Dictionary<BodyType, MinMaxStruct> SystemBodyMassByType = new Dictionary<BodyType, MinMaxStruct>()
            {
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 15 * GameSettings.Units.EarthMassInKG,
                    Max = 500 * GameSettings.Units.EarthMassInKG
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 5 * GameSettings.Units.EarthMassInKG,
                    Max = 30 * GameSettings.Units.EarthMassInKG
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 1 * GameSettings.Units.EarthMassInKG,
                    Max = 15 * GameSettings.Units.EarthMassInKG
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 0.05 * GameSettings.Units.EarthMassInKG,
                    Max = 5 * GameSettings.Units.EarthMassInKG
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 1E16,
                    Max = 1 * GameSettings.Units.EarthMassInKG
                }}, // note 1E16 is 1 nano earth mass.
                {BodyType.DwarfPlanet, new MinMaxStruct
                {
                    Min = 2E20,
                    Max = 5E23
                }},
                {BodyType.Asteroid, new MinMaxStruct
                {
                    Min = 1E15,
                    Max = 9E19
                }},
                {BodyType.Comet, new MinMaxStruct
                {
                    Min = 1E13,
                    Max = 9E14
                }},
            };

            /// <summary>
            /// Limits of a Planets density based on its type, in g/cm3
            /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
            /// </summary>
            public static Dictionary<BodyType, MinMaxStruct> SystemBodyDensityByType = new Dictionary<BodyType, MinMaxStruct>()
            {
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 0.5,
                    Max = 10
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 1,
                    Max = 5
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 1,
                    Max = 8
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 3,
                    Max = 8
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 1.4,
                    Max = 5
                }},
                {BodyType.DwarfPlanet, new MinMaxStruct
                {
                    Min = 1,
                    Max = 6
                }},
                {BodyType.Asteroid, new MinMaxStruct
                {
                    Min = 1,
                    Max = 6
                }},
                {BodyType.Comet, new MinMaxStruct
                {
                    Min = 0.25,
                    Max = 0.7
                }},
            };

            /// <summary>
            /// Orbital distance restrictions (i.e. SemiMajorAxis restrictions) for a planet based upon the type of star it is orbiting.
            /// Units are AU.
            /// @note These numbers, with the exception of G class stars, are based on habital zone calculations. They could be tweaked for gameplay.
            /// </summary>
            public static Dictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType = new Dictionary<SpectralType, MinMaxStruct>()
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 1,
                    Max = 200
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 0.5,
                    Max = 100
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 0.3,
                    Max = 90
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 0.2,
                    Max = 60
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.1,
                    Max = 40
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.01,
                    Max = 18
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.005,
                    Max = 9
                }},
            };

            /// <summary>
            /// The possible ranges for albedo for various planet types.
            /// @note These are WAGs roughly based on the albedo of bodies in our solar system. They couild be tweak for gameplay.
            /// </summary>
            public static Dictionary<BodyType, MinMaxStruct> PlanetAlbedoByType = new Dictionary<BodyType, MinMaxStruct>()
            {
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 0.5,
                    Max = 0.7
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 0.5,
                    Max = 0.7
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 0.3,
                    Max = 0.7
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 0.05,
                    Max = 0.5
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 0.05,
                    Max = 0.5
                }},
                {BodyType.DwarfPlanet, new MinMaxStruct
                {
                    Min = 0.05,
                    Max = 0.95
                }},
                {BodyType.Asteroid, new MinMaxStruct
                {
                    Min = 0.05,
                    Max = 0.15
                }},
                {BodyType.Comet, new MinMaxStruct
                {
                    Min = 0.95,
                    Max = 0.99
                }},
            };

            /// <summary>
            /// The possible range of values for different the magnetic field (aka Magnetosphere) of different planet types.
            /// In microtesla (uT).
            /// @note @note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They couild be tweak for gameplay.
            /// </summary>
            public static Dictionary<BodyType, MinMaxStruct> PlanetMagneticFieldByType = new Dictionary<BodyType, MinMaxStruct>()
            {
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 10,
                    Max = 2000
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 5,
                    Max = 50
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 0.1,
                    Max = 20
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 0.0001,
                    Max = 45
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 0.0001,
                    Max = 1
                }},
                {BodyType.DwarfPlanet, new MinMaxStruct
                {
                    Min = 0.00001,
                    Max = 0.0001
                }},
                {BodyType.Asteroid, new MinMaxStruct
                {
                    Min = 0.000001,
                    Max = 0.00001
                }},
                {BodyType.Comet, new MinMaxStruct
                {
                    Min = 0.0000001,
                    Max = 0.000001
                }},
            };

            /// <summary>
            /// This value is multiplied by (SystemBody Mass / Max Mass for SystemBody Type) i.e. a mass ratio, to get the chance of an atmosphere for this planet.
            /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
            /// </summary>
            public static Dictionary<BodyType, double> AtmosphereGenerationModifier = new Dictionary<BodyType, double>()
            {
                {BodyType.GasGiant, 100000000},
                {BodyType.IceGiant, 100000000},
                {BodyType.GasDwarf, 100000000},
                {BodyType.Terrestrial, 2},
                {BodyType.Moon, 0.5},
                {BodyType.DwarfPlanet, 0},
                {BodyType.Asteroid, 0},
                {BodyType.Comet, 0},
            };

            /// <summary>
            /// This value is used to determin if a planet gets moons. if a random number between 0 and 1 is less then this number then the planet geets moons.
            /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
            /// </summary>
            public static Dictionary<BodyType, double> MoonGenerationChanceByPlanetType = new Dictionary<BodyType, double>()
            {
                {BodyType.GasGiant, 0.99999999},
                {BodyType.IceGiant, 0.99999999},
                {BodyType.GasDwarf, 0.99},
                {BodyType.Terrestrial, 0.5},
                {BodyType.DwarfPlanet, 0.0001},
            };

            public static Dictionary<BodyType, double> MaxMoonOrbitDistanceByPlanetType = new Dictionary<BodyType, double>()
            {
                {BodyType.GasGiant, 60581692 / GameSettings.Units.KmPerAu}, // twice higest jupiter moon orbit
                {BodyType.IceGiant, 49285000 / GameSettings.Units.KmPerAu}, // twice Neptunes highest moon orbit
                {BodyType.GasDwarf, 6058169 / GameSettings.Units.KmPerAu}, // WAG
                {BodyType.Terrestrial, 1923740 / GameSettings.Units.KmPerAu}, // 5 * luna orbit.
                {BodyType.DwarfPlanet, 25000 / GameSettings.Units.KmPerAu}, // WAG
            };

            /// <summary>
            /// The maximum number of moons a planet of a given type can have. 
            /// The bigger the planets the more moons it can have and the closer it will get to having the maximum number.
            /// @note Given the way the calculation for max moons is done it is unlikly that any planet will ever have the maximum number of moon, so pad as desired.
            /// </summary>
            public static Dictionary<BodyType, double> MaxNoOfMoonsByPlanetType = new Dictionary<BodyType, double>()
            {
                {BodyType.GasGiant, 20},
                {BodyType.IceGiant, 15},
                {BodyType.GasDwarf, 8},
                {BodyType.Terrestrial, 4},
                {BodyType.DwarfPlanet, 1},
            };

            /// <summary>
            /// These are the maxinum thresholds fore each type of tectonic activity a planet can have.
            /// Tectonic activity is calculated by Mass (in earth masses) / Star Age. 
            /// Earth has a tectonic activity of 0.217 by this calculation.
            /// So if the tectonic activing number is < the threshold of Earth like but greater than Minor then it will be Earth like.
            /// </summary>
            public static Dictionary<TectonicActivity, double> BodyTectonicsThresholds = new Dictionary<TectonicActivity, double>()
            {
                {TectonicActivity.Dead, 0.01},
                {TectonicActivity.Minor, 0.2},
                {TectonicActivity.EarthLike, 0.4},
                {TectonicActivity.Major, 1} // Not used, just here for completness.
            };

            public static WeightedList<SystemBand> BandBodyWeight = new WeightedList<SystemBand>()
            {
                {0.3, SystemBand.InnerBand},
                {0.1, SystemBand.HabitableBand},
                {0.6, SystemBand.OuterBand},
            };

            private static WeightedList<BodyType> innerBandTypeWeights = new WeightedList<BodyType>()
            {
                {35, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {50, BodyType.Terrestrial},
            };

            private static WeightedList<BodyType> habitableBandTypeWeights = new WeightedList<BodyType>()
            {
                {25, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {60, BodyType.Terrestrial},
            };

            private static WeightedList<BodyType> outerBandTypeWeights = new WeightedList<BodyType>()
            {
                {15, BodyType.Asteroid},
                {20, BodyType.GasDwarf},
                {25, BodyType.GasGiant},
                {20, BodyType.IceGiant},
                {10, BodyType.Terrestrial},
            };

            public static Dictionary<SystemBand, WeightedList<BodyType>> BandBodyTypeWeight = new Dictionary<SystemBand, WeightedList<BodyType>>()
            {
                {SystemBand.InnerBand, innerBandTypeWeights},
                {SystemBand.HabitableBand, habitableBandTypeWeights},
                {SystemBand.OuterBand, outerBandTypeWeights}
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
            public static WeightedList<RuinsDB.RSize> RuinsSizeDisrubution = new WeightedList<RuinsDB.RSize>()
            {
                {40, RuinsDB.RSize.Outpost},
                {30, RuinsDB.RSize.Settlement},
                {20, RuinsDB.RSize.Colony},
                {10, RuinsDB.RSize.City}
            };

            /// <summary>
            /// The chance of any given ruins quility being generated. 
            /// @note There is some special adiyional logic for RuinsDB.RQuality.MultipleIntact.
            /// @note These values can be tweaked as desired for game play.
            /// </summary>
            public static WeightedList<RuinsDB.RQuality> RuinsQuilityDisrubution = new WeightedList<RuinsDB.RQuality>()
            {
                {40, RuinsDB.RQuality.Destroyed},
                {30, RuinsDB.RQuality.Ruined},
                {15, RuinsDB.RQuality.PartiallyIntact},
                {15, RuinsDB.RQuality.Intact}
            };

            /// <summary>
            /// The ranges for the Ruins Count, by Ruins Size.
            /// @note These values can be tweaked as desired for game play.
            /// </summary>
            public static Dictionary<RuinsDB.RSize, MinMaxStruct> RuinsCountRangeBySize = new Dictionary<RuinsDB.RSize, MinMaxStruct>()
            {
                {RuinsDB.RSize.Outpost, new MinMaxStruct
                {
                    Min = 15,
                    Max = 50
                }},
                {RuinsDB.RSize.Settlement, new MinMaxStruct
                {
                    Min = 50,
                    Max = 100
                }},
                {RuinsDB.RSize.Colony, new MinMaxStruct
                {
                    Min = 100,
                    Max = 200
                }},
                {RuinsDB.RSize.City, new MinMaxStruct
                {
                    Min = 500,
                    Max = 1000
                }},
            };

            /// <summary>
            /// The Quility modifiers. Final Ruins count is determined by RuinsCount * QuilityModifier.
            /// @note These values can be tweaked as desired for game play.
            /// </summary>
            public static Dictionary<RuinsDB.RQuality, double> RuinsQuilityAdjustment = new Dictionary<RuinsDB.RQuality, double>()
            {
                {RuinsDB.RQuality.Destroyed, 1.25},
                {RuinsDB.RQuality.Ruined, 1.5},
                {RuinsDB.RQuality.PartiallyIntact, 1.75},
                {RuinsDB.RQuality.Intact, 2.0},
                {RuinsDB.RQuality.MultipleIntact, 3.0}
            };

            #endregion
        }
    }
}
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
        public static Random SeedRNG = new Random();

        public static SystemGenSettingsSD Settings;

        /// <summary>
        /// This function initilises the Static Data struct Settings to some sane default values.
        /// </summary>
        public static void InitToDefaultSettings()
        {
            Settings.RealStarSystems = false;

            Settings.NPRGenerationChance = 0.3333;

            #region Advanced Star Generation Parameters

            // Note that the data is this section is largly based on scientific fact
            // See: http://en.wikipedia.org/wiki/Stellar_classification
            // These values SHOULD NOT be Modified if you want sane star generation.

            Settings.StarTypeDistributionForRealStars = new WeightedList<SpectralType>
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

            Settings.StarTypeDistributionForFakeStars = new WeightedList<SpectralType>
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

            // note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            Settings.StarRadiusBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
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

            // note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            Settings.StarTemperatureBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
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

            // note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            Settings.StarLuminosityBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
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

            // note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            Settings.StarMassBySpectralType = new JDictionary<SpectralType, MinMaxStruct>()
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

            Settings.StarAgeBySpectralType = new JDictionary<SpectralType, MinMaxStruct>()
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

            Settings.PlanetGenerationChance = 0.8;

            // Note that the actual maximum number of planets will be one less then this number.
            Settings.MaxNoOfPlanets = 25;

            Settings.MaxNoOfAsteroidsPerBelt = 150;

            Settings.MaxNoOfAsteroidBelts = 3;

            Settings.NumberOfAsteroidsPerDwarfPlanet = 20;

            Settings.MiniumCometsPerSystem = 0;

            Settings.MaxNoOfComets = 25;

            // The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
            Settings.MaxAsteroidOrbitDeviation = 0.03;

            Settings.MaxBodyInclination = 45; // degrees. used for orbits and axial tilt.

            Settings.MaxMoonMassRelativeToParentBody = 0.4;

            Settings.OrbitGravityFactor = 20;

            Settings.TerrestrialBodyTectonicActiviyChance = 0.5;

            // Epoch used when generating orbits for sol. There should be no reason to change this.
            Settings.J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

            Settings.MiniumPossibleDayLength = 6;

            Settings.MinMoonOrbitMultiplier = 2.5;

            // note These numbers can be tweaked as desired for gameplay. They affect the number of planets generated for a given star type.
            // note Other factors such as the stars lumosoty and mass are also taken into account. So these numbers may not make a whole lot of sense on the surface.
            Settings.StarSpecralTypePlanetGenerationRatio = new JDictionary<SpectralType, double>()
            {
                {SpectralType.O, 0.6},
                {SpectralType.B, 0.7},
                {SpectralType.A, 0.9},
                {SpectralType.F, 0.9},
                {SpectralType.G, 2.1},
                {SpectralType.K, 2.4},
                {SpectralType.M, 1.8},
            };

            // note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
            Settings.SystemBodyMassByType = new JDictionary<BodyType, MinMaxStruct>()
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

            // note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
            Settings.SystemBodyDensityByType = new JDictionary<BodyType, MinMaxStruct>()
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

            // note These numbers, with the exception of G class stars, are based on habital zone calculations. They could be tweaked for gameplay.
            Settings.OrbitalDistanceByStarSpectralType = new JDictionary<SpectralType, MinMaxStruct>()
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

            Settings.BodyEccentricityByType = new JDictionary<BodyType, MinMaxStruct>
            {
                {BodyType.Asteroid, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.Comet, new MinMaxStruct
                {
                    Min = 0.6,
                    Max = 0.8
                }},
                {BodyType.DwarfPlanet, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 0,
                    Max = 0.5
                }}
            };

            // note These are WAGs roughly based on the albedo of bodies in our solar system. They couild be tweak for gameplay.
            Settings.PlanetAlbedoByType = new JDictionary<BodyType, MinMaxStruct>
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

            // note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They couild be tweak for gameplay.
            Settings.PlanetMagneticFieldByType = new JDictionary<BodyType, MinMaxStruct>
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

            // note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
            Settings.AtmosphereGenerationModifier = new JDictionary<BodyType, double>
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

            // note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
            Settings.MoonGenerationChanceByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 0.99999999},
                {BodyType.IceGiant, 0.99999999},
                {BodyType.GasDwarf, 0.99},
                {BodyType.Terrestrial, 0.5},
                {BodyType.DwarfPlanet, 0.0001},
                {BodyType.Moon, -1},
            };

            Settings.MaxMoonOrbitDistanceByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 60581692 / GameSettings.Units.KmPerAu}, // twice higest jupiter moon orbit
                {BodyType.IceGiant, 49285000 / GameSettings.Units.KmPerAu}, // twice Neptunes highest moon orbit
                {BodyType.GasDwarf, 6058169 / GameSettings.Units.KmPerAu}, // WAG
                {BodyType.Terrestrial, 1923740 / GameSettings.Units.KmPerAu}, // 5 * luna orbit.
                {BodyType.DwarfPlanet, 25000 / GameSettings.Units.KmPerAu}, // WAG
            };

            // note Given the way the calculation for max moons is done it is unlikly that any planet will ever have the maximum number of moon, so pad as desired.
            Settings.MaxNoOfMoonsByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 20},
                {BodyType.IceGiant, 15},
                {BodyType.GasDwarf, 8},
                {BodyType.Terrestrial, 4},
                {BodyType.DwarfPlanet, 1},
            };

            Settings.BodyTectonicsThresholds = new JDictionary<TectonicActivity, double>
            {
                {TectonicActivity.Dead, 0.01},
                {TectonicActivity.Minor, 0.2},
                {TectonicActivity.EarthLike, 0.4},
                {TectonicActivity.Major, 1} // Not used, just here for completness.
            };

            Settings.BandBodyWeight = new WeightedList<SystemBand>
            {
                {0.3, SystemBand.InnerBand},
                {0.1, SystemBand.HabitableBand},
                {0.6, SystemBand.OuterBand},
            };

            Settings.InnerBandTypeWeights = new WeightedList<BodyType>()
            {
                {35, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {50, BodyType.Terrestrial},
            };

            Settings.HabitableBandTypeWeights = new WeightedList<BodyType>
            {
                {25, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {60, BodyType.Terrestrial},
            };

            Settings.OuterBandTypeWeights = new WeightedList<BodyType>
            {
                {15, BodyType.Asteroid},
                {20, BodyType.GasDwarf},
                {25, BodyType.GasGiant},
                {20, BodyType.IceGiant},
                {10, BodyType.Terrestrial},
            };

            #endregion

            #region Ruins Generation

            // note A suitable planet/moon includes an atmosphere between 2.5 and 0.01 atm. 
            Settings.RuinsGenerationChance = 0.5;

            // note These values can be tweaked as desired for game play.
            Settings.RuinsSizeDisrubution = new WeightedList<RuinsDB.RSize>()
            {
                {40, RuinsDB.RSize.Outpost},
                {30, RuinsDB.RSize.Settlement},
                {20, RuinsDB.RSize.Colony},
                {10, RuinsDB.RSize.City}
            };

            // note There is some special adiyional logic for RuinsDB.RQuality.MultipleIntact.
            // note These values can be tweaked as desired for game play.
            Settings.RuinsQuilityDisrubution = new WeightedList<RuinsDB.RQuality>()
            {
                {40, RuinsDB.RQuality.Destroyed},
                {30, RuinsDB.RQuality.Ruined},
                {15, RuinsDB.RQuality.PartiallyIntact},
                {15, RuinsDB.RQuality.Intact}
            };

            // note These values can be tweaked as desired for game play.
            Settings.RuinsCountRangeBySize = new JDictionary<RuinsDB.RSize, MinMaxStruct>()
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

            // note These values can be tweaked as desired for game play.
            Settings.RuinsQuilityAdjustment = new JDictionary<RuinsDB.RQuality, double>()
            {
                {RuinsDB.RQuality.Destroyed, 1.25},
                {RuinsDB.RQuality.Ruined, 1.5},
                {RuinsDB.RQuality.PartiallyIntact, 1.75},
                {RuinsDB.RQuality.Intact, 2.0},
                {RuinsDB.RQuality.MultipleIntact, 3.0}
            };


            Settings.MinMineralAccessibility = 0.1;

            Settings.MinHomeworldMineralAccessibility = 0.5;

            Settings.MinHomeworldMineralAmmount = 50000;

            Settings.HomeworldMineralAmmount = 100000;

            Settings.MineralGenerationChanceByBodyType = new JDictionary<BodyType, double>()
            {
                {BodyType.GasGiant, 0.4},
                {BodyType.IceGiant, 0.33},
                {BodyType.GasDwarf, 0.3},
                {BodyType.Terrestrial, 0.5},
                {BodyType.Moon, 0.15},
                {BodyType.DwarfPlanet, 0.15},
                {BodyType.Comet, 1},
                {BodyType.Asteroid, 0.1},
            };

            Settings.MaxMineralAmmountByBodyType = new JDictionary<BodyType, int>()
            {
                {BodyType.GasGiant, 100000000},
                {BodyType.IceGiant, 50000000},
                {BodyType.GasDwarf, 10000000},
                {BodyType.Terrestrial, 5000000},
                {BodyType.Moon, 1000000},
                {BodyType.DwarfPlanet, 500000},
                {BodyType.Comet, 100000},
                {BodyType.Asteroid, 50000},
            };

            #endregion
        }
    }
}
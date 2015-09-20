using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This static data struct holds all the modder tweakable setting used
    /// by system generation.
    /// </summary>
    /// <remarks> 
    /// Unlike other Static data this type is not stored in the StaticDataStore.
    /// Instead it is used to it is stored in GalaxyFactory.Settings.
    /// Note that some of these values can be modified by the Player when creating a new game,
    /// Thus these values may just be the "default" values provided to the player.
    /// Also note that some these values should not be modified if you want sane system generation,
    /// See comments specific values for details.
    /// WARNING: Not including weights/values for all possible enum values (Spectral type, body Type, etc.)
    /// could cause system generation to crash.
    /// </remarks>
    [StaticDataAttribute(false)]
    public struct SystemGenSettingsSD
    {
        public static SystemGenSettingsSD DefaultSettings
        {
            get { return GetDefaultSettings(); }
        }

        /// <summary>
        /// Indicates whether We should generate a Real Star System or a more gamey one.
        /// </summary>
        public bool RealStarSystems;

        /// <summary>
        /// The chance of a Non-player Race being generated on a suitable planet.
        /// </summary>
        public double NPRGenerationChance;

        #region Advanced Star Generation Parameters

        /// <summary>
        /// Distribution of differnt star spectral types. This is based on actual numbers in real life.
        /// See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public WeightedList<SpectralType> StarTypeDistributionForRealStars;

        /// <summary>
        /// Distribution of differnt star spectral types. These numbers are made up and can be tweaked for game balance.
        /// </summary>
        public WeightedList<SpectralType> StarTypeDistributionForFakeStars;

        /// <summary>
        /// This Dictionary holds the minium and maximum radius values (in AU) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> StarRadiusBySpectralType;

        /// <summary>
        /// This Dictionary holds the minium and maximum Temperature (in degrees celsius) values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> StarTemperatureBySpectralType;

        /// <summary>
        /// This Dictionary holds the minium and maximum Luminosity (in Solar luminosity, i.e. Sol = 1). values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> StarLuminosityBySpectralType;

        /// <summary>
        /// This Dictionary holds the minium and maximum mass values (in Kg) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> StarMassBySpectralType;

        /// <summary>
        /// This Dictionary holds the minium and maximum Age values (in years) for a Star given its spectral type.
        /// @note Max age of a star in the Milky Way is 13.2 billion years, the age of the milky way. A star could be older 
        /// (like 100 billion years older if not for the fact that the universion is only about 14 billion years old) but then it wouldn't be in the milky way.
        /// This is used for both K and M type stars both of which can easly be older than the milky way).
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> StarAgeBySpectralType;

        #endregion

        #region Advanced SystemBody and other Body Generation Parameters

        /// <summary>
        /// The chance Planets will be generated around a given star. A number between 0 and 1 (e.g. a 33% chance would be 0.33).
        /// </summary>
        public double PlanetGenerationChance;

        /// <summary>
        /// The maximum number of planets which will be generated.
        /// Note that the actual maximum number of planets will be one less then this number.
        /// </summary>
        public int MaxNoOfPlanets;

        /// <summary>
        /// Asteroids are generate in belts, this controls the max number per belt.
        /// </summary>
        public int MaxNoOfAsteroidsPerBelt;

        /// <summary>
        /// Asteroids are generated in belts, this controls the maximum number of belts.
        /// </summary>
        public int MaxNoOfAsteroidBelts;

        /// <summary>
        /// Used to compute the number of dwarf planets in a given Asteroid belt.
        /// The formular used is: NoOfAsteroidsInBelt / NumberOfAsteroidsPerDwarfPlanet = NoOfDwarfPlanets;
        /// Dwarf planets are always generated along with their asteriod belt. Its the whole "hasn't cleared its orbit" thing.
        /// </summary>
        public int NumberOfAsteroidsPerDwarfPlanet;

        /// <summary>
        /// Minium number of comets each system will have. All systems will be guaranteed to have a least this many comets.
        /// </summary>
        public int MiniumCometsPerSystem;

        /// <summary>
        /// The Maximum number of comets per system. Note that if MiniumCometsPerSystem > MaxNoOfComets then MiniumCometsPerSystem = MaxNoOfComets.
        /// </summary>
        public int MaxNoOfComets;

        /// <summary>
        /// Asteroids and Dwarf planets are generated in belts. To do this a single orbit is first generate as the 
        /// basis for the whole belt. Asteroids then apply a small fluctuation of + or - a percentage of the original orbit 
        /// (except MeanAnomaly, which is the starting point on the orbit. that is random). 
        /// The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
        /// </summary>
        public double MaxAsteroidOrbitDeviation;

        /// <summary>
        /// The maximum SystemBody orbit Inclination. Also used as the maximum orbital tilt.
        /// Angle in degrees.
        /// </summary>
        public double MaxBodyInclination;

        /// <summary>
        /// This controls the maximum moon mass relative to the parent body.
        /// </summary>
        public double MaxMoonMassRelativeToParentBody;

        /// <summary>
        /// We must be OrbitGravityFactor less attracted to any other object ot be "cleared".
        /// <@ todo: Is this comment completely confusing?
        /// </summary>
        public double OrbitGravityFactor;

        /// <summary>
        /// The chance a Terrestrial body will have some form of Tectonic activity.
        /// Note that very small/low mass bodies will still end up dead.
        /// </summary>
        public double TerrestrialBodyTectonicActivityChance;

        /// <summary>
        /// The minium possible length of a day for any system body, in hours.
        /// </summary>
        public int MiniumPossibleDayLength;

        /// <summary>
        /// Is timesed by the total radius of the moon and its parent to come up with a minium orbit distance for that body.
        /// </summary>
        public double MinMoonOrbitMultiplier;

        /// <summary>
        /// Controls how much the type of a star affects the generation of planets.
        /// @note These numbers can be tweaked as desired for gameplay. They affect the number of planets generated for a given star type.
        /// @note Other factors such as the stars lumosoty and mass are also taken into account. So these numbers may not make a whole lot of sense on the surface.
        /// </summary>
        public JDictionary<SpectralType, double> StarSpectralTypePlanetGenerationRatio;

        /// <summary>
        /// Limits of SystemBody masses based on type. Units are Kg.
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> SystemBodyMassByType;

        /// <summary>
        /// Limits of a Planets density based on its type, in g/cm3
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> SystemBodyDensityByType;

        /// <summary>
        /// Orbital distance restrictions (i.e. SemiMajorAxis restrictions) for a planet based upon the type of star it is orbiting.
        /// Units are AU.
        /// @note These numbers, with the exception of G class stars, are based on habitable zone calculations. They could be tweaked for gameplay.
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType;

        /// <summary>
        /// Possible ranges for eccentricity by body type.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> BodyEccentricityByType;

        /// <summary>
        /// The possible ranges for albedo for various planet types.
        /// @note These are WAGs roughly based on the albedo of bodies in our solar system. They could be tweak for gameplay.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> PlanetAlbedoByType;

        /// <summary>
        /// The possible range of values for different the magnetic field (aka Magnetosphere) of different planet types.
        /// In microtesla (uT).
        /// @note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They could be tweaked for gameplay.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> PlanetMagneticFieldByType;

        /// <summary>
        /// This value is multiplied by (SystemBody Mass / Earth's Mass), then clamped between 0 and 1, to get the chance of an atmosphere for this planet. 
        /// a result of 1 will always have an atmosphere (use large numbers to always produce 1), and a result of 0 will never gen an atmoisphere
        /// (use 0 to force this result). 
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
        /// @note On some body types this number can also impact how thick an atmosphere will be, the higher the pre-clamped value the thicker the atmosphere.
        /// </summary>
        public JDictionary<BodyType, double> AtmosphereGenerationModifier;

        /// <summary>
        /// This value is used to determine the percentage of generated atmoispheres that will have a Venus like atmosphere.
        /// It is further modified by the distace from the star, the closer planet the higher the chance.
        /// @note this number can be tweaked as desired for gameplay. They affect the chance of Venus like planets.
        /// </summary>
        public double RunawayGreenhouseEffectChance;

        /// <summary>
        /// This number is multiplyed by the generated atm of the body to produce the final atmospheric pressure.
        /// @note this number can be tweaked as desired for gameplay. it determins how high the prssure of venus like worlds ends up.
        /// </summary>
        public double RunawayGreenhouseEffectMultiplyer;

        /// <summary>
        /// Determins the minimum and maximum pressure of a generated atmosphere.
        /// </summary>
        public MinMaxStruct MinMaxAtmosphericPressure;

        /// <summary>
        /// This value is used to determin if a planet gets moons. If a random number between 0 and 1 is less then this number then the planet geets moons.
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
        /// </summary>
        public JDictionary<BodyType, double> MoonGenerationChanceByPlanetType;

        /// <summary>
        /// This is the maximium orbital distance of moons for each planet type.
        /// </summary>
        public JDictionary<BodyType, double> MaxMoonOrbitDistanceByPlanetType;

        /// <summary>
        /// The maximum number of moons a planet of a given type can have. 
        /// The bigger the planets the more moons it can have and the closer it will get to having the maximum number.
        /// @note Given the way the calculation for max moons is done it is unlikely that any planet will ever have the maximum number of moon, so pad as desired.
        /// </summary>
        public JDictionary<BodyType, double> MaxNoOfMoonsByPlanetType;

        /// <summary>
        /// These are the maximum thresholds fore each type of tectonic activity a planet can have.
        /// Tectonic activity is calculated by Mass (in earth masses) / Star Age. 
        /// Earth has a tectonic activity of 0.217 by this calculation.
        /// So if the tectonic activing number is less than the threshold of Earth like but greater than Minor then it will be Earth like.
        /// </summary>
        public JDictionary<TectonicActivity, double> BodyTectonicsThresholds;

        /// <summary>
        /// Determines how likely a body is to be generated in a given orbital band.
        /// </summary>
        public WeightedList<SystemBand> BandBodyWeight;

        /// <summary>
        /// Determines the chance of a given planet type being generated in the inner orbital band.
        /// </summary>
        public WeightedList<BodyType> InnerBandTypeWeights;

        /// <summary>
        /// Determines the chance of a given planet type being generated in the habitable orbital band.
        /// </summary>
        public WeightedList<BodyType> HabitableBandTypeWeights;

        /// <summary>
        /// Determines the chance of a given planet type being generated in the outer orbital band.
        /// </summary>
        public WeightedList<BodyType> OuterBandTypeWeights;

        /// <summary>
        /// Epoch used when generating orbits. There should be no reason to change this from default.
        /// </summary>
        public DateTime J2000;

        /// <summary>
        /// This is a small helper function which will return the weighted list for body type chances for a given orbital band.
        /// </summary>
        public WeightedList<BodyType> GetBandBodyTypeWeight(SystemBand systemBand)
        {
            switch (systemBand)
            {
                case SystemBand.InnerBand:
                    return InnerBandTypeWeights;
                    break;
                case SystemBand.HabitableBand:
                    return HabitableBandTypeWeights;
                    break;
                case SystemBand.OuterBand:
                default:
                    return OuterBandTypeWeights;
            }
        }

        #endregion

        #region Ruins Generation

        /// <summary>
        /// The chance that ruins will be generated on a suitable planet or moon.
        /// @note A suitable planet/moon includes an atmosphere between 2.5 and 0.01 atm. 
        /// </summary>
        public double RuinsGenerationChance;

        /// <summary>
        /// The chance of any given ruins size being generated.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public WeightedList<RuinsDB.RSize> RuinsSizeDistribution;

        /// <summary>
        /// The chance of any given ruins quality being generated. 
        /// @note There is some special additional logic for RuinsDB.RQuality.MultipleIntact.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public WeightedList<RuinsDB.RQuality> RuinsQualityDistribution;

        /// <summary>
        /// The ranges for the Ruins Count, by Ruins Size.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public JDictionary<RuinsDB.RSize, MinMaxStruct> RuinsCountRangeBySize;

        /// <summary>
        /// The Quality modifiers. Final Ruins count is determined by RuinsCount * QualityModifier.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public JDictionary<RuinsDB.RQuality, double> RuinsQualityAdjustment;

        #endregion

        #region Mineral Generation

        /// <summary>
        /// This is the minium Accessibility of generated minerals.
        /// This value is added onto the generated accessibility to make sure that 
        /// the accessibility is never too low.
        /// @note These values can be tweaked as desired for game play.
        /// @note Should be a value between 0 and 1.
        /// </summary>
        public double MinMineralAccessibility;

        /// <summary>
        /// This is the minium Accessibility of generated minerals for a player/NPR homeworld..
        /// This value is added onto the generated accessibility to make sure that 
        /// the accessibility is never too low.
        /// @note These values can be tweaked as desired for game play.
        /// @note Should be a value between 0 and 1.
        /// </summary>
        public double MinHomeworldMineralAccessibility;

        /// <summary>
        /// This is the minium ammount of generated minerals for a player/NPR homeworld.
        /// This value is added onto the generated ammount to make sure that 
        /// the accessibility is never too low.
        /// @note This value can be tweaked as desired for game play.
        /// </summary>
        public double MinHomeworldMineralAmmount;

        /// <summary>
        /// This value is multiplied to a generation chance for that mineral (mineral abundance * random number)
        /// to decide how much over the minium amount there should be for a given mineral.
        /// This value only applies to player/NPR homeworlds.
        /// @note This value can be tweaked as desired for game play.
        /// </summary>
        public double HomeworldMineralAmmount; 

        /// <summary>
        /// Defines the chance of a body generating minerals, if a generate chance value is higher 
        /// then these then the body will have minerals. 
        /// These values are used both for an initial check to see if any minerals will be on this body
        /// (This check is adjusted by mass, so larger bodies are more likely to succeded and have minerals).
        /// In addition these values are used for a check for the presence of each mineral (this check is
        /// adjusted by the bodies mass as well as the mineral abundance).
        /// @note These values can be tweaked as desired for game play.
        /// @note Should be a value between 0 and 1.
        /// </summary>
        public JDictionary<BodyType, double> MineralGenerationChanceByBodyType;

        /// <summary>
        /// Defines the maximum ammount of a mineral a given body type can support.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public JDictionary<BodyType, int> MaxMineralAmmountByBodyType;

        #endregion

        private static SystemGenSettingsSD GetDefaultSettings()
        {
            SystemGenSettingsSD settings = new SystemGenSettingsSD();

            settings.RealStarSystems = false;

            settings.NPRGenerationChance = 0.3333;

            #region Advanced Star Generation Parameters

            // Note that the data is this section is largely based on scientific fact
            // See: http://en.wikipedia.org/wiki/Stellar_classification
            // These values SHOULD NOT be Modified if you want sane star generation.

            settings.StarTypeDistributionForRealStars = new WeightedList<SpectralType>
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

            settings.StarTypeDistributionForFakeStars = new WeightedList<SpectralType>
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
            settings.StarRadiusBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 6.6 * GameConstants.Units.SolarRadiusInAu,
                    Max = 250 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 1.8 * GameConstants.Units.SolarRadiusInAu,
                    Max = 6.6 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 1.4 * GameConstants.Units.SolarRadiusInAu,
                    Max = 1.8 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 1.15 * GameConstants.Units.SolarRadiusInAu,
                    Max = 1.4 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.96 * GameConstants.Units.SolarRadiusInAu,
                    Max = 1.15 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.7 * GameConstants.Units.SolarRadiusInAu,
                    Max = 0.96 * GameConstants.Units.SolarRadiusInAu
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.12 * GameConstants.Units.SolarRadiusInAu,
                    Max = 0.7 * GameConstants.Units.SolarRadiusInAu
                }},
            };

            // note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
            settings.StarTemperatureBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
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
            settings.StarLuminosityBySpectralType = new JDictionary<SpectralType, MinMaxStruct>
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
            settings.StarMassBySpectralType = new JDictionary<SpectralType, MinMaxStruct>()
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 16 * GameConstants.Units.SolarMassInKG,
                    Max = 265 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 2.1 * GameConstants.Units.SolarMassInKG,
                    Max = 16 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 1.4 * GameConstants.Units.SolarMassInKG,
                    Max = 2.1 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.F, new MinMaxStruct
                {
                    Min = 1.04 * GameConstants.Units.SolarMassInKG,
                    Max = 1.4 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.G, new MinMaxStruct
                {
                    Min = 0.8 * GameConstants.Units.SolarMassInKG,
                    Max = 1.04 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.K, new MinMaxStruct
                {
                    Min = 0.45 * GameConstants.Units.SolarMassInKG,
                    Max = 0.8 * GameConstants.Units.SolarMassInKG
                }},
                {SpectralType.M, new MinMaxStruct
                {
                    Min = 0.08 * GameConstants.Units.SolarMassInKG,
                    Max = 0.45 * GameConstants.Units.SolarMassInKG
                }},
            };

            settings.StarAgeBySpectralType = new JDictionary<SpectralType, MinMaxStruct>()
            {
                {SpectralType.O, new MinMaxStruct
                {
                    Min = 0,
                    Max = 6000000
                }}, // after 6 million years O types either go nova or become B type stars.
                {SpectralType.B, new MinMaxStruct
                {
                    Min = 0,
                    Max = 100000000
                }}, // could not find any info on B type ages, so i made it between O and A (100 million).
                {SpectralType.A, new MinMaxStruct
                {
                    Min = 0,
                    Max = 350000000
                }}, // A type stars are always young, typical a few hundred million years..
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

            settings.PlanetGenerationChance = 0.8;

            // Note that the actual maximum number of planets will be one less then this number.
            settings.MaxNoOfPlanets = 25;

            settings.MaxNoOfAsteroidsPerBelt = 150;

            settings.MaxNoOfAsteroidBelts = 3;

            settings.NumberOfAsteroidsPerDwarfPlanet = 20;

            settings.MiniumCometsPerSystem = 0;

            settings.MaxNoOfComets = 25;

            // The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
            settings.MaxAsteroidOrbitDeviation = 0.03;

            settings.MaxBodyInclination = 0;//45; //degrees. used for orbits and axial tilt. ** 0, because I can't figure the math to draw a plan view of the system - se5a

            settings.MaxMoonMassRelativeToParentBody = 0.4;

            settings.OrbitGravityFactor = 20;

            settings.TerrestrialBodyTectonicActivityChance = 0.5;

            // Epoch used when generating orbits for sol. There should be no reason to change this.
            settings.J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

            settings.MiniumPossibleDayLength = 6;

            settings.MinMoonOrbitMultiplier = 2.5;

            // note These numbers can be tweaked as desired for gameplay. They affect the number of planets generated for a given star type.
            // note Other factors such as the stars luminosity and mass are also taken into account. So these numbers may not make a whole lot of sense on the surface.
            settings.StarSpectralTypePlanetGenerationRatio = new JDictionary<SpectralType, double>()
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
            settings.SystemBodyMassByType = new JDictionary<BodyType, MinMaxStruct>()
            {
                {BodyType.GasGiant, new MinMaxStruct
                {
                    Min = 15 * GameConstants.Units.EarthMassInKG,
                    Max = 500 * GameConstants.Units.EarthMassInKG
                }},
                {BodyType.IceGiant, new MinMaxStruct
                {
                    Min = 5 * GameConstants.Units.EarthMassInKG,
                    Max = 30 * GameConstants.Units.EarthMassInKG
                }},
                {BodyType.GasDwarf, new MinMaxStruct
                {
                    Min = 1 * GameConstants.Units.EarthMassInKG,
                    Max = 15 * GameConstants.Units.EarthMassInKG
                }},
                {BodyType.Terrestrial, new MinMaxStruct
                {
                    Min = 0.05 * GameConstants.Units.EarthMassInKG,
                    Max = 5 * GameConstants.Units.EarthMassInKG
                }},
                {BodyType.Moon, new MinMaxStruct
                {
                    Min = 1E16,
                    Max = 1 * GameConstants.Units.EarthMassInKG
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
            settings.SystemBodyDensityByType = new JDictionary<BodyType, MinMaxStruct>()
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

            // note These numbers, with the exception of G class stars, are based on habitable zone calculations. They could be tweaked for gameplay.
            settings.OrbitalDistanceByStarSpectralType = new JDictionary<SpectralType, MinMaxStruct>()
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

            settings.BodyEccentricityByType = new JDictionary<BodyType, MinMaxStruct>
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

            // note These are WAGs roughly based on the albedo of bodies in our solar system. They could be tweak for gameplay.
            settings.PlanetAlbedoByType = new JDictionary<BodyType, MinMaxStruct>
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

            // note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They could be tweak for gameplay.
            settings.PlanetMagneticFieldByType = new JDictionary<BodyType, MinMaxStruct>
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
            settings.AtmosphereGenerationModifier = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 100000000},
                {BodyType.IceGiant, 100000000},
                {BodyType.GasDwarf, 100000000},
                {BodyType.Terrestrial, 1},
                {BodyType.Moon, 0.5},
                {BodyType.DwarfPlanet, 0},
                {BodyType.Asteroid, 0},
                {BodyType.Comet, 0},
            };

            // note that this number can be tweaked for gameplay. it affects the chance of venus like planets.
            settings.RunawayGreenhouseEffectChance = 0.25;

            settings.MinMaxAtmosphericPressure = new MinMaxStruct(0.000000001, 200);

            // note that this number can be tweaked for gameplay. It affects the chance of venus like planets.
            settings.RunawayGreenhouseEffectMultiplyer = 10;

            // note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
            settings.MoonGenerationChanceByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 0.99999999},
                {BodyType.IceGiant, 0.99999999},
                {BodyType.GasDwarf, 0.99},
                {BodyType.Terrestrial, 0.5},
                {BodyType.DwarfPlanet, 0.0001},
                {BodyType.Moon, -1},
            };

            settings.MaxMoonOrbitDistanceByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 60581692 / GameConstants.Units.KmPerAu}, // twice highest jupiter moon orbit
                {BodyType.IceGiant, 49285000 / GameConstants.Units.KmPerAu}, // twice Neptune's highest moon orbit
                {BodyType.GasDwarf, 6058169 / GameConstants.Units.KmPerAu}, // WAG
                {BodyType.Terrestrial, 1923740 / GameConstants.Units.KmPerAu}, // 5 * luna orbit.
                {BodyType.DwarfPlanet, 25000 / GameConstants.Units.KmPerAu}, // WAG
            };

            // note Given the way the calculation for max moons is done it is unlikely that any planet will ever have the maximum number of moon, so pad as desired.
            settings.MaxNoOfMoonsByPlanetType = new JDictionary<BodyType, double>
            {
                {BodyType.GasGiant, 20},
                {BodyType.IceGiant, 15},
                {BodyType.GasDwarf, 8},
                {BodyType.Terrestrial, 4},
                {BodyType.DwarfPlanet, 1},
            };

            settings.BodyTectonicsThresholds = new JDictionary<TectonicActivity, double>
            {
                {TectonicActivity.Dead, 0.01},
                {TectonicActivity.Minor, 0.2},
                {TectonicActivity.EarthLike, 0.4},
                {TectonicActivity.Major, 1} // Not used, just here for completeness.
            };

            settings.BandBodyWeight = new WeightedList<SystemBand>
            {
                {0.3, SystemBand.InnerBand},
                {0.1, SystemBand.HabitableBand},
                {0.6, SystemBand.OuterBand},
            };

            settings.InnerBandTypeWeights = new WeightedList<BodyType>()
            {
                {35, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {50, BodyType.Terrestrial},
            };

            settings.HabitableBandTypeWeights = new WeightedList<BodyType>
            {
                {25, BodyType.Asteroid},
                {10, BodyType.GasDwarf},
                {5, BodyType.GasGiant},
                {0, BodyType.IceGiant},
                {60, BodyType.Terrestrial},
            };

            settings.OuterBandTypeWeights = new WeightedList<BodyType>
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
            settings.RuinsGenerationChance = 0.5;

            // note These values can be tweaked as desired for game play.
            settings.RuinsSizeDistribution = new WeightedList<RuinsDB.RSize>()
            {
                {40, RuinsDB.RSize.Outpost},
                {30, RuinsDB.RSize.Settlement},
                {20, RuinsDB.RSize.Colony},
                {10, RuinsDB.RSize.City}
            };

            // note There is some special additional logic for RuinsDB.RQuality.MultipleIntact.
            // note These values can be tweaked as desired for game play.
            settings.RuinsQualityDistribution = new WeightedList<RuinsDB.RQuality>()
            {
                {40, RuinsDB.RQuality.Destroyed},
                {30, RuinsDB.RQuality.Ruined},
                {15, RuinsDB.RQuality.PartiallyIntact},
                {15, RuinsDB.RQuality.Intact}
            };

            // note These values can be tweaked as desired for game play.
            settings.RuinsCountRangeBySize = new JDictionary<RuinsDB.RSize, MinMaxStruct>()
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
            settings.RuinsQualityAdjustment = new JDictionary<RuinsDB.RQuality, double>()
            {
                {RuinsDB.RQuality.Destroyed, 1.25},
                {RuinsDB.RQuality.Ruined, 1.5},
                {RuinsDB.RQuality.PartiallyIntact, 1.75},
                {RuinsDB.RQuality.Intact, 2.0},
                {RuinsDB.RQuality.MultipleIntact, 3.0}
            };


            settings.MinMineralAccessibility = 0.1;

            settings.MinHomeworldMineralAccessibility = 0.5;

            settings.MinHomeworldMineralAmmount = 50000;

            settings.HomeworldMineralAmmount = 100000;

            settings.MineralGenerationChanceByBodyType = new JDictionary<BodyType, double>()
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

            settings.MaxMineralAmmountByBodyType = new JDictionary<BodyType, int>()
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

            return settings;
        }
    }
}
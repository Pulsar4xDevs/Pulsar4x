namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This static data struct holds all the modder tweakable setting used
    /// by system generation.
    /// </summary>
    /// <remarks> 
    /// Unlike other Static data this type is not stored in the StaticDataStore.
    /// Instead it is used to it is stored in GalaxyFactory.Settings.
    /// Note that some of these vaules can be modified by the Player when creating a new game,
    /// Thus these values may just be the "default" values provided to the player.
    /// Also note that some these values should not be modified if you want sane system generation,
    /// See comments specific values for details.
    /// WARNING: Not including weights/values for all possible enum values (Spectral type, body Type, etc.)
    /// could cause system generation to crash.
    /// </remarks>
    [StaticDataAttribute(false)]
    public struct SystemGenSettingsSD
    {
        /// <summary>
        /// Indicates weither We should generate a Real Star System or a more gamey one.
        /// </summary>
        public bool RealStarSystems;

        /// <summary>
        /// The chance of a Non-player Race being generated on a suitable planet.
        /// </summary>
        public double NPRGenerationChance;

        #region Advanced Star Generation Parameters

        /// <summary>
        /// Distribution of differnt star spectral types. This is based on actuall numbers in real life.
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
        /// Asteriods are generate in belts, this controls the max number per belt.
        /// </summary>
        public int MaxNoOfAsteroidsPerBelt;

        /// <summary>
        /// Asteriods are generated in belts, this controls the maximum number of belts.
        /// </summary>
        public int MaxNoOfAsteroidBelts;

        /// <summary>
        /// Used to compute the number of dwarf planets in a given steriod belt.
        /// The formular used is: NoOfAsteriodsInBelt / NumberOfAsteroidsPerDwarfPlanet = NoOfDwarfPlanets;
        /// Dwarf planets are always generated along with their asteriod belt. Its the whole "hasn't cleard its orbit" thing.
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
        /// basis for the whole belt. Asteroids then apply a small gitter of + or - a percentage of the original orbit 
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
        public double TerrestrialBodyTectonicActiviyChance;

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
        public JDictionary<SpectralType, double> StarSpecralTypePlanetGenerationRatio;

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
        /// @note These numbers, with the exception of G class stars, are based on habital zone calculations. They could be tweaked for gameplay.
        /// </summary>
        public JDictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType;

        /// <summary>
        /// Possible ranges for eccentricity by body type.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> BodyEccentricityByType;

        /// <summary>
        /// The possible ranges for albedo for various planet types.
        /// @note These are WAGs roughly based on the albedo of bodies in our solar system. They couild be tweak for gameplay.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> PlanetAlbedoByType;

        /// <summary>
        /// The possible range of values for different the magnetic field (aka Magnetosphere) of different planet types.
        /// In microtesla (uT).
        /// @note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They couild be tweaked for gameplay.
        /// </summary>
        public JDictionary<BodyType, MinMaxStruct> PlanetMagneticFieldByType;

        /// <summary>
        /// This value is multiplied by (SystemBody Mass / Max Mass for SystemBody Type) i.e. a mass ratio, to get the chance of an atmosphere for this planet.
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
        /// </summary>
        public JDictionary<BodyType, double> AtmosphereGenerationModifier;

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
        /// @note Given the way the calculation for max moons is done it is unlikly that any planet will ever have the maximum number of moon, so pad as desired.
        /// </summary>
        public JDictionary<BodyType, double> MaxNoOfMoonsByPlanetType;

        /// <summary>
        /// These are the maxinum thresholds fore each type of tectonic activity a planet can have.
        /// Tectonic activity is calculated by Mass (in earth masses) / Star Age. 
        /// Earth has a tectonic activity of 0.217 by this calculation.
        /// So if the tectonic activing number is less than the threshold of Earth like but greater than Minor then it will be Earth like.
        /// </summary>
        public JDictionary<TectonicActivity, double> BodyTectonicsThresholds;

        /// <summary>
        /// Determins how likly a body is to be generated in a given orbital band.
        /// </summary>
        public WeightedList<SystemBand> BandBodyWeight;

        /// <summary>
        /// Determins the chane of a given planet type being generated in the inner orbital band.
        /// </summary>
        public WeightedList<BodyType> InnerBandTypeWeights;

        /// <summary>
        /// Determins the chane of a given planet type being generated in the habitable orbital band.
        /// </summary>
        public WeightedList<BodyType> HabitableBandTypeWeights;

        /// <summary>
        /// Determins the chane of a given planet type being generated in the outer orbital band.
        /// </summary>
        public WeightedList<BodyType> OuterBandTypeWeights;

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
        public WeightedList<RuinsDB.RSize> RuinsSizeDisrubution;

        /// <summary>
        /// The chance of any given ruins quility being generated. 
        /// @note There is some special aditional logic for RuinsDB.RQuality.MultipleIntact.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public WeightedList<RuinsDB.RQuality> RuinsQuilityDisrubution;

        /// <summary>
        /// The ranges for the Ruins Count, by Ruins Size.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public JDictionary<RuinsDB.RSize, MinMaxStruct> RuinsCountRangeBySize;

        /// <summary>
        /// The Quility modifiers. Final Ruins count is determined by RuinsCount * QuilityModifier.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public JDictionary<RuinsDB.RQuality, double> RuinsQuilityAdjustment;

        #endregion
    }
}
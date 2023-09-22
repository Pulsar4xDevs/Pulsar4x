using System;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Blueprints
{
    public class SystemGenSettingsBlueprint : Blueprint
    {
        /// <summary>
        /// Indicates whether We should generate a Real Star System or a more gamey one.
        /// </summary>
        public bool RealStarSystems { get; set; }

        /// <summary>
        /// The chance of a Non-player Race being generated on a suitable planet.
        /// </summary>
        public double NPRGenerationChance { get; set; }

        /// <summary>
        /// Distribution of differnt star spectral types. This is based on actual numbers in real life.
        /// See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public WeightedList<SpectralType> StarTypeDistributionForRealStars { get; set; }

        /// <summary>
        /// Distribution of differnt star spectral types. These numbers are made up and can be tweaked for game balance.
        /// </summary>
        public WeightedList<SpectralType> StarTypeDistributionForFakeStars { get; set; }

        /// <summary>
        /// This Dictionary holds the minium and maximum radius values (in AU) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> StarRadiusBySpectralType { get; set; }

        /// <summary>
        /// This Dictionary holds the minium and maximum Temperature (in degrees celsius) values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> StarTemperatureBySpectralType { get; set; }

        /// <summary>
        /// This Dictionary holds the minium and maximum Luminosity (in Solar luminosity, i.e. Sol = 1). values for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> StarLuminosityBySpectralType { get; set; }

        /// <summary>
        /// This Dictionary holds the minium and maximum mass values (in Kg) for a Star given its spectral type.
        /// @note Do Not Modify these values as they are based on SCIENCE!!! See: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> StarMassBySpectralType { get; set; }

        /// <summary>
        /// This Dictionary holds the minium and maximum Age values (in years) for a Star given its spectral type.
        /// @note Max age of a star in the Milky Way is 13.2 billion years, the age of the milky way. A star could be older
        /// (like 100 billion years older if not for the fact that the universion is only about 14 billion years old) but then it wouldn't be in the milky way.
        /// This is used for both K and M type stars both of which can easly be older than the milky way).
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> StarAgeBySpectralType { get; set; }

        /// <summary>
        /// The chance Planets will be generated around a given star. A number between 0 and 1 (e.g. a 33% chance would be 0.33).
        /// </summary>
        public double PlanetGenerationChance { get; set; }

        /// <summary>
        /// The maximum number of planets which will be generated.
        /// Note that the actual maximum number of planets will be one less then this number.
        /// </summary>
        public int MaxNoOfPlanets { get; set; }

        /// <summary>
        /// Asteroids are generate in belts, this controls the max number per belt.
        /// </summary>
        public int MaxNoOfAsteroidsPerBelt { get; set; }

        /// <summary>
        /// Asteroids are generated in belts, this controls the maximum number of belts.
        /// </summary>
        public int MaxNoOfAsteroidBelts { get; set; }

        /// <summary>
        /// Used to compute the number of dwarf planets in a given Asteroid belt.
        /// The formular used is: NoOfAsteroidsInBelt / NumberOfAsteroidsPerDwarfPlanet = NoOfDwarfPlanets;
        /// Dwarf planets are always generated along with their asteriod belt. Its the whole "hasn't cleared its orbit" thing.
        /// </summary>
        public int NumberOfAsteroidsPerDwarfPlanet { get; set; }

        /// <summary>
        /// Minium number of comets each system will have. All systems will be guaranteed to have a least this many comets.
        /// </summary>
        public int MiniumCometsPerSystem { get; set; }

        /// <summary>
        /// The Maximum number of comets per system. Note that if MiniumCometsPerSystem > MaxNoOfComets then MiniumCometsPerSystem = MaxNoOfComets.
        /// </summary>
        public int MaxNoOfComets { get; set; }

        /// <summary>
        /// Asteroids and Dwarf planets are generated in belts. To do this a single orbit is first generate as the
        /// basis for the whole belt. Asteroids then apply a small fluctuation of + or - a percentage of the original orbit
        /// (except MeanAnomaly, which is the starting point on the orbit. that is random).
        /// The value is a percentage as a number between 0 and 1, tho typically it should be less than 10% (or 0.1).
        /// </summary>
        public double MaxAsteroidOrbitDeviation { get; set; }

        /// <summary>
        /// The maximum SystemBody orbit Inclination. Also used as the maximum orbital tilt.
        /// Angle in degrees.
        /// </summary>
        public double MaxBodyInclination { get; set; }

        /// <summary>
        /// This controls the maximum moon mass relative to the parent body.
        /// </summary>
        public double MaxMoonMassRelativeToParentBody { get; set; }

        /// <summary>
        /// We must be OrbitGravityFactor less attracted to any other object ot be "cleared".
        /// <@ todo: Is this comment completely confusing?
        /// </summary>
        public double OrbitGravityFactor { get; set; }

        /// <summary>
        /// The chance a Terrestrial body will have some form of Tectonic activity.
        /// Note that very small/low mass bodies will still end up dead.
        /// </summary>
        public double TerrestrialBodyTectonicActivityChance { get; set; }

        /// <summary>
        /// The minium possible length of a day for any system body, in hours.
        /// </summary>
        public int MiniumPossibleDayLength { get; set; }

        /// <summary>
        /// Is timesed by the total radius of the moon and its parent to come up with a minium orbit distance for that body.
        /// </summary>
        public double MinMoonOrbitMultiplier { get; set; }

        /// <summary>
        /// Controls how much the type of a star affects the generation of planets.
        /// @note These numbers can be tweaked as desired for gameplay. They affect the number of planets generated for a given star type.
        /// @note Other factors such as the stars lumosoty and mass are also taken into account. So these numbers may not make a whole lot of sense on the surface.
        /// </summary>
        public Dictionary<SpectralType, double> StarSpectralTypePlanetGenerationRatio { get; set; }

        /// <summary>
        /// Limits of SystemBody masses based on type. Units are Kg.
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public Dictionary<BodyType, MinMaxStruct> SystemBodyMassByType { get; set; }

        /// <summary>
        /// Limits of a Planets density based on its type, in g/cm3
        /// @note That these values are based on bodies in our solar system and discovered Exoplanets. Some adjustment can be made for game play.
        /// </summary>
        public Dictionary<BodyType, MinMaxStruct> SystemBodyDensityByType { get; set; }

        /// <summary>
        /// Orbital distance restrictions (i.e. SemiMajorAxis restrictions) for a planet based upon the type of star it is orbiting.
        /// Units are AU.
        /// @note These numbers, with the exception of G class stars, are based on habitable zone calculations. They could be tweaked for gameplay.
        /// </summary>
        public Dictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType_AU { get; set; }


		/// <summary>
		/// Orbital distance restrictions (i.e. SemiMajorAxis restrictions) for a planet based upon the type of star it is orbiting.
		/// Units are AU.
		/// @note These numbers, with the exception of G class stars, are based on habitable zone calculations. They could be tweaked for gameplay.
		/// </summary>
		public Dictionary<SpectralType, MinMaxStruct> OrbitalDistanceByStarSpectralType { get; set; }

		/// <summary>
		/// Possible ranges for eccentricity by body type.
		/// </summary>
		public Dictionary<BodyType, MinMaxStruct> BodyEccentricityByType { get; set; }

        /// <summary>
        /// The possible ranges for albedo for various planet types.
        /// @note These are WAGs roughly based on the albedo of bodies in our solar system. They could be tweak for gameplay.
        /// </summary>
        public Dictionary<BodyType, MinMaxStruct> PlanetAlbedoByType { get; set; }

        /// <summary>
        /// The possible range of values for different the magnetic field (aka Magnetosphere) of different planet types.
        /// In microtesla (uT).
        /// @note These are WAGs roughly based on the Magnetosphere of bodies in our solar system. They could be tweaked for gameplay.
        /// </summary>
        public Dictionary<BodyType, MinMaxStruct> PlanetMagneticFieldByType { get; set; }

        /// <summary>
        /// This value is multiplied by (SystemBody Mass / Earth's Mass), then clamped between 0 and 1, to get the chance of an atmosphere for this planet.
        /// a result of 1 will always have an atmosphere (use large numbers to always produce 1), and a result of 0 will never gen an atmoisphere
        /// (use 0 to force this result).
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of atmosphere generation.
        /// @note On some body types this number can also impact how thick an atmosphere will be, the higher the pre-clamped value the thicker the atmosphere.
        /// </summary>
        public Dictionary<BodyType, double> AtmosphereGenerationModifier { get; set; }

        /// <summary>
        /// This value is used to determine the percentage of generated atmoispheres that will have a Venus like atmosphere.
        /// It is further modified by the distace from the star, the closer planet the higher the chance.
        /// @note this number can be tweaked as desired for gameplay. They affect the chance of Venus like planets.
        /// </summary>
        public double RunawayGreenhouseEffectChance { get; set; }

        /// <summary>
        /// This number is multiplyed by the generated atm of the body to produce the final atmospheric pressure.
        /// @note this number can be tweaked as desired for gameplay. it determins how high the prssure of venus like worlds ends up.
        /// </summary>
        public double RunawayGreenhouseEffectMultiplyer { get; set; }

        /// <summary>
        /// Determins the minimum and maximum pressure of a generated atmosphere.
        /// </summary>
        public MinMaxStruct MinMaxAtmosphericPressure { get; set; }

        /// <summary>
        /// This value is used to determin if a planet gets moons. If a random number between 0 and 1 is less then this number then the planet geets moons.
        /// @note These numbers can be tweaked as desired for gameplay. They effect the chances of a planet having moons.
        /// </summary>
        public Dictionary<BodyType, double> MoonGenerationChanceByPlanetType { get; set; }

        /// <summary>
        /// This is the maximium orbital distance of moons for each planet type.
        /// </summary>
        public Dictionary<BodyType, double> MaxMoonOrbitDistanceByPlanetType { get; set; }

        /// <summary>
        /// The maximum number of moons a planet of a given type can have.
        /// The bigger the planets the more moons it can have and the closer it will get to having the maximum number.
        /// @note Given the way the calculation for max moons is done it is unlikely that any planet will ever have the maximum number of moon, so pad as desired.
        /// </summary>
        public Dictionary<BodyType, double> MaxNoOfMoonsByPlanetType { get; set; }

        /// <summary>
        /// These are the maximum thresholds fore each type of tectonic activity a planet can have.
        /// Tectonic activity is calculated by Mass (in planet masses) / Star Age.
        /// Earth has a tectonic activity of 0.217 by this calculation.
        /// So if the tectonic activing number is less than the threshold of Earth like but greater than Minor then it will be Earth like.
        /// </summary>
        public Dictionary<TectonicActivity, double> BodyTectonicsThresholds { get; set; }

        /// <summary>
        /// Determines how likely a body is to be generated in a given orbital band.
        /// </summary>
        public WeightedList<SystemBand> BandBodyWeight { get; set; }

        /// <summary>
        /// Determines the chance of a given planet type being generated in the inner orbital band.
        /// </summary>
        public WeightedList<BodyType> InnerBandTypeWeights { get; set; }

        /// <summary>
        /// Determines the chance of a given planet type being generated in the habitable orbital band.
        /// </summary>
        public WeightedList<BodyType> HabitableBandTypeWeights { get; set; }

        /// <summary>
        /// Determines the chance of a given planet type being generated in the outer orbital band.
        /// </summary>
        public WeightedList<BodyType> OuterBandTypeWeights { get; set; }

        /// <summary>
        /// Epoch used when generating orbits. There should be no reason to change this from default.
        /// </summary>
        public DateTime J2000 { get; set; }

        /// <summary>
        /// The chance that ruins will be generated on a suitable planet or moon.
        /// @note A suitable planet/moon includes an atmosphere between 2.5 and 0.01 atm.
        /// </summary>
        public double RuinsGenerationChance { get; set; }

        /// <summary>
        /// The chance of any given ruins size being generated.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public WeightedList<RuinsDB.RSize> RuinsSizeDistribution { get; set; }

        /// <summary>
        /// The chance of any given ruins quality being generated.
        /// @note There is some special additional logic for RuinsDB.RQuality.MultipleIntact.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public WeightedList<RuinsDB.RQuality> RuinsQualityDistribution { get; set; }

        /// <summary>
        /// The ranges for the Ruins Count, by Ruins Size.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public Dictionary<RuinsDB.RSize, MinMaxStruct> RuinsCountRangeBySize { get; set; }

        /// <summary>
        /// The Quality modifiers. Final Ruins count is determined by RuinsCount * QualityModifier.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public Dictionary<RuinsDB.RQuality, double> RuinsQualityAdjustment { get; set; }

        /// <summary>
        /// This is the minium Accessibility of generated minerals.
        /// This value is added onto the generated accessibility to make sure that
        /// the accessibility is never too low.
        /// @note These values can be tweaked as desired for game play.
        /// @note Should be a value between 0 and 1.
        /// </summary>
        public double MinMineralAccessibility { get; set; }

        /// <summary>
        /// This is the minium Accessibility of generated minerals for a player/NPR homeworld..
        /// This value is added onto the generated accessibility to make sure that
        /// the accessibility is never too low.
        /// @note These values can be tweaked as desired for game play.
        /// @note Should be a value between 0 and 1.
        /// </summary>
        public double MinHomeworldMineralAccessibility { get; set; }

        /// <summary>
        /// This is the minium ammount of generated minerals for a player/NPR homeworld.
        /// This value is added onto the generated ammount to make sure that
        /// the accessibility is never too low.
        /// @note This value can be tweaked as desired for game play.
        /// </summary>
        public double MinHomeworldMineralAmmount { get; set; }

        /// <summary>
        /// This value is multiplied to a generation chance for that mineral (mineral abundance * random number)
        /// to decide how much over the minium amount there should be for a given mineral.
        /// This value only applies to player/NPR homeworlds.
        /// @note This value can be tweaked as desired for game play.
        /// </summary>
        public double HomeworldMineralAmmount { get; set; }

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
        public Dictionary<BodyType, double> MineralGenerationChanceByBodyType { get; set; }

        /// <summary>
        /// Defines the maximum ammount of a mineral a given body type can support.
        /// @note These values can be tweaked as desired for game play.
        /// </summary>
        public Dictionary<BodyType, int> MaxMineralAmmountByBodyType { get; set; }
    }
}
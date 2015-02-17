using Pulsar4X.Entities;
using Pulsar4X.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X
{
    public static class SystemGen
    {
        /// <summary>
        /// This is the RNG that should be used when generating a system. It will be seeded either by using m_SeedRNG or by a seed passed to CreateSystem().
        /// </summary>
        private static Random m_RNG;

        /// <summary>
        /// Conveinance Class used for passing around the generated data for a star before actually populating the star with this data.
        /// </summary>
        private struct StarData
        {
            public SpectralType _SpectralType;
            public double _Radius;
            public uint _Temp;
            public float _Luminosity;
            public double _Age;
            public double _Mass;
        }

        public static StarSystem CreateSystem(string name)
        {
            return CreateSystem(name, GalaxyGen.SeedRNG.Next());
        }

        public static StarSystem CreateSystem(string name, int seed)
        {
            // create new RNG with Seed.
            m_RNG = new Random(seed);

            StarSystem newSystem = new StarSystem(name, seed);

            int noOfStars = m_RNG.Next(1, 5);
            for (int i = 0; i < noOfStars; ++i)
            {
                GenerateStar(newSystem);
            }

            // Clean up cached RNG:
            m_RNG = null;

            GameState.Instance.StarSystems.Add(newSystem);
            GameState.Instance.StarSystemCurrentIndex++;
            return newSystem;
        }

        #region Create Sol

        public static StarSystem CreateSol()
        {
            StarSystem Sol = new StarSystem("Sol", -1);

            Star Sun = new Star("Sol", Constants.Units.SOLAR_RADIUS_IN_AU, 5778, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Mass = Constants.Units.SOLAR_MASS_IN_KILOGRAMS;
            Sun.Orbit = Orbit.FromStationary();
            Sun.Class = "G2";

            Sun.Radius = 696000 / Constants.Units.KM_PER_AU;

            Sol.Stars.Add(Sun);

            DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

            Planet Mercury = new Planet(Sun);
            Mercury.Name = "Mercury";
            Mercury.Mass = 3.3022E23;
            Mercury.Orbit = Orbit.FromMajorPlanetFormat(Mercury.Mass, Sun.Mass, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, J2000);

            Mercury.Radius = 2439.7 / Constants.Units.KM_PER_AU;

            double x, y;
            Mercury.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Mercury.Position.System = Sol;
            Mercury.Position.X = x;
            Mercury.Position.Y = y;

            Sun.Planets.Add(Mercury);

            Planet Venus = new Planet(Sun);
            Venus.Name = "Venus";
            Venus.Mass = 4.8676E24;
            Venus.Orbit = Orbit.FromMajorPlanetFormat(Venus.Mass, Sun.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, J2000);

            Venus.Radius = 6051.8 / Constants.Units.KM_PER_AU;

            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;


            Sun.Planets.Add(Venus);

            Planet Earth = new Planet(Sun);
            Earth.Name = "Earth";
            Earth.Mass = 5.9726E24;
            Earth.Orbit = Orbit.FromMajorPlanetFormat(Earth.Mass, Sun.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, J2000);

            Earth.Radius = 6378.1 / Constants.Units.KM_PER_AU;

            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;

            Sun.Planets.Add(Earth);

            Planet Moon = new Planet(Earth);
            Moon.Name = "Moon";
            Moon.Mass = 0.073E24;
            Moon.Orbit = Orbit.FromAsteroidFormat(Moon.Mass, Earth.Mass, 384748 / Constants.Units.KM_PER_AU, 0.0549006, 0, 0, 0, 0, J2000);

            Moon.Radius = 1738.14 / Constants.Units.KM_PER_AU;

            Moon.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Moon.Position.System = Sol;
            Moon.Position.X = Earth.Position.X + x;
            Moon.Position.Y = Earth.Position.Y + y;

            Earth.Moons.Add(Moon);

            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            return Sol;
        }

        #endregion

        #region Star Generation Functions

        /// <summary>
        /// Generates a new star and adds it to the provided Star System.
        /// </summary>
        /// <param name="System">The Star System the new Star belongs to.</param>
        /// <returns>A reference to the new Star (in case you need it)</returns>
        private static Star GenerateStar(StarSystem system)
        {
            // Generate star quick and dirty:
            SpectralType st = GenerateSpectralType();
          
            // generate the name:
            string starName = system.Name + " ";
            if (system.Stars.Count == 0)
                starName += "A";
            else if (system.Stars.Count == 1)
                starName += "B";
            else if (system.Stars.Count == 2)
                starName += "C";
            else if (system.Stars.Count == 3)
                starName += "D";
            else if (system.Stars.Count == 4)
                starName += "E";

            Star star = PopulateStarDataBasedOnSpectralType(st, starName, system);
            star.Orbit = Orbit.FromStationary();
            system.Stars.Add(star);
            return star;
        }

        /// <summary>
        /// Generates a Spectral Class for a star, See http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        /// <returns>A randomly generated Spectral type.</returns>
        private static SpectralType GenerateSpectralType()
        {
            double chance = m_RNG.NextDouble();

            // The odds of a system being generated are different depending on the weither we are talking real star suystems or not.
            if (GalaxyGen.RealStarSystems)
            {
                if (chance < 0.7645) // actual chance is 76.45%
                    return SpectralType.M;
                if (chance < 0.8855) // actual chace is 12.1%
                    return SpectralType.K;
                if (chance < 0.9615) // actual chance is 7.6%
                    return SpectralType.G;
                if (chance < 0.9915) // actual chance of 3%
                    return SpectralType.F;
                if (chance < 0.0075) // actual chance 0.6%
                    return SpectralType.A;
                if (chance < 0.9988) // actual; chance of 0.13%
                    return SpectralType.B;
                if (chance < 0.9989) // actual chance is ~0.00003% (so the number should be 0.9988003) but it is rounded up to give a slightly better chance.
                    return SpectralType.O;
            }
            else
            {
                // For fake star system we adjust the odds to give the player more G, F, A, B, O type stars. 
                // the handwavium for this is that JPs like attaching to the biggest stars!
                if (chance < 0.3333) // actual chance is 33.33%
                    return SpectralType.M;
                if (chance < 0.5833) // actual chace is 25%
                    return SpectralType.K;
                if (chance < 0.7833) // actual chance is 20%
                    return SpectralType.G;
                if (chance < 0.87) // actual chance of 8.67%
                    return SpectralType.F;
                if (chance < 0.91) // actual chance 4%
                    return SpectralType.A;
                if (chance < 0.935) // actual; chance of 2.5%
                    return SpectralType.B;
                if (chance < 0.95) // actual chance is 01.5%
                    return SpectralType.O;

                // Left 5% chance so we could have black wholes and suff :) at the moment it just flows to another M class star.
            }

            ///< @todo Support Other Spectral Class types, such as C & D. also things like Black holes??

            return SpectralType.M; // if in doubt it's an M class, as they are most common.
        }

        /// <summary>
        /// Generates Data for a star based on it's spectral type and populates it with the data.
        /// \note Does not generate a name for the star.
        /// \note This is not very scientific and that 'magic' the numbers are sourced from Wikipedia. mostly here: http://en.wikipedia.org/wiki/Stellar_classification
        /// </summary>
        /// <remarks>
        /// This function randomly generates the Radius, Temperature, Luminosity, Mass and Age of a star and then returns a star populated with those generated values.
        /// What follows is a breif description of how that is done for each data point:
        /// <list type="Bullet">
        /// <item>
        /// <b>Radius:</b> The radius of a star is generated by first getting a random number between 0 and 1 and then using that to pick a position in a range of 
        /// possible radii for a star of a given spectral type. 
        /// </item>
        /// <item>
        /// <b>Temperature:</b> The Temp. of the star is obtained by using the Randon.Next(min, max) function to get a random Temp. in the range a star of the given 
        /// spectral type.
        /// </item>
        /// <item>
        /// <b>Luminosity:</b> The Luminosity of a star is calculated by using the RNG_NextDoubleRange() function to get a random Luminosity in the range a star of the 
        /// given spectral type.
        /// </item>
        /// <item>
        /// <b>Mass:</b> The mass of a star is generated by first getting a random number between 0 and 1 and then using that to pick a position in a range of 
        /// possible Masses for a star of a given spectral type. 
        /// </item>
        /// <item>
        /// <b>Age:</b> The possible ages for a star depend largly on its mass. The bigger and heaver the star the more pressure is put on its core where fusion occure 
        /// which increases the rate that it burns Hydrodgen which reduces the life of the star. The Big O class stars only last a few million years before either 
        /// going Hyper Nova or devolving into a class B star. on the other hand a class G star (like Sol) has a life expectancy of about 10 billion years while a 
        /// little class M star could last 100 billion years or more (hard to tell given that the Milky way is 13.2 billion years old and the univers is only 
        /// about a billion years older then that). Given this we first use the mass of the star to produce a number between 0 and 1 that we can use to pick a 
        /// possible age from the range (just like all the above). To get the number between 0 and 1 we use the following formula:
        /// <c>1 - Mass / MaxMassOfStarOfThisType</c>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="spectralType">The Spectral Type of the star.</param>
        /// <param name="name">The Stars Name.</param>
        /// <param name="system">The Star System the star belongs to.</param>
        /// <returns>A star Populated with data generated based on Spectral Type provided.</returns>
        private static Star PopulateStarDataBasedOnSpectralType(SpectralType spectralType, string name, StarSystem system)
        {
            double maxStarAge = GalaxyGen.StarAgeBySpectralType[spectralType]._max;

            StarData data = new StarData();
            data._SpectralType = spectralType;
            data._Radius = RNG_NextDoubleRange(GalaxyGen.StarRadiusBySpectralType[spectralType]._min, GalaxyGen.StarRadiusBySpectralType[spectralType]._max);
            data._Temp = (uint)m_RNG.Next((int)GalaxyGen.StarTemperatureBySpectralType[spectralType]._min, (int)GalaxyGen.StarTemperatureBySpectralType[spectralType]._max);
            data._Luminosity = (float)RNG_NextDoubleRange(GalaxyGen.StarLuminosityBySpectralType[spectralType]._min, GalaxyGen.StarLuminosityBySpectralType[spectralType]._max);
            data._Mass = RNG_NextDoubleRange(GalaxyGen.StarMassBySpectralType[spectralType]._min, GalaxyGen.StarMassBySpectralType[spectralType]._max);
            data._Age = (1 - data._Mass / GalaxyGen.StarMassBySpectralType[spectralType]._max) * maxStarAge; // note the fiddle math at the start here is to make more massive stars younger.

            // create star and populate data:
            Star star = new Star(name, data._Radius, data._Temp, data._Luminosity, data._SpectralType, system);
            star.Mass = data._Mass;
            star.Age = data._Age;

            return star;
        }

        #endregion

        #region Planet Generation Functions

        private static void GeneratePlanetsForStar(Star star)
        {
            // lets start by determining if planets will be generated at all:
            if (m_RNG.NextDouble() > GalaxyGen.PlanetGenerationChance)
                return;  // nope, this star has no planets.

            // now lets try and work out how many planets there should be.
            // My "common knowladge" science tells me that in general terms
            // the bigger the star the more material there was in its general 
            // vacinity when it formed (had to be for the star to get that big in the first place).
            // But the REALLY big stars (types O and B) that the blow away any 
            // left over material very quickly after fusion starts due to massive solar winds.
            // so we want to generate very few planets for smaller and Huge stars 
            // and quite a lot for stars more like our own (type G).
            // given this planet generation is balanced as follows:
            // A/B/O (0.8% chance of occuring in real stars) will have:
            //      -- A medium number of planets. Favoring Gass Giants.
            //      -- Lots of resources on planets.
            //      -- High chance of a body having a research anomly (say 10%).
            //      -- low chance for ruins (say 1%).
            //      -- Lowest chace for NPR races (or even habital planets) are in these systems.
            // F/G/K (~23% generation chance in RS) will have
            //      -- a large number of all planet types,
            //      -- moderate resources on planets.
            //      -- a small chance for research anomlies (say 1%).
            //      -- a moderate chance for ruins (say 5%).
            //      -- Best chance for NPR races is in these systems.
            // M (~76% generation chance in RS) will have
            //      -- a small number of planets. Favoring Gas Dwarfs.
            //      -- Low resources
            //      -- a low chance for anomlies (say 0.2%)
            //      -- High chance of ruins (say 10%)
            //
            // of course the other major factor in this would be system age:
            //      -- Young systems have lower Ruins/NPR chances, higher Anomly chances and More resources.
            //      -- older systems would have less resources and a lower Anomly chance but higher NPR/Ruins chances.
            // The system age thing lines up with the age of the different star classes so these two thing should compond each other.


            double starMassRatio = Clamp01(star.Mass / (1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS));        // heavy star = more material.
            double starSpecralTypeRatio =  GalaxyGen.StarSpecralTypePlanetGenerationRatio[star.SpectralType];

            double starLuminosityRatio = 1;
            if (star.Luminosity > GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max)
                starLuminosityRatio = 1 - Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.O]._max);   // really bright stars blow away material.
            else
                starLuminosityRatio = Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max);       // realy dim stars don't.

            // final 'chance' for number of planets generated. take into consideration star mass, solar wind (via luminosity)
            // and balance decisions for star class.
            double finalGenerationChance = starMassRatio * starLuminosityRatio * starSpecralTypeRatio;

            // using the planet generation chance we will calculate the number of additional 
            // planets over and above the minium of 1. 
            int noOfPlanetsToGenerate = (int)(finalGenerationChance * GalaxyGen.MaxNoOfPlanets) + 1;

            // now loop and generate the planets:
            for (int i = 0; i < noOfPlanetsToGenerate; ++i)
            {

            }
        }
        


        #endregion

        #region Asteriod Generation Functions

        ///< @todo Generate Asteriods.

        #endregion

        #region Comet Generation Functions

        ///< @todo Generate Comets.

        #endregion

        #region Jump Point Generation functions

        ///< @todo Generate JHump Points.

        #endregion

        #region NPR Generation Functions

        ///< @todo Generate NPRs.

        #endregion

        #region Until Functions

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range.
        /// </summary>
        public static double RNG_NextDoubleRange(double min, double max)
        {
            return min + m_RNG.NextDouble() * (max - min);
        }

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range timesd by a constant value (e.g. a unit of some sort).
        /// </summary>
        /// <param name="constant"> A constant which will be multiplied agains min and max, use for units etc.</param>
        /// <returns>Random value between min and max adjusted according to the constant value provided.</returns>
        public static double RNG_NextDoubleRange(double min, double max, double constant)
        {
            min *= constant;
            return min + m_RNG.NextDouble() * ((max * constant) - min);
        }

        /// <summary>
        /// Clamps a number between 0 and 1.
        /// </summary>
        public static double Clamp01(double value)
        {
            if (value > 1)
                return 1;
            else if (value < 0)
                return 0;

            return value;
        }

        #endregion
    }
}


/*
            switch (star.SpectralType)
            {
                case SpectralType.O:
                    break;

                case SpectralType.B:
                    break;

                case SpectralType.A:
                    break;

                case SpectralType.F:
                    break;

                case SpectralType.G:
                    break;

                case SpectralType.K:
                    break;

                default: // SpectralType.M
                    break;
            } */
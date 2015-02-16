using Pulsar4X.Entities;
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

            Star Sun = new Star("Sol", Constants.Units.SOLAR_RADIUS_IN_AU, 5778, 1, Sol);
            Sun.Age = 4.6E9;
            Sun.Mass = Constants.Units.SOLAR_MASS_IN_KILOGRAMS;
            Sun.Class = "G2";

            Sol.Stars.Add(Sun);

            Planet Mercury = new Planet(Sun);
            Mercury.Name = "Mercury";
            Mercury.Eccentricity = 0.205630;
            Mercury.LongitudeOfPeriapsis = 29.124;
            Mercury.IsMoon = false;
            Mercury.Mass = 3.3022E23;
            Mercury.OrbitalPeriod = 87.969;

            Mercury.Radius = 2439.7;
            Mercury.SemiMajorAxis = 0.387098;
            Mercury.TimeSinceApogee = 0;
            Mercury.TimeSinceApogeeRemainder = 0;

            Sun.Planets.Add(Mercury);

            Planet Venus = new Planet(Sun);
            Venus.Name = "Venus";
            Venus.Eccentricity = 0.00677323;
            Venus.LongitudeOfPeriapsis = 131.53298;
            Venus.IsMoon = false;
            Venus.Mass = 4.8676E24;
            Venus.OrbitalPeriod = 224.7;

            Venus.Radius = 6051.8;
            Venus.SemiMajorAxis = 0.72333199;
            Venus.TimeSinceApogee = 0;
            Venus.TimeSinceApogeeRemainder = 0;

            Sun.Planets.Add(Venus);

            Planet Earth = new Planet(Sun);

            Earth.Name = "Earth";
            Earth.Eccentricity = 1.671022E-2;
            Earth.LongitudeOfPeriapsis = 102.94719;
            Earth.IsMoon = false;
            Earth.Mass = 5.97219E24;
            Earth.OrbitalPeriod = 365;

            Earth.Radius = 6378.1;
            Earth.SemiMajorAxis = 1;
            Earth.TimeSinceApogee = 0;
            Earth.TimeSinceApogeeRemainder = 0;

            Sun.Planets.Add(Earth);

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
        /// \note This is not very scientific and that 'magic' the numbers are sourced from Wikipedia. mostly here: http://en.wikipedia.org/wiki/Stellar_classification#Class_A
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
            const double maxStarAgeO = 6000000;         // after 6 million years O types eiother go nova or become B type stars.
            const double maxStarAgeB = 100000000;       // could not find any info on B type ages, so i made it between O and A (100 million).
            const double maxStarAgeA = 350000000;       // A type stars are always young, typicall a few hundred million years..
            const double maxStarAgeF = 3000000000;      // Could not find any info again, chose a number between B and G stars (3 billion)
            const double maxStarAgeG = 10000000000;     // The life of a G class star is about 10 billion years.

            // Max age of a star in the Milky Way is 13.2 billion years, the age of the milky way. A star could be older 
            //(like 100 billion years older if not for the fact that the universion is only about 14 billion years old) but then it wouldn't be in the milky way.
            // This is used for both K and M type stars both of which can easly out live the milky way).
            const double maxStarAge = 13200000000;      

            StarData data = new StarData();
            data._SpectralType = spectralType;
            switch (data._SpectralType)
            {
                case SpectralType.O:
                    data._Radius = RNG_NextDoubleRange(6.6, 250.0, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(30000, 60000);
                    data._Luminosity = (float)RNG_NextDoubleRange(30000, 1000000);
                    data._Mass = RNG_NextDoubleRange(16.0, 265.0, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (265 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAgeO; // note the fiddle math at the start here is to make more massive stars younger stars younger.
                    break;

                case SpectralType.B:
                    data._Radius = RNG_NextDoubleRange(1.8, 6.6, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(10000, 30000);
                    data._Luminosity = (float)RNG_NextDoubleRange(25, 30000);
                    data._Mass = RNG_NextDoubleRange(2.1, 16.0, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (16.0 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAgeB;
                    break;

                case SpectralType.A:
                    data._Radius = RNG_NextDoubleRange(1.4, 1.8, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(7500, 10000);
                    data._Luminosity = (float)RNG_NextDoubleRange(5, 25);
                    data._Mass = RNG_NextDoubleRange(1.4, 2.1, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (2.1 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAgeA;
                    break;

                case SpectralType.F:
                    data._Radius = RNG_NextDoubleRange(1.15, 1.4, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(6000, 7500);
                    data._Luminosity = (float)RNG_NextDoubleRange(1.5, 5);
                    data._Mass = RNG_NextDoubleRange(1.04, 1.4, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAgeF;
                    break;

                case SpectralType.G:
                    data._Radius = RNG_NextDoubleRange(0.96, 1.15, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(5200, 6000);
                    data._Luminosity = (float)RNG_NextDoubleRange(0.6, 1.5);
                    data._Mass = RNG_NextDoubleRange(0.8, 1.04, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (1.04 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAgeG;
                    break;

                case SpectralType.K:
                    data._Radius = RNG_NextDoubleRange(0.7, 0.96, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(3700, 5200);
                    data._Luminosity = (float)RNG_NextDoubleRange(0.08, 0.6);
                    data._Mass = RNG_NextDoubleRange(0.45, 0.8, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (0.8 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAge;
                    break;

                default: // SpectralType.M
                    data._Radius = RNG_NextDoubleRange(0.12, 0.7, Constants.Units.SOLAR_RADIUS_IN_AU);
                    data._Temp = (uint)m_RNG.Next(2400, 3700);
                    data._Luminosity = (float)RNG_NextDoubleRange(0.0001, 0.08);
                    data._Mass = RNG_NextDoubleRange(0.08, 0.45, Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
                    data._Age = (1 - data._Mass / (0.45 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS)) * maxStarAge;
                    break;
            }
            
            // create star and populate data:
            Star star = new Star(name, data._Radius, data._Temp, data._Luminosity, system);
            star.Class = data._SpectralType.ToString();
            star.Mass = data._Mass;
            star.Age = data._Age;

            return star;
        }

        #endregion

        #region Planet Generation Functions

        ///< @todo Generate Planets.

        #endregion

        #region Asteriod Generation Functions

        ///< @todo Generate Asteriods.

        #endregion

        #region Comet Generation Functions

        ///< @todo Generate Comets.

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

        #endregion
    }
}

using Pulsar4X.Entities;
using Pulsar4X.Lib;
using Pulsar4X.Helpers.GameMath;
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

        public static StarSystem CreateSystem(string name, int seed, int numJumpPoints = -1)
        {
            // create new RNG with Seed.
            m_RNG = new Random(seed);

            StarSystem newSystem = new StarSystem(name, seed);

            int noOfStars = m_RNG.Next(1, 5);
            for (int i = 0; i < noOfStars; ++i)
            {
                Star newStar = GenerateStar(newSystem);

                GeneratePlanetsForStar(newStar);
            }

            GenerateJumpPoints(newSystem, numJumpPoints);

            // Clean up cached RNG:
            m_RNG = null;

            GameState.Instance.StarSystems.Add(newSystem);
            GameState.Instance.StarSystemCurrentIndex++;
            return newSystem;
        }

        #region Create Sol

        public static StarSystem CreateStressTest()
        {
            StarSystem Sol= new StarSystem("StressTest", -1);

            Star Sun = new Star("Sol", Constants.Units.SOLAR_RADIUS_IN_AU, 5778, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Orbit = Orbit.FromStationary(Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
            Sun.Class = "G2";

            Sun.Radius = 696000 / Constants.Units.KM_PER_AU;

            Sol.Stars.Add(Sun);

            DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

            Random RNG = new Random();

            for (int i = 0; i < 500; i++)
            {
                i++;
                Planet newPlanet = new Planet(Sun);
                newPlanet.Name = "New Planet " + i;

                newPlanet.Orbit = Orbit.FromAsteroidFormat(5.9726E24, Sun.Orbit.Mass, RNG.NextDouble() * 100, RNG.NextDouble(), 0, RNG.NextDouble() * 360, RNG.NextDouble() * 360, RNG.NextDouble() * 360, J2000);

                double x, y;

                newPlanet.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

                newPlanet.Position.System = Sol;

                newPlanet.Position.X = x;
                newPlanet.Position.Y = y;

                Sun.Planets.Add(newPlanet);
            }

            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            return Sol;
        }

        public static StarSystem CreateSol()
        {
            StarSystem Sol = new StarSystem("Sol", GalaxyGen.SeedRNG.Next());

            // Used for JumpPoint generation.
            m_RNG = new Random(Sol.Seed);

            Star Sun = new Star("Sol", Constants.Units.SOLAR_RADIUS_IN_AU, 5505, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Orbit = Orbit.FromStationary(Constants.Units.SOLAR_MASS_IN_KILOGRAMS);
            Sun.Class = "G2";

            Sun.Radius = 696000 / Constants.Units.KM_PER_AU;

            Sol.Stars.Add(Sun);

            DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0);

            Planet Mercury = new Planet(Sun);
            Mercury.Name = "Mercury";
            Mercury.Orbit = Orbit.FromMajorPlanetFormat(3.3022E23, Sun.Orbit.Mass, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, J2000);

            Mercury.Radius = 2439.7 / Constants.Units.KM_PER_AU;

            double x, y;
            Mercury.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Mercury.Position.System = Sol;
            Mercury.Position.X = x;
            Mercury.Position.Y = y;

            Sun.Planets.Add(Mercury);

            Planet Venus = new Planet(Sun);
            Venus.Name = "Venus";
            Venus.Orbit = Orbit.FromMajorPlanetFormat(4.8676E24, Sun.Orbit.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, J2000);

            Venus.Radius = 6051.8 / Constants.Units.KM_PER_AU;

            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;


            Sun.Planets.Add(Venus);

            Planet Earth = new Planet(Sun);
            Earth.Name = "Earth";
            Earth.Orbit = Orbit.FromMajorPlanetFormat(5.9726E24, Sun.Orbit.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, J2000);

            Earth.Radius = 6378.1 / Constants.Units.KM_PER_AU;

            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;

            Sun.Planets.Add(Earth);

            Planet Moon = new Planet(Earth);
            Moon.Name = "Moon";
            Moon.Orbit = Orbit.FromAsteroidFormat(0.073E24, Earth.Orbit.Mass, 384748 / Constants.Units.KM_PER_AU, 0.0549006, 0, 0, 0, 0, J2000);

            Moon.Radius = 1738.14 / Constants.Units.KM_PER_AU;

            Moon.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Moon.Position.System = Sol;
            Moon.Position.X = Earth.Position.X + x;
            Moon.Position.Y = Earth.Position.Y + y;

            Earth.Moons.Add(Moon);

            GenerateJumpPoints(Sol);

            // Clean up cached RNG:
            m_RNG = null;

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

            // <@ todo: Generate orbits for stars in multi-star systems.

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
        /// \note This is not very scientific and that the 'magic' numbers are sourced from Wikipedia, mostly here: http://en.wikipedia.org/wiki/Stellar_classification
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
            star.Age = data._Age;

            // Temporary orbit to store mass.
            // Calculate real orbit later.
            star.Orbit = Orbit.FromStationary(data._Mass);

            return star;
        }

        #endregion

        #region Planet Generation Functions

        /// <summary>
        /// This function Works out how many planets to generate for a given star.
        /// </summary>
        /// <remarks>
        /// Now lets try and work out how many planets there should be.
        /// My "common knowladge" science tells me that in general terms
        /// the bigger the star the more material there was in its general 
        /// vacinity when it formed (had to be for the star to get that big in the first place).
        /// But the REALLY big stars (types O, B and A) blow away any left over 
        /// material very quickly after fusion starts due to massive solar winds.
        /// so we want to generate very few planets for smaller and Huge stars 
        /// and quite a lot for stars more like our own (type G).
        /// given this planet generation is balanced somthing like this:
        /// 
        /// A/B/O (0.8% chance of occuring in real stars) will have:
        /// <list type="Bullet">
        /// <item>
        /// A medium number of planets. Favoring Gass Giants.
        /// </item>
        /// <item>
        /// Lots of resources on planets.
        /// </item>
        /// <item>
        /// High chance of a body having a research anomly (say 10%).
        /// </item>
        /// <item>
        /// low chance for ruins (say 1%).
        /// </item>
        /// <item>
        /// Lowest chace for NPR races (or even habital planets) are in these systems.
        /// </item>
        /// </list>
        /// 
        /// F/G/K (~23% generation chance in RS) will have:
        /// <list type="Bullet">
        /// <item>
        /// A large number of all planet types.
        /// </item>
        /// <item>
        /// moderate resources on planets.
        /// </item>
        /// <item>
        /// a small chance for research anomlies (say 1%).
        /// </item>
        /// <item>
        /// a moderate chance for ruins (say 5%).
        /// </item>
        /// <item>
        /// Best chance for NPR races is in these systems.
        /// </item>
        /// </list>
        ///
        /// M (~76% generation chance in RS) will have
        /// <list type="Bullet">
        /// <item>
        /// a small number of planets. Favoring Gas Dwarfs.
        /// </item>
        /// <item>
        /// Low resources
        /// </item>
        /// <item>
        /// a low chance for anomlies (say 0.2%)
        /// </item>
        /// <item>
        /// High chance of ruins (say 10%)
        /// </item>
        /// </list>
        ///      
        /// (see GalaxyGen.StarSpecralTypePlanetGenerationRatio for a tweakalbe for no. of Planets).
        /// 
        /// Of course the other major factor in this would be system age:
        /// <list type="Bullet">
        /// <item>
        /// Young systems have lower Ruins/NPR chances, higher Anomly chances and More resources.
        /// </item>
        /// <item>
        /// Older systems would have less resources and a lower Anomly chance but higher NPR/Ruins chances.
        /// </item>
        /// </list>
        ///
        /// The system age thing lines up with the age of the different star classes so these two thing should compond each other.
        /// </remarks>
        private static void GeneratePlanetsForStar(Star star)
        {
            // lets start by determining if planets will be generated at all:
            if (m_RNG.NextDouble() > GalaxyGen.PlanetGenerationChance)
                return;  // nope, this star has no planets.

            double starMassRatio = Clamp01(star.Orbit.Mass / (1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS));   // heavy star = more material.
            double starSpecralTypeRatio =  GalaxyGen.StarSpecralTypePlanetGenerationRatio[star.SpectralType];    // tweakble

            double starLuminosityRatio = 1;
            if (star.Luminosity > GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max)
                starLuminosityRatio = 1 - Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.O]._max);   // really bright stars blow away material.
            else
                starLuminosityRatio = Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max);       // realy dim stars don't.

            // final 'chance' for number of planets generated. take into consideration star mass, solar wind (via luminosity)
            // and balance decisions for star class.
            double finalGenerationChance = Clamp01(starMassRatio * starLuminosityRatio * starSpecralTypeRatio);

            // using the planet generation chance we will calculate the number of additional 
            // planets over and above the minium of 1. 
            int noOfPlanetsToGenerate = (int)(finalGenerationChance * GalaxyGen.MaxNoOfPlanets) + 1;

            // now loop and generate the planets:
            for (int i = 0; i < noOfPlanetsToGenerate; ++i)
            {
                GeneratePlanet(star, finalGenerationChance, i + 1);
            }
        }

        /// <summary>
        /// Works out what type of planet to generate based on the distrabution in GalaxyGen.PlanetTypeDisrubution.
        /// It then calls a more type specific function to complete generation of the planet.
        /// </summary>
        private static void GeneratePlanet(Star star, double planetGenerationChance, int number)
        {
            // we'll start by determining the planet type:
            double planetTypeChance = m_RNG.NextDouble();
            Planet.PlanetType pt = Planet.PlanetType.Terrestrial;  // init planet type for safty.

            double dist = GalaxyGen.PlanetTypeDisrubution[Planet.PlanetType.GasGiant];
            if (planetTypeChance < dist)
                pt = Planet.PlanetType.GasGiant;

            dist = GalaxyGen.PlanetTypeDisrubution[Planet.PlanetType.IceGiant];
            if (planetTypeChance < dist)
                pt = Planet.PlanetType.IceGiant;

            dist = GalaxyGen.PlanetTypeDisrubution[Planet.PlanetType.GasDwarf];
            if (planetTypeChance < dist)
                pt = Planet.PlanetType.GasDwarf;

            dist = GalaxyGen.PlanetTypeDisrubution[Planet.PlanetType.Terrestrial];
            if (planetTypeChance < dist)
                pt = Planet.PlanetType.Terrestrial;

            // now that we know the planet type we can generate the correct type:
            Planet newPlanet = null;
            switch (pt)
            {
                case Planet.PlanetType.GasGiant:
                    newPlanet = GenerateGasGiant(star, planetGenerationChance);
                    break;

                case Planet.PlanetType.IceGiant:
                    newPlanet = GenerateIceGiant(star, planetGenerationChance);
                    break;

                case Planet.PlanetType.GasDwarf:
                    newPlanet = GenerateGasDwarf(star, planetGenerationChance);
                    break;

                case Planet.PlanetType.Terrestrial:
                default:
                    newPlanet = GenerateTerrestrial(star, planetGenerationChance);
                    break;
            }

            newPlanet.Name = star.Name + " - " + number.ToString();

            star.Planets.Add(newPlanet);
        }

       /// <summary>
       /// Do we Need this???
       /// </summary>
        private static Planet GenerateGasGiant(Star star, double planetGenerationChance)
        {

            return new Planet(star);
        }

        /// <summary>
        /// Do we Need this???
        /// </summary>
        private static Planet GenerateIceGiant(Star star, double planetGenerationChance)
        {

            return new Planet(star);
        }

        /// <summary>
        /// Do we Need this???
        /// </summary>
        private static Planet GenerateGasDwarf(Star star, double planetGenerationChance)
        {

            return new Planet(star);
        }

        /// <summary>
        /// Generates a Terrestrial Planet.
        /// @todo I think we can make this a general function to generate any type of system body that is not a star!!
        /// </summary>
        /// <remarks>
        /// Quite a lot of data goes into making a planet. What follows is a list of the different data
        /// points which need to be either randomly generated or infered through previously generated data.
        /// The List is in the required order of generation.
        /// <list type="Bullet">
        /// <item>
        /// <b>Mass:</b> Randomly selected based on a range for the given planet type, see GalaxyGen.PlanetMassByType.
        /// </item>
        /// <item>
        /// <b>Density:</b> Randomly selected based on a range for the given planet type, see GalaxyGen.PlanetDensityByType.
        /// </item>
        /// <item>
        /// <b>Radius:</b> Inferd from mass and densitiy using the formular: r = ((3M)/(4pD))^(1/3), where p = PI, D = Density, and M = Mass.
        /// </item>
        /// <item>
        /// <b>Surface Gravity:</b> Is calculated with the following formular: g = (G * M) / r^2, where G = Gravatational Constant, M = Mass and r = radius.
        /// </item>
        /// <item>
        /// <b>Axial Tilt:</b> A random value between 0 and GalaxyGen.MaxPlanetInclination is generated.
        /// </item>
        /// <item>
        /// <b>Orbit:</b> To create the orbit of a planet 6 seperate values must first be generated:
        ///     <list type="Bullet">
        ///     <item>
        ///     <b>SemiMajorAxis:</b> Randomly selected based on a range for the given star type, see: GalaxyGen.OrbitalDistanceByStarSpectralType.
        ///     @note This should probably be change so we generate the closest planets first, Rather then just dropping them any old where.
        ///     </item>
        ///     <item>
        ///     <b>Eccentricity:</b> A random value between 0 and 1.0 (currently 0.8 due to bugs!!) is generated.
        ///     </item>
        ///     <item>
        ///     <b>Inclination:</b> A random value between 0 and GalaxyGen.MaxPlanetInclination is generated.
        ///     </item>
        ///     <item>
        ///     <b>Argument Of Periapsis:</b> A random value between 0 and 360 is generated.
        ///     </item>
        ///     <item>
        ///     <b>Mean Anomaly:</b> A random value between 0 and 360 is generated.
        ///     </item>
        ///     <item>
        ///     <b>Longitude Of Ascending Node:</b> A random value between 0 and 360 is generated.
        ///     </item>
        ///     </list>
        /// </item>
        /// <item>
        /// <b>Length Of Day:</b> Randomly generated TimeSpan with a range of 6 hours to the year length of the body (tho never less than 6 hours).
        /// </item>
        /// <item>
        /// <b>Base Temperature:</b> This is calculated using the Stefan–Boltzmann law, See http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
        /// </item>
        /// <item>
        /// <b>Techtonics:</b> This is both randomly generated and infered from mass and age. 
        /// First every body has 50/50 odds of being a dead world. If it is not dead then
        /// a techtonics chance is calculated using the following formular:
        /// techtonicsChance = M / star.Age, where M is the planets Mass in Earth Masses.
        /// techtonicsChance is then clamped to a number between 0 and 1. 
        /// For earth this process results in a number of 0.217... 
        /// This is then used to select one of the possible TechtonicActivity values.
        /// </item>
        /// <item>
        /// <b>Magnetic Feild:</b> Randomly selected based on a range for the given planet type, see GalaxyGen.PlanetMagneticFieldByType.
        /// If the planet does not have some type of Plate Techtonics (i.e. it is a dead world) then the magnectic field is reduced in strength by a factor of 10.
        /// </item>
        /// <item>
        /// <b>Atmosphere:</b> TODO
        /// </item>
        /// <item>
        /// <b>Planetary Ruins:</b> TODO
        /// </item>
        /// <item>
        /// <b>Minerials:</b> Currently an ugly hace of using homworld minerials. Note that minerial generation 
        /// functions should be part of the Planet class as the player will likly want to re-generate them.
        /// </item>
        /// </tem>
        /// <b>Moons:</b> Might be dove via some indirect recursive calling of a generilised version of this function.
        /// </item>
        /// </list>
        /// </remarks>
        private static Planet GenerateTerrestrial(Star star, double planetGenerationChance)
        {
            // still need to move the following into remarks above:
            // Atmosphere (i'm thinking Atmosphere should be its own thing like Orbit is.)
            //  -- presure
            //  -- Hydrosphere
            //      -- Hydrosphere extent
            //  -- Greenhouse Factor
            //  -- Albedo (affected by Hydrosphere how exactly?)
            //  -- surface Temp. (based on base temp + greehhouse factor + Albedo).

            // Create the Planet:
            Planet planet = new Planet(star);
            planet.Type = Planet.PlanetType.Terrestrial;

            // Creat some of the basic stats:
            double mass = RNG_NextDoubleRange(GalaxyGen.PlanetMassByType[planet.Type]._min, GalaxyGen.PlanetMassByType[planet.Type]._max);
            planet.Density = RNG_NextDoubleRange(GalaxyGen.PlanetDensityByType[planet.Type]._min, GalaxyGen.PlanetDensityByType[planet.Type]._max); ;
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * planet.Density), (1 / 3));
            radius = radius / 1000 / Constants.Units.KM_PER_AU;     // convert from meters to AU, also keep the temp var as it is easer to read then planet.Radius.
            planet.Radius = radius;
            planet.SurfaceGravity = (float)((Constants.Science.GRAVITATIONAL_CONSTANT * mass) / (radius * radius));
            planet.AxialTilt = (float)(m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination);

            // Create the orbital values:
            double smeiMajorAxis = RNG_NextDoubleRange(GalaxyGen.OrbitalDistanceByStarSpectralType[star.SpectralType]._min, GalaxyGen.OrbitalDistanceByStarSpectralType[star.SpectralType]._max);
            double eccentricity = m_RNG.NextDouble() * 0.8; // get random eccentricity needs better distrubution.
            double inclination = m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination; // doesn't do much at the moment but may as well be there. Neet better Dist.
            double argumentOfPeriapsis = m_RNG.NextDouble() * 360;
            double meanAnomaly = m_RNG.NextDouble() * 360;
            double longitudeOfAscendingNode = m_RNG.NextDouble() * 360;

            // now Create the orbit:
            DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0); ///< @todo J2000 datetime should be in GalaxyGen!!
            planet.Orbit = Orbit.FromAsteroidFormat(mass, star.Orbit.Mass, smeiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, J2000);
            
            // generate the planets day length:
            ///< @todo Move some of these length of day magic numbers into GalaxyGen
            ///< @todo Should we do Tidle Locked bodies??? iirc bodies trend toward being tidaly locked over time...
            planet.LengthOfDay = new TimeSpan(m_RNG.Next(0, planet.Orbit.OrbitalPeriod.Days), m_RNG.Next(0, 24), m_RNG.Next(0, 60), 0);
            if (planet.LengthOfDay < TimeSpan.FromHours(6))
                planet.LengthOfDay += TimeSpan.FromHours(6);  // just a basic sainty check to make sure we dont end up with a planet rotating once every 3 minutes, It' pull itself apart!!

            // to calculate base temp see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
            // Note that base temp does not take into account albedo or atmosphere.
            double starTemp = (star.Temperature + Constants.Units.DEGREES_C_TO_KELVIN); // we need to work in kelvin here.
            double planetTemp = starTemp * Math.Sqrt(star.Radius / (2 * smeiMajorAxis));
            planetTemp += Constants.Units.KELVIN_TO_DEGREES_C;  // convert back to degrees.
            planet.BaseTemperature = (float)planetTemp;

            // generate Plate techtonics
            if (m_RNG.Next(0,1) == 0)
            {
                // this planet has some plate techtonics:
                // this should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
                ///< @todo make techtonics generation tweakable.
                double techtonicsChance = mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS / star.Age;  
                techtonicsChance = Clamp01(techtonicsChance);

                if (techtonicsChance < 0.01)
                    planet.Techtonics = Planet.TechtonicActivity.Dead;
                if (techtonicsChance < 0.02)
                    planet.Techtonics = Planet.TechtonicActivity.Minor;
                if (techtonicsChance < 0.04)
                    planet.Techtonics = Planet.TechtonicActivity.EarthLike;
                else
                    planet.Techtonics = Planet.TechtonicActivity.Major;
            }

            // Generate Magnetic field:
            planet.MagneticFeild = (float)(RNG_NextDoubleRange(GalaxyGen.PlanetMagneticFieldByType[planet.Type]._min,
                                                    GalaxyGen.PlanetMagneticFieldByType[planet.Type]._min));
            if (planet.Techtonics == Planet.TechtonicActivity.Dead)
                planet.MagneticFeild *= 0.1F; // reduce magnetic field of a dead world.
            
            // now lets generate the atmosphere:
            planet.Atmosphere = GenerateAtmosphere(planet);

            ///< @todo Generate Ruins
            
            ///< @todo Generate Minerials Properly instead of this ugle hack:
            planet.HomeworldMineralGeneration();

            // generate moons:
            GenerateMoons(planet);

            // force the planet to have the correct position for it and any of its moons:
            planet.UpdatePosition(0);  ///< @todo wrap this in an if so it cannot be called in moons??

            return planet;
        }

        /// <summary>
        /// Generates an atmosphere for the provided planet based on its type.
        /// </summary>
        private static Atmosphere GenerateAtmosphere(Planet planet)
        {
            Atmosphere atmo = new Atmosphere(planet);

            // calc albedo:
            atmo.Albedo = (float)RNG_NextDoubleRange(GalaxyGen.PlanetAlbedoByType[planet.Type]._min, GalaxyGen.PlanetAlbedoByType[planet.Type]._max);

            // some safe defaults:
            atmo.HydrosphereExtent = 0;
            atmo.Hydrosphere = false;

            double atmoChance = Clamp01(GalaxyGen.AtmosphereGenerationModifier[planet.Type] * (planet.Orbit.Mass / GalaxyGen.PlanetMassByType[planet.Type]._max));
            if (m_RNG.NextDouble() > atmoChance)
            {
                // we uses these to keep a running tally of how much gass we have generated.
                float totalATM = 0;
                float currATM = 0;
                int noOfTraceGases = 0;

                // Generate an Atmosphere
                ///< @todo Remove some of this hard coding:
                switch (planet.Type)
                {
                    case Planet.PlanetType.GasDwarf:
                    case Planet.PlanetType.GasGiant:
                        // Start with the ammount of heilum:
                        currATM = (float)RNG_NextDoubleRange(0.05, 0.3);
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(1), currATM); 
                        totalATM += currATM;

                        // next get a random number/ammount of trace gases:
                        noOfTraceGases = m_RNG.Next(1, 4);
                        totalATM += AddTraceGases(atmo, noOfTraceGases);

                        // now make the remaining amount Hydrogen:
                        currATM = 1 - totalATM; // get the remaining ATM.
                        AddGasToAtmoSafely(atmo, AtmosphericGas.AtmosphericGases.SelectAt(0), currATM);
                        break;

                    case Planet.PlanetType.IceGiant:
                        // Start with the ammount of heilum:
                        currATM = (float)RNG_NextDoubleRange(0.1, 0.25);
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(1), currATM); 
                        totalATM += currATM;

                        // next add a small amount of Methane:
                        currATM = (float)RNG_NextDoubleRange(0.01, 0.03);
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(2), currATM); 
                        totalATM += currATM;

                        // Next some water and ammonia:
                        currATM = (float)RNG_NextDoubleRange(0.0, 0.01);
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(3), currATM); 
                        totalATM += currATM;
                        currATM = (float)RNG_NextDoubleRange(0.0, 0.01);
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(4), currATM); 
                        totalATM += currATM;

                         // now make the remaining amount Hydrogen:
                        currATM = 1 - totalATM; // get the remaining ATM.
                        atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(0), currATM);
                        break;

                    case Planet.PlanetType.Moon:
                    case Planet.PlanetType.Terrestrial:
                        // Terrestrial Planets can have very large ammount of ATM.
                        // so we will generate a number to act as the total:
                        float planetsATM = (float)RNG_NextDoubleRange(0.1, 100); 
                        // reduce my mass ratio relative to earth (so really small bodies cannot have massive atmos:
                        double massRatio = planet.Orbit.Mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS;
                        planetsATM = (float)Clamp((double)planetsATM * massRatio, 0.01, 200);

                        // Start with the ammount of Oxygen or Carbin Di-oxide or methane:
                        int atmoTypeChance = m_RNG.Next(0, 2);
                        if (atmoTypeChance == 0)            // methane
                        {
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.40);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(2), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else if (atmoTypeChance == 1)   // Carbon Di-Oxide
                        {
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.90);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(1), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else                        // oxygen
                        {
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.40);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(9), currATM * planetsATM);
                            totalATM += currATM;

                            // Gen Hydrosphere for these planets:
                            if (m_RNG.Next(0, 1) == 0)
                            {
                                atmo.Hydrosphere = true;
                                atmo.HydrosphereExtent = (short)Math.Round(m_RNG.NextDouble() * 100);
                            }
                        }

                        // next get a random number/ammount of trace gases:
                        noOfTraceGases = m_RNG.Next(1, 4);
                        totalATM += AddTraceGases(atmo, noOfTraceGases, planetsATM);
                        
                        // now make the remaining amount Nitrogen:
                        currATM = 1 - totalATM; // get the remaining ATM.
                        AddGasToAtmoSafely(atmo, AtmosphericGas.AtmosphericGases.SelectAt(6), currATM * planetsATM);
                        break;

                    case Planet.PlanetType.IceMoon:
                    case Planet.PlanetType.DwarfPlanet:
                    case Planet.PlanetType.Asteriod:
                    case Planet.PlanetType.Comet:
                    default:
                        break; // none
                }
            }

            // now calc data resulting from above:
            atmo.UpdateState();

            return atmo;
        }

        /// <summary>
        /// Just adds the specified ammount of gas to the specified atmosphere safely.
        /// </summary>
        private static void AddGasToAtmoSafely(Atmosphere atmo, AtmosphericGas gas, float ammount)
        {
            if (atmo.Composition.ContainsKey(gas))
                atmo.Composition[gas] += ammount;
            else
                atmo.Composition.Add(gas, ammount);
        }

        /// <summary>
        /// A small helper function for GenerateAtmosphere. It generates up to the specified number of
        /// "trace" gases and adds them to the atmosphere.
        /// </summary>
        /// <param name="scaler">The ammount of gass added is multiplyed by this before being added to the Atmosphere.</param>
        /// <returns>The ammount of gas added in ATMs (pre scaler)</returns>
        private static float AddTraceGases(Atmosphere atmo, int number, float scaler = 1)
        {
            float totalATMAdded = 0;
            for (int i = 0; i < number; ++i)
            {
                float currATM = (float)RNG_NextDoubleRange(0, 0.005);
                var gas = AtmosphericGas.AtmosphericGases.Select(m_RNG.NextDouble());
                if (atmo.Composition.ContainsKey(gas))
                    continue;           // just skip it.
                atmo.Composition.Add(gas, currATM * scaler);   // up to max half a percent.  
                totalATMAdded += currATM;
            }

            return totalATMAdded;
        }

        /// <summary>
        /// @todo I'm thinking the GenerateTerrestrial() function could be generlised. If so then this would just loop for a suitable count and call it to generate the moons.
        /// </summary>
        private static void GenerateMoons(Planet planet)
        {
            ///< @todo moon Generation.
        }

        #endregion

        #region Asteriod Generation Functions

        ///< @todo Generate Asteriods.

        #endregion

        #region Comet Generation Functions

        ///< @todo Generate Comets.

        #endregion

        #region Jump Point Generation functions

        /// <summary>
        /// Generates a jump points in the designated system.
        /// Used by JumpPoint class when connecting an a existing system
        /// with no unconnected jump points.
        /// </summary>
        public static JumpPoint GenerateJumpPoint(StarSystem system)
        {
            Star luckyStar = system.Stars[m_RNG.Next(system.Stars.Count)];

            return GenerateJumpPoint(luckyStar);
        }

        /// <summary>
        /// Generates Jump Points for this system.
        /// If numJumpPoints is not specified, we will generate the "Natural" amount
        /// based on GetNaturalJumpPointGeneration(Star)
        /// </summary>
        /// <param name="system">System to generate JumpPoints in.</param>
        /// <param name="numJumpPoints">Specific number of jump points to create.</param>
        private static void GenerateJumpPoints(StarSystem system, int numJumpPoints = -1)
        {
            WeightedList<Star> starList = new WeightedList<Star>();

            foreach (Star currentStar in system.Stars)
            {
                // Build our weighted list based on how many JP's the star naturally
                // wants to generate.
                starList.Add(GetNaturalJumpPointGeneration(currentStar), currentStar);
            }

            // If numJumpPoints wasn't specified by the systemGen,
            // then just make as many jumpPoints as our stars cumulatively want to make.
            if (numJumpPoints == -1)
            {
                numJumpPoints = (int)starList.TotalWeight;
            }

            numJumpPoints = (int)Math.Round(numJumpPoints * Constants.GameSettings.JumpPointConnectivity);

            int jumpPointsGenerated = 0;
            while (jumpPointsGenerated < numJumpPoints)
            {
                double rnd = m_RNG.NextDouble();

                // Generate a jump point on a star from the weighted list.
                GenerateJumpPoint(starList.Select(rnd));

                jumpPointsGenerated++;
            }

        }

        /// <summary>
        /// Returns the number of JumpPoints that this star wants to generate.
        /// Currently based exclusivly on the mass of the planets around the star.
        /// </summary>
        private static int GetNaturalJumpPointGeneration(Star star)
        {
            int numJumpPoints = 1; // Each star always generates a JP.

            // Give a chance per planet to generate a JumpPoint
            foreach (Planet p in star.Planets)
            {
                int chance = Constants.GameSettings.JumpPointGenerationChance;

                // Higher mass planets = higher chance.
                double planetEarthMass = p.Orbit.Mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS;
                if (planetEarthMass > 1)
                {
                    chance = chance + 2;
                    if (planetEarthMass > 3)
                    {
                        chance = chance + 3;
                    }
                    if (planetEarthMass > 5)
                    {
                        chance = chance + 5;
                    }
                }
                if (chance >= m_RNG.Next(101))
                {
                    numJumpPoints++;
                }
            }

            return numJumpPoints;
        }

        /// <summary>
        /// Generates a JumpPoint on the designated star.
        /// Clamps JumpPoint generation to be within the planetary
        /// field of the star.
        /// </summary>
        private static JumpPoint GenerateJumpPoint(Star star)
        {
            double minRadius = double.MaxValue;
            double maxRadius = double.MinValue;

            // Clamp generation to within the planetary system.
            foreach (Planet currentPlanet in star.Planets)
            {
                if (minRadius > currentPlanet.Orbit.Periapsis)
                {
                    minRadius = currentPlanet.Orbit.Periapsis;
                }
                if (maxRadius < currentPlanet.Orbit.Apoapsis)
                {
                    maxRadius = currentPlanet.Orbit.Apoapsis;
                }
            }

            // Determine a location for the new JP.
            // Location will be between minDistance and 75% of maxDistance.
            double offsetX = (maxRadius - minRadius) * RNG_NextDoubleRange(0.0d, 0.75d) + minRadius;
            double offsetY = (maxRadius - minRadius) * RNG_NextDoubleRange(0.0d, 0.75d) + minRadius;

            // Randomly flip the sign of the offsets.
            if (m_RNG.NextDouble() >= 0.5)
            {
                offsetX = -offsetX;
            }
            if (m_RNG.NextDouble() >= 0.5)
            {
                offsetY = -offsetY;
            }

            // Create the new jumpPoint and link it to it's parent system.
            JumpPoint newJumpPoint = new JumpPoint(star, offsetX, offsetY);

            return newJumpPoint;
        }

        #endregion

        #region NPR Generation Functions

        ///< @todo Generate NPRs.

        #endregion

        #region Util Functions

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range.
        /// </summary>
        public static double RNG_NextDoubleRange(double min, double max)
        {
            return min + m_RNG.NextDouble() * (max - min);
        }

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range times by a constant value (e.g. a unit of some sort).
        /// </summary>
        /// <param name="constant"> A constant which will be multiplied agains min and max, use for units etc.</param>
        /// <returns>Random value between min and max adjusted according to the constant value provided.</returns>
        public static double RNG_NextDoubleRange(double min, double max, double constant)
        {
            min *= constant;
            return min + m_RNG.NextDouble() * ((max * constant) - min);
        }
        

        /// <summary>
        /// Clamps a value between the provided man and max.
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            else if (value < min)
                return min;

            return value;
        }


        /// <summary>
        /// Clamps a number between 0 and 1.
        /// </summary>
        public static double Clamp01(double value)
        {
            return Clamp(value, 0, 1);
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
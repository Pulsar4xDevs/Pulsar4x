using Pulsar4X.Entities;
using Pulsar4X.Helpers.GameMath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        /// <summary>
        /// A small struct to hold a system body type and mass before we have generated its orbit.
        /// </summary>
        private struct ProtoSystemBody
        {
            public double _mass;
            public SystemBody.PlanetType _type;
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

                GenerateSystemBodiesForStar(newStar);
                //GenerateAsteroidBelts(newStar);
                GenerateComets(newStar);

                // sort the stars children:
                List<SystemBody> sorted = new List<SystemBody>(newStar.Planets.ToArray());
                sorted.Sort(delegate(SystemBody a, SystemBody b)
                {
                    return a.Orbit.SemiMajorAxis.CompareTo(b.Orbit.SemiMajorAxis);
                });
                newStar.Planets = new BindingList<SystemBody>(sorted);
            }

            GenerateStarOrbits(newSystem);

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

            Random RNG = new Random();

            for (int i = 0; i < 500; i++)
            {
                i++;
                SystemBody newPlanet = new SystemBody(Sun, SystemBody.PlanetType.Comet);
                newPlanet.Name = "New Planet " + i;

                newPlanet.Orbit = Orbit.FromAsteroidFormat(5.9726E24, Sun.Orbit.Mass, RNG.NextDouble() * 100, RNG.NextDouble(), 0, RNG.NextDouble() * 360, RNG.NextDouble() * 360, RNG.NextDouble() * 360, GalaxyGen.J2000);

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

            SystemBody Mercury = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Mercury.Name = "Mercury";
            Mercury.Orbit = Orbit.FromMajorPlanetFormat(3.3022E23, Sun.Orbit.Mass, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, GalaxyGen.J2000);

            Mercury.Radius = 2439.7 / Constants.Units.KM_PER_AU;

            double x, y;
            Mercury.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Mercury.Position.System = Sol;
            Mercury.Position.X = x;
            Mercury.Position.Y = y;

            Sun.Planets.Add(Mercury);

            SystemBody Venus = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Venus.Name = "Venus";
            Venus.Orbit = Orbit.FromMajorPlanetFormat(4.8676E24, Sun.Orbit.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, GalaxyGen.J2000);

            Venus.Radius = 6051.8 / Constants.Units.KM_PER_AU;

            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;


            Sun.Planets.Add(Venus);

            SystemBody Earth = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Earth.Name = "Earth";
            Earth.Orbit = Orbit.FromMajorPlanetFormat(5.9726E24, Sun.Orbit.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, GalaxyGen.J2000);

            Earth.Radius = 6378.1 / Constants.Units.KM_PER_AU;

            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;

            Sun.Planets.Add(Earth);

            SystemBody Moon = new SystemBody(Earth, SystemBody.PlanetType.Moon);
            Moon.Name = "Moon";
            Moon.Orbit = Orbit.FromAsteroidFormat(0.073E24, Earth.Orbit.Mass, 384748 / Constants.Units.KM_PER_AU, 0.0549006, 0, 0, 0, 0, GalaxyGen.J2000);

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

        private static void GenerateStarOrbits(StarSystem system)
        {
            List<Star> starList = system.Stars.ToList();

            // Sort by mass.
            starList.Sort(
                (Star starA, Star starB) => 
                { 
                    if (starA.Orbit.Mass < starB.Orbit.Mass)
                    {
                        return 1;
                    }
                    if (starA.Orbit.Mass > starB.Orbit.Mass)
                    {
                        return -1;
                    }
                    return 0;
                }
            );

            Star primaryStar = starList[0];
            primaryStar.UpdatePosition(0);

            for (int i = 1; i < starList.Count; i++)
            {
                Star parentStar = starList[i - 1];
                Star childStar = starList[i];

                double orbitalDistance = CalcStarOrbitDistance(parentStar, childStar);
                double otherDirection = CalcStarOrbitDistance(childStar, parentStar);

                if (orbitalDistance < otherDirection)
                {
                    orbitalDistance = otherDirection;
                }

                // Let's add some PADDING to that face!
                orbitalDistance = (orbitalDistance * ((m_RNG.NextDouble() / 3) + 1)) + parentStar.Orbit.Apoapsis;

                double eccentricity = Math.Pow(m_RNG.NextDouble() * 0.8, 3);
                double sma = orbitalDistance / (1 - eccentricity);

                childStar.Orbit = Orbit.FromAsteroidFormat(childStar.Orbit.Mass, primaryStar.Orbit.Mass, sma, eccentricity, m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination, m_RNG.NextDouble() * 360, m_RNG.NextDouble() * 360, m_RNG.NextDouble() * 360, GameState.Instance.CurrentDate);
                childStar.Parent = primaryStar;
                childStar.UpdatePosition(0);
            }

            system.Stars = new BindingList<Star>(starList);
        }

        private static double CalcStarOrbitDistance(Star star1, Star star2)
        {
            if (star1.Planets.Count == 0)
            {
                return 0;
            }

            double maxApo = 0;
            double planetMass = 0;
            foreach (SystemBody p in star1.Planets)
            {
                if (p.Orbit.Apoapsis > maxApo)
                {
                    maxApo = p.Orbit.Apoapsis;
                    planetMass = p.Orbit.Mass;
                }
            }
            // http://en.wikipedia.org/wiki/Newton%27s_law_of_universal_gravitation
            double gravAttractionToParent = Constants.Science.GRAVITATIONAL_CONSTANT * star1.Orbit.Mass * planetMass / (maxApo * maxApo);

            // Solve for distance to star2 with 10x less gravitational attraction than to star1.
            // (Note, 10x less depends on a 0.1 value for GalaxyGen.StarOrbitGravityFactor
            return Math.Sqrt(Constants.Science.GRAVITATIONAL_CONSTANT * star2.Orbit.Mass * planetMass / gravAttractionToParent * GalaxyGen.StarOrbitGravityFactor);
        }

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
            double randomSelection = m_RNG.NextDouble(); // we will use the one random number to select from all the spectral type ranges. Should give us saner numbers for stars.
            data._SpectralType = spectralType;
            data._Radius = SelectFromRange(GalaxyGen.StarRadiusBySpectralType[spectralType], randomSelection);
            data._Temp = (uint)Math.Round(SelectFromRange(GalaxyGen.StarTemperatureBySpectralType[spectralType], randomSelection));
            data._Luminosity = (float)SelectFromRange(GalaxyGen.StarLuminosityBySpectralType[spectralType], randomSelection);
            data._Mass = SelectFromRange(GalaxyGen.StarMassBySpectralType[spectralType], randomSelection);
            data._Age = (1 - data._Mass / GalaxyGen.StarMassBySpectralType[spectralType]._max) * maxStarAge; // note the fiddly math at the start here is to make more massive stars younger.

            // create star and populate data:
            Star star = new Star(name, data._Radius, data._Temp, data._Luminosity, data._SpectralType, system);
            star.Age = data._Age;
            SetHabitableZone(star);     // calculate habitable zone
            CalculateFullSpectralClass(star);

            // Temporary orbit to store mass, Calculate real orbit later
            star.Orbit = Orbit.FromStationary(data._Mass);

            return star;
        }

        /// <summary>
        /// Calculates and sets the Habitable Zone of this star based on it Luminosity.
        /// calculated according to this site: http://www.planetarybiology.com/calculating_habitable_zone.html
        /// </summary>
        /// <returns>Average Habitable Zone</returns>
        public static void SetHabitableZone(Star star)
        {
            star.MinHabitableRadius = Math.Sqrt(star.Luminosity / 1.1);
            star.MaxHabitableRadius = Math.Sqrt(star.Luminosity / 0.53);
            star.EcoSphereRadius = (star.MinHabitableRadius + star.MaxHabitableRadius) / 2; // our habital zone number is in the middle of our min/max values.
        }

        public static void CalculateFullSpectralClass(Star star)
        {
            // start by getting the sub-division, which is based on temp.
            double sub = star.Temperature / GalaxyGen.StarTemperatureBySpectralType[star.SpectralType]._max;  // temp rang from 0 to 1.
            star.SpectralSubDivision = (ushort)Math.Round( (1 - sub) * 10 );  // invert temp range as 0 is hottest, 9 is coolest.

            // now get the luminosity class
            ///< @todo For right now everthing is just main sequence. see http://en.wikipedia.org/wiki/Stellar_classification
            /// on how this should be done. For right now tho class V is fine (its just flavor text).
            star.LuminosityClass = LuminosityClass.V;

            // finally add them all up to get the class string:
            star.Class = star.SpectralType.ToString() + star.SpectralSubDivision.ToString() + "-" + star.LuminosityClass.ToString();
        }

        #endregion

        #region SystemBody Generation Functions

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
        private static void GenerateSystemBodiesForStar(Star star)
        {
            // lets start by determining if planets will be generated at all:
            if (m_RNG.NextDouble() > GalaxyGen.PlanetGenerationChance)
                return;  // nope, this star has no planets.

            double starMassRatio = GMath.Clamp01(star.Orbit.Mass / (1.4 * Constants.Units.SOLAR_MASS_IN_KILOGRAMS));   // heavy star = more material.
            double starSpecralTypeRatio =  GalaxyGen.StarSpecralTypePlanetGenerationRatio[star.SpectralType];    // tweakble

            double starLuminosityRatio = 1;
            if (star.Luminosity > GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max)
                starLuminosityRatio = 1 - GMath.Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.O]._max);   // really bright stars blow away material.
            else
                starLuminosityRatio = GMath.Clamp01(star.Luminosity / GalaxyGen.StarLuminosityBySpectralType[SpectralType.F]._max);       // realy dim stars don't.

            // final 'chance' for number of planets generated. take into consideration star mass, solar wind (via luminosity)
            // and balance decisions for star class.
            double finalGenerationChance = GMath.Clamp01(starMassRatio * starLuminosityRatio * starSpecralTypeRatio);

            // using the planet generation chance we will calculate the number of additional 
            // planets over and above the minium of 1. 
            int noOfPlanetsToGenerate = (int)(finalGenerationChance * GalaxyGen.MaxNoOfPlanets) + 1;

            // create protPlanet list:
            List<ProtoSystemBody> protoPlanets = new List<ProtoSystemBody>();
            double totalSystemMass = 0;

            // first we need to loop through and generate a prot-planet with just it's mass & type
            for (int i = 0; i < noOfPlanetsToGenerate; ++i)
            {
                ProtoSystemBody protoPlanet = new ProtoSystemBody();                                    // create the proto planet
                protoPlanet._type = GalaxyGen.PlanetTypeDisrubution.Select(m_RNG.NextDouble());         // Determining the planet type
                protoPlanet._mass = GeneratePlanetMass(protoPlanet._type);                              // Generate Mass
                totalSystemMass += protoPlanet._mass;                                                   // add mass to total mass.
                protoPlanets.Add(protoPlanet);                                                          // Add to list!!
            }

            // now lets work out how many asteriod belts we want, from 0 - MaxNoOfAsteroidBelts.
            int noOfBelts = m_RNG.Next(0, GalaxyGen.MaxNoOfAsteroidBelts + 1);

            // add them to the list:
            for (int i = 0; i < noOfBelts; ++i)
            {
                ProtoSystemBody protoBelt = new ProtoSystemBody();                                      // Create the Proto Asteroid
                protoBelt._type = SystemBody.PlanetType.Asteroid;
                protoBelt._mass = GeneratePlanetMass(SystemBody.PlanetType.Asteroid);                   // get its mass
                protoPlanets.Add(protoBelt);
            }

            RandomShuffle(protoPlanets); // make sure the list is completly random, given that we added things in a specific order.

            // now lets generate orbits for all that:
            List<SystemBody> systemBodies = GenerateStarSystemOrbits(star, protoPlanets, totalSystemMass);

            // now loop and flesh out the planets (and maybe an asteroid belt or two):
            int bodyNo = 1;
            int beltNo = 1;
            foreach(SystemBody body in systemBodies)
            {
                if (body.Type != SystemBody.PlanetType.Asteroid)
                {
                    // flesh out planet:
                    body.Name = star.Name + " - " + bodyNo.ToString();
                    GenerateSystemBody(star, body);
                    bodyNo++;
                    star.Planets.Add(body);     // don't forget to add the planet to the star!!
                }
                else 
                {
                    // flesh out asteroid belt
                    GenerateAsteroidBelt(star, body, beltNo);       // this will add each asteriod to the star for us!!
                    beltNo++;
                }
            }
        }

        /// <summary>
        /// Creates planet orbits for the given system (star + proto-planets). 
        /// Not all proto-planets are guaranteed to remian in the system. 
        /// Also creates orbits for asteroid belts cause they have to happen at the same time for it all to shake out right.
        /// </summary>
        /// <param name="star">The Parent star of the system.</param>
        /// <param name="protoPlanets">List of Proto planets, i.e. a list of planets and their type.</param>
        /// <param name="totalSystemMass">The total mass of all planets in the system.</param>
        /// <returns>List of all the planets (SystemBody) in the system. the list should be sorted from nearst to the star to farthest away.</returns>
        private static List<SystemBody> GenerateStarSystemOrbits(Star star, List<ProtoSystemBody> protoPlanets, double totalSystemMass)
        {
            List<SystemBody> planets = new List<SystemBody>();

            foreach(ProtoSystemBody protoPlanet in protoPlanets)
            {
                // first create our system body:
                SystemBody planet = new SystemBody(star, protoPlanet._type);

                // temp hack to keep things working:
                if (protoPlanet._type != SystemBody.PlanetType.Asteroid)
                    GenerateSystemBodyOrbit(star, planet, protoPlanet._mass);  

                // okay Rod, this is where you can do your thing...

                // note that if protoPlanet._type == Asteroid then that 
                // body will become a reference for an asteriod belt... 
                // assume its mass is typical for objects in it 
                // (as for total mass of the system, asteriods are so small they count for almost nothing)
                // be aware that they do deviate from the refereence orbit by +/- MaxAsteroidOrbitDeviation (by default 0.05 ot 5%).
                if (protoPlanet._type == SystemBody.PlanetType.Asteroid)
                    planet.Orbit = GenerateAsteroidBeltReferenceOrbit(star);  // turns out it's an asteriod belt.

                planets.Add(planet);
            }

            return planets;
        }

        /// <summary>
        /// Same as GenerateStarSystemOrbits, but for plantary systems (i.e. moons).
        /// </summary>
        private static List<SystemBody> GeneratePlanetSystemOrbits(SystemBody parent, List<ProtoSystemBody> protoMoons, double totalMoonMass)
        {
            List<SystemBody> moons = new List<SystemBody>();

            foreach (ProtoSystemBody protoMoon in protoMoons)
            {
                // first create our moon:
                SystemBody moon = new SystemBody(parent, protoMoon._type);

                // Temp hack to keep things working:
                GenerateSystemBodyOrbit(parent, moon, protoMoon._mass);

                // okay Rod, this is where you can do your thing...

                moons.Add(moon);
            }

            return moons;
        }


        /// <summary>
        /// Generates a solar system body based on type.
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
        /// functions should be part of the SystemBody class as the player will likly want to re-generate them.
        /// </item>
        /// <item>
        /// <b>Moons:</b> Might be dove via some indirect recursive calling of a generilised version of this function.
        /// </item>
        /// </list>
        /// </remarks>
        private static void GenerateSystemBody(Star star, SystemBody body, SystemBody parent = null, Orbit referenceOrbit = null)
        {
            // Create the SystemBody:
            //SystemBody body = null;
           // if (IsMoon(planetType))
            //    body = new SystemBody(parent);
           // else
            //    body = new SystemBody(star);

           // body.Type = planetType;
           // body.Name = name;

            // if we have been passed a reference orbit, use it:
            if (referenceOrbit != null) 
            {
                double mass = GenerateSystemBodyMass(body, parent);
                GenerateAsteroidBeltBodyOrbit(star, body, mass, referenceOrbit);
            }
            else if (body.Type == SystemBody.PlanetType.Comet)  // if we are a comet we will need an orbit as thoses aren't pre-generated.
            {
                double mass = GenerateSystemBodyMass(body, parent);
                GenerateSystemBodyOrbit(star, body, mass);
            }

            // Create some of the basic stats:
            body.Density = RNG_NextDoubleRange(GalaxyGen.PlanetDensityByType[body.Type]);
            body.Radius = CalculateRadiusOfBody(body.Orbit.Mass, body.Density);
            double radiusSquaredInM = (body.Radius * Constants.Units.M_PER_AU) * (body.Radius * Constants.Units.M_PER_AU); // conver to m from au.
            body.SurfaceGravity = (float)((Constants.Science.GRAVITATIONAL_CONSTANT * body.Orbit.Mass) / radiusSquaredInM); // see: http://nova.stanford.edu/projects/mod-x/ad-surfgrav.html
            body.AxialTilt = (float)(m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination);
                
            // generate the planets day length:
            ///< @todo Should we do Tidaly Locked bodies??? iirc bodies trend toward being tidaly locked over time...
            body.LengthOfDay = new TimeSpan((int)Math.Round(RNG_NextDoubleRange(0, body.Orbit.OrbitalPeriod.TotalDays)), m_RNG.Next(0, 24), m_RNG.Next(0, 60), 0);
            // just a basic sainty check to make sure we dont end up with a planet rotating once every 3 minutes, It'd pull itself apart!!
            if (body.LengthOfDay < TimeSpan.FromHours(GalaxyGen.MiniumPossibleDayLength))
                body.LengthOfDay += TimeSpan.FromHours(GalaxyGen.MiniumPossibleDayLength);  

            // Note that base temp does not take into account albedo or atmosphere.
            if (IsMoon(body.Type))
                body.BaseTemperature = (float)CalculateBaseTemperatureOfBody(star, parent.Orbit.SemiMajorAxis);
            else
                body.BaseTemperature = (float)CalculateBaseTemperatureOfBody(star, body.Orbit.SemiMajorAxis);

            // generate Plate tectonics
            body.Tectonics = GenerateTectonicActivity(star, body);

            // Generate Magnetic field:
            body.MagneticFeild = (float)RNG_NextDoubleRange(GalaxyGen.PlanetMagneticFieldByType[body.Type]);
            if (body.Tectonics == SystemBody.TectonicActivity.Dead)
                body.MagneticFeild *= 0.1F; // reduce magnetic field of a dead world.
            
            // now lets generate the atmosphere:
            body.Atmosphere = GenerateAtmosphere(body);

            // Generate Ruins, note that it will only do so for suitable planets:
            GenerateRuins(star, body);
            
            ///< @todo Generate Minerials Properly instead of this ugly hack:
            body.HomeworldMineralGeneration();

            // generate moons if required for this body type:
            if (IsPlanet(body.Type))
                GenerateMoons(star, body);
        }

        /// <summary>
        /// Generates the Mass for the system body by selecting it randomly 
        /// from the range specified in GalaxyGen.PlanetMassByType.
        /// Some extra logic is run for moon to prevent them being larger then the planet they orbit.
        /// the maximum mass of a moon relative to the parent body is controlled by GalaxyGen.MaxMoonMassRelativeToParentBody.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static double GenerateSystemBodyMass(SystemBody body, SystemBody parent)
        {
            if (IsMoon(body.Type))
            {
                // quick safty check:
                if (parent == null)
                    throw new System.ArgumentException("Parent cannot be null when generating the mass of a moon.");

                // these bodies have special mass limits over and above whats in PlanetMassByType.
                double min, max;
                min = GalaxyGen.PlanetMassByType[body.Type]._min;
                max = GalaxyGen.PlanetMassByType[body.Type]._max;

                if (max > parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody)
                    max = parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody;
                if (min > max)
                    min = max;      // just to make sure we get sane values.

                return RNG_NextDoubleRange(min, max);
            }

            return RNG_NextDoubleRange(GalaxyGen.PlanetMassByType[body.Type]);
        }

        private static double GeneratePlanetMass(SystemBody.PlanetType type)
        {
            return RNG_NextDoubleRange(GalaxyGen.PlanetMassByType[type]);
        }

        private static double GenerateMoonMass(SystemBody parent, SystemBody.PlanetType type)
        {
            // quick safty check:
            if (parent == null)
                throw new System.ArgumentException("Parent cannot be null when generating the mass of a moon.");

            // these bodies have special mass limits over and above whats in PlanetMassByType.
            double min, max;
            min = GalaxyGen.PlanetMassByType[type]._min;
            max = GalaxyGen.PlanetMassByType[type]._max;

            if (max > parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody)
                max = parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody;
            if (min > max)
                min = max;      // just to make sure we get sane values.

            return RNG_NextDoubleRange(min, max);
        }

        /// <summary>
        /// Generate plate techtonics taking into consideration the mass of the planet and its age (via Star.Age).
        /// </summary>
        private static SystemBody.TectonicActivity GenerateTectonicActivity(Star star, SystemBody body)
        {
            if (body.Type != SystemBody.PlanetType.Terrestrial && body.Type != SystemBody.PlanetType.Terrestrial)
            {
                return SystemBody.TectonicActivity.NA;  // We are not a Terrestrial body, we have no Tectonics!!!
            }
            else if (m_RNG.NextDouble() < GalaxyGen.TerrestrialBodyTectonicActiviyChance)
            {
                // this planet has some plate tectonics:
                // the following should give us a number between 0 and 1 for most bodies. Earth has a number of 0.217...
                // we conver age in billion years instead of years (otherwise we get tiny numbers).
                double tectonicsChance = body.Orbit.Mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS / star.Age * 100000000; 
                tectonicsChance = GMath.Clamp01(tectonicsChance);

                SystemBody.TectonicActivity t = SystemBody.TectonicActivity.NA;

                // step down the thresholds to get the correct activity:
                if (tectonicsChance < GalaxyGen.BodyTectonicsThresholds[SystemBody.TectonicActivity.Major])
                    t = SystemBody.TectonicActivity.Major;
                if (tectonicsChance < GalaxyGen.BodyTectonicsThresholds[SystemBody.TectonicActivity.EarthLike])
                    t = SystemBody.TectonicActivity.EarthLike;
                if (tectonicsChance < GalaxyGen.BodyTectonicsThresholds[SystemBody.TectonicActivity.Minor])
                    t = SystemBody.TectonicActivity.Minor;
                if (tectonicsChance < GalaxyGen.BodyTectonicsThresholds[SystemBody.TectonicActivity.Dead])
                    t = SystemBody.TectonicActivity.Dead;

                return t;
            }

            return SystemBody.TectonicActivity.Dead;
        }

        /// <summary>
        /// Generates an orbit around a parent Star. 
        /// </summary>
        private static void GenerateSystemBodyOrbit(Star parent, SystemBody child, double childMass)
        {
            // Create the orbital values:
            double smeiMajorAxis =  RNG_NextDoubleRangeDistributedByPower(GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType],
                                                                          GalaxyGen.OrbitalDistanceDistributionByPlanetType[child.Type]);
            double eccentricity = 0;
            if (child.Type == SystemBody.PlanetType.Comet)
                eccentricity = RNG_NextDoubleRange(0.6, 0.8);       ///< @todo more magic numbers.
            else
                eccentricity = Math.Pow(RNG_NextDoubleRange(0, 0.8), 3); // get random eccentricity needs better distrubution.

            double inclination = m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination; // doesn't do much at the moment but may as well be there. Neet better Dist.
            double argumentOfPeriapsis = m_RNG.NextDouble() * 360;
            double meanAnomaly = m_RNG.NextDouble() * 360;
            double longitudeOfAscendingNode = m_RNG.NextDouble() * 360;

            // now Create the orbit:
            child.Orbit = Orbit.FromAsteroidFormat(childMass, parent.Orbit.Mass, smeiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, GalaxyGen.J2000);
        }

        /// <summary>
        /// Generates an orbit around a parent System body. 
        /// </summary>
        private static void GenerateSystemBodyOrbit(SystemBody parent, SystemBody child, double childMass)
        {
            // Create smeiMajorAxis:
            // this need to have sane min/max values given the radius of the two bodies:
            double min, max;
            min = (parent.Radius + child.Radius) * GalaxyGen.MinMoonOrbitMultiplier;
            max = GMath.Clamp((parent.Radius + child.Radius) * GalaxyGen.AbsoluteMaxMoonOrbitDistance, min, 
                                                parent.Orbit.Periapsis * GalaxyGen.RelativeMaxMoonOrbitDistance);
            double smeiMajorAxis = RNG_NextDoubleRange(min, max);  // moons dont need to be raised to a power, they have a nice range :)

            // Create the other orbital values:
            double eccentricity = Math.Pow(RNG_NextDoubleRange(0, 0.8), 2); // get random eccentricity needs better distrubution.
            double inclination = m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination; // doesn't do much at the moment but may as well be there. Neet better Dist.
            double argumentOfPeriapsis = m_RNG.NextDouble() * 360;
            double meanAnomaly = m_RNG.NextDouble() * 360;
            double longitudeOfAscendingNode = m_RNG.NextDouble() * 360;

            // now Create the orbit:
            child.Orbit = Orbit.FromAsteroidFormat(childMass, parent.Orbit.Mass, smeiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, GalaxyGen.J2000);
        }

        /// <summary>
        /// Generates an orbit for an Asteroid or Dwarf SystemBody. The orbit will be a slight deviation of the reference orbit provided.
        /// </summary>
        private static void GenerateAsteroidBeltBodyOrbit(Star parent, SystemBody child, double childMass, Orbit referenceOrbit)
        {
            // we will use the reference orbit + MaxAsteriodOrbitDeviation to constrain the orbit values:

            // Create smeiMajorAxis:
            double min, max, deviation;
            deviation = referenceOrbit.SemiMajorAxis * GalaxyGen.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.SemiMajorAxis - deviation;
            max = referenceOrbit.SemiMajorAxis + deviation;
            double smeiMajorAxis = RNG_NextDoubleRange(min, max);  // dont need to raise to power, reference orbit already did that.

            deviation = referenceOrbit.Eccentricity * Math.Pow(GalaxyGen.MaxAsteroidOrbitDeviation, 2);
            min = referenceOrbit.Eccentricity - deviation;
            max = referenceOrbit.Eccentricity + deviation;
            double eccentricity = RNG_NextDoubleRange(min, max); // get random eccentricity needs better distrubution.

            deviation = referenceOrbit.Inclination * GalaxyGen.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.Inclination - deviation;
            max = referenceOrbit.Inclination + deviation;
            double inclination = RNG_NextDoubleRange(min, max); // doesn't do much at the moment but may as well be there. Neet better Dist.

            deviation = referenceOrbit.ArgumentOfPeriapsis * GalaxyGen.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.ArgumentOfPeriapsis - deviation;
            max = referenceOrbit.ArgumentOfPeriapsis + deviation;
            double argumentOfPeriapsis = RNG_NextDoubleRange(min, max);

            deviation = referenceOrbit.LongitudeOfAscendingNode * GalaxyGen.MaxAsteroidOrbitDeviation;
            min = referenceOrbit.LongitudeOfAscendingNode - deviation;
            max = referenceOrbit.LongitudeOfAscendingNode + deviation;
            double longitudeOfAscendingNode = RNG_NextDoubleRange(min, max);

            // Keep the starting point of the orbit completly random.
            double meanAnomaly = m_RNG.NextDouble() * 360;      
            
            // now Create the orbit:
            child.Orbit = Orbit.FromAsteroidFormat(childMass, parent.Orbit.Mass, smeiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, GalaxyGen.J2000);
        }

        /// <summary>
        /// Generates an atmosphere for the provided planet based on its type.
        /// Atmosphere needs to gen:
        /// -- Albedo (affected by Hydrosphere how exactly?)  
        /// -- presure
        /// -- Hydrosphere
        /// -- Hydrosphere extent
        /// And the following are worked out by the Atmosphere:
        /// -- Greenhouse Factor
        /// -- surface Temp. (based on base temp, greehhouse factor and Albedo).
        /// </summary>
        private static Atmosphere GenerateAtmosphere(SystemBody planet)
        {
            Atmosphere atmo = new Atmosphere(planet);

            // calc albedo:
            atmo.Albedo = (float)RNG_NextDoubleRange(GalaxyGen.PlanetAlbedoByType[planet.Type]);

            // some safe defaults:
            atmo.HydrosphereExtent = 0;
            atmo.Hydrosphere = false;

            // we uses these to keep a running tally of how much gass we have generated.
            float totalATM = 0;
            float currATM = 0;
            int noOfTraceGases = 0;

            // Generate an Atmosphere
            ///< @todo Remove some of this hard coding:
            switch (planet.Type)
            {
                case SystemBody.PlanetType.GasDwarf:
                case SystemBody.PlanetType.GasGiant:
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

                case SystemBody.PlanetType.IceGiant:
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

                case SystemBody.PlanetType.Moon:
                case SystemBody.PlanetType.Terrestrial:
                    // Only Terrestrial like planets have a limited chance of having an atmo:
                    double atmoChance = GMath.Clamp01(GalaxyGen.AtmosphereGenerationModifier[planet.Type] * 
                                                        (planet.Orbit.Mass / GalaxyGen.PlanetMassByType[planet.Type]._max));

                    if (m_RNG.NextDouble() > atmoChance)
                    {
                        // Terrestrial Planets can have very large ammount of ATM.
                        // so we will generate a number to act as the total:
                        float planetsATM = (float)RNG_NextDoubleRange(0.1, 100);
                        // reduce my mass ratio relative to earth (so really small bodies cannot have massive atmos:
                        double massRatio = planet.Orbit.Mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS;
                        planetsATM = (float)GMath.Clamp((double)planetsATM * massRatio, 0.01, 200);

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
                    }
                    break;

                    // Everthing else has no atmosphere at all.
                case SystemBody.PlanetType.IceMoon:
                case SystemBody.PlanetType.DwarfPlanet:
                case SystemBody.PlanetType.Asteroid:
                case SystemBody.PlanetType.Comet:
                default:
                    break; // none
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
        /// Calls GenerateSystemBody to generate the required moons.
        /// </summary>
        private static void GenerateMoons(Star star, SystemBody parent)
        {
            // first lets see if this planet gets moons:
            if (m_RNG.NextDouble() > GalaxyGen.MoonGenerationChanceByPlanetType[parent.Type])
                return; // no moons for you :(
            
            // Okay lets work out the number of moons based on:
            // The mass of the parent in proportion to the maximum mass for a planet of that type.
            // The MaxNoOfMoonsByPlanetType
            // And a random number for randomness.
            double massRatioOfParent = parent.Orbit.Mass / GalaxyGen.PlanetMassByType[parent.Type]._max;
            double moonGenChance = massRatioOfParent * m_RNG.NextDouble() * GalaxyGen.MaxNoOfMoonsByPlanetType[parent.Type];
            moonGenChance = GMath.Clamp(moonGenChance, 1, GalaxyGen.MaxNoOfMoonsByPlanetType[parent.Type]);
            int noOfMoons = (int)Math.Round(moonGenChance);

            // now we need to work out the moon type
            // we will do this by looking at the base temp of the parent.
            // if the base temp of the planet / 150K is  > 1 then it will always be terrestrial.
            // i.e. a planet hotter then GalaxyGen.IceMoonMaximumParentTemperature will always have PlanetType.Moon.
            double tempRatio = (parent.BaseTemperature + Constants.Units.DEGREES_C_TO_KELVIN) / GalaxyGen.IceMoonMaximumParentTemperature;
            SystemBody.PlanetType pt = SystemBody.PlanetType.Moon;

            // first pass to gen mass etc:
            List<ProtoSystemBody> protoMoons = new List<ProtoSystemBody>();
            double totalMoonMass = 0;
            for (int i = 0; i < noOfMoons; ++i)
            {
                ProtoSystemBody protoMoon = new ProtoSystemBody();                     // create the proto moon
                if (m_RNG.NextDouble() > tempRatio)                                    
                    protoMoon._type = SystemBody.PlanetType.IceMoon;                   // if a random number is > tempRatio it will be an ice moon.
                else
                    protoMoon._type = SystemBody.PlanetType.IceMoon;                   // else it is a Terrestrial Moon

                protoMoon._mass = GenerateMoonMass(parent, protoMoon._type);           // Generate Mass
                totalMoonMass += protoMoon._mass;                                      // add mass to total mass.
                protoMoons.Add(protoMoon);
            }

            List<SystemBody> moons = GeneratePlanetSystemOrbits(parent, protoMoons, totalMoonMass);

            int moonNo = 1;
            foreach(SystemBody moon in moons)
            {
                moon.Name = parent.Name + " - Moon " + moonNo.ToString();

                // flesh out moon details:
                GenerateSystemBody(star, moon, parent);
                parent.Moons.Add(moon);
            }

            // if a random number is > tempRatio it will be an ice moon:
            //List<SystemBody> sorted = new List<SystemBody>();
            //for (int i = 0; i < noOfMoons; ++i)
            //{
            //    if (tempRatio < 1)
            //    {
            //        // get random type:
            //        if (m_RNG.NextDouble() > tempRatio)
            //            pt = SystemBody.PlanetType.IceMoon;
            //    }

                

            //    SystemBody newMoon = new SystemBody(parent, pt);

            //    ///< @todo fix Moons!!!!!!!!!!!!!!!!

            //    newMoon.Name = parent.Name + " - Moon " + i.ToString();
            //    GenerateSystemBody(star, newMoon, parent);
            //    sorted.Add(newMoon); // we will sort this when we are done!
            //}

            //// sort moons in decending order by semiMajorAxis:
            ////BindingList<SystemBody> sorted = parent.Moons;
            //sorted.Sort(delegate(SystemBody a, SystemBody b)
            //    {
            //        return a.Orbit.SemiMajorAxis.CompareTo(b.Orbit.SemiMajorAxis);
            //    });

            //parent.Moons = new BindingList<SystemBody>(sorted);  // now add the sorted list!!
        }

        /// <summary>
        /// This function generate ruins for the specified system Body.
        /// @todo Make Ruins Generation take star age/type into consideration??
        /// </summary>
        private static void GenerateRuins(Star star, SystemBody body)
        {
            // first we will check that this body type can have ruins on it:
            if (body.Type != SystemBody.PlanetType.Terrestrial
                || body.Type != SystemBody.PlanetType.Moon)
            {
                return; // wrong type.
            }
            else if (body.Atmosphere.Exists == false && (body.Atmosphere.Pressure > 2.5 || body.Atmosphere.Pressure < 0.01))
            {
                return; // no valid atmosphere!
            }
            else if (m_RNG.NextDouble() > 0.5)
            {
                return; // thats right... lucked out on this one.
            }

            // now if we have survived the guantlet lets gen some Ruins!!
            Ruins ruins = new Ruins();

            ruins.RuinSize = GalaxyGen.RuinsSizeDisrubution.Select(m_RNG.Next(0, 100));

            int quality = GameState.RNG.Next(0, 100);
            ruins.RuinQuality = GalaxyGen.RuinsQuilityDisrubution.Select(quality);
            if (ruins.RuinSize == Ruins.RSize.City && quality >= 95)
                    ruins.RuinQuality = Ruins.RQuality.MultipleIntact;  // special case!!

            // Ruins count:
            ruins.RuinCount = RNG_NextRange(GalaxyGen.RuinsCountRangeBySize[ruins.RuinSize]);    
            ruins.RuinCount = (uint)Math.Round(GalaxyGen.RuinsQuilityAdjustment[ruins.RuinQuality] * ruins.RuinCount);

            body.PlanetaryRuins = ruins;
        }

        #endregion

        #region Asteriod Generation Functions

        /// <summary>
        /// Generates a asteroid belt around a star based on a given reference orbit. This will include a number of Dwarf Planets.
        /// </summary>
        private static void GenerateAsteroidBelt(Star star, SystemBody referenceBody, int beltNo)
        {
            // lets calculate how many asteriods in this belt:
            int noOfAsteroids = (int)Math.Round(m_RNG.NextDouble() * GalaxyGen.MaxNoOfAsteroidsPerBelt);

            // Generate the asteriods:
            for (int i = 0; i < noOfAsteroids; ++i)
            {
                SystemBody asteroid = new SystemBody(star, SystemBody.PlanetType.Asteroid);
                asteroid.Name = star.Name + " - Asteriod " + beltNo.ToString() + "-" + i.ToString();
                   
                // Generate the asteriod:
                GenerateSystemBody(star, asteroid, null, referenceBody.Orbit);
                star.Planets.Add(asteroid);
            }

            // now lets work out how many dwarf planets there should be:
            int noOfDwarfPlanets = noOfAsteroids / GalaxyGen.NumberOfAsteroidsPerDwarfPlanet;
            for (int i = 0; i < noOfDwarfPlanets; ++i)
            {
                SystemBody dwarf = new SystemBody(star, SystemBody.PlanetType.Asteroid);
                dwarf.Name = star.Name + " - Dwarf Planet" + beltNo.ToString() + "-" + i.ToString();

                // Generate the asteriod:
                GenerateSystemBody(star, dwarf, null, referenceBody.Orbit);
                star.Planets.Add(dwarf);
            }
        }

        /// <summary>
        /// Generates a non-functions reference orbit to be used for generating orbits for specific asteroids and dwarf planets.
        /// </summary>
        private static Orbit GenerateAsteroidBeltReferenceOrbit(Star parent)
        {
            // create stationary orbit to hold data:
            Orbit referenceOrbit = Orbit.FromStationary(1);

            // create values:
            referenceOrbit.SemiMajorAxis = RNG_NextDoubleRangeDistributedByPower(GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType],
                                                                         GalaxyGen.OrbitalDistanceDistributionByPlanetType[SystemBody.PlanetType.Asteroid]);
            referenceOrbit.Eccentricity = Math.Pow(RNG_NextDoubleRange(0, 0.8), 3); // get random eccentricity needs better distrubution.
            referenceOrbit.Inclination = m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination; // doesn't do much at the moment but may as well be there. Neet better Dist.
            referenceOrbit.ArgumentOfPeriapsis = m_RNG.NextDouble() * 360;
            referenceOrbit.MeanAnomaly = m_RNG.NextDouble() * 360;
            referenceOrbit.LongitudeOfAscendingNode = m_RNG.NextDouble() * 360;

            return referenceOrbit;
        }

        #endregion

        #region Comet Generation Functions

        /// <summary>
        /// Generates a random number of comets for a given star. The number of gererated will 
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        private static void GenerateComets(Star star)
        {
            // first lets get a random number between our minium nad maximum number of comets:
            int min = GalaxyGen.MiniumCometsPerSystem;
            if (min > GalaxyGen.MaxNoOfComets)
                min = GalaxyGen.MaxNoOfComets;

            int noOfComets = m_RNG.Next(min, GalaxyGen.MaxNoOfComets + 1);
            
            // now lets create the comets:
            for (int i = 0; i < noOfComets; ++i)
            {
                SystemBody newComet = new SystemBody(star, SystemBody.PlanetType.Comet);

                newComet.Name = star.Name + " - Comet" + i.ToString();

                GenerateSystemBody(star, newComet);
                star.Planets.Add(newComet);
            }
        }

        #endregion

        #region Jump Point Generation functions

        /// <summary>
        /// Generates a jump points in the designated system.
        /// Used by JumpPoint class when connecting an a existing system
        /// with no unconnected jump points.
        /// </summary>
        public static JumpPoint GenerateJumpPoint(StarSystem system)
        {
            m_RNG = new Random(GalaxyGen.SeedRNG.Next()); // Is there a better way?

            Star luckyStar;
            do
            {
                luckyStar = system.Stars[m_RNG.Next(system.Stars.Count)];
            } while (luckyStar.Planets.Count != 0);

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
            if (star.Planets.Count == 0)
            {
                return 0; // Don't generate JP's on planetless stars.
            }

            int numJumpPoints = 1; // Each star always generates a JP.

            // Give a chance per planet to generate a JumpPoint
            foreach (SystemBody currentPlanet in star.Planets)
            {
                if (currentPlanet.Type == SystemBody.PlanetType.Comet || currentPlanet.Type == SystemBody.PlanetType.Asteroid)
                {
                    // Don't gen JP's around comets or asteroids.
                    continue;
                }

                int chance = Constants.GameSettings.JumpPointGenerationChance;

                // Higher mass planets = higher chance.
                double planetEarthMass = currentPlanet.Orbit.Mass / Constants.Units.EARTH_MASS_IN_KILOGRAMS;
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
            foreach (SystemBody currentPlanet in star.Planets)
            {
                if (currentPlanet.Type == SystemBody.PlanetType.Comet || currentPlanet.Type == SystemBody.PlanetType.Asteroid)
                {
                    // Don't gen JP's around comets or asteroids.
                    continue;
                }

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

        private static bool IsMoon(SystemBody.PlanetType pt)
        {
            if (pt == SystemBody.PlanetType.Moon
                || pt == SystemBody.PlanetType.IceMoon)
                return true;

            return false;
        }

        private static bool IsPlanet(SystemBody.PlanetType pt)
        {
            if (pt == SystemBody.PlanetType.Terrestrial
                || pt == SystemBody.PlanetType.GasDwarf
                || pt == SystemBody.PlanetType.GasGiant
                || pt == SystemBody.PlanetType.IceGiant)
                return true;

            return false;
        }

        private static bool IsPlanetOrDwarfPlanet(SystemBody.PlanetType pt)
        {
            if (pt == SystemBody.PlanetType.Terrestrial
                || pt == SystemBody.PlanetType.GasDwarf
                || pt == SystemBody.PlanetType.GasGiant
                || pt == SystemBody.PlanetType.IceGiant
                || pt == SystemBody.PlanetType.DwarfPlanet)
                return true;

            return false;
        }

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range.
        /// </summary>
        public static double RNG_NextDoubleRange(double min, double max)
        {
            return min + m_RNG.NextDouble() * (max - min);
        }

        /// <summary>
        /// Version of RNG_NextDoubleRange(double min, double max) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        public static double RNG_NextDoubleRange(GalaxyGen.MinMaxStruct minMax)
        {
            return RNG_NextDoubleRange(minMax._min, minMax._max);
        }

        /// <summary>
        /// Raises the random number generated to the power provided to produce a non-uniform selection from the range.
        /// </summary>
        public static double RNG_NextDoubleRangeDistributedByPower(double min, double max, double power)
        {
            return min + Math.Pow(m_RNG.NextDouble(), power) * (max - min);
        }

        /// <summary>
        /// Version of RNG_NextDoubleRangeDistributedByPower(double min, double max, double power) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        public static double RNG_NextDoubleRangeDistributedByPower(GalaxyGen.MinMaxStruct minMax, double power)
        {
            return RNG_NextDoubleRangeDistributedByPower(minMax._min, minMax._max, power);
        }

        /// <summary>
        /// Returns a value between the min and max.
        /// </summary>
        public static uint RNG_NextRange(GalaxyGen.MinMaxStruct minMax)
        {
            return (uint)m_RNG.Next((int)minMax._min, (int)minMax._max);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(GalaxyGen.MinMaxStruct minMax, double selection)
        {
            return minMax._min + selection * (minMax._max - minMax._min); ;
        }

        /// <summary>
        /// Randomly reverses the current sign of the value, i.e. it will randomly make the number positive or negative.
        /// </summary>
        public static double RandomizeSign(double value)
        {
            // 50/50 odds of reversing the sign:
            if (m_RNG.NextDouble() > 0.5)
                return value * -1;

            return value;
        }

        /// <summary>
        /// Calculates the radius of a body.
        /// </summary>
        /// <param name="mass">The mass of the body in Kg</param>
        /// <param name="density">The density in g/cm^2</param>
        /// <returns>The radius in AU</returns>
        public static double CalculateRadiusOfBody(double mass, double density)
        {
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * (density / 1000)), 0.3333333333); // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
                                                                                                   // 0.3333333333 should be 1/3 but 1/3 gives radius of 0.999999 for any mass/density pair, so i used 0.3333333333
            return radius / 1000 / 100 / Constants.Units.KM_PER_AU;     // convert from cm to AU.
        }

        /// <summary>
        /// Calculates the temperature of a body given its parent star and its distance from that star.
        /// @note For info on how the Temp. is calculated see: http://en.wikipedia.org/wiki/Stefan%E2%80%93Boltzmann_law
        /// </summary>
        /// <param name="parentStar">The star this body is orbiting.</param>
        /// <param name="distanceFromStar">the SemiMojorAxis of the body with regards to the star. (i.e. for moons it is the sma of its parent planet).</param>
        /// <returns>Temperature in Degrees C</returns>
        public static double CalculateBaseTemperatureOfBody(Star parentStar, double distanceFromStar)
        {
            double temp = (parentStar.Temperature + Constants.Units.DEGREES_C_TO_KELVIN); // we need to work in kelvin here.
            temp = temp * Math.Sqrt(parentStar.Radius / (2 * distanceFromStar));
            return temp + Constants.Units.KELVIN_TO_DEGREES_C;  // convert back to degrees.
        }

        /// <summary>
        /// Very simple random shuffle.
        /// </summary>
        private static void RandomShuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = m_RNG.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #endregion
    }
}

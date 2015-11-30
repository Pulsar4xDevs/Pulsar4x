using Pulsar4X.Entities;
using Pulsar4X.Helpers.GameMath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Pulsar4X
{
    /// <summary>
    /// Stargen containes functions for generating new star systems based on the information contained in GalaxyGen. 
    /// </summary>
    /// <remarks>
    /// Stargen works by the following process:
    /// <list type="number">
    /// <item>
    /// First the Stars are generated, starting with their spectral type and then using that to generate sane vaslues for all other star data.
    /// </item>
    /// <item>
    /// Once a star is generated Planets, Asteriods, Dwarf planets and Moons (aka System Bodies) are generated for the star. 
    /// This process is quite a lot more complicated but can best be describe as a three stage process:
    ///     <list type="number">
    ///     <item>
    ///     The first stage defines a number "Protoplanets" consiting the mass and planet type. amoungst these "Protoplanets" are 
    ///     single Asteroids which will act as references for later generation of entire Asteroid belts, including Dwarf Planets. 
    ///     The list of protoplanets is sorted to make sure Terrestrial planets are mostly at the top of the list.
    ///     Note that moons are not generated at this stage.
    ///     </item>
    ///     <item>
    ///     The second stage involves generating orbits for the planets. This is done in much the same was as for Stars, 
    ///     by making sure that planets are at least 10x more gravationaly bound to the parent star then they are to their 
    ///     nearest neighbours we ensure some "sane" seperation between planets.
    ///     </item>
    ///     The third and final pass involes fleshing out the remain properties of the planets. This includes Densite, Radius, 
    ///     Temerature, Atmosphere, Ruins, Minerials, etc. For each reference asteroid an aproprate number of dwarf planets and asteroids
    ///     Are generated (note that they only go though this last stage with mass and orbits generated based on the reference asteroid provided).
    ///     Planets also have their moons generated at this stage. It is worth noting that moons go through the same 3 stage process, it is just nested
    ///     inside the 3rd stage for planets.
    ///     </list>
    /// Generation of system bodies repsent the meat of Star Generation.
    /// </item>
    /// <item>
    /// Comets are generated in their own Single stage process. it is almost identical to the process Asteriod have, except they do not have a reference orbit
    /// (A lot of code is reused for all System Body generation, including comets). 
    /// </item>
    /// <item>
    /// Orbits are generated for the stars. This is done is such a way as to ensure that the stars are more gravitationaly bound to the 
    /// Parent (largest) star in the system then to any of the child stars.
    /// </item>
    /// <item>
    /// Jump points are generated for each star in the system. Unlike Aurora where only the first star in a system had jump points in 
    /// Pulsar every star has its own, even in multi star systems. 
    /// </item>
    /// <item>
    /// Jump points are generated for each star in the system. Unlike Aurora where only the first star in a system had jump points in 
    /// Pulsar every star has its own, even in multi star systems. 
    /// </item>
    /// <item>
    /// Finally NPRs are generated. Note that this has not yet been implemented as NPR Factions are not yet supported.
    /// </item>
    /// </list>
    /// In addition it contains a function which will return a hard coded veriosn of our own Solar System as well as several related convience functions.
    /// </remarks>
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

        /// <summary>
        /// Creates a single Star system with the provided name. It generates a new seed for the system.
        /// </summary>
        public static StarSystem CreateSystem(string name)
        {
            return CreateSystem(name, -1);
        }

        /// <summary>
        /// Creates a single Star System using the random seed provided. 
        /// If given the same seed twice it should generate 2 identical systems even on different PCs.
        /// </summary>
        public static StarSystem CreateSystem(string name, int seed, int numJumpPoints = -1)
        {
            // create new RNG with Seed.
            if (seed == -1)
            {
                seed = GalaxyGen.SeedRNG.Next();
            }

            m_RNG = new Random(seed);

            StarSystem newSystem = new StarSystem(name, seed);

            int noOfStars = m_RNG.Next(1, 5);
            for (int i = 0; i < noOfStars; ++i)
            {
                Star newStar = GenerateStar(newSystem);

                GenerateSystemBodiesForStar(newStar);
                GenerateComets(newStar);
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

        /// <summary>
        /// A system specifically generated to stress test orbit code.
        /// </summary>
        public static StarSystem CreateStressTest()
        {
            StarSystem Sol= new StarSystem("StressTest", -1);

            Star Sun = new Star("Sol", Constants.Units.SolarRadiusInAu, 5778, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Orbit = Orbit.FromStationary(Constants.Units.SolarMassInKG);
            Sun.Class = "G2";

            Sun.Radius = Distance.ToAU(696000);

            Sol.Stars.Add(Sun);

            Random RNG = new Random();

            for (int i = 0; i < 500; i++)
            {
                i++;
                SystemBody newPlanet = new SystemBody(Sun, SystemBody.PlanetType.Comet);
                newPlanet.Name = "New Planet " + i;

                newPlanet.Orbit = Orbit.FromAsteroidFormat(5.9726E24, Sun.Orbit.Mass, RNG.NextDouble() * 100, RNG.NextDouble(), 0, RNG.NextDouble() * 360, RNG.NextDouble() * 360, RNG.NextDouble() * 360, GameState.Instance.CurrentDate);

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

        /// <summary>
        /// Creates our own solar system.
        /// </summary>
        public static StarSystem CreateSol()
        {
            StarSystem Sol = new StarSystem("Sol", GalaxyGen.SeedRNG.Next());

            // Used for JumpPoint generation.
            m_RNG = new Random(Sol.Seed);

            Star Sun = new Star("Sol", Constants.Units.SolarRadiusInAu, 5505, 1, SpectralType.G, Sol);
            Sun.Age = 4.6E9;
            Sun.Orbit = Orbit.FromStationary(Constants.Units.SolarMassInKG);
            Sun.Class = "G2";
            SetHabitableZone(Sun);
            Sol.Stars.Add(Sun);
            Sol.GenerateSurveyPoints(); //must be done after the construction of orbit,and after stars[0] is created as that is where the mass value in use is created.

            SystemBody Mercury = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Mercury.Name = "Mercury";
            Mercury.Orbit = Orbit.FromMajorPlanetFormat(3.3022E23, Sun.Orbit.Mass, 0.387098, 0.205630, 0, 48.33167, 29.124, 252.25084, GalaxyGen.J2000);
            Mercury.Radius = Distance.ToAU(2439.7);
            double x, y;
            Mercury.SurfaceGravity = 3.724f; //from aurora, not necessarily accurate.
            Mercury.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Mercury.Position.System = Sol;
            Mercury.Position.X = x;
            Mercury.Position.Y = y;
            Sun.Planets.Add(Mercury);

            SystemBody Venus = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Venus.Name = "Venus";
            Venus.Orbit = Orbit.FromMajorPlanetFormat(4.8676E24, Sun.Orbit.Mass, 0.72333199, 0.00677323, 0, 76.68069, 131.53298, 181.97973, GalaxyGen.J2000);
            Venus.Radius = Distance.ToAU(6051.8);
            Venus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Venus.SurfaceGravity = 8.918f; //from aurora, not necessarily accurate.
            AddGasToAtmoSafely(Venus.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 50.0f); //N, value is from aurora
            AddGasToAtmoSafely(Venus.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(12), 50.0f); //CO2, value is from aurora
            Venus.Atmosphere.UpdateState();
            Venus.Position.System = Sol;
            Venus.Position.X = x;
            Venus.Position.Y = y;
            Sun.Planets.Add(Venus);

            SystemBody Earth = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Earth.Name = "Earth";
            Earth.Orbit = Orbit.FromMajorPlanetFormat(5.9726E24, Sun.Orbit.Mass, 1.00000011, 0.01671022, 0, -11.26064, 102.94719, 100.46435, GalaxyGen.J2000);
            Earth.Radius = Distance.ToAU(6378.1);
            Earth.BaseTemperature = Temperature.ToCelsius(279.3f);  //(float)CalculateBaseTemperatureOfBody(Sun, Earth.Orbit.SemiMajorAxis);
            Earth.Tectonics = SystemBody.TectonicActivity.EarthLike;
            Earth.SurfaceGravity = 9.8f;
            Earth.Atmosphere = new Atmosphere(Earth);
            Earth.Atmosphere.Albedo = 0.306f;
            Earth.Atmosphere.SurfaceTemperature = Earth.BaseTemperature;
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.78f);  // N
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.21f);  // O
            AddGasToAtmoSafely(Earth.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.01f);  // Ar
            Earth.Atmosphere.UpdateState();
            Earth.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Earth.Position.System = Sol;
            Earth.Position.X = x;
            Earth.Position.Y = y;
            Sun.Planets.Add(Earth);

            SystemBody Moon = new SystemBody(Earth, SystemBody.PlanetType.Moon);
            Moon.Name = "Moon";
            Moon.Orbit = Orbit.FromAsteroidFormat(0.073E24, Earth.Orbit.Mass, Distance.ToAU(384748), 0.0549006, 0, 0, 0, 0, GalaxyGen.J2000);
            Moon.Radius = Distance.ToAU(1738.14);
            Moon.SurfaceGravity = 1.666f; //value from aurora
            Moon.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Moon.Position.System = Sol;
            Moon.Position.X = Earth.Position.X + x;
            Moon.Position.Y = Earth.Position.Y + y;
            Earth.Moons.Add(Moon);

            SystemBody Mars = new SystemBody(Sun, SystemBody.PlanetType.Terrestrial);
            Mars.Name = "Mars";
            Mars.Orbit = Orbit.FromMajorPlanetFormat(0.64174E24, Sun.Orbit.Mass, 1.52366231, 0.09341233, 1.85061, 49.57854, 336.04084, 355.45332, GalaxyGen.J2000);
            Mars.Radius = Distance.ToAU(3396.2);
            Mars.BaseTemperature = (float)CalculateBaseTemperatureOfBody(Sun, Mars.Orbit.SemiMajorAxis);// 210.1f + (float)Constants.Units.KELVIN_TO_DEGREES_C;
            Mars.Tectonics = SystemBody.TectonicActivity.Dead;
            Mars.SurfaceGravity = 3.71f;
            Mars.Atmosphere = new Atmosphere(Mars);
            Mars.Atmosphere.Albedo = 0.250f;
            Mars.Atmosphere.SurfaceTemperature = Mars.BaseTemperature;
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(12), 0.95f * 0.01f);  // C02% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(6), 0.027f * 0.01f);  // N% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(9), 0.007f * 0.01f);  // O% * Mars Atms
            AddGasToAtmoSafely(Mars.Atmosphere, AtmosphericGas.AtmosphericGases.SelectAt(11), 0.016f * 0.01f);  // Ar% * Mars Atms
            Mars.Atmosphere.UpdateState();
            Mars.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Mars.Position.System = Sol;
            Mars.Position.X = x;
            Mars.Position.Y = y;
            Sun.Planets.Add(Mars);

            SystemBody Jupiter = new SystemBody(Sun, SystemBody.PlanetType.GasGiant);
            Jupiter.Name = "Jupiter";
            Jupiter.Orbit = Orbit.FromMajorPlanetFormat(1898.3E24, Sun.Orbit.Mass, 5.20336301, 0.04839266, 1.30530, 100.55615, 14.75385, 34.40438, GalaxyGen.J2000);
            Jupiter.Radius = Distance.ToAU(71492);
            Jupiter.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Jupiter.Position.System = Sol;
            Jupiter.Position.X = x;
            Jupiter.Position.Y = y;
            Sun.Planets.Add(Jupiter);

            SystemBody Saturn = new SystemBody(Sun, SystemBody.PlanetType.GasGiant);
            Saturn.Name = "Saturn";
            Saturn.Orbit = Orbit.FromMajorPlanetFormat(568.36E24, Sun.Orbit.Mass, 9.53707032, 0.05415060, 2.48446, 113.71504, 92.43194, 49.94432, GalaxyGen.J2000);
            Saturn.Radius = Distance.ToAU(60268);
            Saturn.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Saturn.Position.System = Sol;
            Saturn.Position.X = x;
            Saturn.Position.Y = y;
            Sun.Planets.Add(Saturn);

            SystemBody Uranus = new SystemBody(Sun, SystemBody.PlanetType.IceGiant);
            Uranus.Name = "Uranus";
            Uranus.Orbit = Orbit.FromMajorPlanetFormat(86.816E24, Sun.Orbit.Mass, 19.19126393, 0.04716771, 0.76986, 74.22988, 170.96424, 313.23218, GalaxyGen.J2000);
            Uranus.Radius = Distance.ToAU(25559);
            Uranus.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Uranus.Position.System = Sol;
            Uranus.Position.X = x;
            Uranus.Position.Y = y;
            Sun.Planets.Add(Uranus);

            SystemBody Neptune = new SystemBody(Sun, SystemBody.PlanetType.IceGiant);
            Neptune.Name = "Neptune";
            Neptune.Orbit = Orbit.FromMajorPlanetFormat(102E24, Sun.Orbit.Mass, Distance.ToAU(4495.1E6), 0.011, 1.8, 131.72169, 44.97135, 304.88003, GalaxyGen.J2000);
            Neptune.Radius = Distance.ToAU(24764);
            Neptune.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Neptune.Position.System = Sol;
            Neptune.Position.X = x;
            Neptune.Position.Y = y;
            Sun.Planets.Add(Neptune);

            SystemBody Pluto = new SystemBody(Sun, SystemBody.PlanetType.DwarfPlanet);
            Pluto.Name = "Pluto";
            Pluto.Orbit = Orbit.FromMajorPlanetFormat(0.0131E24, Sun.Orbit.Mass, Distance.ToAU(5906.38E6), 0.24880766, 17.14175, 110.30347, 224.06676, 238.92881, GalaxyGen.J2000);
            Pluto.Radius = Distance.ToAU(1195);
            Pluto.Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);
            Pluto.Position.System = Sol;
            Pluto.Position.X = x;
            Pluto.Position.Y = y;
            Sun.Planets.Add(Pluto);

            GenerateJumpPoints(Sol);

            // Clean up cached RNG:
            m_RNG = null;

            GameState.Instance.StarSystems.Add(Sol);
            GameState.Instance.StarSystemCurrentIndex++;
            return Sol;
        }

        #endregion

        #region Orbit Generation Functions

        /// <summary>
        /// Generates Star orbits.
        /// </summary>
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
            double gravAttractionToParent = Constants.Science.GravitationalConstant * star1.Orbit.Mass * planetMass / (maxApo * maxApo);

            // Solve for distance to star2 with 10x less gravitational attraction than to star1.
            // (Note, 10x less depends on a 0.1 value for GalaxyGen.StarOrbitGravityFactor
            return Math.Sqrt(Constants.Science.GravitationalConstant * star2.Orbit.Mass * planetMass / gravAttractionToParent * GalaxyGen.StarOrbitGravityFactor);
        }

        /// <summary>
        /// Creates planet orbits for the given system (star + proto-planets). 
        /// Not all proto-planets are guaranteed to remian in the system. 
        /// Also creates orbits for asteroid belts cause they have to happen at the same time for it all to shake out right.
        /// </summary>
        /// <param name="parent">The Parent star of the system.</param>
        /// <param name="protoPlanets">List of Proto planets, i.e. a list of planets and their type.</param>
        /// <param name="totalSystemMass">The total mass of all planets in the system.</param>
        /// <returns>List of all the planets (SystemBody) in the system. the list should be sorted from nearst to the star to farthest away.</returns>
        private static List<SystemBody> GenerateStarSystemOrbits(Star parent, List<ProtoSystemBody> protoPlanets, double totalSystemMass)
        {
            List<SystemBody> planets = new List<SystemBody>();
            List<SystemBody> rejected = new List<SystemBody>();
            double remainingSystemMass = totalSystemMass;
            double minDistance = GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType]._min;
            double remainingDistance = GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType]._max - minDistance;
            double insideOrbitApoapsis = 0;
            double insideOrbitMass = 0;

            for (int i = 0; i < protoPlanets.Count; i++)
            {
                ProtoSystemBody currentProto = protoPlanets[i];

                double massRatio = currentProto._mass / remainingSystemMass;
                double maxDistance = remainingDistance * massRatio + minDistance;

                if (currentProto._type == SystemBody.PlanetType.IceGiant)
                {
                    if (maxDistance < parent.MaxHabitableRadius)
                    {
                        // We're too close for an ice giant right now.
                        // Find a next non-ice giant and swap it out.
                        int insideI = i;
                        while (insideI < protoPlanets.Count && protoPlanets[insideI]._type == SystemBody.PlanetType.IceGiant)
                        {
                            insideI++;
                        }
                        if (insideI == protoPlanets.Count)
                        {
                            // We couldn't find a non-ice giant planet to swap with. This ice giant got blown out of the system.
                            protoPlanets.Remove(currentProto);
                        }
                        else
                        {
                            // Found a body to swap with at index insideI.
                            protoPlanets[i] = protoPlanets[insideI];
                            protoPlanets[insideI] = currentProto;
                        }
                        i--; // Since we swapped a new planet into this position, we want to reevaluate this position.
                        continue;
                    }
                    if (minDistance < parent.MaxHabitableRadius)
                    {
                        minDistance = parent.MaxHabitableRadius;
                    }
                }

                // Create our system body:
                SystemBody planet = new SystemBody(parent, currentProto._type);

                planet.Orbit = FindClearOrbit(parent.Orbit.Mass, currentProto._mass, insideOrbitMass, insideOrbitApoapsis, minDistance, maxDistance);

                if (planet.Orbit.Apoapsis > GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType]._max)
                {
                    // Planet could not fit in the system.
                    remainingSystemMass -= currentProto._mass;
                    rejected.Add(planet);
                    continue;
                }

                planets.Add(planet);

                // Prep for next loop pass.
                insideOrbitApoapsis = planet.Orbit.Apoapsis;
                minDistance = planet.Orbit.Apoapsis;
                remainingDistance = GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType]._max - planet.Orbit.Apoapsis;

                insideOrbitMass = planet.Orbit.Mass;
                remainingSystemMass -= planet.Orbit.Mass;
            }

            return planets;
        }

        private static Orbit FindClearOrbit(double parentMass, double mass, double insideOrbitMass, double insideOrbitApoapsis, double minDistance, double maxDistance)
        {
            // Adjust minDistance
            double graveAttractionInsiderNumerator = Constants.Science.GravitationalConstant * mass * insideOrbitMass;
            double graveAttractionParentNumerator = Constants.Science.GravitationalConstant * mass * parentMass;
            double gravAttractionToInsideOrbit = graveAttractionInsiderNumerator / ((minDistance - insideOrbitApoapsis) * (minDistance - insideOrbitApoapsis));
            double gravAttractionToParent = graveAttractionParentNumerator / (minDistance * minDistance);

            // Make sure we're 10x more attracted to our Parent, then our inside neighbor.
            while (gravAttractionToInsideOrbit * GalaxyGen.PlanetOrbitGravityFactor > gravAttractionToParent)
            {
                // We're too attracted to our inside neighbor, increase minDistance by 1%.
                // Assuming our parent is more massive than our inside neightbor, then this will "tip" us to be more attracted to parent.
                minDistance += minDistance * 0.01;

                // Reevaluate our gravitational attractions with new minDistance.
                gravAttractionToInsideOrbit = graveAttractionInsiderNumerator / ((minDistance - insideOrbitApoapsis) * (minDistance - insideOrbitApoapsis));
                gravAttractionToParent = graveAttractionParentNumerator / (minDistance * minDistance);
            }

            double sma;
            double eccentricity;

            if (minDistance > maxDistance)
            {
                // We don't want eccentricity to be 0, but we also don't want to go below minDistance,
                // but we ALSO don't want to go too far ABOVE maxDistance.
                // Get a LOW random eccentricity.
                eccentricity = RNG_NextDoubleRange(0, 0.1);

                // Calculate our SemiMajorAxis from our periapsis (minDistance) and our generated eccentricity.
                sma = minDistance / (1 - eccentricity);

                // Note: Our Apoapsis is going to be above maxDistance. We will "scrunch" the rest of the system to compenstate.
                // if Apoapsis > our StarTypeMaxDistance, then we'll remove the planet in the calling function.
            }
            else
            {
                // Pick a random SMA between minDistance and maxDistance.
                sma = RNG_NextDoubleRange(minDistance, maxDistance);

                // Calculate max eccentricity.
                // First calc max eccentricity for the apoapsis.
                double maxApoEccentricity = (maxDistance - sma) / sma;
                // Now calc max eccentricity for periapsis.
                double minPeriEccentricity = -((minDistance - sma) / sma);

                // Use the smaller value.
                if (minPeriEccentricity < maxApoEccentricity)
                {
                    // We use maxApoEccentricity in next calc.
                    maxApoEccentricity = minPeriEccentricity;
                }

                // Now scale down eccentricity by a random factor.
                eccentricity = m_RNG.NextDouble() * maxApoEccentricity;
            }

            Orbit clearOrbit = Orbit.FromAsteroidFormat(mass, parentMass, sma, eccentricity, m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination, m_RNG.NextDouble() * 360, m_RNG.NextDouble() * 360, m_RNG.NextDouble() * 360, GameState.Instance.CurrentDate);

            return clearOrbit;
        }

        /// <summary>
        /// Same as GenerateStarSystemOrbits, but for plantary systems (i.e. moons).
        /// </summary>
        private static List<SystemBody> GeneratePlanetSystemOrbits(SystemBody parent, List<ProtoSystemBody> protoMoons, double totalSystemMass, double systemMinDistance, double systemMaxDistance)
        {
            List<SystemBody> moons = new List<SystemBody>();
            List<SystemBody> rejected = new List<SystemBody>();
            double remainingSystemMass = totalSystemMass;
            double remainingDistance = systemMaxDistance - systemMinDistance;
            double minDistance = systemMinDistance;
            double insideOrbitApoapsis = 0;
            double insideOrbitMass = 0;

            for (int i = 0; i < protoMoons.Count; i++)
            {
                ProtoSystemBody currentProto = protoMoons[i];

                double massRatio = currentProto._mass / remainingSystemMass;
                double maxDistance = remainingDistance * massRatio + minDistance;

                // Create our system body:
                SystemBody planet = new SystemBody(parent, currentProto._type);

                planet.Orbit = FindClearOrbit(parent.Orbit.Mass, currentProto._mass, insideOrbitMass, insideOrbitApoapsis, minDistance, maxDistance);

                if (planet.Orbit.Apoapsis > systemMaxDistance)
                {
                    // Proto could not fit in the system.
                    remainingSystemMass -= currentProto._mass;
                    rejected.Add(planet); // This is purely for debugging.
                    continue;
                }

                moons.Add(planet);

                // Prep for next loop pass.
                insideOrbitApoapsis = planet.Orbit.Apoapsis;
                minDistance = planet.Orbit.Apoapsis;
                remainingDistance = systemMaxDistance - planet.Orbit.Apoapsis;

                insideOrbitMass = planet.Orbit.Mass;
                remainingSystemMass -= planet.Orbit.Mass;
            }

            return moons;
        }

        /// <summary>
        /// Generates an orbit around a parent Star. User for Comet orbits for the most part.
        /// </summary>
        /// <remarks>
        /// To create the orbit of a System Body 6 seperate values must first be generated:
        ///     <list type="Bullet">
        ///     <item>
        ///     <b>SemiMajorAxis:</b> Randomly selected based on a range for the given star type, see: GalaxyGen.OrbitalDistanceByStarSpectralType
        ///     and GalaxyGen.OrbitalDistanceDistributionByPlanetType.
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
        /// </remarks>
        private static void GenerateSystemBodyOrbit(Star parent, SystemBody child, double childMass)
        {
            // Create the orbital values:
            double smeiMajorAxis = RNG_NextDoubleRangeDistributedByPower(GalaxyGen.OrbitalDistanceByStarSpectralType[parent.SpectralType],
                                                                          GalaxyGen.OrbitalDistanceDistributionByPlanetType[child.Type]);
            double eccentricity = 0;
            if (child.Type == SystemBody.PlanetType.Comet)
                eccentricity = RNG_NextDoubleRange(0.6, 0.8);       ///< @todo more magic numbers.
            else
                eccentricity = Math.Pow(RNG_NextDoubleRange(0, 0.8), 3); // get random eccentricity less magic numbers

            double inclination = m_RNG.NextDouble() * GalaxyGen.MaxPlanetInclination; // doesn't do much at the moment but may as well be there.
            double argumentOfPeriapsis = m_RNG.NextDouble() * 360;
            double meanAnomaly = m_RNG.NextDouble() * 360;
            double longitudeOfAscendingNode = m_RNG.NextDouble() * 360;

            // now Create the orbit:
            child.Orbit = Orbit.FromAsteroidFormat(childMass, parent.Orbit.Mass, smeiMajorAxis, eccentricity, inclination,
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, GameState.Instance.CurrentDate);
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
                                                    longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, GameState.Instance.CurrentDate);
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
            SpectralType st = SpectralType.M;
            if (GalaxyGen.RealStarSystems)
                st = GalaxyGen.StarTypeDistributionForRealStars.Select(m_RNG.NextDouble());
            else
                st = GalaxyGen.StarTypeDistributionForFakeStars.Select(m_RNG.NextDouble());
          
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
            system.GenerateSurveyPoints(); //must happen after mass is created. PopulateStarData sets the mass value among other things up properly.
            return star;
        }

        /// <summary>
        /// Generates Data for a star based on it's spectral type and populates it with the data.
        /// @note Does not generate a name for the star.
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
            GenerateFullSpectralClass(star);

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

        /// <summary>
        /// Generates a string specifing the full spectral class form a star.
        /// </summary>
        public static void GenerateFullSpectralClass(Star star)
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
        /// This function intigates each stage of System Body generation for a given star. 
        /// Note that most of the actual wortk is done by other functions. 
        /// </summary>
        /// <remarks>
        /// While most of the actual work for each stage is done by other functions this
        /// function is responsable for determining the nubmer of stare a given star should have
        /// and the number of asteroid belts it should have, if any at all.
        /// 
        /// Ideally the bigger the star the more planets we should generate. So we use a mass ratio 
        /// in addition to a Second Tweakable ratio to get a number we can mulitply against the 
        /// maximum number of planets to get the final number to generate. The tweakable ration is 
        /// defined in GalaxyGen.StarSpecralTypePlanetGenerationRatio.
        /// 
        /// The generated list of protoplanets is sorted to push Terrestrial planets to the top of the list.
        /// This is done to ensure that their orbits are in the inner system.
        /// 
        /// Deciding on the number of Asteroid belts is simply done by randomly choosing a number 
        /// from 0 to GalaxyGen.MaxNoOfAsteroidBelts. The result is the number of asteriod belts to generate.
        /// 
        /// Most of the work for stage 2, i.e. Orbits, is done by GenerateStarSystemOrbits.
        /// 
        /// Most of the work for stage 3, i.e. fleshing out the Planet details, is done by GenerateSystemBody
        /// or GenerateAsteroidBelt, depending on the SystemBody type.
        /// </remarks>
        private static void GenerateSystemBodiesForStar(Star star)
        {
            // lets start by determining if planets will be generated at all:
            if (m_RNG.NextDouble() > GalaxyGen.PlanetGenerationChance)
                return;  // nope, this star has no planets.

            double starMassRatio = GMath.Clamp01(star.Orbit.Mass / Constants.Units.SolarMassInKG);   // heavy star = more material, in relation to Sol.
            double starSpecralTypeRatio =  GalaxyGen.StarSpecralTypePlanetGenerationRatio[star.SpectralType];    // tweakble

            // final 'chance' for number of planets generated. take into consideration star mass and balance decisions for star class.
            double finalGenerationChance = GMath.Clamp01(starMassRatio * starSpecralTypeRatio);

            // using the planet generation chance we will calculate the number of additional planets over and above the minium of 1. 
            int noOfPlanetsToGenerate = (int)GMath.Clamp(finalGenerationChance * GalaxyGen.MaxNoOfPlanets, 1, GalaxyGen.MaxNoOfPlanets);

            // create protoPlanet list:
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
                totalSystemMass += protoBelt._mass;
                protoPlanets.Add(protoBelt);
            }

            RandomShuffle(protoPlanets); // make sure the list is completly random, given that we added things in a specific order.
            SortPlanetList(protoPlanets);

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
                    FinalizeSystemBodyGeneration(star, body);
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
        /// Sorts a list of protoplanets, putting terra planets at the front.
        /// This is not a perfect sort it just pushes the terra planets higher up the list.
        /// </summary>
        private static void SortPlanetList(List<ProtoSystemBody> list)
        {
            if (list.Count < 4)
                return; // nothing we can do.

            int halfListCount = list.Count / 2;

            // we want to move terra planets higher up.
            int swapIndex = 0;
            for (int i = halfListCount; i < list.Count; ++i)
            {
                if (list[i]._type == SystemBody.PlanetType.Terrestrial)
                {
                    // lets just make sure we don't swap with another terra planet.
                    while (list[swapIndex]._type == SystemBody.PlanetType.Terrestrial && swapIndex < halfListCount)
                    {
                        swapIndex++;
                    }

                    if (swapIndex == halfListCount)
                        return;     // we can do no more good here

                    var temp = list[swapIndex];
                    list[swapIndex] = list[i];
                    list[i] = temp;
                    swapIndex++;        // so we don't keep swapping back and fourth!!
                }
            }
        }

        /// <summary>
        /// This function Finalizes the generation of System Bodies. Note that in the case of 
        /// Asteroids/Dwarf Planets it is resposnable for the complete creation of those bodies 
        /// based on a reference Asteroid provided. The same is also true of Comets, however no reference 
        /// body/orbit is required for them.
        /// </summary>
        /// <remarks>
        /// Quite a lot of data goes into making a System Body. What follows is a list of the different data
        /// points which need to be either randomly generated or infered through previously generated data. 
        /// @note Some data points require other data to be generate before them, the list order takes that into consideration.
        /// @note This function does not generate all of the following data in all cases, it often depends on they type of System Body it is generating.
        /// <list type="Bullet">
        /// <item>
        /// <b>Mass:</b> If required it is generated by a call to GenerateSystemBodyMass().
        /// </item>
        /// <item>
        /// <b>Orbit:</b> If required an orbit is generated with either GenerateAsteroidBeltBodyOrbit() or GenerateSystemBodyOrbit()
        /// Depending on weither or not a reference orbit was provided.
        /// </item>
        /// <item>
        /// <b>Density:</b> Randomly selected based on a range for the given planet type, see GalaxyGen.SystemBodyDensityByType.
        /// </item>
        /// <item>
        /// <b>Radius:</b> Generated using CalculateRadiusOfBody().
        /// </item>
        /// <item>
        /// <b>Surface Gravity:</b> Is calculated with the following formular: 
        /// <c>g = (G * M) / r^2</c>
        /// Where G = Gravatational Constant, M = Mass and r = radius.
        /// </item>
        /// <item>
        /// <b>Axial Tilt:</b> A random value between 0 and GalaxyGen.MaxPlanetInclination is generated.
        /// </item>
        /// <item>
        /// <b>Length Of Day:</b> Randomly generated TimeSpan with a range of GalaxyGen.MiniumPossibleDayLength hours to the 
        /// year length of the body (tho never less than GalaxyGen.MiniumPossibleDayLength).
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
        /// <b>Atmosphere:</b> Is generated by GenerateAtmosphere().
        /// </item>
        /// <item>
        /// <b>Planetary Ruins:</b> is generated by GenerateRuins().
        /// </item>
        /// <item>
        /// <b>Minerials:</b> Currently an ugly hack of using homworld minerials. Note that minerial generation 
        /// functions should be part of the SystemBody class as the player will likly want to re-generate them.
        /// </item>
        /// <item>
        /// <b>Moons:</b> If we a generating a planet we genmerate moons using GenerateMoons().
        /// </item>
        /// </list>
        /// </remarks>
        private static void FinalizeSystemBodyGeneration(Star star, SystemBody body, SystemBody parent = null, Orbit referenceOrbit = null)
        {
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
            body.Density = RNG_NextDoubleRange(GalaxyGen.SystemBodyDensityByType[body.Type]);
            body.Radius = CalculateRadiusOfBody(body.Orbit.Mass, body.Density);
            double radiusSquaredInM = (body.Radius * Constants.Units.MetersPerAu) * (body.Radius * Constants.Units.MetersPerAu); // conver to m from au.
            body.SurfaceGravity = (float)((Constants.Science.GravitationalConstant * body.Orbit.Mass) / radiusSquaredInM); // see: http://nova.stanford.edu/projects/mod-x/ad-surfgrav.html
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
        /// from the range specified in GalaxyGen.SystemBodyMassByType.
        /// Some extra logic is run for moon to prevent them being larger then the planet they orbit.
        /// the maximum mass of a moon relative to the parent body is controlled by GalaxyGen.MaxMoonMassRelativeToParentBody.
        /// </summary>
        private static double GenerateSystemBodyMass(SystemBody body, SystemBody parent)
        {
            if (IsMoon(body.Type))
            {
                // quick safty check:
                if (parent == null)
                    throw new System.ArgumentException("Parent cannot be null when generating the mass of a moon.");

                // these bodies have special mass limits over and above whats in PlanetMassByType.
                double min, max;
                min = GalaxyGen.SystemBodyMassByType[body.Type]._min;
                max = GalaxyGen.SystemBodyMassByType[body.Type]._max;

                if (max > parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody)
                    max = parent.Orbit.Mass * GalaxyGen.MaxMoonMassRelativeToParentBody;
                if (min > max)
                    min = max;      // just to make sure we get sane values.

                return RNG_NextDoubleRange(min, max);
            }

            return RNG_NextDoubleRange(GalaxyGen.SystemBodyMassByType[body.Type]);
        }

        /// <summary>
        /// This function follows a similar 3 stage process to the generation of other system bodies.
        /// It first determins if this Planet will have any moons using GalaxyGen.MoonGenerationChanceByPlanetType.
        /// It then works out how many moons it has using GalaxyGen.MaxNoOfMoonsByPlanetType.
        /// thenm it generates Proto Moons before calling GeneratePlanetSystemOrbits() to generate orbits for Protomoons. 
        /// Finally it calls GenerateSystemBody() to finish the moons.
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
            double massRatioOfParent = parent.Orbit.Mass / GalaxyGen.SystemBodyMassByType[parent.Type]._max;
            double moonGenChance = massRatioOfParent * m_RNG.NextDouble() * GalaxyGen.MaxNoOfMoonsByPlanetType[parent.Type];
            moonGenChance = GMath.Clamp(moonGenChance, 1, GalaxyGen.MaxNoOfMoonsByPlanetType[parent.Type]);
            int noOfMoons = (int)Math.Round(moonGenChance);

            // now we need to work out the moon type
            // we will do this by looking at the base temp of the parent.
            // if the base temp of the planet / 150K is  > 1 then it will always be terrestrial.
            // i.e. a planet hotter then GalaxyGen.IceMoonMaximumParentTemperature will always have PlanetType.Moon.
            double tempRatio = Temperature.ToKelvin(parent.BaseTemperature) / GalaxyGen.IceMoonMaximumParentTemperature;
            SystemBody.PlanetType pt = SystemBody.PlanetType.Moon;

            // first pass to gen mass etc:
            List<ProtoSystemBody> protoMoons = new List<ProtoSystemBody>();
            double totalMoonMass = 0;
            for (int i = 0; i < noOfMoons; ++i)
            {
                ProtoSystemBody protoMoon = new ProtoSystemBody();                      // create the proto moon
                if (m_RNG.NextDouble() > tempRatio)
                    protoMoon._type = SystemBody.PlanetType.IceMoon;                    // if a random number is > tempRatio it will be an ice moon.
                else
                    protoMoon._type = SystemBody.PlanetType.Moon;                       // else it is a Terrestrial Moon

                protoMoon._mass = GenerateMoonMass(parent, protoMoon._type);            // Generate Mass
                totalMoonMass += protoMoon._mass;                                       // add mass to total mass.
                protoMoons.Add(protoMoon);
            }

            double minMoonOrbitDist = parent.Radius * GalaxyGen.MinMoonOrbitMultiplier;
            double maxMoonDistance = GalaxyGen.MaxMoonOrbitDistanceByPlanetType[parent.Type] * massRatioOfParent;
            List<SystemBody> moons = GeneratePlanetSystemOrbits(parent, protoMoons, totalMoonMass, minMoonOrbitDist, maxMoonDistance);

            int moonNo = 1;
            foreach (SystemBody moon in moons)
            {
                moon.Name = parent.Name + " - Moon " + moonNo.ToString();

                // flesh out moon details:
                FinalizeSystemBodyGeneration(star, moon, parent);
                parent.Moons.Add(moon);
            }
        }

        /// <summary>
        /// Generates mass for a Planet. tho it also works for Comets and Asteroids and Dwarf Planets... just not moons.
        /// </summary>
        private static double GeneratePlanetMass(SystemBody.PlanetType type)
        {
            return RNG_NextDoubleRange(GalaxyGen.SystemBodyMassByType[type]);
        }

        /// <summary>
        /// Generates Mass for a Moon, it makes sure that the mass of the moon will never be more then that of it's parent body.
        /// </summary>
        private static double GenerateMoonMass(SystemBody parent, SystemBody.PlanetType type)
        {
            // quick safty check:
            if (parent == null)
                throw new System.ArgumentException("Parent cannot be null when generating the mass of a moon.");

            // these bodies have special mass limits over and above whats in PlanetMassByType.
            double min, max;
            min = GalaxyGen.SystemBodyMassByType[type]._min;
            max = GalaxyGen.SystemBodyMassByType[type]._max;

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
                double tectonicsChance = body.Orbit.Mass / Constants.Units.EarthMassInKG / star.Age * 100000000; 
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

        #region Atmosphere Generation

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
            atmo.SurfaceTemperature = planet.BaseTemperature;       // we need something sane to star us off.

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
                                                        (planet.Orbit.Mass / GalaxyGen.SystemBodyMassByType[planet.Type]._max));

                    if (m_RNG.NextDouble() < atmoChance)
                    {
                        // Terrestrial Planets can have very large ammount of ATM.
                        // so we will generate a number to act as the total:
                        double planetsATMChance = m_RNG.NextDouble();// (float)RNG_NextDoubleRange(0.1, 100);
                        // get my mass ratio relative to earth (so really small bodies cannot have massive atmos:
                        double massRatio = planet.Orbit.Mass / Constants.Units.EarthMassInKG;
                        float planetsATM = 1;

                        // Start with the ammount of Oxygen or Carbin Di-oxide or methane:
                        int atmoTypeChance = m_RNG.Next(0, 3);
                        if (atmoTypeChance == 0)            // methane
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 5);
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.40);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(2), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else if (atmoTypeChance == 1)   // Carbon Di-Oxide
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 200); // allow presure cooker atmos!!
                            currATM = (float)RNG_NextDoubleRange(0.05, 0.90);
                            atmo.Composition.Add(AtmosphericGas.AtmosphericGases.SelectAt(12), currATM * planetsATM);
                            totalATM += currATM;
                        }
                        else                        // oxygen
                        {
                            planetsATM = (float)GMath.Clamp((double)planetsATMChance * 5 * massRatio, 0.01, 5);
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
                        noOfTraceGases = m_RNG.Next(1, 3);
                        totalATM += AddTraceGases(atmo, noOfTraceGases, planetsATM, false);

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
        /// <param name="totalAtmoPressure">The ammount of gass added is multiplyed by this before being added to the Atmosphere.</param>
        /// <returns>The ammount of gas added in ATMs</returns>
        private static float AddTraceGases(Atmosphere atmo, int numberToAdd, float totalAtmoPressure = 1, bool allowHydrogenOrHelium = true)
        {
            float totalATMAdded = 0;
            int gassesAdded = 0;
            while (gassesAdded < numberToAdd)
            {
                //float currATM = (float)RNG_NextDoubleRange(0, 0.01);
                var gas = AtmosphericGas.AtmosphericGases.Select(m_RNG.NextDouble());
                if (allowHydrogenOrHelium == false)
                {
                    if (gas.ChemicalSymbol == "H" || gas.ChemicalSymbol == "He")
                        continue;
                }
                if (atmo.Composition.ContainsKey(gas))
                    continue;
                atmo.Composition.Add(gas, 0.01f * totalAtmoPressure);   // add 1% for trace gasses.
                totalATMAdded += 0.01f * totalAtmoPressure;
                gassesAdded++;
            }

            return totalATMAdded;
        }

        #endregion

        /// <summary>
        /// This function generate ruins for the specified system Body.
        /// @todo Make Ruins Generation take star age/type into consideration?? why? ruins in game will yield TN artifacts, which means that the host civ had space travel at a bare minimum.
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
                return; // no valid atmosphere! why is that a problem? bodies without atmospheres can be colonized.
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
                FinalizeSystemBodyGeneration(star, asteroid, null, referenceBody.Orbit);
                star.Planets.Add(asteroid);
            }

            // now lets work out how many dwarf planets there should be:
            int noOfDwarfPlanets = noOfAsteroids / GalaxyGen.NumberOfAsteroidsPerDwarfPlanet;
            for (int i = 0; i < noOfDwarfPlanets; ++i)
            {
                SystemBody dwarf = new SystemBody(star, SystemBody.PlanetType.Asteroid);
                dwarf.Name = star.Name + " - Dwarf Planet" + beltNo.ToString() + "-" + i.ToString();

                // Generate the asteriod:
                FinalizeSystemBodyGeneration(star, dwarf, null, referenceBody.Orbit);
                star.Planets.Add(dwarf);
            }
        }

        #endregion

        #region Comet Generation Functions

        /// <summary>
        /// Generates a random number of comets for a given star. The number of gererated will 
        /// be at least GalaxyGen.MiniumCometsPerSystem and never more then GalaxyGen.MaxNoOfComets.
        /// </summary>
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

                FinalizeSystemBodyGeneration(star, newComet);
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

            List<Star> systemStars = new List<Star>(system.Stars.ToArray()); // Deep copy.

            /// <summary>
            /// Only the system primary will have jumppoints if this is true.
            /// </summary>
            if (Constants.GameSettings.PrimaryOnlyJumpPoints == true)
            {
                luckyStar = system.Stars[0];
            }
            else
            {
                do
                {
                    luckyStar = system.Stars[m_RNG.Next(system.Stars.Count)];

                    systemStars.Remove(luckyStar);
                } while (luckyStar.Planets.Count != 0 && systemStars.Count > 0);

                if (systemStars.Count == 0)
                {
                    luckyStar = system.Stars[m_RNG.Next(system.Stars.Count)];
                }
            }

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

            /// <summary>
            /// Only the system primary will have jumppoints if this is true.
            /// </summary>
            int i = 0;
            do
            {
                Star currentStar = system.Stars[i];
                starList.Add(GetNaturalJumpPointGeneration(currentStar), currentStar);
                i++;
            }
            while (i < system.Stars.Count && !Constants.GameSettings.PrimaryOnlyJumpPoints);

            // If numJumpPoints wasn't specified by the systemGen,
            // then just make as many jumpPoints as our stars cumulatively want to make.
            if (numJumpPoints == -1)
                numJumpPoints = (int)starList.TotalWeight;

            numJumpPoints = (int)Math.Round(numJumpPoints * Constants.GameSettings.JumpPointConnectivity);

            if (Constants.GameSettings.SystemJumpPointHubChance > m_RNG.Next(100))
            {
                numJumpPoints = (int)Math.Round(numJumpPoints * Constants.GameSettings.JumpPointHubConnectivity);
            }

            /// <summary>
            /// 6.5 jumppoint generation rules. This will generate JPs on a flat chance based only on star mass, not on system bodies or other criteria.
            /// </summary>
            if (Constants.GameSettings.Aurora65JPGeneration == true)
            {
                int numJPs = 10;
                int BaseJPChance = 90;
                int JPChance = (BaseJPChance + (int)Math.Round(system.Stars[0].Orbit.MassRelativeToSol));
                while (m_RNG.Next(100) < JPChance)
                {
                    numJPs++;

                    if (BaseJPChance == 40)
                        BaseJPChance = 30;
                    else if (BaseJPChance == 60)
                        BaseJPChance = 40;
                    else if (BaseJPChance == 90)
                        BaseJPChance = 60;

                    JPChance = (BaseJPChance + (int)Math.Round(system.Stars[0].Orbit.MassRelativeToSol));
                }

                numJumpPoints = numJPs;
            }

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
            if (star.Planets.Count == 0 && star != star.Position.System.Stars[0])
            {
                return 0; // Don't generate JP's on non-primary planetless stars.
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
                double planetEarthMass = currentPlanet.Orbit.Mass / Constants.Units.EarthMassInKG;
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
            JumpPoint newJumpPoint;
            /// <summary>
            /// Generate jump points connected to survey locations.
            /// </summary>
            if (Constants.GameSettings.PrimaryOnlyJumpPoints == true)
            {
                /// <summary>
                /// Get the SP this JP should belong to.
                /// </summary>
                int SP = m_RNG.Next(30);
                double RingDist = Constants.SensorTN.EarthRingDistance * Math.Sqrt(star.Orbit.MassRelativeToSol);
                int RingFactor = 0;
                int angle = -1;
                int angleMax = 30;

                /// <summary>
                /// And get the angle for this SP. 0 - 5 for the 1st 6.
                /// </summary>
                if (SP <= 5)
                {
                    RingFactor = 1;
                    angleMax = 60;
                    int SPCount = 0;
                    for (int surveyPointIterator = 30; surveyPointIterator < 360; surveyPointIterator += 60)
                    {
                        if (SPCount == SP)
                        {
                            angle = surveyPointIterator;
                            break;
                        }
                        SPCount++;
                    }
                }
                /// <summary>
                /// 6 through 17 for the next 12
                /// </summary>
                else if (SP <= 17)
                {
                    RingFactor = 2;
                    int SPCount = 6;
                    for (int surveyPointIterator = 15; surveyPointIterator < 360; surveyPointIterator += 30)
                    {
                        if (SPCount == SP)
                        {
                            angle = surveyPointIterator;
                            break;
                        }
                        SPCount++;
                    }
                }
                else
                {
                    RingFactor = 3;
                    int SPCount = 18;
                    for (int surveyPointIterator = 0; surveyPointIterator < 360; surveyPointIterator += 30)
                    {
                        if (SPCount == SP)
                        {
                            angle = surveyPointIterator;
                            break;
                        }
                        SPCount++;
                    }
                }

                /// <summary>
                /// Generate a random angle from the above
                /// </summary>
                int TheAngle = 0;
                if (angle == 0)
                {
                    if (m_RNG.Next(100) > 50)
                    {
                        TheAngle = 0 + m_RNG.Next((angleMax / 2));
                    }
                    else
                    {
                        TheAngle = 360 - (angleMax / 2) + m_RNG.Next((angleMax / 2));
                    }
                }
                else
                {
                    TheAngle = angle - (angleMax / 2) + m_RNG.Next(angleMax);
                }

                /// <summary>
                /// Now get a distance for this jump point. RingFactor of 1 means from 0 to RingDist. RingFactor of 2 means from RingDist to RingDist * 2. and RingFactor of 3 means from
                /// RingDist * 2 to RingDist * 3
                /// </summary>
                double Distance = (RingDist * ((double)m_RNG.Next(100000) / 100000.0)) + (RingDist * ((RingFactor - 1))) ;

                double fX = Math.Cos(Helpers.GameMath.Angle.ToRadians((double)TheAngle)) * Distance;
                double fY = Math.Sin(Helpers.GameMath.Angle.ToRadians((double)TheAngle)) * Distance;

                newJumpPoint = new JumpPoint(star, fX, fY);
            }
            else
            {

                double minRadius = GalaxyGen.OrbitalDistanceByStarSpectralType[star.SpectralType]._min;
                double maxRadius = GalaxyGen.OrbitalDistanceByStarSpectralType[star.SpectralType]._max;

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
                newJumpPoint = new JumpPoint(star, offsetX, offsetY);
            }
            return newJumpPoint;
        }

        #endregion

        #region NPR Generation Functions

        ///< @todo Generate NPRs.

        #endregion

        #region Util Functions

        public static bool IsMoon(SystemBody.PlanetType pt)
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

        /// <summary>
        /// Returns the next Double from m_RNG adjusted to be between the min and max range.
        /// </summary>
        private static double RNG_NextDoubleRange(double min, double max)
        {
            return (min + m_RNG.NextDouble() * (max - min));
        }

        /// <summary>
        /// Version of RNG_NextDoubleRange(double min, double max) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        private static double RNG_NextDoubleRange(GalaxyGen.MinMaxStruct minMax)
        {
            return RNG_NextDoubleRange(minMax._min, minMax._max);
        }

        /// <summary>
        /// Raises the random number generated to the power provided to produce a non-uniform selection from the range.
        /// </summary>
        private static double RNG_NextDoubleRangeDistributedByPower(double min, double max, double power)
        {
            return min + Math.Pow(m_RNG.NextDouble(), power) * (max - min);
        }

        /// <summary>
        /// Version of RNG_NextDoubleRangeDistributedByPower(double min, double max, double power) that takes GalaxyGen.MinMaxStruct directly.
        /// </summary>
        private static double RNG_NextDoubleRangeDistributedByPower(GalaxyGen.MinMaxStruct minMax, double power)
        {
            return RNG_NextDoubleRangeDistributedByPower(minMax._min, minMax._max, power);
        }

        /// <summary>
        /// Returns a value between the min and max.
        /// </summary>
        private static uint RNG_NextRange(GalaxyGen.MinMaxStruct minMax)
        {
            return (uint)m_RNG.Next((int)minMax._min, (int)minMax._max);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        private static double SelectFromRange(GalaxyGen.MinMaxStruct minMax, double selection)
        {
            return minMax._min + selection * (minMax._max - minMax._min); ;
        }

        /// <summary>
        /// Randomly reverses the current sign of the value, i.e. it will randomly make the number positive or negative.
        /// </summary>
        private static double RandomizeSign(double value)
        {
            // 50/50 odds of reversing the sign:
            if (m_RNG.NextDouble() > 0.5)
                return value * -1;

            return value;
        }

        /// <summary>
        /// Calculates the radius of a body from mass and densitiy using the formular: 
        /// <c>r = ((3M)/(4pD))^(1/3)</c>
        /// Where p = PI, D = Density, and M = Mass.
        /// </summary>
        /// <param name="mass">The mass of the body in Kg</param>
        /// <param name="density">The density in g/cm^2</param>
        /// <returns>The radius in AU</returns>
        public static double CalculateRadiusOfBody(double mass, double density)
        {
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * (density / 1000)), 0.3333333333); // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
                                                                                                   // 0.3333333333 should be 1/3 but 1/3 gives radius of 0.999999 for any mass/density pair, so i used 0.3333333333
            return Distance.ToAU(radius / 1000 / 100);     // convert from cm to AU.
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
#warning Does this take into account multiple stars? should it? TN does not.
            double temp = Temperature.ToKelvin(parentStar.Temperature);
            temp = temp * Math.Sqrt(parentStar.Radius / (2 * distanceFromStar));
            return Temperature.ToCelsius(temp);
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

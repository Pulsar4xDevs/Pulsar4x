using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Entities;
using Pulsar4X.Stargen;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class StargenTests
    {
        private const double MinAge = 1.0E9;
        private const double MaxAge = 6.0E9;

        [Test]
        public void Stargen_Default_StarSystem_With_Moons()
        {
            var ssf = new StarSystemFactory(true);
            StarSystem ss = null;
            for (int i = 0; i < 200; i++)
            {
                ss = ssf.Create("Proxima");

                Assert.IsNotNull(ss);
                Assert.AreEqual("Proxima", ss.Name);
                Assert.IsNotNull(ss.Stars);
                Assert.GreaterOrEqual(ss.Stars.Count, 1);
            }

            PrintSummary(ss);
        }

        [Test]
        public void Stargen_Default_StarSystem_Without_Moons()
        {
            var ssf = new StarSystemFactory(false);
            StarSystem ss = null;
            for (int i = 0; i < 200; i++)
            {
                ss = ssf.Create("Proxima");

                Assert.IsNotNull(ss);
                Assert.AreEqual("Proxima", ss.Name);
                Assert.IsNotNull(ss.Stars);
                Assert.GreaterOrEqual(ss.Stars.Count, 1);
            }

            PrintSummary(ss);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void Stargen_Default_StarSystem_With_No_Name_Throws()
        {
            var ssf = new StarSystemFactory(true);

            var ss = ssf.Create("");

            Assert.IsNotNull(ss);
            Assert.AreEqual("", ss.Name);
            Assert.IsNotNull(ss.Stars);
            Assert.GreaterOrEqual(ss.Stars.Count, 1);


            PrintSummary(ss);
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void StarFactory_Create_With_No_Name_Throws()
        {
            var sf = new StarFactory(MinAge, MaxAge);
            sf.Create("");
        }

        [Test]
        public void StarFactory_Create_With_Override()
        {
            var sf = new StarFactory(MinAge, MaxAge);
            var stars = sf.Create("Sol", 4);

            Assert.IsNotNull(stars);
            Assert.AreEqual(4, stars.Count);
        }
        
        [Test]
        public void StarFactory_Create_With_Defaults()
        {
            var sf = new StarFactory(MinAge, MaxAge);
            var stars = sf.Create("Sol");

            Assert.IsNotNull(stars);
            Assert.Less(0, stars.Count);
        }

        /// <summary>
        /// Print out the star system from the test for visual review
        /// </summary>
        /// <param name="ssy"></param>
        protected void PrintSummary(StarSystem ssy)
        {
            foreach (var ss in ssy.Stars)
            {
                Console.WriteLine("System Name: {0}", ss.Name);
                Console.WriteLine("Total Number of Planets: {0}", ss.Planets.Count());
                Console.WriteLine("Number of Terestrials: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Terrestrial));
                Console.WriteLine("Number of Asteroids: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Asteroid));
                Console.WriteLine("Number of Water: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Water));
                Console.WriteLine("Number of Rock: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Rock));
                Console.WriteLine("Number of Martian: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Martian));
                Console.WriteLine("Number of Venusian: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Venusian));
                Console.WriteLine("Number of Ice: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.Ice));
                Console.WriteLine("Number of Dwarf Giant: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.GasDwarf));
                Console.WriteLine("Number of Ice Giant: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.IceGiant));
                Console.WriteLine("Number of Gas Giant: {0}",
                                  ss.Planets.Count(x => x.PlanetType == PlanetTypes.GasGiant));
                Console.WriteLine("Number of Unknown: {0}",
                                                ss.Planets.Count(x => x.PlanetType == PlanetTypes.Unknown));
            }
        }
    }
}

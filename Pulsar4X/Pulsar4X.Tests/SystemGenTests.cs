using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using Pulsar4X.Entities;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        [TestFixtureSetUp]
        public void GlobalInit()
        {
            var game = new Game(); // init the game class as we will need it for these tests.
            GalaxyFactory.InitToDefaultSettings(); // make sure default settings are loaded.
        }

        [Test]
        [Description("Creates and tests a single star sytem")]
        public void CreateAndFillStarSystem()
        {
            var system = StarSystemFactory.CreateSystem("Argon Prime"); // Keeping with the X3 theme :P

            // lets test that the stars generated okay:
            var stars = system.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>();
            Assert.IsNotEmpty(stars);

            if (stars.Count > 1)
            {
                Entity rootStar = stars[0].GetDataBlob<OrbitDB>().Root;
                double highestMass = rootStar.GetDataBlob<MassVolumeDB>().Mass;
                Entity highestMassStar = rootStar;
                foreach (Entity star in stars)
                {
                    var massDB = star.GetDataBlob<MassVolumeDB>();
                    if (massDB.Mass > highestMass)
                        highestMassStar = star;
                }

                // the first star in the system should have the highest mass:
                Assert.AreSame(rootStar, highestMassStar);
            }
        }

        [Test]
        [Description("generates 1000 test systems to test performance of the run.")]
        public void PerformanceTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true); 

            timer.Start();
            for (int i = 0; i < 1000; i++)
            {
                StarSystemFactory.CreateSystem("Performance Test No " + i.ToString());
            }

            timer.Stop();
            double totalTime = timer.Elapsed.TotalSeconds;

            long endMemory = GC.GetTotalMemory(true); 
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB.", 
                totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }

        [Test]
        [Description("generates 1000 test systems to test performance of the run.")]
        [Ignore]
        public void OldSystemGenPerformanceTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true);

            timer.Start();
            for (int i = 0; i < 1000; i++)
            {
                SystemGen.CreateSystem("Performance Test No " + i.ToString());
            }

            timer.Stop();
            double totalTime = timer.Elapsed.TotalSeconds;

            long endMemory = GC.GetTotalMemory(true);
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory used: {2} MB, per system: {3} KB.",
                totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }
    }
}
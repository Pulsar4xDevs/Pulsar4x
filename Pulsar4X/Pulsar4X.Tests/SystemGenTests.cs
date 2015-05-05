using System;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        [TestFixtureSetUp]
        public void GlobalInit()
        {
            var game = new Game(); // init the game class as we will need it for these tests.
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
                double highestMass = stars[1].GetDataBlob<MassVolumeDB>().Mass;
                for (int i = 2; i < stars.Count; i++)
                {
                    var massDB = stars[i].GetDataBlob<MassVolumeDB>();
                    if (massDB.Mass > highestMass)
                        highestMass = massDB.Mass;
                }

                // the first star in the system should have the hiogst mass:
                Assert.IsTrue(stars[0].GetDataBlob<MassVolumeDB>().Mass > highestMass);
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
    }
}
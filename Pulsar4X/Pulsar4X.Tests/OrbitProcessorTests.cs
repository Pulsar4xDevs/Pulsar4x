using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture(Category = "Processor Tests", Description = "Tests for the Orbit Processor.")]
    class OrbitProcessorTests
    {
        private List<StarSystem> _systems;
        
        [SetUp]
        public void Init()
        {
            var game = new Game(); // init the game class as we will need it for these tests.
            GalaxyFactory.InitToDefaultSettings(); // make sure default settings are loaded.
            const int numSystems = 1000;
            _systems = new List<StarSystem>(numSystems);

            for (int i = 0; i < numSystems; i++)
            {
                _systems.Add(StarSystemFactory.CreateSystem("Stress System " + i));
            }

            OrbitProcessor.Initialize();
        }

        [Test]
        public void OrbitStressTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true);

            timer.Start();

            OrbitProcessor.Process(_systems, 60);

            timer.Stop();

            double totalTime = timer.Elapsed.TotalSeconds;

            GC.Collect();
            long endMemory = GC.GetTotalMemory(true);
            double totalMemory = (endMemory - startMemory) / 1024.0;  // in KB

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = String.Format("Total run time: {0}s, per system: {1}ms. total memory leaked: {2} MB, per system: {3} KB.",
                totalTime.ToString("N4"), (totalTime).ToString("N2"), (totalMemory / 1024.0).ToString("N2"), (totalMemory / 1000).ToString("N2"));

            // print results:
            Console.WriteLine(output);
            Assert.Pass(output);
        }
    }
}

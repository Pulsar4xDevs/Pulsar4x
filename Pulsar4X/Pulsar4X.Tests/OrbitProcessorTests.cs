using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using JetBrains.dotMemoryUnit.Kernel;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture(Category = "Processor Tests", Description = "Tests for the Orbit Processor.")]
    class OrbitProcessorTests
    {
        private List<StarSystem> _systems;
        private const int _numSystems = 1000;
        
        [SetUp]
        public void Init()
        {
            var game = new Game(); // init the game class as we will need it for these tests.
            GalaxyFactory.InitToDefaultSettings(); // make sure default settings are loaded.
            _systems = new List<StarSystem>(_numSystems);
            var seeds = new int[_numSystems];

            for (int i = 0; i  < _numSystems; i ++)
            {
                // Pregenerate seeds so we get the same systems
                // in the same order each time.
                seeds[i] = GalaxyFactory.SeedRNG.Next();
            }

            Parallel.For(0, _numSystems, i =>
            {
                _systems.Add(StarSystemFactory.CreateSystem("Performance Test No " + i.ToString(), seeds[i]));
            });

            OrbitProcessor.Initialize();
        }

        [DotMemoryUnit(SavingStrategy = SavingStrategy.OnAnyFail, Directory = @"C:\tmp\dotMemory")]
        [Test]
        public void OrbitStressTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory;
            long endMemory;
            double totalTime;

            GC.Collect();
            MemoryCheckPoint memoryCheckPoint = dotMemory.Check();
            startMemory = GC.GetTotalMemory(true);

            timer.Start();

            OrbitProcessor.Process(_systems, 60);

            timer.Stop();

            totalTime = timer.Elapsed.TotalSeconds;

            // Check memory afterwords.
            // Note: dotMemory.Check doesn't work unless run with dotMemory unit.
            GC.Collect();
            endMemory = GC.GetTotalMemory(true);

            dotMemory.Check(memory =>
            {
                Assert.That(memory.GetDifference(memoryCheckPoint)
                    .GetNewObjects()
                    .GetObjects(where => where.Namespace.Like("Pulsar4X.ECSLib"))
                    .ObjectsCount,
                    Is.EqualTo(memory.GetDifference(memoryCheckPoint)
                    .GetDeadObjects()
                    .GetObjects(where => where.Namespace.Like("Pulsar4X.ECSLib"))
                    .ObjectsCount));
            }); 
            
            long totalMemory = endMemory - startMemory;

            Assert.LessOrEqual(0, totalMemory);

            // note that because we do 1000 systems total time taken as miliseconds is the time for a single sysmte, on average.
            string output = string.Format("Total run time: {0}s, per system: {1}ms.",
                totalTime.ToString("N4"), ((totalTime / _numSystems ) * 1000).ToString("N4"));

            // print results:
            Console.WriteLine(output);

            Assert.Pass(output);
        }
    }
}

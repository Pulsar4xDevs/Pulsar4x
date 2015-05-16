using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
        private const int NumSystems = 1000;
        
        [SetUp]
        public void Init()
        {
            Game game = new Game(); // init the game class as we will need it for these tests.
            GalaxyFactory.InitToDefaultSettings(); // make sure default settings are loaded.

            OrbitProcessor.Initialize();
        }

        [DotMemoryUnit(SavingStrategy = SavingStrategy.OnAnyFail, Directory = @"C:\tmp\dotMemory")]
        [Test]
        public void OrbitStressTest()
        {
            // Setup systems to stress test.
            _systems = new List<StarSystem>(NumSystems);

            Parallel.For(0, NumSystems, i =>
            {
                _systems.Add(StarSystemFactory.CreateSystem("Performance Test No " + i.ToString(), i));
            });

            // use a stop watch to get more accurate time.
            Stopwatch timer = new Stopwatch();

            // Declare variables before usage to keep memory usage constant.
            long startMemory;
            long endMemory;

            // lets get our memory before starting:
            GC.Collect();
            MemoryCheckPoint memoryCheckPoint = dotMemory.Check();
            startMemory = GC.GetTotalMemory(true);

            timer.Start();

            OrbitProcessor.Process(_systems, 60);

            timer.Stop();

            // Check memory afterwords.
            // Note: dotMemory.Check doesn't work unless run with dotMemory unit.
            GC.Collect();
            endMemory = GC.GetTotalMemory(true);

            // Check for leaked objects using dotMemory.
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
            
            // Check total memory usage.
            long totalMemory = endMemory - startMemory;
            Assert.LessOrEqual(totalMemory, 0); // Might be negative if GC cleans something up before.

            // note that because we do 1000 systems total time taken as milliseconds is the time for a single system, on average.
            string output = string.Format("Total run time: {0}s, per system: {1}ms. Totals orbits processed: {2}. Time required per orbit: {3}ns.",
                timer.Elapsed.TotalSeconds.ToString("N4"), (timer.Elapsed.TotalMilliseconds / NumSystems).ToString("N4"), OrbitProcessor.OrbitsUpdatedLastProcess, ((timer.ElapsedMilliseconds * 1000000.0) / OrbitProcessor.OrbitsUpdatedLastProcess).ToString("N2"));

            // print results:
            Console.WriteLine(output);

            Assert.Pass(output);
        }
    }
}

using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        private Game _game;

        [OneTimeSetUpAttribute]
        public void GlobalInit()
        {
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 10); // init the game class as we will need it for these tests.
        }

        [Test]
        [Description("Outputs all the systems generated in the init of this test to XML")]
        public void OutputToXML()
        {
            SerializationManager.ExportStarSystemsToXML(_game);
        }

        [Test]
        [Description("Creates and tests a single star system")]
        public void CreateAndFillStarSystem()
        {
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 0); // reinit with empty game, so we can do a clean test.
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSystem(_game, "Argon Prime"); // Keeping with the X3 theme :P

            // lets test that the stars generated okay:
            List<Entity> stars = system.SystemManager.GetAllEntitiesWithDataBlob<StarInfoDB>();
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

            const int numSystems = 1000;
            _game = Game.NewGame("Unit Test Game", DateTime.Now, 0); // reinit with empty game, so we can do a clean test.
            GC.Collect();

            var ssf = new StarSystemFactory(_game);

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true);
            timer.Start();

            for (int i = 0; i < numSystems; i++)
            {
                ssf.CreateSystem(_game, "Performance Test No " + i, i);
            }

            timer.Stop();
            double totalTime = timer.Elapsed.TotalSeconds;

            int totalEntities = 0;
            foreach (KeyValuePair<Guid, StarSystem> system in _game.Systems)
            {
                List<Entity> entities = system.Value.SystemManager.GetAllEntitiesWithDataBlob<OrbitDB>();
                totalEntities += entities.Count;
            }

            long endMemory = GC.GetTotalMemory(true);
            double totalMemory = (endMemory - startMemory) / 1024.0; // in KB

            // note that because we do 1000 systems total time taken as milliseconds is the time for a single system, on average.
            string output = $"Total run time: {totalTime.ToString("N4")}s, per system: {(totalTime / numSystems * 1000).ToString("N2")}ms.\ntotal memory used: {(totalMemory / 1024.0).ToString("N2")} MB, per system: {(totalMemory / numSystems).ToString("N2")} KB.\nTotal Entities: {totalEntities}, per system: {totalEntities / (float)numSystems}.\nMemory per entity: {(totalMemory / totalEntities).ToString("N2")}KB";

            Console.WriteLine(output);



            // print results:
            Assert.Pass(output);
        }

        [Test]
        [Description("Allows statisical analysis of the connectivity of generated systems")]
        [Ignore("Manual statistical analysis")]
        public void JPConnectivity()
        {
            const int numSystems = 2000;
            _game = Game.NewGame("JPConnectivity Test", DateTime.Now, numSystems);
            List<StarSystem> systems = _game.GetSystems(new AuthenticationToken(_game.SpaceMaster));


            var jumpPointCounts = new Dictionary<Guid, int>();

            foreach (StarSystem starSystem in systems)
            {
                List<Entity> systemJumpPoints = starSystem.SystemManager.GetAllEntitiesWithDataBlob<TransitableDB>();

                jumpPointCounts.Add(starSystem.Guid, systemJumpPoints.Count);
            }

            var statisticalSpread = new Dictionary<int, int>();
            foreach (KeyValuePair<Guid, int> keyValuePair in jumpPointCounts)
            {
                int numJPs = keyValuePair.Value;

                statisticalSpread.SafeValueAdd(numJPs, 1);
            }

            var statisticalSpreadList = new List<double>();
            foreach (KeyValuePair<int, int> keyValuePair in statisticalSpread)
            {
                int numJPs = keyValuePair.Key;
                int numSystemsStats = keyValuePair.Value;

                while (statisticalSpreadList.Count - 1 < numJPs)
                {
                    statisticalSpreadList.Add(0);
                }
                statisticalSpreadList[numJPs] = numSystemsStats;
            }

            for (int index = 0; index < statisticalSpreadList.Count; index++)
            {
                double d = statisticalSpreadList[index];

                Console.WriteLine($"Number of systems with {index} JumpPoints: {d} ({d / (double)numSystems * 100d}% of all systems)");
            }
        }
}
}
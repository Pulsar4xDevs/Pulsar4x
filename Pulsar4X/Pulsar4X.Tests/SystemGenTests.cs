using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class SystemGenTests
    {
        private Game _game;
        private AuthenticationToken _smAuthToken;

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
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0, CreatePlayerFaction = false }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSystem(_game, "Argon Prime", 12345); // Keeping with the X3 theme :P

            // Test Item Counts are as expected
            Assert.AreEqual(1, system.GetNumberOfStars());

            // lets test that the stars generated okay:
            List<Entity> stars = system.GetAllEntitiesWithDataBlob<StarInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(stars);

            StarInfoDB argonPrimeA = stars[0].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(argonPrimeA.Age, 173752610.02727583);
            Assert.AreEqual(argonPrimeA.Class, "F0-V");
            Assert.AreEqual(argonPrimeA.EcoSphereRadius_AU, 2.3878481355737571);
            Assert.AreEqual(argonPrimeA.Luminosity, 4.2116780281066895);
            Assert.AreEqual(argonPrimeA.LuminosityClass, LuminosityClass.V);
            Assert.AreEqual(argonPrimeA.MaxHabitableRadius_AU, 2.8189647598333742);
            Assert.AreEqual(argonPrimeA.MinHabitableRadius_AU, 1.9567315113141397);
            Assert.AreEqual(argonPrimeA.SpectralSubDivision, 0);
            Assert.AreEqual(argonPrimeA.SpectralType, SpectralType.F);
            Assert.AreEqual(argonPrimeA.Temperature, 7162);

            MassVolumeDB argonPrimeAMV = stars[0].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(935668.67593512533, argonPrimeAMV.RadiusInKM);
            Assert.AreEqual(199.94046491477221, argonPrimeAMV.SurfaceGravity);

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            Assert.AreEqual(93, system.GetNumberOfBodies());
            Assert.AreEqual(18, system.GetNumberOfComets());
            Assert.AreEqual(13, system.GetNumberOfMoons());
            Assert.AreEqual(2, system.GetNumberOfDwarfPlanets());
            Assert.AreEqual(1, system.GetNumberOfIceGiants());
            Assert.AreEqual(4, system.GetNumberOfGasGiants());
            Assert.AreEqual(2, system.GetNumberOfTerrestrialPlanets());
        }


        [Test]
        [Description("Creates and tests the Sol star system")]
        public void CreateAndFillSolStarSystem()
        {
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0 }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSol(_game);

            // Test Item Counts are as expected
            Assert.AreEqual(1, system.GetNumberOfStars());
            Assert.AreEqual(1, system.GetNumberOfComets());
            Assert.AreEqual(5, system.GetNumberOfMoons());
            Assert.AreEqual(5, system.GetNumberOfDwarfPlanets());
            Assert.AreEqual(2, system.GetNumberOfIceGiants());
            Assert.AreEqual(2, system.GetNumberOfGasGiants());
            Assert.AreEqual(4, system.GetNumberOfTerrestrialPlanets());

            // lets test that the stars generated okay:
            List<Entity> stars = system.GetAllEntitiesWithDataBlob<StarInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(stars);

            StarInfoDB sol = stars[0].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(sol.Age, 4600000000);
            Assert.AreEqual(sol.Class, "G");
            Assert.AreEqual(sol.EcoSphereRadius_AU, 1.1635341143662412);
            Assert.AreEqual(sol.Luminosity, 1);
            Assert.AreEqual(sol.LuminosityClass, LuminosityClass.V);
            Assert.AreEqual(sol.MaxHabitableRadius_AU, 1.3736056394868901);
            Assert.AreEqual(sol.MinHabitableRadius_AU, 0.95346258924559235);
            Assert.AreEqual(sol.SpectralSubDivision, 0);
            Assert.AreEqual(sol.SpectralType, SpectralType.G);
            Assert.AreEqual(sol.Temperature, 5778);

            // now confirm the system bodies all created

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            List<SystemBodyInfoDB> bodies = system.GetAllDataBlobsOfType<SystemBodyInfoDB>();

            // Mercury
            var mercury = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Mercury"));
            Assert.IsNotNull(mercury);

            // Venus
            var venus = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Venus"));
            Assert.IsNotNull(venus);

            // Earth
            var earth = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Earth"));
            Assert.IsNotNull(earth);

            // Luna
            var luna = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Luna"));
            Assert.IsNotNull(luna);

            // Mars
            var mars = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Mars"));
            Assert.IsNotNull(mars);

            // Jupiter
            var jupiter = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Jupiter"));
            Assert.IsNotNull(jupiter);

            // Saturn
            var saturn = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Saturn"));
            Assert.IsNotNull(saturn);

            // Uranus
            var uranus = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Uranus"));
            Assert.IsNotNull(uranus);

            // Neptune
            var neptune = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Neptune"));
            Assert.IsNotNull(neptune);

            // Pluto
            var pluto = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Pluto"));
            Assert.IsNotNull(pluto);

            // Haumea
            var haumea = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Haumea"));
            Assert.IsNotNull(haumea);

            // Makemake
            var makemake = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Makemake"));
            Assert.IsNotNull(makemake);

            // Eris
            var eris = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Eris"));
            Assert.IsNotNull(eris);

            // Ceres
            var ceres = bodies.FirstOrDefault(x => x.OwningEntity.GetDataBlob<NameDB>().DefaultName.Equals("Ceres"));
            Assert.IsNotNull(ceres);
        }

        [Test]
        [Description("generates 1000 test systems to test performance of the run.")]
        [Ignore("Long-Running Integration Test")]
        public void PerformanceTest()
        {
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            const int numSystems = 1000;
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = 0 }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            var ssf = new StarSystemFactory(_game);

            GC.Collect();

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
            foreach (StarSystem system in _game.GetSystems(_smAuthToken))
            {
                List<Entity> entities = system.GetAllEntitiesWithDataBlob<OrbitDB>(_smAuthToken);
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
        [Ignore("Manual statistical analysis Integration test")]
        public void JPConnectivity()
        {
            const int numSystems = 2000;
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = DateTime.Now, MaxSystems = numSystems });
            List<StarSystem> systems = _game.GetSystems(new AuthenticationToken(_game.SpaceMaster));


            var jumpPointCounts = new Dictionary<Guid, int>();

            foreach (StarSystem starSystem in systems)
            {
                List<Entity> systemJumpPoints = starSystem.GetAllEntitiesWithDataBlob<TransitableDB>(_smAuthToken);

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

                Assert.Pass($"Number of systems with {index} JumpPoints: {d} ({d / (double)numSystems * 100d}% of all systems)");
            }
        }
}
}
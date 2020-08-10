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
            Assert.AreEqual(173752610.02727583, argonPrimeA.Age);
            Assert.AreEqual("F0-V", argonPrimeA.Class);
            Assert.AreEqual(2.3878481355737571, argonPrimeA.EcoSphereRadius_AU);
            Assert.AreEqual(4.2116780281066895, argonPrimeA.Luminosity);
            Assert.AreEqual(LuminosityClass.V, argonPrimeA.LuminosityClass);
            Assert.AreEqual(2.8189647598333742, argonPrimeA.MaxHabitableRadius_AU);
            Assert.AreEqual(1.9567315113141397, argonPrimeA.MinHabitableRadius_AU);
            Assert.AreEqual(0, argonPrimeA.SpectralSubDivision);
            Assert.AreEqual(SpectralType.F, argonPrimeA.SpectralType);
            Assert.AreEqual(7162, argonPrimeA.Temperature);

            MassVolumeDB argonPrimeAMV = stars[0].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(935668.67593512533, argonPrimeAMV.RadiusInKM);
            Assert.AreEqual(199.94046491477221, argonPrimeAMV.SurfaceGravity);

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            Assert.AreEqual(2, system.GetNumberOfTerrestrialPlanets(), "TerrestrialPlanets");
            Assert.AreEqual(2, system.GetNumberOfDwarfPlanets(), "DwarfPlanets");
            Assert.AreEqual(1, system.GetNumberOfIceGiants(), "IceGiants");
            Assert.AreEqual(4, system.GetNumberOfGasGiants(), "GasGiants");
            Assert.AreEqual(13, system.GetNumberOfMoons(), "Moons");
            var hash0 = systemBodies[1].GetValueCompareHash();
            var hash = systemBodies[21].GetValueCompareHash();
            Assert.AreEqual(18, system.GetNumberOfComets(), "Comets");
            Assert.AreEqual(93, system.GetNumberOfBodies(), "TotalBodies");
        }


        [Test]
        [Description("Creates and tests another single star system")]
        public void CreateAndFillStarSystemB()
        {
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0, CreatePlayerFaction = false }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSystem(_game, "Robin Prime", 22367);

            // Test Item Counts are as expected
            Assert.AreEqual(2, system.GetNumberOfStars());

            // lets test that the stars generated okay:
            List<Entity> stars = system.GetAllEntitiesWithDataBlob<StarInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(stars);

            StarInfoDB robinPrimeA = stars[0].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(5587857073.9472551, robinPrimeA.Age);
            Assert.AreEqual("K3-V", robinPrimeA.Class);
            Assert.AreEqual(0.36210213820970505, robinPrimeA.EcoSphereRadius_AU);
            Assert.AreEqual(0.096850961446762085, robinPrimeA.Luminosity);
            Assert.AreEqual(LuminosityClass.V, robinPrimeA.LuminosityClass);
            Assert.AreEqual(0.42747826039121367, robinPrimeA.MaxHabitableRadius_AU);
            Assert.AreEqual(0.29672601602819648, robinPrimeA.MinHabitableRadius_AU);
            Assert.AreEqual(3, robinPrimeA.SpectralSubDivision);
            Assert.AreEqual(SpectralType.K, robinPrimeA.SpectralType);
            Assert.AreEqual(3749, robinPrimeA.Temperature);

            MassVolumeDB robinPrimeAMV = stars[0].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(493306.41725497658, robinPrimeAMV.RadiusInKM);
            Assert.AreEqual(251.60390836758395, robinPrimeAMV.SurfaceGravity);

            StarInfoDB robinPrimeB = stars[1].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(951751181.94079089, robinPrimeB.Age);
            Assert.AreEqual("G1-V", robinPrimeB.Class);
            Assert.AreEqual(1.2362061717155353, robinPrimeB.EcoSphereRadius_AU);
            Assert.AreEqual(1.128817081451416, robinPrimeB.Luminosity);
            Assert.AreEqual(LuminosityClass.V, robinPrimeB.LuminosityClass);
            Assert.AreEqual(1.459398352030155, robinPrimeB.MaxHabitableRadius_AU);
            Assert.AreEqual(1.0130139914009157, robinPrimeB.MinHabitableRadius_AU);
            Assert.AreEqual(1, robinPrimeB.SpectralSubDivision);
            Assert.AreEqual(SpectralType.G, robinPrimeB.SpectralType);
            Assert.AreEqual(5670, robinPrimeB.Temperature);

            MassVolumeDB robinPrimeBMV = stars[1].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(746227.35086028383, robinPrimeBMV.RadiusInKM);
            Assert.AreEqual(224.27634601638144, robinPrimeBMV.SurfaceGravity);

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            //Assert.AreEqual(1, system.GetNumberOfComets());
            //Assert.AreEqual(13, system.GetNumberOfMoons());
            //Assert.AreEqual(2, system.GetNumberOfDwarfPlanets());
            //Assert.AreEqual(1, system.GetNumberOfIceGiants());
            //Assert.AreEqual(4, system.GetNumberOfGasGiants());
            //Assert.AreEqual(5, system.GetNumberOfTerrestrialPlanets());
            //Assert.AreEqual(147, system.GetNumberOfBodies());
        }

        [Test]
        [Description("Creates and tests a single star system")]
        public void CreateAndCheckDeterministic()
        {
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0, CreatePlayerFaction = false }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system1 = ssf.CreateSystem(_game, "Argon Prime", 12345); // Keeping with the X3 theme :P
            var systemtwin = ssf.CreateSystem(_game, "Argon Prime", 12345);
            var orbitEntites = system1.GetAllEntitiesWithDataBlob<OrbitDB>();
            var orbitTwins = systemtwin.GetAllEntitiesWithDataBlob<OrbitDB>();
            
            Assert.AreEqual(orbitEntites.Count, orbitTwins.Count);
            for (int i = 0; i < orbitEntites.Count; i++)
            {
                var entityPrime = orbitEntites[i];
                var entityTwin = orbitTwins[i];
                var db1 = entityPrime.GetDataBlob<OrbitDB>();
                var db2 = entityTwin.GetDataBlob<OrbitDB>();
                Assert.AreEqual(db1.GetValueCompareHash(), db2.GetValueCompareHash());

                for (int j = 0; j < entityPrime.DataBlobs.Count; j++)
                {
                    var blob1 = entityPrime.DataBlobs[j];
                    var blob2 = entityTwin.DataBlobs[j];
                    Assert.IsTrue(blob1.GetType().ToString() == blob2.GetType().ToString());
                
                    if (blob1 is IGetValuesHash)
                    {
                        IGetValuesHash hashblob1 = (IGetValuesHash)blob1;
                        IGetValuesHash hashblob2 = (IGetValuesHash)blob2;
                        var hash1 = hashblob1.GetValueCompareHash();
                        var hash2 = hashblob2.GetValueCompareHash();
                        Assert.AreEqual(hash1, hash2, "Hashes for itteration" + j + " type " +blob1.GetType().ToString() + "Don't match");
                    }
                }
                
            }
            
            
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
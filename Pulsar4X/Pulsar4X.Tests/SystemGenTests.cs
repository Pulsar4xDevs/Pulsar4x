using NUnit.Framework;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Auth;
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
            Assert.AreEqual(935668.67593512533, argonPrimeAMV.RadiusInKM, 1e-9);
            Assert.AreEqual(199.94046491477221, argonPrimeAMV.SurfaceGravity, 1e-9);

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>();
            Assert.IsNotEmpty(systemBodies);
            var numbodies = system.GetNumberOfBodies();
            Assert.AreEqual(1, system.GetNumberOfStars());
            Assert.AreEqual(2, system.GetNumberOfTerrestrialPlanets(), "TerrestrialPlanets");
            Assert.AreEqual(2, system.GetNumberOfDwarfPlanets(), "DwarfPlanets");
            Assert.AreEqual(1, system.GetNumberOfIceGiants(), "IceGiants");
            Assert.AreEqual(4, system.GetNumberOfGasGiants(), "GasGiants");
            Assert.AreEqual(13, system.GetNumberOfMoons(), "Moons");
            Assert.AreEqual(49, system.GetNumberOfAsteroids(), "Asteroids");
            Assert.AreEqual(0, system.GetNumberOfUnknownObjects(), "unknown");

            Assert.AreEqual(18, system.GetNumberOfComets(), "Comets");

            Assert.AreEqual(93, system.GetNumberOfBodies(), "TotalBodies");
            Assert.AreEqual(systemBodies.Count, system.GetNumberOfBodies());
        }


        [Test]
        [Description("Creates and tests another single star system")]
        public void CreateAndFillStarSystemB()
        {
            var startDate = new DateTime(2050, 1, 1);
            _game = new Game(new NewGameSettings { GameName = "Unit Test Game", StartDateTime = startDate, MaxSystems = 0, CreatePlayerFaction = false }); // reinit with empty game, so we can do a clean test.
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            StarSystemFactory ssf = new StarSystemFactory(_game);
            var system = ssf.CreateSystem(_game, "Robin Prime", 22367, true);

            // Test Item Counts are as expected
            Assert.AreEqual(2, system.GetNumberOfStars());

            // lets test that the stars generated okay:
            List<Entity> stars = system.GetStarsSortedByDryMass();
            Assert.IsNotEmpty(stars);

            StarInfoDB robinPrimeA = stars[0].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(951751181.94079089, robinPrimeA.Age);
            Assert.AreEqual("G1-V", robinPrimeA.Class);
            Assert.AreEqual(1.2362061717155353, robinPrimeA.EcoSphereRadius_AU);
            Assert.AreEqual(1.128817081451416, robinPrimeA.Luminosity);
            Assert.AreEqual(LuminosityClass.V, robinPrimeA.LuminosityClass);
            Assert.AreEqual(1.459398352030155, robinPrimeA.MaxHabitableRadius_AU);
            Assert.AreEqual(1.0130139914009157, robinPrimeA.MinHabitableRadius_AU);
            Assert.AreEqual(1, robinPrimeA.SpectralSubDivision);
            Assert.AreEqual(SpectralType.G, robinPrimeA.SpectralType);
            Assert.AreEqual(5670, robinPrimeA.Temperature);

            MassVolumeDB robinPrimeAMV = stars[0].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(746227.35086028383, robinPrimeAMV.RadiusInKM, 1e-9);
            Assert.AreEqual(224.27634601638144, robinPrimeAMV.SurfaceGravity, 1e-9);
            Assert.AreEqual(1.8712610994637707E30, robinPrimeAMV.MassDry);

            StarInfoDB robinPrimeB = stars[1].GetDataBlob<StarInfoDB>();
            Assert.AreEqual(5587857073.9472551, robinPrimeB.Age);
            Assert.AreEqual("K3-V", robinPrimeB.Class);
            Assert.AreEqual(0.36210213820970505, robinPrimeB.EcoSphereRadius_AU);
            Assert.AreEqual(0.096850961446762085, robinPrimeB.Luminosity);
            Assert.AreEqual(LuminosityClass.V, robinPrimeB.LuminosityClass);
            Assert.AreEqual(0.42747826039121367, robinPrimeB.MaxHabitableRadius_AU);
            Assert.AreEqual(0.29672601602819648, robinPrimeB.MinHabitableRadius_AU);
            Assert.AreEqual(3, robinPrimeB.SpectralSubDivision);
            Assert.AreEqual(SpectralType.K, robinPrimeB.SpectralType);
            Assert.AreEqual(3749, robinPrimeB.Temperature);

            MassVolumeDB robinPrimeBMV = stars[1].GetDataBlob<MassVolumeDB>();
            Assert.AreEqual(493306.41725497658, robinPrimeBMV.RadiusInKM, 1e-9);
            Assert.AreEqual(251.60390836758395, robinPrimeBMV.SurfaceGravity, 1e-9);
            Assert.AreEqual(9.1740162518801132E29, robinPrimeBMV.MassDry);

            Assert.IsTrue(robinPrimeAMV.MassDry > robinPrimeBMV.MassDry, "Star A should be the most massive.");

            List<Entity> systemBodies = system.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>(_smAuthToken);
            Assert.IsNotEmpty(systemBodies);

            Assert.AreEqual(2, system.GetNumberOfTerrestrialPlanets(), "TerrestrialPlanets");
            Assert.AreEqual(3, system.GetNumberOfDwarfPlanets(), "DwarfPlanets");
            Assert.AreEqual(1, system.GetNumberOfIceGiants(), "IceGiants");
            Assert.AreEqual(3, system.GetNumberOfGasGiants(), "GasGiants");
            Assert.AreEqual(8, system.GetNumberOfMoons(), "Moons");
            Assert.AreEqual(62, system.GetNumberOfAsteroids(), "Asteroids");
            Assert.AreEqual(0, system.GetNumberOfUnknownObjects(), "unknown");

            Assert.AreEqual(15, system.GetNumberOfComets(), "Comets");

            Assert.AreEqual(96, system.GetNumberOfBodies(), "TotalBodies");
            Assert.AreEqual(systemBodies.Count, system.GetNumberOfBodies());

            // Test initial mineral generation
            var totalMinerals = system.GetTotalSystemMinerals(_game.StaticData);
            Assert.AreEqual(27084962760, totalMinerals["Sorium"], "Sorium");
            Assert.AreEqual(2504615582, totalMinerals["Neutronium"], "Neutronium");
            Assert.AreEqual(35111974, totalMinerals["Iron"], "Iron");
            Assert.AreEqual(39722250, totalMinerals["Aluminium"], "Aluminium");
            Assert.AreEqual(29868472, totalMinerals["Lithium"], "Lithium");
            Assert.AreEqual(22030529, totalMinerals["Fissionables"], "Fissionables");
            Assert.AreEqual(1216134, totalMinerals["Duranium"], "Duranium");
            Assert.AreEqual(49794944, totalMinerals["Corbomite"], "Corbomite");
            Assert.AreEqual(19768268, totalMinerals["Copper"], "Copper");
            Assert.AreEqual(29407250, totalMinerals["Titanium"], "Titanium");
            Assert.AreEqual(39038047, totalMinerals["Tritanium"], "Tritanium");
            Assert.AreEqual(27187517, totalMinerals["Boronide"], "Boronide");
            Assert.AreEqual(60503947, totalMinerals["Corundium"], "Corundium");
            Assert.AreEqual(24266487, totalMinerals["Mercassium"], "Mercassium");
            Assert.AreEqual(59797777, totalMinerals["Vendarite"], "Vendarite");
            Assert.AreEqual(24001331, totalMinerals["Gallicite"], "Gallicite");
            Assert.AreEqual(17760843, totalMinerals["Chromium"], "Chromium");
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
                        Assert.AreEqual(hash1, hash2, "Hashes for itteration" + j + " type " + blob1.GetType().ToString() + "Don't match");
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
            Assert.AreEqual(17, system.GetNumberOfMoons());
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
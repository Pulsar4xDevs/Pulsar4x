using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Processor Tests")]
    class ProcessorTests
    {
        private Game _game;
        EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;

        [SetUp]
        public void Init()
        {
            _game = new Game();
            StaticDataManager.LoadFromDefaultDataDirectory();
            _entityManager = new EntityManager();
            _faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terrian");

            _duraniumSD = StaticDataManager.StaticDataStore.Minerals.Find(m => m.Name == "Duranium");
            _corundiumSD = StaticDataManager.StaticDataStore.Minerals.Find(m => m.Name == "Corundium");

            //StarSystem starSystem = StarSystemFactory.CreateSystem("Sol", 1);
            //Entity planetEntity = SystemBodyFactory.CreateBaseBody(starSystem);
            //SystemBodyDB planetDB = planetEntity.GetDataBlob<SystemBodyDB>();
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            SystemBodyDB planetDB = new SystemBodyDB();
            planetDB.SupportsPopulations = true;

            blobs.Add(planetDB);
            Entity planetEntity = Entity.Create(_entityManager, blobs);

            JDictionary<Guid, MineralDepositInfo> minerals = planetDB.Minerals;

            MineralDepositInfo duraniumDeposit = new MineralDepositInfo { Amount = 10000, Accessibility = 1, HalfOrigionalAmount = 5000 };
            
            minerals.Add(_duraniumSD.ID, duraniumDeposit);

            MineralDepositInfo corundiumDeposit = new MineralDepositInfo { Amount = 1000, Accessibility = 0.5, HalfOrigionalAmount = 500 };
            
            minerals.Add(_corundiumSD.ID, corundiumDeposit);

            Entity species = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);

            _colonyEntity = ColonyFactory.CreateColony(_faction, species, planetEntity);

            InstallationsDB installationsDB = _colonyEntity.GetDataBlob<InstallationsDB>();
            
            //wow holy shit, this is a pain. definatly need to add an "AddInstallation" to the InstallationProcessor. (and RemoveInstallation);
            Guid mineguidGuid = new Guid("406E22B5-65DB-4C7E-B956-B120B0466503");
            //InstallationSD mineSD = StaticDataManager.StaticDataStore.Installations[mineguidGuid];
            installationsDB.Installations[mineguidGuid] = 1f;
            InstallationEmployment installationEmployment = new InstallationEmployment {Enabled = true, Type = mineguidGuid};
            installationsDB.EmploymentList.Add(installationEmployment);

        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _colonyEntity = null;
            StaticDataManager.ClearAllData();
        }

        [Test]
        public void TestMineing()
        {
            //first with no population;
            Entity colonyEntity = _faction.GetDataBlob<FactionDB>().Colonies[0];
            InstallationsDB installations = colonyEntity.GetDataBlob<InstallationsDB>();
            JDictionary<Guid, float> mineralstockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            JDictionary<Guid, float> mineralstockpilePreMined = new JDictionary<Guid, float>(mineralstockpile);
            
            
            InstallationProcessor.Employment(colonyEntity); //do employment check;
            InstallationProcessor.Mine(_faction, colonyEntity); //run mines
            
            Assert.AreEqual(mineralstockpile[_corundiumSD.ID], 0);
            Assert.AreEqual(mineralstockpile[_duraniumSD.ID], 0);


            ColonyInfoDB colonyInfo = colonyEntity.GetDataBlob<ColonyInfoDB>();
            JDictionary<Entity, long> pop = colonyInfo.Population;
            var species = pop.Keys.ToList();
            colonyInfo.Population[species[0]] = 5; //5mil pop

            InstallationProcessor.Employment(colonyEntity); //do employment check;
            InstallationProcessor.Mine(_faction, colonyEntity); //run mines

            Assert.AreNotEqual(mineralstockpile[_corundiumSD.ID], 10);
            Assert.AreNotEqual(mineralstockpile[_duraniumSD.ID], 5);
            
        }

        [Test]
        public void TestConstruction()
        {
            ColonyInfoDB colonyInfo = _colonyEntity.GetDataBlob<ColonyInfoDB>();
            Guid itemConstructing = new Guid();//just a random guid for now.
            double ablityPointsThisColony = 100;
            List<ConstructionJob> jobList = new List<ConstructionJob>();
            
            JDictionary<Guid,float> stockpileOut = new JDictionary<Guid, float>();

            PercentValue priority = new PercentValue {Percent = 1};
            JDictionary<Guid,int> jobRawMaterials = new JDictionary<Guid, int>();
            jobRawMaterials.Add(_duraniumSD.ID, 5000); //500 per item
            jobRawMaterials.Add(_corundiumSD.ID, 70); //7 per item
            ConstructionJob newJob = new ConstructionJob 
            {
                Type = itemConstructing,  
                ItemsRemaining = 10, 
                PriorityPercent = priority,
                RawMaterialsRemaining = jobRawMaterials,
                BuildPointsRemaining = 1000,
                BuildPointsPerItem = 100
            };
            jobList.Add(newJob);
            
            colonyInfo.MineralStockpile.Add(_duraniumSD.ID, 2250); //not enough of this should get 4.5  total installations. 
            colonyInfo.MineralStockpile.Add(_corundiumSD.ID, 100); //enough of this
            stockpileOut.Add(itemConstructing,0);

            
            //firstpass 
            InstallationProcessor.GenericConstructionJobs(0, jobList, colonyInfo, stockpileOut);
            Assert.AreEqual(0, stockpileOut[itemConstructing], "Should not have constructed anything due to no buildpoints");
            Assert.AreEqual(2250, colonyInfo.MineralStockpile[_duraniumSD.ID], "Mineral Usage Incorrect");
            Assert.AreEqual(100, colonyInfo.MineralStockpile[_corundiumSD.ID], "Mineral Usage Incorrect");
            
            //todo: fix floating point math.

            //secondPass
            InstallationProcessor.GenericConstructionJobs(100, jobList, colonyInfo, stockpileOut);
            Assert.AreEqual(1, stockpileOut[itemConstructing]);

            //thirdPass
            //InstallationProcessor.GenericConstructionJobs(50, jobList, colonyInfo, stockpileOut);
            //Assert.AreEqual(1.5, stockpileOut[itemConstructing]);            
            
            //fourthPass
            //InstallationProcessor.GenericConstructionJobs(5000, jobList, colonyInfo, stockpileOut);
            //Assert.AreEqual(4.5, stockpileOut[itemConstructing]);

            //todo there's probilby some edge cases to check.
        }

        [Test]
        [Ignore("Long running stress test.")]
        public void OrbitStressTest()
        {
            var game = new Game(); // init the game class as we will need it for these tests.
            GalaxyFactory.InitToDefaultSettings(); // make sure default settings are loaded.
            const int numSystems = 1000;
            List<StarSystem> systems = new List<StarSystem>(numSystems);

            for (int i = 0; i < numSystems; i++)
            {
                systems.Add(StarSystemFactory.CreateSystem("Stress System " + i));
            }

            OrbitProcessor.Initialize();
            // use a stop watch to get more accurate time.
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

            // lets get our memory before starting:
            long startMemory = GC.GetTotalMemory(true);

            timer.Start();

            OrbitProcessor.Process(systems, 60);

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

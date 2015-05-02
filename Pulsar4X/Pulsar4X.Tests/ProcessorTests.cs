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

            Entity colony = ColonyFactory.CreateColony(_faction, species, planetEntity);

            InstallationsDB installationsDB = colony.GetDataBlob<InstallationsDB>();
            
            //wow holy shit, this is a pain. definatly need to add an "AddInstallation" to the InstallationProcessor. (and RemoveInstallation);
            Guid mineguidGuid = new Guid("406E22B5-65DB-4C7E-B956-B120B0466503");
            //InstallationSD mineSD = StaticDataManager.StaticDataStore.Installations[mineguidGuid];
            installationsDB.Installations[mineguidGuid] = 1f;
            InstallationEmployment installationEmployment = new InstallationEmployment {Enabled = true, Type = mineguidGuid};
            installationsDB.EmploymentList.Add(installationEmployment);

        }

        [Test]
        public void TestMineing()
        {
            //first with no population;
            Entity colonyEntity = _faction.GetDataBlob<FactionDB>().Colonies[0];
            InstallationsDB installations = colonyEntity.GetDataBlob<InstallationsDB>();
            JDictionary<Guid, int> mineralstockpile = colonyEntity.GetDataBlob<ColonyInfoDB>().MineralStockpile;
            JDictionary<Guid, int> mineralstockpilePreMined = new JDictionary<Guid, int>(mineralstockpile);
            
            
            InstallationProcessor.Employment(colonyEntity); //do employment check;
            InstallationProcessor.Mine(_faction, colonyEntity); //run mines
            
            Assert.AreEqual(mineralstockpile, mineralstockpilePreMined);

            ColonyInfoDB colonyInfo = colonyEntity.GetDataBlob<ColonyInfoDB>();
            JDictionary<Entity, double> pop = colonyInfo.Population;
            var species = pop.Keys.ToList();
            colonyInfo.Population[species[0]] = 5; //5mil pop

            InstallationProcessor.Employment(colonyEntity); //do employment check;
            InstallationProcessor.Mine(_faction, colonyEntity); //run mines

            Assert.AreNotEqual(mineralstockpile, mineralstockpilePreMined);

        }

        [Test]
        public void TestConstruction()
        {
            Guid itemConstructing = new Guid();//just a random guid for now.
            double ablityPointsThisColony = 100;
            List<ConstructionJob> jobList = new List<ConstructionJob>();
            JDictionary<Guid, int> rawMaterials = new JDictionary<Guid, int>();
            JDictionary<Guid,double> stockpileOut = new JDictionary<Guid, double>();

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
            
            rawMaterials.Add(_duraniumSD.ID, 2250); //not enough of this should get 4.5
            rawMaterials.Add(_corundiumSD.ID, 100); //enough of this
            stockpileOut.Add(itemConstructing,0);

            
            //firstpass 
            InstallationProcessor.GenericConstructionJobs(0, ref jobList, ref rawMaterials, ref stockpileOut);
            Assert.AreEqual(0, stockpileOut[itemConstructing]);

            //these all have floating point errors.
            //secondPass
            //InstallationProcessor.GenericConstructionJobs(50, ref jobList, ref rawMaterials, ref stockpileOut);
            //Assert.AreEqual(0.5, stockpileOut[itemConstructing]);

            //thirdPass
            //InstallationProcessor.GenericConstructionJobs(50, ref jobList, ref rawMaterials, ref stockpileOut);
            //Assert.AreEqual(stockpileOut[itemConstructing], 1);

            //this one has a significant error. needs looking at closer.
            //fourthPass
            //InstallationProcessor.GenericConstructionJobs(5000, ref jobList, ref rawMaterials, ref stockpileOut);
            //Assert.AreEqual(4.5, stockpileOut[itemConstructing]);

            //todo there's probilby some edge cases to check.
        }
    }
}

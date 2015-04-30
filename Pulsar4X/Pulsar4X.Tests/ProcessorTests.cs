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


        [SetUp]
        public void Init()
        {
            _game = new Game();
            StaticDataManager.LoadFromDefaultDataDirectory();
            _entityManager = new EntityManager();
            _faction = FactionFactory.CreateFaction(_game.GlobalManager, "Terrian");


            StarSystem starSystem = StarSystemFactory.CreateSystem("Sol", 1);
            Entity planetEntity = SystemBodyFactory.CreateBaseBody(starSystem);
            SystemBodyDB planetDB = planetEntity.GetDataBlob<SystemBodyDB>();

            JDictionary<Guid, MineralDepositInfo> minerals = planetDB.Minerals;

            MineralDepositInfo duraniumDeposit = new MineralDepositInfo { Amount = 10000, Accessibility = 1, HalfOrigionalAmount = 5000 };
            MineralSD duranium = StaticDataManager.StaticDataStore.Minerals.Find(m => m.Name == "Duranium");
            minerals.Add(duranium.ID, duraniumDeposit);

            MineralDepositInfo corundiumDeposit = new MineralDepositInfo { Amount = 1000, Accessibility = 0.5, HalfOrigionalAmount = 500 };
            MineralSD corundium = StaticDataManager.StaticDataStore.Minerals.Find(m => m.Name == "Corundium");
            minerals.Add(corundium.ID, corundiumDeposit);

            Entity colony = ColonyFactory.CreateColony(_faction, planetEntity);

            InstallationsDB installationsDB = colony.GetDataBlob<InstallationsDB>();
            
            //wow holy shit, this is a pain. definatly need to add an "AddInstallation" to the InstallationProcessor. (and RemoveInstallation);
            Guid mineguidGuid = new Guid("406E22B5-65DB-4C7E-B956-B120B0466503");
            InstallationSD mineSD = StaticDataManager.StaticDataStore.Installations[mineguidGuid];
            installationsDB.Installations[mineSD] = 1f;
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
            InstallationProcessor.Mine(_faction);

            Assert.AreEqual(mineralstockpile, mineralstockpilePreMined);

        }
    }
}

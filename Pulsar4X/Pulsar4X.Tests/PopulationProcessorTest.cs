using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;
<<<<<<< HEAD
using System.Diagnostics;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Population Growth Test")]
    public class PopulationProcessorTest
=======

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    class PopulationProcessorTest
>>>>>>> 011f193b77bfc0ed11bbe2221626ea1a5413264f
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _planetEntity;
        private Entity _faction;
        private Entity _colonyEntity;
        private GalaxyFactory _galaxyFactory;
        private StarSystemFactory _starSystemFactory;
        private StarSystem _starSystem;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings());
<<<<<<< HEAD
            StaticDataManager.LoadData("", _game);  // TODO: Figure out correct directory
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");  // Terrian?
            
             

            /*_galaxyFactory = new GalaxyFactory();
            _starSystemFactory = new StarSystemFactory(_galaxyFactory);

            _starSystem = _starSystemFactory.CreateSol(_game);*/

            _planetEntity = Entity.Create(_entityManager, SystemBodyFactory.CreateBaseBody());
            _planetEntity.SetDataBlob(new ColonyInfoDB());
            _planetEntity.GetDataBlob<NameDB>().SetName(_faction, "Terran");
=======
            //StaticDataManager.LoadData(".\\", _game);  // TODO: Figure out correct directory
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terrian");  // Terrian? 

            _galaxyFactory = new GalaxyFactory();
            _starSystemFactory = new StarSystemFactory(_galaxyFactory);

            _starSystem = _starSystemFactory.CreateSol(_game);

            _planetEntity = (Entity)SystemBodyFactory.CreateBaseBody();
            _planetEntity.SetDataBlob(new ColonyInfoDB());
>>>>>>> 011f193b77bfc0ed11bbe2221626ea1a5413264f

            SystemBodyDB planetDB = _planetEntity.GetDataBlob<SystemBodyDB>();

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            planetDB.SupportsPopulations = true;

            blobs.Add(planetDB);

            Entity species = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);

            _colonyEntity = ColonyFactory.CreateColony(_faction, species, _planetEntity);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _colonyEntity = null;
            //StaticDataManager..ClearAllData();
        }

        [Test]
        public void testPopulationGrowth()
        {
<<<<<<< HEAD
            // Assumption - the population of any planet will not reach the maximum size of a long variable type, therefore
            // I am not testing that edge case


            long[] basePop = new long[] { 0, 5, 10, 100, 999, 1000, 10000, 100000 };
            long newPop;

            // Set the colony population to five million to start
            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

            // Single growth test
            int i;

            for (i = 0; i < basePop.Length; i++)
            {
                foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                {
                    if (pop.ContainsKey(kvp.Key))
                    {
                        pop[kvp.Key] = basePop[i];
                    }

                    newPop = calcGrowthIteration(basePop[i]);
                    PopulationProcessor.GrowPopulation(_colonyEntity);
                    Assert.AreEqual(newPop, _colonyEntity.GetDataBlob<ColonyInfoDB>().Population[kvp.Key]);
                }
            }

            // Multiple growth test
            for(int j = 1; j < 10; j++)
                for(i = 0; i < basePop.Length; i++)
                {
                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        if (pop.ContainsKey(kvp.Key))
                        {
                            pop[kvp.Key] = basePop[i];
                        }
                        newPop = basePop[i];

                        for (int k = 0;k < j;k++)
                        {
                            newPop = calcGrowthIteration(newPop);
                            PopulationProcessor.GrowPopulation(_colonyEntity);
                        }

                        Assert.AreEqual(newPop, _colonyEntity.GetDataBlob<ColonyInfoDB>().Population[kvp.Key]);
                    }
                }

        }

        private long calcGrowthIteration(long lastPop)
        {
            long newPop;
            double growthRate = (20.0 / (Math.Pow(lastPop, (1.0 / 3.0))));
            if (growthRate > 10.0)
                growthRate = 10.0; // Capped at 10%
            newPop = (long)(lastPop * (1.0 + growthRate));
            if (newPop < 0)
                newPop = 0;
            return newPop;
=======
            long basePop = 5;

            // Set the colony population to five million to start
            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;
            foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
            {
                if (pop.ContainsKey(kvp.Key))
                {
                    pop[kvp.Key] = 0;
                }
            }

            PopulationProcessor.GrowPopulation(_colonyEntity);

>>>>>>> 011f193b77bfc0ed11bbe2221626ea1a5413264f
        }


        //[test]
        //public void testmineing()
        //{
        //    //first with no population;
        //    entity colonyentity = _faction.getdatablob<factiondb>().colonies[0];
        //    installationsdb installations = colonyentity.getdatablob<installationsdb>();
        //    dictionary<guid, float> mineralstockpile = colonyentity.getdatablob<colonyinfodb>().mineralstockpile;
        //    dictionary<guid, float> mineralstockpilepremined = new dictionary<guid, float>(mineralstockpile);


        //    installationprocessor.employment(colonyentity); //do employment check;
        //    installationprocessor.mine(_faction, colonyentity); //run mines

        //    assert.areequal(mineralstockpile[_corundiumsd.id], 0);
        //    assert.areequal(mineralstockpile[_duraniumsd.id], 0);


        //    colonyinfodb colonyinfo = colonyentity.getdatablob<colonyinfodb>();
        //    dictionary<entity, long> pop = colonyinfo.population;
        //    var species = pop.keys.tolist();
        //    colonyinfo.population[species[0]] = 5; //5mil pop

        //    installationprocessor.employment(colonyentity); //do employment check;
        //    installationprocessor.mine(_faction, colonyentity); //run mines

        //    assert.arenotequal(mineralstockpile[_corundiumsd.id], 10);
        //    assert.arenotequal(mineralstockpile[_duraniumsd.id], 5);

        //}

        //        [Test]
        //        public void TestConstruction()
        //        {
        //            ColonyInfoDB colonyInfo = _colonyEntity.GetDataBlob<ColonyInfoDB>();
        //            Guid itemConstructing = new Guid();//just a random guid for now.
        //            double ablityPointsThisColony = 100;
        //            List<ConstructionJob> jobList = new List<ConstructionJob>();

        //            Dictionary<Guid,float> stockpileOut = new Dictionary<Guid, float>();

        //            PercentValue priority = new PercentValue {Percent = 1};
        //            Dictionary<Guid,int> jobRawMaterials = new Dictionary<Guid, int>();
        //            jobRawMaterials.Add(_duraniumSD.ID, 5000); //500 per item
        //            jobRawMaterials.Add(_corundiumSD.ID, 70); //7 per item
        //            ConstructionJob newJob = new ConstructionJob 
        //            {
        //                Type = itemConstructing,  
        //                ItemsRemaining = 10, 
        //                PriorityPercent = priority,
        //                RawMaterialsRemaining = jobRawMaterials,
        //                BuildPointsRemaining = 1000,
        //                BuildPointsPerItem = 100
        //            };
        //            jobList.Add(newJob);

        //            colonyInfo.MineralStockpile.Add(_duraniumSD.ID, 2250); //not enough of this should get 4.5  total installations. 
        //            colonyInfo.MineralStockpile.Add(_corundiumSD.ID, 100); //enough of this
        //            stockpileOut.Add(itemConstructing,0);


        //            //firstpass 
        //            InstallationProcessor.GenericConstructionJobs(0, jobList, colonyInfo, stockpileOut);
        //            Assert.AreEqual(0, stockpileOut[itemConstructing], "Should not have constructed anything due to no buildpoints");
        //            Assert.AreEqual(2250, colonyInfo.MineralStockpile[_duraniumSD.ID], "Mineral Usage Incorrect");
        //            Assert.AreEqual(100, colonyInfo.MineralStockpile[_corundiumSD.ID], "Mineral Usage Incorrect");

        //            //todo: fix floating point math.

        //            //secondPass
        //            InstallationProcessor.GenericConstructionJobs(100, jobList, colonyInfo, stockpileOut);
        //            Assert.AreEqual(1, stockpileOut[itemConstructing]);

        //            //thirdPass
        //            //InstallationProcessor.GenericConstructionJobs(50, jobList, colonyInfo, stockpileOut);
        //            //Assert.AreEqual(1.5, stockpileOut[itemConstructing]);            

        //            //fourthPass
        //            //InstallationProcessor.GenericConstructionJobs(5000, jobList, colonyInfo, stockpileOut);
        //            //Assert.AreEqual(4.5, stockpileOut[itemConstructing]);

        //            //todo there's probilby some edge cases to check.
        //        }

        //        [Test]
        //        public void TestEconTick()
        //        {
        //            Game.Instance.AdvanceTime(68300);
        //            Game.Instance.AdvanceTime(100);
        //            Game.Instance.AdvanceTime(68400);
        //        }
        //    }
    }
}

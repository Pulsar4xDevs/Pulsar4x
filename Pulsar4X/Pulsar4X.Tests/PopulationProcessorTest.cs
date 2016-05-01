using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    class PopulationProcessorTest
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
            //StaticDataManager.LoadData("", _game);  // TODO: Figure out correct directory
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");  // Terrian?
            
             

            /*_galaxyFactory = new GalaxyFactory();
            _starSystemFactory = new StarSystemFactory(_galaxyFactory);

            _starSystem = _starSystemFactory.CreateSol(_game);*/  // Seems unnecessary for now

            _planetEntity = Entity.Create(_entityManager, SystemBodyFactory.CreateBaseBody());
            _planetEntity.SetDataBlob(new ColonyInfoDB());
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");

            _planetEntity = Entity.Create(_entityManager, SystemBodyFactory.CreateBaseBody());
            _planetEntity.SetDataBlob(new ColonyInfoDB());
            _planetEntity.GetDataBlob<NameDB>().SetName(_faction, "Terran");

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
        }

        [Test]
        public void testPopulationGrowth()
        {
            // Assumption - the population of any planet will not reach the maximum size of a long variable type, therefore
            // I am not testing that edge case


            long[] basePop = new long[] { 0, 5, 10, 100, 999, 1000, 10000, 100000 };
            long newPop;

            // Set the colony population to five million to start
            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

            // Single iteration growth test
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

            // Multiple iteration growth test
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

        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    public class PopulationProcessorTest
    {
        private TestGame _game;
        private EntityManager _entityManager;



        private Dictionary<string, GasBlueprint> _gasDictionary;
        private List<Entity> _planetsList;
        private List<Entity> _speciesList;

        [SetUp]
        public void Init()
        {
            _game  = new TestGame(1);

            _entityManager = new EntityManager();
            _entityManager.Initialize(_game.Game);

            // Initialize gas dictionary - haven't found a good way to look up gases without doing this
            _gasDictionary = new Dictionary<string, GasBlueprint>();

            foreach (var (id, gas) in _game.Game.AtmosphericGases)
            {
                _gasDictionary.Add(gas.UniqueID, gas);
            }


            _planetsList = new List<Entity>();
            _planetsList.Add(_game.Earth);

            _speciesList = new List<Entity>();
            _speciesList.Add(_game.HumanSpecies);
            //_speciesList.Add(_game.GreyAlienSpecies);

            // Set up colonies
            // @todo: add more colonies, especially ones with multiple species in one colony


            // ComponentTemplateSD infrastructureSD = _game.Game.StaticData.ComponentTemplates[new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb")];
            // ComponentDesigner infrastructureDesigner = new ComponentDesigner(infrastructureSD, _game.HumanFaction.GetDataBlob<FactionTechDB>());
            // EntityManipulation.AddComponentToEntity(_game.EarthColony, infrastructureDesigner.CreateDesign(_game.HumanFaction));

            // ReCalcProcessor.ReCalcAbilities(_game.EarthColony);

        }

        [TearDown]
        public void Cleanup()
        {
            // @todo: hit all entities
            _game = null;
            _entityManager = null;

            _gasDictionary.Clear();
            _gasDictionary = null;
            _planetsList.Clear();
            _planetsList = null;
            _speciesList.Clear();
            _speciesList = null;
    }

        [Test]
        public void testPopulationGrowth()
        {
            // Set the colony population to five million to start

            foreach (var planet in _planetsList)
            {
                foreach (var species in _speciesList)
                {
                    // set up population and infrastructure for each test
                    testPlanetAndSpecies(planet, species);
                }

            }

        }

        private void testPlanetAndSpecies(Entity planet, Entity species)
        {
            long[] basePop = new long[] { 0, 5, 10, 100, 999, 1000, 10000, 100000, 10000000 };
            long[] infrastructureAmounts = new long[] { 0, 1, 5, 100 };
            Dictionary<int, long> newPop, returnedPop;

            int i, j, k;

            string infGUID = "infrastruture";
            var factionDataStore = species.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data;
            ComponentTemplateBlueprint infrastructureSD = factionDataStore.ComponentTemplates[infGUID];

            ComponentDesigner infrastructureDesigner = new ComponentDesigner(infrastructureSD, factionDataStore, _game.HumanFaction.GetDataBlob<FactionTechDB>());
            ComponentDesign infrastructureDesign = infrastructureDesigner.CreateDesign(_game.HumanFaction);

            var pop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;


            // Single iteration growth test
            for (i = 0; i < infrastructureAmounts.Length; i++)
            {
                // Create a new colony with this planet and species, add infrastructure item to it
                _game.EarthColony = ColonyFactory.CreateColony(_game.HumanFaction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    _game.EarthColony.AddComponent(infrastructureDesign);

                ReCalcProcessor.ReCalcAbilities(_game.EarthColony);

                for (j = 0; j < basePop.Length; j++)
                {
                    // set up population and infrastructure for each test
                    newPop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (var (id, value) in newPop.ToArray())
                    {
                        newPop[id] = basePop[j];
                    }

                    //var infrastuctures = _game.EarthColony.GetDataBlob<ComponentInstancesDB>().SpecificInstances[infrastructureEntity].Where(inf => inf.DesignEntity.HasDataBlob<LifeSupportAbilityDB>());

                    returnedPop = calcGrowthIteration(_game.EarthColony, newPop);
                    PopulationProcessor.GrowPopulation(_game.EarthColony);

                    foreach (var (id, value) in pop.ToArray())
                    {
                        Assert.AreEqual(returnedPop[id], pop[id]);
                    }
                }

            }

            // Multiple iteration growth test
            for (i = 0; i < infrastructureAmounts.Length; i++)
            {
                // Create a new colony with this planet and species, add infrastructure item to it
                _game.EarthColony = ColonyFactory.CreateColony(_game.HumanFaction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    _game.EarthColony.AddComponent(infrastructureDesign);
                ReCalcProcessor.ReCalcAbilities(_game.EarthColony);

                for (j = 0; j < basePop.Length; j++)
                {
                    // set up population and infrastructure for each test
                    newPop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (var (id, value) in newPop.ToArray())
                    {
                        newPop[id] = basePop[j];
                    }

                    for(k = 0; k < 10; k++)
                    {
                        newPop = calcGrowthIteration(_game.EarthColony, newPop);
                        PopulationProcessor.GrowPopulation(_game.EarthColony);
                    }

                    foreach (var (id, value) in pop.ToArray())
                    {
                        Assert.AreEqual(newPop[id], pop[id]);
                    }
                }

            }
        }


        // Calculates the new population.  If maxPop = -1, there is no cap
        private long calcNewPop(long lastPop, long maxPop)
        {
            long newPop = 0;
            double growthRate = 0;

            if(maxPop >= 0)
            {
                if (lastPop > maxPop)
                    growthRate = -50.0;
            }
            else
            {
                growthRate = (20.0 / (Math.Pow(lastPop, (1.0 / 3.0))));
            }

            newPop = (long)(lastPop * (1.0 + growthRate));
            if (newPop > maxPop)
                newPop = maxPop;
            if (newPop < 0)
                newPop = 0;

            return newPop;
        }

        private Dictionary<int, long> calcGrowthIteration(Entity colony, Dictionary<int, long> lastPop )
        {
            // Get current population
            var returnPop = new Dictionary<int, long>();
            Entity colonyPlanet = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            var instancesDB = colony.GetDataBlob<ComponentInstancesDB>();
            var popSupportTypes = instancesDB.GetDesignsByType(typeof(PopulationSupportAtbDB));


            long popSupportValue = 0;
            foreach (var design in popSupportTypes)
            {
                var designValue = design.GetAttribute<PopulationSupportAtbDB>().PopulationCapacity;
                var numberOf = instancesDB.GetNumberOfComponentsOfDesign(design.UniqueID);
                popSupportValue = designValue * numberOf;
            }


            returnPop.Clear();


            long needsSupport = 0;

            foreach (var (id, value) in lastPop)
            {
                // count the number of different population groups that need infrastructure support
                var species = colony.Manager.GetGlobalEntityById(id).GetDataBlob<SpeciesDB>();
                if (species.ColonyCost(colonyPlanet) > 1.0)
                    needsSupport++;
            }

            foreach (var (id, value) in lastPop.ToArray())
            {
                var specDb = colony.Manager.GetGlobalEntityById(id).GetDataBlob<SpeciesDB>();
                double colonyCost = specDb.ColonyCost(colonyPlanet);
                long maxPopulation;
                long newPop;

                if (colonyCost > 1.0)
                {
                    maxPopulation = popSupportValue / needsSupport;
                }
                else
                    maxPopulation = -1;

                newPop = calcNewPop(value, maxPopulation);

                returnPop.Add(id, newPop);
            }

            return returnPop;

        }

        // Sets a planet entity to earth normal
        private Entity setEarthPlanet()
        {
            Entity resultPlanet;

            var atmoGasses = new Dictionary<string, float>();

            atmoGasses.Add("nitrogen", 0.79f);
            atmoGasses.Add("oxygen", 0.20f);
            atmoGasses.Add("argon", 0.01f);
            AtmosphereDB atmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 57.2f, atmoGasses);

            resultPlanet = setAtmosphere(atmosphereDB);

            resultPlanet.GetDataBlob<SystemBodyInfoDB>().BaseTemperature = 14.0f;
            resultPlanet.GetDataBlob<SystemBodyInfoDB>().Gravity = 1.0;


            return resultPlanet;
        }

        // Sets an entity to earth normal aside from the atmosphere
        private Entity setAtmosphere(AtmosphereDB atmosDB)
        {
            SystemBodyInfoDB earthBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true };
            NameDB earthNameDB = new NameDB("Earth");
            earthBodyDB.Gravity = 1.0;
            earthBodyDB.BaseTemperature = 20.0f;

            Entity resultPlanet = Entity.Create();
            _entityManager.AddEntity(resultPlanet, new List<BaseDataBlob> { earthBodyDB, earthNameDB, atmosDB });
            return resultPlanet;
        }


    }


}


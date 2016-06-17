﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    public class PopulationProcessorTest
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _earthPlanet, _marsPlanet, _lunaPlanet;
        private Entity _faction;
        private Entity _colonyEntity;
        private GalaxyFactory _galaxyFactory;
        private StarSystemFactory _starSystemFactory;
        private StarSystem _starSystem;
        private Dictionary<string, AtmosphericGasSD> _gasDictionary;
        private List<Entity> _planetsList;
        private List<Entity> _speciesList;

        [SetUp]
        public void Init()
        {
            _game = new Game(new NewGameSettings());
            StaticDataManager.LoadData("Pulsar4x", _game);  // TODO: Figure out correct directory
            _entityManager = new EntityManager(_game);
            _faction = FactionFactory.CreateFaction(_game, "Terran");  // Terrian?


            // Initialize gas dictionary - haven't found a good way to look up gases without doing this
            _gasDictionary = new Dictionary<string, AtmosphericGasSD>();

            foreach (WeightedValue<AtmosphericGasSD> atmos in _game.StaticData.AtmosphericGases)
            {
                _gasDictionary.Add(atmos.Value.ChemicalSymbol, atmos.Value);
            }

            // Set up planets
            // @todo: add more planets
            _earthPlanet = setEarthPlanet();

            _planetsList = new List<Entity>();
            _planetsList.Add(_earthPlanet);


            // Set up species
            // @todo: add more species
            Entity species = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);

            _speciesList = new List<Entity>();
            _speciesList.Add(species);


            // Set up colonies
            // @todo: add more colonies, especially ones with multiple species in one colony
            _colonyEntity = ColonyFactory.CreateColony(_faction, species, _earthPlanet);

            ComponentTemplateSD infrastructureSD = _game.StaticData.Components[new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb")];
            ComponentDesign infrastructureDesign = GenericComponentFactory.StaticToDesign(infrastructureSD, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            Entity infrastructureEntity = GenericComponentFactory.DesignToEntity(_game, _faction, infrastructureDesign);

            ShipAndColonyInfoProcessor.AddComponentDesignToEntity(infrastructureEntity, _colonyEntity);

            ReCalcProcessor.ReCalcAbilities(_colonyEntity);

        }

        [TearDown]
        public void Cleanup()
        {
            // @todo: hit all entities
            _game = null;
            _entityManager = null;
            _faction = null;
            _colonyEntity = null;


            _earthPlanet = null;
            _marsPlanet = null;
            _lunaPlanet = null; ;
            _galaxyFactory = null;
            _starSystemFactory = null;
            _starSystem = null; 
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
            Dictionary<Entity, long> newPop, returnedPop;

            int i, j, k;

            Guid infGUID = new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb");
            ComponentTemplateSD infrastructureSD = _game.StaticData.Components[infGUID];
            ComponentDesign infrastructureDesign = GenericComponentFactory.StaticToDesign(infrastructureSD, _faction.GetDataBlob<FactionTechDB>(), _game.StaticData);
            Entity infrastructureEntity = GenericComponentFactory.DesignToEntity(_game, _faction, infrastructureDesign);

            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;


            // Single iteration growth test
            for (i = 0; i < infrastructureAmounts.Length; i++)
            {
                // Create a new colony with this planet and species, add infrastructure item to it
                _colonyEntity = ColonyFactory.CreateColony(_faction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    ShipAndColonyInfoProcessor.AddComponentDesignToEntity(infrastructureEntity, _colonyEntity);
                ReCalcProcessor.ReCalcAbilities(_colonyEntity);


                for (j = 0; j < basePop.Length; j++)
                {

                    // set up population and infrastructure for each test
                    newPop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (KeyValuePair<Entity, long> kvp in newPop.ToArray())
                    {
                        newPop[kvp.Key] = basePop[j];
                    }

                    //var infrastuctures = _colonyEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances[infrastructureEntity].Where(inf => inf.DesignEntity.HasDataBlob<LifeSupportAbilityDB>());

                    returnedPop = calcGrowthIteration(_colonyEntity, newPop);
                    PopulationProcessor.GrowPopulation(_colonyEntity);

                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        Assert.AreEqual(returnedPop[kvp.Key], pop[kvp.Key]);
                    }
                }

            }


            // Multiple iteration growth test
            for (i = 0; i < infrastructureAmounts.Length; i++)
            {
                // Create a new colony with this planet and species, add infrastructure item to it
                _colonyEntity = ColonyFactory.CreateColony(_faction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    ShipAndColonyInfoProcessor.AddComponentDesignToEntity(infrastructureEntity, _colonyEntity);
                ReCalcProcessor.ReCalcAbilities(_colonyEntity);

                for (j = 0; j < basePop.Length; j++)
                {
                    // set up population and infrastructure for each test
                    newPop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (KeyValuePair<Entity, long> kvp in newPop.ToArray())
                    {
                        newPop[kvp.Key] = basePop[j];
                    }

                    for(k = 0; k < 10; k++)
                    {
                        newPop = calcGrowthIteration(_colonyEntity, newPop);
                        PopulationProcessor.GrowPopulation(_colonyEntity);
                    }

                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        Assert.AreEqual(newPop[kvp.Key], pop[kvp.Key]);
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

        private Dictionary<Entity, long> calcGrowthIteration(Entity colony, Dictionary<Entity, long> lastPop )
        {
            // Get current population
            Dictionary<Entity, long> returnPop = new Dictionary<Entity, long>();
            Entity colonyPlanet = colony.GetDataBlob<ColonyInfoDB>().PlanetEntity;

            List<KeyValuePair<Entity, List<ComponentInstance>>> infrastructure = colony.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<PopulationSupportAbilityDB>()).ToList();
            long popSupportValue;

            //  Pop Cap = Total Population Support Value / Colony Cost
            // Get total popSupport
            popSupportValue = 0;

            returnPop.Clear();

            foreach (var installation in infrastructure)
            {
                popSupportValue += installation.Key.GetDataBlob<PopulationSupportAbilityDB>().PopulationCapacity;
            }

            long needsSupport = 0;

            foreach (KeyValuePair<Entity, long> kvp in lastPop)
            {
                // count the number of different population groups that need infrastructure support
                if (SpeciesProcessor.ColonyCost(colonyPlanet, kvp.Key.GetDataBlob<SpeciesDB>()) > 1.0)
                    needsSupport++;
            }

            foreach (KeyValuePair<Entity, long> kvp in lastPop.ToArray())
            {
                double colonyCost = SpeciesProcessor.ColonyCost(colonyPlanet, kvp.Key.GetDataBlob<SpeciesDB>());
                long maxPopulation;
                long newPop;

                if (colonyCost > 1.0)
                {
                    maxPopulation = popSupportValue / needsSupport;
                }
                else
                    maxPopulation = -1;

                newPop = calcNewPop(kvp.Value, maxPopulation);

                returnPop.Add(kvp.Key, newPop);
            }

            return returnPop;

        }

        // Sets a planet entity to earth normal
        private Entity setEarthPlanet()
        {
            Entity resultPlanet;

            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>();

            atmoGasses.Add(_gasDictionary["N"], 0.79f);
            atmoGasses.Add(_gasDictionary["O"], 0.20f);
            atmoGasses.Add(_gasDictionary["Ar"], 0.01f);
            AtmosphereDB atmosphereDB = new AtmosphereDB(1f, true, 71, 1f, 1f, 0.3f, 57.2f, atmoGasses);

            resultPlanet = setAtmosphere(atmosphereDB);

            resultPlanet.GetDataBlob<SystemBodyDB>().BaseTemperature = 14.0f;
            resultPlanet.GetDataBlob<SystemBodyDB>().Gravity = 1.0;
            

            return resultPlanet;
        }

        // Sets an entity to earth normal aside from the atmosphere
        private Entity setAtmosphere(AtmosphereDB atmosDB)
        {
            SystemBodyDB earthBodyDB = new SystemBodyDB { Type = BodyType.Terrestrial, SupportsPopulations = true };
            NameDB earthNameDB = new NameDB("Earth");
            earthBodyDB.Gravity = 1.0;
            earthBodyDB.BaseTemperature = 20.0f;

            Entity resultPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, atmosDB });
            return resultPlanet;
        }


    }


}


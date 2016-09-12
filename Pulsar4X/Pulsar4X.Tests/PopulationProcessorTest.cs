using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Population Growth Test")]
    public class PopulationProcessorTest
    {
        private TestGame _game;
        private EntityManager _entityManager;



        private Dictionary<string, AtmosphericGasSD> _gasDictionary;
        private List<Entity> _planetsList; 
        private List<Entity> _speciesList;

        [SetUp]
        public void Init()
        {
            _game = new TestGame();
            StaticDataManager.LoadData("Pulsar4x", _game.Game);  // TODO: Figure out correct directory
            _entityManager = _game.Game.GlobalManager;



            // Initialize gas dictionary - haven't found a good way to look up gases without doing this
            _gasDictionary = new Dictionary<string, AtmosphericGasSD>();

            foreach (WeightedValue<AtmosphericGasSD> atmos in _game.Game.StaticData.AtmosphericGases)
            {
                _gasDictionary.Add(atmos.Value.ChemicalSymbol, atmos.Value);
            }


            _planetsList = new List<Entity>();
            _planetsList.Add(_game.Earth);

            _speciesList = new List<Entity>();
            _speciesList.Add(_game.HumanSpecies);
            //_speciesList.Add(_game.GreyAlienSpecies);

            // Set up colonies
            // @todo: add more colonies, especially ones with multiple species in one colony


            ComponentTemplateSD infrastructureSD = _game.Game.StaticData.ComponentTemplates[new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb")];
            ComponentDesign infrastructureDesign = GenericComponentFactory.StaticToDesign(infrastructureSD, _game.HumanFaction.GetDataBlob<FactionTechDB>(), _game.Game.StaticData);
            Entity infrastructureEntity = GenericComponentFactory.DesignToDesignEntity(_game.Game, _game.HumanFaction, infrastructureDesign);

            EntityManipulation.AddComponentToEntity(_game.EarthColony, infrastructureEntity);

            ReCalcProcessor.ReCalcAbilities(_game.EarthColony);

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
            Dictionary<Entity, long> newPop, returnedPop;

            int i, j, k;

            Guid infGUID = new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb");
            ComponentTemplateSD infrastructureSD = _game.Game.StaticData.ComponentTemplates[infGUID];
            ComponentDesign infrastructureDesign = GenericComponentFactory.StaticToDesign(infrastructureSD, _game.HumanFaction.GetDataBlob<FactionTechDB>(), _game.Game.StaticData);
            Entity infrastructureEntity = GenericComponentFactory.DesignToDesignEntity(_game.Game, _game.HumanFaction, infrastructureDesign);

            Dictionary<Entity, long> pop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;


            // Single iteration growth test
            for (i = 0; i < infrastructureAmounts.Length; i++)
            {
                // Create a new colony with this planet and species, add infrastructure item to it
                _game.EarthColony = ColonyFactory.CreateColony(_game.HumanFaction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    EntityManipulation.AddComponentToEntity(_game.EarthColony, infrastructureEntity);
                ReCalcProcessor.ReCalcAbilities(_game.EarthColony);


                for (j = 0; j < basePop.Length; j++)
                {

                    // set up population and infrastructure for each test
                    newPop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (KeyValuePair<Entity, long> kvp in newPop.ToArray())
                    {
                        newPop[kvp.Key] = basePop[j];
                    }

                    //var infrastuctures = _game.EarthColony.GetDataBlob<ComponentInstancesDB>().SpecificInstances[infrastructureEntity].Where(inf => inf.DesignEntity.HasDataBlob<LifeSupportAbilityDB>());

                    returnedPop = calcGrowthIteration(_game.EarthColony, newPop);
                    PopulationProcessor.GrowPopulation(_game.EarthColony);

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
                _game.EarthColony = ColonyFactory.CreateColony(_game.HumanFaction, species, planet);

                // Add the correct number of infrastructure to the colony
                for (k = 0; k < infrastructureAmounts[i]; k++)
                    EntityManipulation.AddComponentToEntity(_game.EarthColony, infrastructureEntity);
                ReCalcProcessor.ReCalcAbilities(_game.EarthColony);

                for (j = 0; j < basePop.Length; j++)
                {
                    // set up population and infrastructure for each test
                    newPop = _game.EarthColony.GetDataBlob<ColonyInfoDB>().Population;

                    foreach (KeyValuePair<Entity, long> kvp in newPop.ToArray())
                    {
                        newPop[kvp.Key] = basePop[j];
                    }

                    for(k = 0; k < 10; k++)
                    {
                        newPop = calcGrowthIteration(_game.EarthColony, newPop);
                        PopulationProcessor.GrowPopulation(_game.EarthColony);
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

            List<KeyValuePair<Entity, List<Entity>>> infrastructure = colony.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<PopulationSupportAtbDB>()).ToList();
            long popSupportValue;

            //  Pop Cap = Total Population Support Value / Colony Cost
            // Get total popSupport
            popSupportValue = 0;

            returnPop.Clear();

            foreach (var installation in infrastructure)
            {
                popSupportValue += installation.Key.GetDataBlob<PopulationSupportAtbDB>().PopulationCapacity;
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

            Entity resultPlanet = new Entity(_entityManager, new List<BaseDataBlob> { earthBodyDB, earthNameDB, atmosDB });
            return resultPlanet;
        }


    }


}


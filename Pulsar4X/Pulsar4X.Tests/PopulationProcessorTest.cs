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
            



            // Set up species
            // @todo: add more species
            Entity species = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);


            // Set up colonies
            // @todo: add more colonies, especially ones with multiple species in one colony
            _colonyEntity = ColonyFactory.CreateColony(_faction, species, _earthPlanet);
            
            _colonyEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Add()
        }

        [TearDown]
        public void Cleanup()
        {
            // @todo: hit all entities
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

            /*
            // @todo: fix
            Dictionary<Entity, long> returnedPop;


            long[] basePop = new long[] { 0, 5, 10, 100, 999, 1000, 10000, 100000, 10000000 };
            long[] infrastructureAmounts = new long[] { 0, 1, 5, 100 };
            Dictionary<Entity, long> newPop;

            // Set the colony population to five million to start
            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

            // Single iteration growth test
            int i, j;



            for (i = 0; i < basePop.Length; i++)
            {
                for (j = 0; j < infrastructureAmounts.Length; j++)
                {
                    // set up population and infrastructure for each test
                    newPop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;
                    returnedPop = calcGrowthIteration(_colonyEntity, newPop);
                    PopulationProcessor.GrowPopulation(_colonyEntity);

                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        Assert.AreEqual(returnedPop[kvp.Key], pop[kvp.Key]);
                    }
                }

            }

            newPop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;
            returnedPop = calcGrowthIteration(_colonyEntity, newPop);
            PopulationProcessor.GrowPopulation(_colonyEntity);


            // Multiple iteration growth test
            for (int j = 1; j < 10; j++)
            {

                for (i = 0; i < basePop.Length; i++)
                {
                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        if (pop.ContainsKey(kvp.Key))
                        {
                            pop[kvp.Key] = basePop[i];
                        }
                        newPop = basePop[i];

                        for (int k = 0; k < j; k++)
                        {
                            newPop = calcGrowthIteration(newPop);
                            PopulationProcessor.GrowPopulation(_colonyEntity);
                        }

                        Assert.AreEqual(returnedPop[kvp.Key], pop[kvp.Key]);
                    }
                }
            }*/

        }

        private void testPlanetAndSpecies(Entity planet, Entity species)
        {
            long[] basePop = new long[] { 0, 5, 10, 100, 999, 1000, 10000, 100000, 10000000 };
            long[] infrastructureAmounts = new long[] { 0, 1, 5, 100 };
            Dictionary<Entity, long> newPop, returnedPop;

            Entity testColony;

            long maxPop, lastPop;

            // Set the colony population to five million to start
            Dictionary<Entity, long> pop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;

            // Single iteration growth test
            int i, j;

            // Create a new colony with this planet and species
            testColony = ColonyFactory.CreateColony(_faction, species, planet);

            // Create an infrastructure item
            List<KeyValuePair<Entity, List<ComponentInstance>>> infrastructure = testColony.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<PopulationSupportAbilityDB>()).ToList();

            if (infrastructure.LongCount() == 0) // Empty
            {
                // Add infrastructure to the list of installations
                //Entity infraItem = new Entity(;

                //ComponentInfoDB infra = ;
                //infra.

                //testColony.GetDataBlob<ComponentInstancesDB>().SpecificInstances
            }

            for (i = 0; i < basePop.Length; i++)
            {
                for (j = 0; j < infrastructureAmounts.Length; j++)
                {
                    int supportValue = 0;

                    // set up population and infrastructure for each test
                    newPop = testColony.GetDataBlob<ColonyInfoDB>().Population;
                    foreach (KeyValuePair<Entity, long> kvp in newPop.ToArray())
                    {
                        newPop[kvp.Key] = basePop[i];
                    }

                    for(int k = 0; k < infrastructureAmounts[j]; k++)
                    {


                        foreach (var installation in infrastructure)
                        {
                            supportValue += installation.Key.GetDataBlob<LifeSupportAbilityDB>().LifeSupportCapacity;
                        }
                        
                    }

                        returnedPop = calcGrowthIteration(_colonyEntity, newPop);
                    PopulationProcessor.GrowPopulation(_colonyEntity);

                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        Assert.AreEqual(returnedPop[kvp.Key], pop[kvp.Key]);
                    }
                }

            }

            newPop = _colonyEntity.GetDataBlob<ColonyInfoDB>().Population;
            returnedPop = calcGrowthIteration(_colonyEntity, newPop);
            PopulationProcessor.GrowPopulation(_colonyEntity);


            // Multiple iteration growth test
            for (j = 1; j < 10; j++)
            {

                for (i = 0; i < basePop.Length; i++)
                {
                    foreach (KeyValuePair<Entity, long> kvp in pop.ToArray())
                    {
                        if (pop.ContainsKey(kvp.Key))
                        {
                            pop[kvp.Key] = basePop[i];
                        }
                        newPop[kvp.Key] = basePop[i];

                        for (int k = 0; k < j; k++)
                        {
                            newPop = calcGrowthIteration(testColony, newPop);
                            PopulationProcessor.GrowPopulation(testColony);
                        }

                        Assert.AreEqual(returnedPop[kvp.Key], pop[kvp.Key]);
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
            
            List<KeyValuePair<Entity, List<ComponentInstance>>> infrastructure = colony.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<PopulationSupportAbilityDB>()).ToList();
            long popSupportValue;

            // @todo: Get colony cost and infrastructure, figure out population cap
            //  Pop Cap = Total Population Support Value / Colony Cost
            // Get total popSupport
            popSupportValue = 0;

            returnPop.Clear();

            foreach (var installation in infrastructure)
            {
                //if(installations[kvp.Key]
                popSupportValue += installation.Key.GetDataBlob<PopulationSupportAbilityDB>().PopulationCapacity;
            }

            long needsSupport = 0;

            foreach (KeyValuePair<Entity, long> kvp in lastPop)
            {
                // count the number of different population groups that need infrastructure support
                if (SpeciesProcessor.ColonyCost(colony, kvp.Key.GetDataBlob<SpeciesDB>()) > 1.0)
                    needsSupport++;
            }

            // find colony cost, divide the population support value by it
            // @todo: Get colony cost, or do I need to calculate it?



            foreach (KeyValuePair<Entity, long> kvp in lastPop.ToArray())
            {
                double colonyCost = SpeciesProcessor.ColonyCost(colony, kvp.Key.GetDataBlob<SpeciesDB>());
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

        private void AddComponentDesignToEntity(ComponentInstance specificInstance, Entity parentEntity)
        {
            if (parentEntity.HasDataBlob<ComponentInstancesDB>())
            {
                ComponentInstancesDB componentInstance = parentEntity.GetDataBlob<ComponentInstancesDB>();

                if (!componentInstance.SpecificInstances.ContainsKey(specificInstance.DesignEntity)) //if the entity doesnt already have this component design listed, 
                    componentInstance.SpecificInstances.Add(specificInstance.DesignEntity, new List<ComponentInstance>()); //add the design ID to the dictionary with a new empty list
                componentInstance.SpecificInstances[specificInstance.DesignEntity].Add(specificInstance); //add the specificInstance
                ReCalcProcessor.ReCalcAbilities(parentEntity);
            }
            else throw new Exception("parentEntiy does not contain a ComponentInstanceDB");
        }

        public static ComponentTemplateSD InfrastructureInstallation()
        {
            ComponentTemplateSD component = new ComponentTemplateSD();
            component.Name = "Infrastructure";
            component.Description = "Component and buildings needed to keep colonists alive on hostile worlds";
            component.ID = new Guid("08b3e64c-912a-4cd0-90b0-6d0f1014e9bb");

            component.SizeFormula = "1";

            component.HTKFormula = "[Size]";

            component.CrewReqFormula = "0";

            component.ResearchCostFormula = "0";

            component.BuildPointCostFormula = "[Size]";

            component.MineralCostFormula = new Dictionary<Guid, string> {{new Guid("2dfc78ea-f8a4-4257-bc04-47279bf104ef"), "10"}};

            component.CreditCostFormula = "0";

            component.MountType = ComponentMountType.PlanetInstallation | ComponentMountType.ShipCargo;

            component.ComponentAbilitySDs = new List<ComponentTemplateAbilitySD>();


            ComponentTemplateAbilitySD infrastructureAbility = new ComponentTemplateAbilitySD();
            infrastructureAbility.Name = "Support Colonists";
            infrastructureAbility.Description = "Keeps colonists alive on hostile worlds";
            infrastructureAbility.GuiHint = GuiHint.None;
            infrastructureAbility.GuidDictionary = new Dictionary<object, string>
            {
            };
            infrastructureAbility.AbilityDataBlobType = typeof(LifeSupportAbilityDB).ToString();
            component.ComponentAbilitySDs.Add(infrastructureAbility);

            return component;
        }
    }


}


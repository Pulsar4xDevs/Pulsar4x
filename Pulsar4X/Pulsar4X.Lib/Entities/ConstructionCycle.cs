using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using System.Drawing;
using System.Collections.ObjectModel;

namespace Pulsar4X.Entities
{
    /// <summary>
    /// Construction cycle will perform construction, mining, ship building, and research related work.
    /// </summary>
    public class ConstructionCycle
    {
        public static void DoConstructionCycle(BindingList<Faction> P)
        {
            foreach (Faction CurrentFaction in P)
            {
#warning Implement gas giant harvesters, they should be separate from the population based refineries below.
                foreach (Population CurrentPopulation in CurrentFaction.Populations)
                {
                    /// <summary>
                    /// There are some floating point normalization issues with ConstructionFactoryBuild. I could fix them by making only integer construction possible however. Not sure if I want to do that.
                    /// maybe changing construction to decimals and not floats could work. or some kind of normalization kludge.
                    /// </summary>

                    /// <summary>
                    /// Mining should happen "first" since these should all happen quasi-simultaneously and mining is the only one that they all depend on to produce mineral resources.
                    /// </summary>
                    ConstructionCycle.MinePlanets(CurrentFaction,CurrentPopulation);

                    /// <summary>
                    /// The rest of these don't particularly depend on one another so they can happen at any time.
                    /// </summary>
                    ConstructionCycle.ConstructionFactoryBuild(CurrentFaction, CurrentPopulation);
                    ConstructionCycle.OrdnanceFactoryBuild(CurrentFaction, CurrentPopulation);
                    ConstructionCycle.RefineFuel(CurrentFaction, CurrentPopulation);
                    ConstructionCycle.ProcessShipyards(CurrentFaction, CurrentPopulation);
                    ConstructionCycle.TerraformPlanets(CurrentFaction, CurrentPopulation);

                    /// <summary>
                    /// Population growth should happen after production, or else the statistics printed to the UI regarding any employment based penalties could be inaccurate.
                    /// </summary>
                    ConstructionCycle.PopulationGrowth(CurrentFaction, CurrentPopulation);
                }
            }
        }

        /// <summary>
        /// Processes construction factory work for every faction.
        /// </summary>
        /// <param name="P">List of factions.</param>
        public static void ConstructionFactoryBuild(Faction CurrentFaction, Population CurrentPopulation)
        {
            /// <summary>
            /// How much construction work per day does this colony do? default should be 5 day construction cycle.
            /// </summary>
            float CurrentIndustry = CurrentPopulation.CalcTotalIndustry() * Constants.Colony.ConstructionCycleFraction;
            float BuildPercentage = 0.0f;

            foreach (ConstructionBuildQueueItem CurrentConstruction in CurrentPopulation.ConstructionBuildQueue)
            {
                /// <summary>
                /// Check to see if this item is in the current build queue, or has to wait for capacity to free up.
                /// </summary>
                if ((BuildPercentage + CurrentConstruction.buildCapacity) <= 100.0f)
                {
                    BuildPercentage = BuildPercentage + CurrentConstruction.buildCapacity;
                }
                else
                {
                    break;
                }

                /// <summary>
                /// Calculate Completion Estimate:
                /// </summary>
                float BPRequirement = (float)Math.Floor(CurrentConstruction.numToBuild) * (float)CurrentConstruction.costPerItem;
                float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;
                float YearsOfProduction = (BPRequirement / CurrentConstruction.buildCapacity);

                if (CurrentConstruction.buildCapacity != 0.0f && YearsOfProduction < Constants.Colony.TimerYearMax)
                {
                    int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                    DateTime EstTime = GameState.Instance.GameDateTime;
                    TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                    EstTime = EstTime.Add(TS);
                    CurrentConstruction.completionDate = EstTime;
                }

                /// <summary>
                /// This construction project is paused right now.
                /// </summary>
                if (CurrentConstruction.inProduction == false)
                {
                    continue;
                }

                /// <summary>
                /// how much of total industry does this build project use?
                /// </summary>
                float DevotedIndustry = (CurrentConstruction.buildCapacity / 100.0f) * CurrentIndustry;
                float Completion = DevotedIndustry / (float)CurrentConstruction.costPerItem;

                bool CIRequired = false;
                bool MineRequired = false;
                bool CanBuild = false;

                /// <summary>
                /// Conventional industry must also be used.
                /// </summary>
                if (CurrentConstruction.buildType == ConstructionBuildQueueItem.CBType.PlanetaryInstallation)
                {
                    if ((int)CurrentConstruction.installationBuild.Type >= (int)Installation.InstallationType.ConvertCIToConstructionFactory &&
                       (int)CurrentConstruction.installationBuild.Type <= (int)Installation.InstallationType.ConvertCIToOrdnanceFactory)
                    {
                        CIRequired = true;
                        CanBuild = CurrentPopulation.CIRequirement(Completion);

                        if (CanBuild == false)
                        {
                            String Entry = String.Format("Insufficent Conventional Industry to continue build order on {0} for {1}x {2}", CurrentPopulation, CurrentConstruction.numToBuild,
                                CurrentConstruction.Name);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.ColonyLacksCI, CurrentPopulation.Contact.Position.System, CurrentPopulation.Contact,
                                                                GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
                            CurrentFaction.MessageLog.Add(Msg);
                            continue;
                        }
                    }

                    if ((int)CurrentConstruction.installationBuild.Type == (int)Installation.InstallationType.ConvertMineToAutomated)
                    {
                        MineRequired = true;
                        CanBuild = CurrentPopulation.MineRequirement(Completion);

                        if (CanBuild == false)
                        {
                            String Entry = String.Format("Insufficent Mines to continue build order on {0} for {1}x {2}", CurrentPopulation, CurrentConstruction.numToBuild,
                                CurrentConstruction.Name);
                            MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.ColonyLacksCI, CurrentPopulation.Contact.Position.System, CurrentPopulation.Contact,
                                                                GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
                            CurrentFaction.MessageLog.Add(Msg);
                            continue;
                        }
                    }
                }


                /// <summary>
                /// Check mineral costs to see if this can be built.
                /// </summary>
                switch (CurrentConstruction.buildType)
                {
#warning PDC construction needs to be implemented here and slightly further down.
                    case ConstructionBuildQueueItem.CBType.PlanetaryInstallation:
                        CanBuild = CurrentPopulation.MineralRequirement(CurrentConstruction.installationBuild.MinerialsCost, Completion);
                        break;
                    case ConstructionBuildQueueItem.CBType.ShipComponent:
                        CanBuild = CurrentPopulation.MineralRequirement(CurrentConstruction.componentBuild.minerialsCost, Completion);
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCConstruction:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCPrefab:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCAssembly:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCRefit:
                        break;
                    case ConstructionBuildQueueItem.CBType.MaintenanceSupplies:
                        CanBuild = CurrentPopulation.MineralRequirement(Constants.Colony.MaintenanceMineralCost, Completion);
                        break;
                }

                if (CanBuild == false)
                {
                    String Entry = String.Format("Insufficent Minerals to continue build order on {0} for {1}x {2}", CurrentPopulation, CurrentConstruction.numToBuild,
                                CurrentConstruction.Name);
                    MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Contact.Position.System, CurrentPopulation.Contact,
                                                        GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
                    CurrentFaction.MessageLog.Add(Msg);
                    continue;
                }


                /// <summary>
                /// Adjust number to build downward to reflect construction happening.
                /// </summary>
                CurrentConstruction.numToBuild = CurrentConstruction.numToBuild - Completion;

                /// <summary>
                /// Handle the cost and build the item.
                /// </summary>
                switch (CurrentConstruction.buildType)
                {
                    case ConstructionBuildQueueItem.CBType.PlanetaryInstallation:
                        if (CIRequired == false && MineRequired == false)
                            CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, CurrentConstruction.installationBuild.MinerialsCost, Completion);
                        else if (CIRequired == true && MineRequired == false)
                            CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, CurrentConstruction.installationBuild.MinerialsCost, Completion, CIRequired, MineRequired);
                        else if (CIRequired == false && MineRequired == true)
                            CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, CurrentConstruction.installationBuild.MinerialsCost, Completion, CIRequired, MineRequired);
                        CurrentPopulation.AddInstallation(CurrentConstruction.installationBuild, Completion);
                        break;
                    case ConstructionBuildQueueItem.CBType.ShipComponent:

                        /// <summary>
                        /// Component issues seem more blatant than construction issues.
                        /// </summary>
                        if (CurrentConstruction.numToBuild < 0.0f)
                        {
                            Completion = Completion + CurrentConstruction.numToBuild;
                        }

                        CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, CurrentConstruction.componentBuild.minerialsCost, Completion);
                        CurrentPopulation.AddComponentsToStockpile(CurrentConstruction.componentBuild, Completion);
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCConstruction:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCPrefab:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCAssembly:
                        break;
                    case ConstructionBuildQueueItem.CBType.PDCRefit:
                        break;
                    case ConstructionBuildQueueItem.CBType.MaintenanceSupplies:

                        /// <summary>
                        /// Component issues seem more blatant than construction issues.
                        /// </summary>
                        if (CurrentConstruction.numToBuild < 0.0f)
                        {
                            Completion = Completion + CurrentConstruction.numToBuild;
                        }

                        CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, Constants.Colony.MaintenanceMineralCost, Completion);
                        CurrentPopulation.AddMSP(Completion);
                        break;
                }

            }

            /// <summary>
            /// Cleanup the CBQ here.
            /// </summary>
            for (int CBQIterator = 0; CBQIterator < CurrentPopulation.ConstructionBuildQueue.Count; CBQIterator++)
            {
                if (CurrentPopulation.ConstructionBuildQueue[CBQIterator].numToBuild <= 0.0f)
                {
                    CurrentPopulation.ConstructionBuildQueue.RemoveAt(CBQIterator);
                    CBQIterator--;
                }

            }            
        }

        /// <summary>
        /// Handles ordnance factory construction.
        /// </summary>
        /// <param name="P">List of factions.</param>
        public static void OrdnanceFactoryBuild(Faction CurrentFaction, Population CurrentPopulation)
        {
            /// <summary>
            /// How much construction work per day does this colony do? default should be 5 day construction cycle.
            /// </summary>
            float CurrentIndustry = CurrentPopulation.CalcTotalOrdnanceIndustry() * Constants.Colony.ConstructionCycleFraction;
            float BuildPercentage = 0.0f;

            foreach (MissileBuildQueueItem CurrentConstruction in CurrentPopulation.MissileBuildQueue)
            {
                /// <summary>
                /// Check to see if this item is in the current build queue, or has to wait for capacity to free up.
                /// </summary>
                if ((BuildPercentage + CurrentConstruction.buildCapacity) <= 100.0f)
                {
                    BuildPercentage = BuildPercentage + CurrentConstruction.buildCapacity;
                }
                else
                {
                    break;
                }

                /// <summary>
                /// Calculate Completion Estimate:
                /// </summary>
                float BPRequirement = (float)Math.Floor(CurrentConstruction.numToBuild) * (float)CurrentConstruction.costPerItem;
                float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;
                float YearsOfProduction = (BPRequirement / CurrentConstruction.buildCapacity);

                if (CurrentConstruction.buildCapacity != 0.0f && YearsOfProduction < Constants.Colony.TimerYearMax)
                {
                    int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                    DateTime EstTime = GameState.Instance.GameDateTime;
                    TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                    EstTime = EstTime.Add(TS);
                    CurrentConstruction.completionDate = EstTime;
                }

                /// <summary>
                /// This construction project is paused right now.
                /// </summary>
                if (CurrentConstruction.inProduction == false)
                {
                    continue;
                }

                /// <summary>
                /// how much of total industry does this build project use?
                /// </summary>
                float DevotedIndustry = (CurrentConstruction.buildCapacity / 100.0f) * CurrentIndustry;
                float Completion = DevotedIndustry / (float)CurrentConstruction.costPerItem;

                /// <summary>
                /// Do I have the minerals to build this missile?
                /// </summary>
                bool CanBuild = CurrentPopulation.MineralRequirement(CurrentConstruction.ordnanceDef.minerialsCost, Completion);

                if (CanBuild == false)
                {
                    String Entry = String.Format("Insufficent Minerals to continue build order on {0} for {1}x {2}", CurrentPopulation, CurrentConstruction.numToBuild,
                                CurrentConstruction.Name);
                    MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Contact.Position.System, CurrentPopulation.Contact,
                                                        GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
                    CurrentFaction.MessageLog.Add(Msg);
                    continue;
                }


                /// <summary>
                /// Adjust number to build downward to reflect construction happening.
                /// </summary>
                CurrentConstruction.numToBuild = CurrentConstruction.numToBuild - Completion;

                CurrentPopulation.HandleBuildItemCost(CurrentConstruction.costPerItem, CurrentConstruction.ordnanceDef.minerialsCost, Completion);

                CurrentPopulation.LoadMissileToStockpile(CurrentConstruction.ordnanceDef, Completion);
            }

            /// <summary>
            /// Cleanup the CBQ here.
            /// </summary>
            for (int MBQIterator = 0; MBQIterator < CurrentPopulation.MissileBuildQueue.Count; MBQIterator++)
            {
                if (CurrentPopulation.MissileBuildQueue[MBQIterator].numToBuild <= 0.0f)
                {
                    CurrentPopulation.MissileBuildQueue.RemoveAt(MBQIterator);
                    MBQIterator--;
                }

            }            
        }

        /// <summary>
        /// Perform mining on all faction owned colonies.
        /// </summary>
        /// <param name="P">Binding list of factions</param>
        public static void MinePlanets(Faction CurrentFaction, Population CurrentPopulation)
        {
            if (CurrentPopulation.Planet.GeoSurveyList.ContainsKey(CurrentFaction) == true)
            {
                /// <summary>
                /// see what I mean about superflous? going to keep it like this for right now however.
                /// </summary>
                if (CurrentPopulation.Planet.GeoSurveyList[CurrentFaction] == true)
                {
                    /// <summary>
                    /// Calculate the construction time cycle sliver of the year to use. all production is done annually so this must be adjusted here.
                    /// </summary>
                    float CurrentMining = CurrentPopulation.CalcTotalMining() * Constants.Colony.ConstructionCycleFraction;

                    /// <summary>
                    /// Don't run this loop if no mining can be done.
                    /// </summary>
                    if (CurrentMining > 0.0f)
                    {
                        for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                        {
                            float MineAmount = CurrentMining * CurrentPopulation.Planet.MinerialAccessibility[mineralIterator];

                            /// <summary>
                            /// no negative minerals. hopefully.
                            /// </summary>
                            if (CurrentPopulation.Planet.MinerialReserves[mineralIterator] < MineAmount)
                            {
                                MineAmount = CurrentPopulation.Planet.MinerialReserves[mineralIterator];
                            }

                            /// <summary>
                            /// Add to population stockpile and take from planetary reserves.
                            /// </summary>
                            CurrentPopulation.Minerials[mineralIterator] = CurrentPopulation.Minerials[mineralIterator] + MineAmount;
                            CurrentPopulation.Planet.MinerialReserves[mineralIterator] = CurrentPopulation.Planet.MinerialReserves[mineralIterator] - MineAmount;

                            /// <summary>
                            /// Ultra-paranoia check here.
                            /// </summary>
                            if (CurrentPopulation.Planet.MinerialReserves[mineralIterator] < 0.0f)
                                CurrentPopulation.Planet.MinerialReserves[mineralIterator] = 0.0f;
                        }
                    }
                }
            }            
        }

        /// <summary>
        /// Do all fuel refining on planets and later on gas giant harvesters.
        /// </summary>
        /// <param name="P">list of factions.</param>
        public static void RefineFuel(Faction CurrentFaction, Population CurrentPopulation)
        {
            /// <summary>
            /// Skip this population.
            /// </summary>
            if (CurrentPopulation.IsRefining == false)
                return;

            float CurrentRefining = CurrentPopulation.CalcTotalRefining() * Constants.Colony.ConstructionCycleFraction;

            /// <summary>
            /// If the planet has no refineries or no sorium then no refining happens.
            /// </summary>
            if (CurrentRefining > 0.0f && CurrentPopulation.Minerials[(int)Constants.Minerals.MinerialNames.Sorium] > 0.0f)
            {
                /// <summary>
                /// 1 sorium = 2000 fuel
                /// </summary>
                float SoriumRequirement = CurrentRefining / Constants.Colony.SoriumToFuel;
                if (CurrentPopulation.Minerials[(int)Constants.Minerals.MinerialNames.Sorium] < SoriumRequirement)
                {
                    SoriumRequirement = CurrentPopulation.Minerials[(int)Constants.Minerals.MinerialNames.Sorium];
                    CurrentRefining = SoriumRequirement * Constants.Colony.SoriumToFuel;
                }

                /// <summary>
                /// Convert Sorium into fuel.
                /// </summary>
                CurrentPopulation.Minerials[(int)Constants.Minerals.MinerialNames.Sorium] = CurrentPopulation.Minerials[(int)Constants.Minerals.MinerialNames.Sorium] - SoriumRequirement;
                CurrentPopulation.FuelStockpile = CurrentPopulation.FuelStockpile + CurrentRefining;
            }
            else if (CurrentRefining > 0.0f)
            {
                String Entry = String.Format("Insufficient Sorium on {0} to continue refining.", CurrentPopulation);
                MessageEntry Msg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Contact.Position.System, CurrentPopulation.Contact,
                                                    GameState.Instance.GameDateTime, GameState.Instance.LastTimestep, Entry);
                CurrentFaction.MessageLog.Add(Msg);
            }
        }

        /// <summary>
        /// Build and modify ships and shipyards respectively.
        /// *** also missing the shipyard tabs grid work that displays ship construction progress.
        /// </summary>
        /// <param name="P">List of factions.</param>
        public static void ProcessShipyards(Faction CurrentFaction, Population CurrentPopulation)
        {
            int CY = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].Number);
            int NY = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number);

            List<Installation.ShipyardInformation.ShipyardTask> SortedList = CurrentPopulation.ShipyardTasks.Keys.ToList().OrderBy(o => o.Priority).ToList();

            BuildShips(CurrentFaction, CurrentPopulation, SortedList);

            for (int SYIterator = 0; SYIterator < CY; SYIterator++)
            {
                Installation.ShipyardInformation SYInfo = CurrentPopulation.Installations[(int)Installation.InstallationType.CommercialShipyard].SYInfo[SYIterator];
                PerformShipyardActivity(CurrentFaction, CurrentPopulation, SYInfo);

                //BuildShips(CurrentFaction, CurrentPopulation, SYInfo); //build the ships above from the sorted list, not here.
            }

            for (int SYIterator = 0; SYIterator < NY; SYIterator++)
            {
                Installation.ShipyardInformation SYInfo = CurrentPopulation.Installations[(int)Installation.InstallationType.NavalShipyardComplex].SYInfo[SYIterator];
                PerformShipyardActivity(CurrentFaction, CurrentPopulation, SYInfo);

                //BuildShips(CurrentFaction, CurrentPopulation, SYInfo); //build the ships above from the sorted list, not here.
            }            
        }

        /// <summary>
        /// Reduce radiation and atmospheric dust on planets that have been hit with weapon strikes.
        /// </summary>
        public static void CleanUpPlanets()
        {
            BindingList<SystemBody> CleanupList = new BindingList<SystemBody>();
            foreach (SystemBody DmgPlanet in GameState.Instance.DamagedPlanets)
            {
                float RadReduction = Constants.Colony.RadiationDecayPerYear * Constants.Colony.ConstructionCycleFraction;
                float DustReduction = Constants.Colony.AtmosphericDustDecayPerYear * Constants.Colony.ConstructionCycleFraction;

                DmgPlanet.RadiationLevel = DmgPlanet.RadiationLevel - RadReduction;
                DmgPlanet.AtmosphericDust = DmgPlanet.AtmosphericDust - DustReduction;

                if (DmgPlanet.RadiationLevel < 0.0f)
                    DmgPlanet.RadiationLevel = 0.0f;
                if (DmgPlanet.AtmosphericDust < 0.0f)
                    DmgPlanet.AtmosphericDust = 0.0f;

                if (DmgPlanet.RadiationLevel == 0.0f && DmgPlanet.AtmosphericDust == 0.0f)
                    CleanupList.Add(DmgPlanet);
            }

            if (CleanupList.Count != 0)
            {
                foreach (SystemBody CleanPlanet in CleanupList)
                {
                    GameState.Instance.DamagedPlanets.Remove(CleanPlanet);
                }
            }
        }

        /// <summary>
        /// Handle all terraform orders. Were there an explicit order system it would not be necessary to loop through all worlds, likewise all of these loops
        /// should be combined at a later date.
        /// </summary>
        /// <param name="P">List of factions</param>
        public static void TerraformPlanets(Faction CurrentFaction, Population pop)
        {
#warning How should terraforming orders be handled? see above comments.
            /// <summary>
            /// If gas to add is null, or if the colony can't do any terraforming then do not run terraforming.
            /// </summary>
            if (pop._GasToAdd != null && (pop._OrbitalTerraformModules != 0 || (int)Math.Floor(pop.Installations[(int)Installation.InstallationType.TerraformingInstallation].Number) >= 1))
            {
                float CurrentGasAmt = 0.0f;
                if (pop.Planet.Atmosphere.Composition.ContainsKey(pop._GasToAdd) == true)
                    CurrentGasAmt = pop.Planet.Atmosphere.Composition[pop._GasToAdd];

                if (pop._GasAddSubtract == true)
                {
                    if (CurrentGasAmt < pop._GasAmt)
                    {
                        float CurrentTerraforming = pop.CalcTotalTerraforming() * Constants.Colony.ConstructionCycleFraction;

                        /// <summary>
                        /// Terraforming will go over the limit specified by the user.
                        /// </summary>
                        if (CurrentTerraforming + CurrentGasAmt > pop._GasAmt)
                        {
                            CurrentTerraforming = pop._GasAmt - CurrentGasAmt;
                        }

                        pop.Planet.Atmosphere.AddGas(pop._GasToAdd, CurrentTerraforming);
                    }
                }
                else if (pop._GasAddSubtract == false)
                {
                    if (CurrentGasAmt > pop._GasAmt)
                    {
                        float CurrentTerraforming = pop.CalcTotalTerraforming() * Constants.Colony.ConstructionCycleFraction;

                        /// <summary>
                        /// Terraforming will go under the limit specified by the user.
                        /// </summary>
                        if (CurrentTerraforming - CurrentGasAmt < pop._GasAmt)
                        {
                            CurrentTerraforming = CurrentGasAmt - pop._GasAmt;
                        }
                        pop.Planet.Atmosphere.AddGas(pop._GasToAdd, CurrentTerraforming);
                    }
                }
            }            
        }

        /// <summary>
        /// Handle population growth for this world. Get the colony rating, infrastructure, radiation,etc and then calculate whether the population should grow or shrink based
        /// on those factors.
        /// </summary>
        /// <param name="CurrentFaction">Current faction this population belongs to</param>
        /// <param name="CurrentPopulation">The Population in question.</param>
        public static void PopulationGrowth(Faction CurrentFaction, Population CurrentPopulation)
        {
#warning Update the UI to reflect dieoff if present.
            if (CurrentPopulation.CivilianPopulation > 0.0f)
            {
                float CurrentPopGrowth = CurrentPopulation.CalcPopulationGrowth() * Constants.Colony.ConstructionCycleFraction;

                float ColonyCost = CurrentPopulation.Species.GetTNHabRating(CurrentPopulation.Planet);

                if (ColonyCost == 0.0f)
                {
                    CurrentPopulation.AddPopulation(CurrentPopGrowth);
                }
                else
                {
                    /// <summary>
                    /// How much Infra does this colony need per 1M colonists?
                    /// </summary>
                    int InfrastructureRequirement = (int)Math.Floor(ColonyCost * 100.0f);

                    /// <summary>
                    /// How much infra is currently on the planet?
                    /// </summary>
                    int CurrentInfrastructure = (int)Math.Floor(CurrentPopulation.Installations[(int)Installation.InstallationType.Infrastructure].Number);

                    /// <summary>
                    /// How much does this planet need? if the planet does need some it should generate a little on its own.
                    /// </summary>
                    int TotalInfraRequirement = (int)Math.Floor(InfrastructureRequirement * CurrentPopulation.CivilianPopulation);

                    if (TotalInfraRequirement < CurrentInfrastructure)
                    {
                        CurrentPopulation.AddPopulation(CurrentPopGrowth);
                    }
                    else
                    {
                        /// <summary>
                        /// Calculate how much population should die off instead of be added.
                        /// </summary>
#warning Is this ok for the dieoff calculation for population?
                        float CurrentDieoff = -1.0f *( ((float)CurrentInfrastructure / (float)TotalInfraRequirement) * Constants.Colony.ConstructionCycleFraction);

                        CurrentPopulation.AddPopulation(CurrentDieoff);
                    }
                }
            }
        }

        #region Private Methods related to shipyard work.
        /// <summary>
        /// Do all of the tasks that this shipyard has assigned to it.
        /// </summary>
        /// <param name="CurrentFaction">Faction both the population and the shipyard belong to.</param>
        /// <param name="CurrentPopulation">Population the shipyard is on</param>
        /// <param name="SYInfo">Shipyard the tasks are happening at</param>
        private static void BuildShips(Faction CurrentFaction, Population CurrentPopulation, List<Installation.ShipyardInformation.ShipyardTask> SortedList)
        {
            BindingList<Installation.ShipyardInformation.ShipyardTask> TasksToRemove = new BindingList<Installation.ShipyardInformation.ShipyardTask>();
            foreach(Installation.ShipyardInformation.ShipyardTask Task in SortedList)
            {
                if (Task.IsPaused() == true)
                    continue;

                /// <summary>
                /// the Annual Build Rate(ABR) is the number of BP per year that will be devoted to this activity. this is the number of BP produced only this cycle.
                /// </summary>
                float CycleBuildRate = Task.ABR * Constants.Colony.ConstructionCycleFraction;

                /// <summary>
                /// How much of this task will be completed this construction cycle?
                /// </summary>
                float CurrentProgress = CycleBuildRate / (float)Task.Cost;
                if ((Task.Progress + (decimal)CurrentProgress) > 1.0m)
                {
                    CurrentProgress = (float)(1.0m - Task.Progress);
                }

                /// <summary>
                /// Can this shipyard Task be built this construction cycle?
                /// </summary>
                bool CanBuild = CurrentPopulation.MineralRequirement(Task.minerialsCost, CurrentProgress);

                if (CanBuild == true && Task.CurrentTask != Constants.ShipyardInfo.Task.Scrap)
                {
                    CurrentPopulation.HandleShipyardCost(Task.Cost, Task.minerialsCost, CurrentProgress);
                    Task.Progress = Task.Progress + (decimal)CurrentProgress;
                }
                else if (Task.CurrentTask == Constants.ShipyardInfo.Task.Scrap)
                {
                    /// <summary>
                    /// Return money to the population from the scrap.
                    /// </summary>
                    CurrentPopulation.HandleShipyardCost(Task.Cost, Task.minerialsCost, (CurrentProgress * -1.0f));
                    Task.Progress = Task.Progress + (decimal)CurrentProgress;
                }
                else
                {
                    String Entry = String.Format("Not enough minerals to finish task {0} at Shipyard {1} on Population {2}", Task.CurrentTask, CurrentPopulation.ShipyardTasks[Task], CurrentPopulation);
                    MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Position.System, CurrentPopulation, GameState.Instance.GameDateTime,
                                                       GameState.Instance.LastTimestep, Entry);
                    GameState.Instance.Factions[0].MessageLog.Add(NMsg);
                }


                /// <summary>
                /// handle standard task completion here.
                /// </summary>
                if (Task.Progress >= 1.0m)
                {
                    TasksToRemove.Add(Task);
                    switch (Task.CurrentTask)
                    {
                        case Constants.ShipyardInfo.Task.Construction:
                            Task.AssignedTaskGroup.AddShip(Task.ConstructRefitTarget, Task.Title);
                            CurrentPopulation.FuelStockpile = Task.AssignedTaskGroup.Ships[Task.AssignedTaskGroup.Ships.Count - 1].Refuel(CurrentPopulation.FuelStockpile);
                            break;
                        case Constants.ShipyardInfo.Task.Repair:
                            /// <summary>
                            /// Set the Armor to fully repaired, set all components as not destroyed, and reduce the maintenance clock by a certain amount.
                            /// </summary>
#warning maintenance clock work should be performed here and in refit as well.
                            Task.CurrentShip.ShipArmor.RepairAllArmor();
                            foreach (ComponentTN CurComp in Task.CurrentShip.ShipComponents)
                            {
                                CurComp.isDestroyed = false;
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Refit:
                            /// <summary>
                            /// need to remove the old ship, put in the new ship, copy over important information, adjust refueling,MSP,etc?
                            /// </summary>

                            /// <summary>
                            /// Credit the population with the fuel and ordnance on the ship. the old ships MSP will be considered where the new ship got its MSP from.
                            /// </summary>
                            CurrentPopulation.FuelStockpile = CurrentPopulation.FuelStockpile + Task.CurrentShip.CurrentFuel;
                            foreach (KeyValuePair<OrdnanceDefTN, int> OrdnancePair in Task.CurrentShip.ShipOrdnance)
                            {
                                CurrentPopulation.LoadMissileToStockpile(OrdnancePair.Key, (float)OrdnancePair.Value);
                            }
                            /// <summary>
                            /// Destroy the ship. just use the existing code to remove the ship from the simulation, no point in reduplicating all of it.
                            /// </summary>
                            Task.CurrentShip.IsDestroyed = true;
                            if (CurrentFaction.RechargeList.ContainsKey(Task.CurrentShip) == true)
                            {
                                if ((CurrentFaction.RechargeList[Task.CurrentShip] & (int)Faction.RechargeStatus.Destroyed) != (int)Faction.RechargeStatus.Destroyed)
                                {
                                    CurrentFaction.RechargeList[Task.CurrentShip] = CurrentFaction.RechargeList[Task.CurrentShip] + (int)Faction.RechargeStatus.Destroyed;
                                }
                            }
                            else
                            {
                                CurrentFaction.RechargeList.Add(Task.CurrentShip, (int)Faction.RechargeStatus.Destroyed);
                            }

                            /// <summary>
                            /// Add in the "new" ship.
                            /// </summary>
                            Task.AssignedTaskGroup.AddShip(Task.ConstructRefitTarget,Task.CurrentShip.Name);
                            Task.AssignedTaskGroup.Ships[Task.AssignedTaskGroup.Ships.Count - 1].TFTraining = Task.CurrentShip.TFTraining;
                            Task.AssignedTaskGroup.Ships[Task.AssignedTaskGroup.Ships.Count - 1].ShipGrade = Task.CurrentShip.ShipGrade;
                            CurrentPopulation.FuelStockpile = Task.AssignedTaskGroup.Ships[Task.AssignedTaskGroup.Ships.Count - 1].Refuel(CurrentPopulation.FuelStockpile);
                            break;
                        case Constants.ShipyardInfo.Task.Scrap:
                            /// <summary>
                            /// All non-destroyed components from the ship need to be put into the population stockpile.
                            /// This further includes fuel, MSP, and ordnance as well as eventually officers and crew.
                            /// </summary>
#warning Handle officers and crew on ship scrapping.
                            BindingList<ComponentDefTN> CompDefList = Task.CurrentShip.ShipClass.ListOfComponentDefs;
                            BindingList<short> CompDefCount = Task.CurrentShip.ShipClass.ListOfComponentDefsCount;
                            BindingList<ComponentTN> ShipCompList = Task.CurrentShip.ShipComponents;
                            BindingList<ushort> ComponentDefIndex = Task.CurrentShip.ComponentDefIndex;
                            int DefCount = Task.CurrentShip.ShipClass.ListOfComponentDefs.Count;
                            for (int CompDefIndex = 0; CompDefIndex < DefCount; CompDefIndex++)
                            {
                                ComponentDefTN CurrentCompDef = CompDefList[CompDefIndex];
                                short CurrentCompCount = CompDefCount[CompDefIndex];

                                int destCount = 0;
                                for (int CompIndex = 0; CompIndex < CurrentCompCount; CompIndex++)
                                {
                                    if (ShipCompList[ComponentDefIndex[CompDefIndex] + CompIndex].isDestroyed == true)
                                    {
                                        destCount++;
                                    }
                                }

                                if (destCount != CurrentCompCount)
                                {
                                    CurrentPopulation.AddComponentsToStockpile(CurrentCompDef, (float)(CurrentCompCount - destCount));
                                }
                            }

                            CurrentPopulation.FuelStockpile = CurrentPopulation.FuelStockpile + Task.CurrentShip.CurrentFuel;
                            CurrentPopulation.MaintenanceSupplies = CurrentPopulation.MaintenanceSupplies + Task.CurrentShip.CurrentMSP;
                            foreach (KeyValuePair<OrdnanceDefTN, int> OrdnancePair in Task.CurrentShip.ShipOrdnance)
                            {
                                CurrentPopulation.LoadMissileToStockpile(OrdnancePair.Key, (float)OrdnancePair.Value);
                            }

                            /// <summary>
                            /// Finally destroy the ship. just use the existing code to remove the ship from the simulation, no point in reduplicating all of it.
                            /// </summary>
                            Task.CurrentShip.IsDestroyed = true;
                            if (CurrentFaction.RechargeList.ContainsKey(Task.CurrentShip) == true)
                            {
                                if ((CurrentFaction.RechargeList[Task.CurrentShip] & (int)Faction.RechargeStatus.Destroyed) != (int)Faction.RechargeStatus.Destroyed)
                                {
                                    CurrentFaction.RechargeList[Task.CurrentShip] = CurrentFaction.RechargeList[Task.CurrentShip] + (int)Faction.RechargeStatus.Destroyed;
                                }
                            }
                            else
                            {
                                CurrentFaction.RechargeList.Add(Task.CurrentShip, (int)Faction.RechargeStatus.Destroyed);
                            }
                            break;
                    }
                }
                else
                {
                    /// <summary>
                    /// Update the timer since this project won't finish just yet.
                    /// </summary>
                    decimal CostLeft = Task.Cost * (1.0m - Task.Progress);
                    float YearsOfProduction = (float)CostLeft / Task.ABR;
                    DateTime EstTime = GameState.Instance.GameDateTime;
                    if (YearsOfProduction < Constants.Colony.TimerYearMax)
                    {
                        float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;
                        int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                        TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                        EstTime = EstTime.Add(TS);
                    }
                    Task.CompletionDate = EstTime;
                }
            }

            /// <summary>
            /// Remove all the tasks that are now completed.
            /// </summary>
            foreach (Installation.ShipyardInformation.ShipyardTask Task in TasksToRemove)
            {
                /// <summary>
                /// Sanity check here.
                /// </summary>
                if (Task.Progress >= 1.0m)
                {
                    Installation.ShipyardInformation SYI = CurrentPopulation.ShipyardTasks[Task];
                    SYI.BuildingShips.Remove(Task);
                    CurrentPopulation.ShipyardTasks.Remove(Task);
                }
            }
            TasksToRemove.Clear();
        }

        /// <summary>
        /// Shipyard modifications are done here.
        /// </summary>
        /// <param name="CurrentFaction">Current faction this shipyard belongs to.</param>
        /// <param name="CurrentPopulation">Current population this shipyard is on.</param>
        /// <param name="SYInfo">The shipyard itself.</param>
        private static void PerformShipyardActivity(Faction CurrentFaction, Population CurrentPopulation, Installation.ShipyardInformation SYInfo)
        {
            if (SYInfo.CurrentActivity.Activity != Constants.ShipyardInfo.ShipyardActivity.NoActivity && SYInfo.CurrentActivity.Paused == false)
            {
                float SYBP = SYInfo.CalcAnnualSYProduction() * Constants.Colony.ConstructionCycleFraction;

                int Adjustment = 1;
                if (SYInfo.ShipyardType == Constants.ShipyardInfo.SYType.Naval)
                {
                    Adjustment = Constants.ShipyardInfo.NavalToCommercialRatio;
                }

                /// <summary>
                /// Don't bother with completion date, or progress, just add the capacity.
                /// </summary>
                if (SYInfo.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.CapExpansion || SYInfo.CurrentActivity.Activity ==
                     Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                {
                    /// <summary>
                    /// How many tons could this shipyard expand capacity by in this time increment? SYBP is the number of BP produced this cycle. BaseTotalCostOfExpansion is the cost for 500 tons.
                    /// Adjustment is the factor that accounts for whether or not this is a commercial yard or a naval yard.
                    /// </summary>
                    float BaseCostIncrement = SYBP / (float)(Constants.ShipyardInfo.BaseTotalCostOfExpansion * Adjustment);
                    float TonsPerCycle = BaseCostIncrement * (float)Constants.ShipyardInfo.TonnageDenominator;

                    /// <summary>
                    /// Don't build more than this many tons if the activity is CapX.
                    /// </summary>
                    if (SYInfo.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                    {
                        if (SYInfo.Tonnage + (int)Math.Floor(TonsPerCycle) > SYInfo.CurrentActivity.CapExpansionLimit)
                        {
                            TonsPerCycle = SYInfo.CurrentActivity.CapExpansionLimit - SYInfo.Tonnage;
                        }

                        SYInfo.CurrentActivity.Progress = 1.0m - (((decimal)SYInfo.CurrentActivity.CapExpansionLimit - (decimal)(SYInfo.Tonnage + TonsPerCycle)) / (decimal)SYInfo.CurrentActivity.CapExpansionLimit);
                    }

                    decimal Cost = (Constants.ShipyardInfo.BaseTotalCostOfExpansion*Adjustment) * ((decimal)TonsPerCycle / (decimal)Constants.ShipyardInfo.TonnageDenominator) * SYInfo.Slipways * Adjustment;
                    decimal[] mCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];
                    mCost[(int)Constants.Minerals.MinerialNames.Duranium] = Cost / 2.0m;
                    mCost[(int)Constants.Minerals.MinerialNames.Neutronium] = Cost / 2.0m;

                    /// <summary>
                    /// Can I build this tick's worth of production?
                    /// </summary>
                    bool CanBuild = CurrentPopulation.MineralRequirement(mCost, 1.0f);

                    if (CanBuild == true)
                    {
                        CurrentPopulation.HandleShipyardCost(Cost, mCost, 1.0f);
                        SYInfo.AddTonnage(CurrentFaction, (int)Math.Floor(TonsPerCycle));
                    }
                    else
                    {
                        String Entry = String.Format("Not enough minerals to finish task {0} at Shipyard {1} on Population {2}", SYInfo.CurrentActivity.Activity, SYInfo, CurrentPopulation);
                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Position.System, CurrentPopulation, GameState.Instance.GameDateTime,
                                                           GameState.Instance.LastTimestep, Entry);
                        GameState.Instance.Factions[0].MessageLog.Add(NMsg);
                    }
                }
                else
                {
                    /// <summary>
                    /// BP produced this construction cycle / the total cost of the activity.
                    /// </summary>
                    float CurrentProgress = SYBP / (float)SYInfo.CurrentActivity.CostOfActivity;

                    if ((SYInfo.CurrentActivity.Progress + (decimal)CurrentProgress) > 1.0m)
                    {
                        CurrentProgress = (float)(1.0m - SYInfo.CurrentActivity.Progress);
                    }

                    bool CanBuild = CurrentPopulation.MineralRequirement(SYInfo.CurrentActivity.minerialsCost, CurrentProgress);

                    if (CanBuild == true)
                    {
                        CurrentPopulation.HandleShipyardCost(SYInfo.CurrentActivity.CostOfActivity, SYInfo.CurrentActivity.minerialsCost, CurrentProgress);
                        SYInfo.CurrentActivity.Progress = SYInfo.CurrentActivity.Progress + (decimal)CurrentProgress;
                    }
                    else
                    {
                        String Entry = String.Format("Not enough minerals to finish task {0} at Shipyard {1} on Population {2}", SYInfo.CurrentActivity.Activity, SYInfo, CurrentPopulation);
                        MessageEntry NMsg = new MessageEntry(MessageEntry.MessageType.ColonyLacksMinerals, CurrentPopulation.Position.System, CurrentPopulation, GameState.Instance.GameDateTime,
                                                           GameState.Instance.LastTimestep, Entry);
                        GameState.Instance.Factions[0].MessageLog.Add(NMsg);
                    }

                    /// <summary>
                    /// handle standard task completion here.
                    /// </summary>
                    if (SYInfo.CurrentActivity.Progress >= 1.0m)
                    {
                        switch (SYInfo.CurrentActivity.Activity)
                        {
                            case Constants.ShipyardInfo.ShipyardActivity.AddSlipway:
                                SYInfo.Slipways = SYInfo.Slipways + 1;
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Add500Tons:
                                SYInfo.AddTonnage(CurrentFaction, Constants.ShipyardInfo.TonnageDenominator);
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Add1000Tons:
                                SYInfo.AddTonnage(CurrentFaction, (Constants.ShipyardInfo.TonnageDenominator * 2));
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Add2000Tons:
                                SYInfo.AddTonnage(CurrentFaction, (Constants.ShipyardInfo.TonnageDenominator * 4));
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Add5000Tons:
                                SYInfo.AddTonnage(CurrentFaction, (Constants.ShipyardInfo.TonnageDenominator * 10));
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Add10000Tons:
                                SYInfo.AddTonnage(CurrentFaction, (Constants.ShipyardInfo.TonnageDenominator * 20));
                                break;
                            case Constants.ShipyardInfo.ShipyardActivity.Retool:
                                SYInfo.AssignedClass = SYInfo.CurrentActivity.TargetOfRetool;
                                if (SYInfo.AssignedClass.IsLocked == false)
                                    SYInfo.AssignedClass.IsLocked = true;
                                break;
                        }
                    }
                    else
                    {
                        /// <summary>
                        /// Update the timer since this project won't finish just yet.
                        /// </summary>
                        decimal CostLeft = SYInfo.CurrentActivity.CostOfActivity * (1.0m - SYInfo.CurrentActivity.Progress);
                        float YearsOfProduction = (float)CostLeft / SYInfo.CalcAnnualSYProduction();
                        
                        DateTime EstTime = GameState.Instance.GameDateTime;
                        if (YearsOfProduction < Constants.Colony.TimerYearMax)
                        {
                            float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;
                            int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                            TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                            EstTime = EstTime.Add(TS);
                        }
                        SYInfo.CurrentActivity.CompletionDate = EstTime;
                    }
                }

                /// <summary>
                /// Lastly clean up any completed activities.
                /// </summary>
                if (SYInfo.CurrentActivity.Activity == Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX)
                {
                    if (SYInfo.Tonnage >= SYInfo.CurrentActivity.CapExpansionLimit)
                    {
                        /// <summary>
                        /// This activity has completed so end it.
                        /// </summary>
                        SYInfo.CurrentActivity = new Installation.ShipyardInformation.ShipyardActivity();
                    }
                }
                else if (SYInfo.CurrentActivity.Activity != Constants.ShipyardInfo.ShipyardActivity.CapExpansion &&
                        SYInfo.CurrentActivity.Activity != Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                {
                    if (SYInfo.CurrentActivity.Progress >= 1.0m)
                    {
                        /// <summary>
                        /// This activity has completed so end it.
                        /// </summary>
                        SYInfo.CurrentActivity = new Installation.ShipyardInformation.ShipyardActivity();
                    }
                }
            }
        }
        #endregion
    }
}

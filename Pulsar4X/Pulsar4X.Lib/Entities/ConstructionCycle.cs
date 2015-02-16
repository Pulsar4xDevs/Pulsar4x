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
        /// <summary>
        /// Processes construction factory work for every faction.
        /// </summary>
        /// <param name="P">List of factions.</param>
        public static void ConstructionFactoryBuild(BindingList<Faction> P)
        {
            /// <summary>
            /// Subtract the construction cycle from the construction tick.
            /// </summary>

            foreach (Faction CurrentFaction in P)
            {
                foreach (Population CurrentPopulation in CurrentFaction.Populations)
                {
                    /// <summary>
                    /// How much construction work per day does this colony do? default should be 5 day construction cycle.
                    /// </summary>
                    float TimeAdjust = (float)Constants.Colony.ConstructionCycle / (float)Constants.TimeInSeconds.Year;
                    float CurrentIndustry = CurrentPopulation.CalcTotalIndustry() * TimeAdjust;
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
            }
        }

        /// <summary>
        /// Handles ordnance factory construction.
        /// </summary>
        /// <param name="P">List of factions.</param>
        public static void OrdnanceFactoryBuild(BindingList<Faction> P)
        {
            /// <summary>
            /// Subtract the construction cycle from the construction tick.
            /// </summary>

            foreach (Faction CurrentFaction in P)
            {
                foreach (Population CurrentPopulation in CurrentFaction.Populations)
                {
                    /// <summary>
                    /// How much construction work per day does this colony do? default should be 5 day construction cycle.
                    /// </summary>
                    float TimeAdjust = (float)Constants.Colony.ConstructionCycle / (float)Constants.TimeInSeconds.Year;
                    float CurrentIndustry = CurrentPopulation.CalcTotalOrdnanceIndustry() * TimeAdjust;
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
            }
        }

        /// <summary>
        /// Perform mining on all faction owned colonies.
        /// </summary>
        /// <param name="P">Binding list of factions</param>
        public static void MinePlanets(BindingList<Faction> P)
        {
            foreach (Faction CurrentFaction in P)
            {
                foreach (Population CurrentPopulation in CurrentFaction.Populations)
                {
                    // <? todo: Impliment Mining.
                }
            }
        }

        /// <summary>
        /// Do all fuel refining on planets and later on gas giant harvesters.
        /// </summary>
        /// <param name="P">list of factions.</param>
        public static void RefineFuel(BindingList<Faction> P)
        {
            foreach (Faction CurrentFaction in P)
            {
#warning Implement gas giant harvesters here.
                foreach (Population CurrentPopulation in CurrentFaction.Populations)
                {
                    /// <summary>
                    /// Skip this population.
                    /// </summary>
                    if (CurrentPopulation.IsRefining == false)
                        continue;

                    float TimeAdjust = (float)Constants.Colony.ConstructionCycle / (float)Constants.TimeInSeconds.Year;
                    float CurrentRefining = CurrentPopulation.CalcTotalRefining() * TimeAdjust;

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
            }
        }

    }
}

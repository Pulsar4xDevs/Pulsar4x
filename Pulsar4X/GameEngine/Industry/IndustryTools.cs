using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.DataStructures;
using Pulsar4X.Factions;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry
{
    public static class IndustryTools
    {
        public static void AddJob(Entity industryEntity, string plineID, IndustryJob job)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            AddJob(industryDB, plineID, job);
        }

        public static void AddJob(IndustryAbilityDB industryDB, string plineID, IndustryJob job)
        {
            lock(industryDB.ProductionLines[plineID])
            {
                var pline = industryDB.ProductionLines[plineID];
                pline.Jobs.Add(job);
            }
            // A new job can become a line's head job: its draw counts from now, not tomorrow.
            Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryDB.OwningEntity);
        }

        public static void ChangeJobPriority(Entity industryEntity, string prodLine, string jobID, int delta)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                var currentIndex = jobList.IndexOf(job);
                var newIndex = currentIndex + delta;
                if (newIndex <= 0)
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Insert(0, job);
                }
                else if (newIndex >= jobList.Count - 1)
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Add(job);
                }
                else
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Insert(newIndex, job);
                }
                Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryEntity);
            }
        }

        public static void EditExsistingJob(Entity industryEntity, string prodLine, string jobID, bool RepeatJob = false, ushort NumberOrderd = 1, bool autoInstall = false)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                job.Auto = RepeatJob;
                job.NumberOrdered = NumberOrderd;
                Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryEntity);

                /*if (job is ConstructJob)
                {
                    var cj = (ConstructJob)job;
                    cj.InstallOn = industryEntity;
                }*/

            }
        }

        public static void CancelExsistingJob(Entity industryEntity, string prodLine, string jobID)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                jobList.Remove(job);
                Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryEntity);
            }
        }

        internal static void ConstructStuff(Entity industryEntity)
        {
            if(!industryEntity.TryGetDataBlob<CargoStorageDB>(out var stockpile))
            {
                throw new Exception("Tried to ConstructStuff on an entity with no CargoStorageDB");
            }

            if(!industryEntity.Manager.Game.Factions.ContainsKey(industryEntity.FactionOwnerID))
            {
                throw new Exception("Unable to find the faction entity");
            }
            var faction = industryEntity.Manager.Game.Factions[industryEntity.FactionOwnerID];

            if(!faction.TryGetDataBlob<FactionInfoDB>(out var factionInfo))
            {
                throw new Exception("Unable to find FactionInfoDB");
            }

            if(!industryEntity.TryGetDataBlob<IndustryAbilityDB>(out var industryDB))
            {
                throw new Exception("Unable to find IndustryAbilityDB");
            }

            // Infrastructure is the limiting factor on a colony's output: when the colony's
            // buildings exceed its infrastructure capacity, every production rate is scaled down.
            double infraEfficiency = InfrastructureProcessor.GetEfficiency(industryEntity);
            // Power: industry runs at its tier's share of demand (1 with no grid or no shortage).
            double powerEfficiency = Pulsar4X.Energy.ColonyPowerProcessor.Satisfaction(industryEntity, Pulsar4X.Energy.PowerTier.Industry);
            // What the lines draw now; if a job finishes or stalls today, demand is refreshed at the end.
            string drawBefore = Pulsar4X.Energy.ColonyPowerProcessor.ProductionDrawKey(industryDB);
            int queuedBefore = industryEntity.TryGetDataBlob<LocalConstructionDB>(out var siteQueue) ? siteQueue.BuildQueue.Count : 0;

            foreach (var (prodLineID, prodLine) in industryDB.ProductionLines.ToArray())
            {
                var industryPointsRemaining = new Dictionary<string, int>();
                foreach (var rate in prodLine.IndustryTypeRates)
                    industryPointsRemaining[rate.Key] = (int)(rate.Value * infraEfficiency * powerEfficiency);

                // Build energy: each point spends its job's kWh per point. A line draws at most its
                // facility's nameplate for the day (scaled by the power share), so a recipe hungrier
                // than the nameplate runs slower instead of drawing more.
                double energyBudget = LineEnergyBudget_kWh(industryEntity, prodLineID, powerEfficiency);
                // Industrial Energy Efficiency scales every point's kWh (1 at level 0).
                double energyFactor = Pulsar4X.Energy.EnergyTech.IndustrialEnergyFactor(factionInfo.Data);
                double energyUsed = 0;

                foreach(var batchJob in prodLine.Jobs.ToArray())
                {
                    IConstructableDesign designInfo = factionInfo.IndustryDesigns[batchJob.ItemGuid];
                    float industryPointsToUse = industryPointsRemaining[designInfo.IndustryTypeID];// * productionPercentage;

                    if(batchJob.Status != IndustryJobStatus.Completed)
                    {
                        batchJob.Status = IndustryJobStatus.Queued;
                    }

                    if(industryPointsToUse < 1) continue;

                    //total number of resources requred for a single job in this batch
                    var resourceSum = batchJob.ResourcesCosts.Sum(item => item.Value);
                    //how many construction points each resourcepoint is worth.
                    if (resourceSum == 0)
                        throw new Exception("resources can't cost 0");

                    float pointPerResource = (float)designInfo.IndustryPointCosts / (float)resourceSum;
                    float startingPointsLeft = batchJob.ProductionPointsLeft;
                    float startingPointsToUse = industryPointsToUse;

                    while (
                        batchJob.NumberCompleted < batchJob.NumberOrdered &&
                        industryPointsToUse >= 1)
                    {
                        //gather availible resorces for this job.
                        //right now we take all the resources we can, for an individual item in the batch.
                        //even if we're taking more than we can use in this turn, we're using/storing it.
                        IDictionary<string, long> resourceCosts = batchJob.ResourcesRequiredRemaining;

                        var totalResourceReq = resourceCosts.Sum(item => item.Value);

                        //Note: this is editing batchjob.ResourcesRequired variable (as ref resourceCosts).
                        ConsumeResources(stockpile, ref resourceCosts);
                        //we calculate the difference between the design resources and the amount of resources we've squirreled away.

                        // this is the total of the resources that we don't have access to for this item.
                        var totalResourceStillReq = resourceCosts.Sum(item => item.Value);

                        // this is the total resources that can be used on this item.
                        var totalResourcesUsed = totalResourceReq - totalResourceStillReq;
                        // the industry Points equivelent of total used resources.
                        var totalIPEquvelent = totalResourcesUsed * pointPerResource;

                        int pointsToUse = 0;
                        industryPointsToUse = Math.Min(industryPointsRemaining[designInfo.IndustryTypeID], batchJob.ProductionPointsLeft);
                        if (totalResourceStillReq == 0)
                        {
                            pointsToUse = Math.Max((int)industryPointsToUse, 1);
                        }
                        else
                        {
                            industryPointsToUse = Math.Min(industryPointsToUse, totalIPEquvelent);
                            pointsToUse = (int)Math.Floor(industryPointsToUse);
                        }

                        double energyPerPoint = batchJob.EnergyPerPoint * energyFactor;
                        if (energyPerPoint > 0 && pointsToUse > 0)
                        {
                            double affordable = Math.Floor((energyBudget - energyUsed) / energyPerPoint + 1e-9);
                            if (affordable < pointsToUse)
                                pointsToUse = (int)Math.Max(0, affordable);
                            if (pointsToUse <= 0)
                                break; // the line's energy for today is spent
                        }
                        energyUsed += pointsToUse * energyPerPoint;
                        batchJob.EnergyUsed += pointsToUse * energyPerPoint;

                        //construct only enough for the amount of resources we have.
                        batchJob.ProductionPointsLeft -= pointsToUse;
                        industryPointsRemaining[designInfo.IndustryTypeID] -= pointsToUse;

                        if(startingPointsLeft == batchJob.ProductionPointsLeft
                            && batchJob.ProductionPointsCost > startingPointsToUse)
                        {
                            // Didn't make any progress mark as missing resources
                            batchJob.Status = IndustryJobStatus.MissingResources;
                        }
                        else if(pointsToUse >= 1 || totalResourcesUsed > 0)
                        {
                            batchJob.Status = IndustryJobStatus.Processing;
                        }

                        if (batchJob.ProductionPointsLeft == 0 && totalResourceStillReq == 0)
                        {
                            batchJob.Status = IndustryJobStatus.Completed;
                            designInfo.OnConstructionComplete(industryEntity, stockpile, prodLineID, batchJob, designInfo);
                        }
                    }
                }
                prodLine.EnergyUsedLastDay = energyUsed;
            }

            // A finished or stalled job changes its line's draw, and a finished site-only facility
            // queues its assembly (which may carry scrap energy): refresh demand now, once.
            int queuedAfter = siteQueue != null ? siteQueue.BuildQueue.Count : 0;
            if (queuedAfter != queuedBefore || Pulsar4X.Energy.ColonyPowerProcessor.ProductionDrawKey(industryDB) != drawBefore)
                Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryEntity);
        }

        /// <summary>
        /// One line's nameplate draw, kW: its facility's <see cref="Pulsar4X.Energy.PowerConsumerAtb"/>
        /// x health. Null when the facility has no draw (it builds without an energy cap).
        /// </summary>
        internal static double? LineNameplate_kW(Entity industryEntity, string lineId)
        {
            if (!industryEntity.TryGetDataBlob<ComponentInstancesDB>(out var instances)
                || !instances.AllComponents.TryGetValue(lineId, out var facility)
                || facility?.Design == null
                || !facility.Design.TryGetAttribute<Pulsar4X.Energy.PowerConsumerAtb>(out var consumer))
                return null;
            return consumer.Draw_kW * Math.Clamp((double)facility.HealthPercent, 0, 1);
        }

        /// <summary>Energy a line may spend in one daily pass, kWh. Unlimited for a line with no nameplate.</summary>
        internal static double LineEnergyBudget_kWh(Entity industryEntity, string lineId, double powerShare)
        {
            var nameplate = LineNameplate_kW(industryEntity, lineId);
            if (nameplate == null)
                return double.PositiveInfinity;
            return nameplate.Value * 24 * Math.Clamp(powerShare, 0, 1);
        }

        internal static void ConsumeResources(CargoStorageDB fromCargo, ref IDictionary<string, long> toUse)
        {
            foreach (var kvp in toUse.ToArray())
            {
                ICargoable? cargoItem = fromCargo.OwningEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(kvp.Key);//fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(kvp.Key);
                if (cargoItem is null)
                {
                    if (fromCargo.OwningEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().InternalComponentDesigns.TryGetValue(kvp.Key, out var design))
                    {
                        if (design != null)
                            cargoItem = (ICargoable)design;
                    }
                    else
                    {
                        throw new Exception("Cant build from non ICargoable Items");
                    }
                }
                string cargoTypeID = cargoItem.CargoTypeID;
                long amountUsedThisTick = 0;
                if (fromCargo.TypeStores.ContainsKey(cargoTypeID))
                {
                    if (fromCargo.TypeStores[cargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                    {
                        amountUsedThisTick = Math.Min(fromCargo.TypeStores[cargoTypeID].CurrentStoreInUnits[cargoItem.ID], kvp.Value);
                    }
                }

                if (amountUsedThisTick > 0)
                {
                    long used = fromCargo.RemoveCargoByUnit(cargoItem, amountUsedThisTick);
                    toUse[kvp.Key] -= used;
                }
            }
        }

        public static void AutoAddSubJobs(Entity industryEntity, IndustryJob job)
        {
            if(!industryEntity.TryGetDataBlob<CargoStorageDB>(out var stockpile))
            {
                throw new Exception("Tried to ConstructStuff on an entity with no CargoStorageDB");
            }
            if(!industryEntity.TryGetDataBlob<IndustryAbilityDB>(out var industryDB))
            {
                throw new Exception("Unable to find IndustryAbilityDB");
            }

            var resReq = job.ResourcesRequiredRemaining;
            foreach (var kvp in resReq)
            {
                ICargoable? cargoItem = industryEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods.GetAny(kvp.Key);
                if (cargoItem is null)
                {
                    if (industryEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().IndustryDesigns.TryGetValue(kvp.Key, out var design)
                        && design != null)
                    {
                        cargoItem = (ICargoable)design;
                    }
                    else
                    {
                        continue;
                    }
                }
                var numStored = stockpile.GetUnitsStored(cargoItem, false);
                var numReq = kvp.Value - numStored;
                if (numReq > 0)
                {
                    if (cargoItem is IConstructableDesign)
                    {
                        IConstructableDesign des = (IConstructableDesign)cargoItem;
                        IndustryJob newjob = new IndustryJob(des);
                        newjob.InitialiseJob((ushort)numReq, false);
                        SetJobToFastest(industryDB, newjob);
                        AutoAddSubJobs(industryEntity, newjob); //recursivly add jobs.
                    }
                }
            }


        }
        internal static void SetJobToFastest(IndustryAbilityDB industrydb, IndustryJob job)
        {
            var typID = job.TypeID;
            (string lineID, int rate) bestLine = (String.Empty, 0);
            var plines = industrydb.ProductionLines;
            foreach (var line in plines)
            {
                if (!line.Value.IndustryTypeRates.TryGetValue(typID, out int rate))
                    rate = -1;
                if (rate > bestLine.rate)
                    bestLine = (line.Key, rate);
            }
            if(bestLine.lineID != String.Empty)
                AddJob(industrydb, bestLine.lineID, job);
        }
    }
}
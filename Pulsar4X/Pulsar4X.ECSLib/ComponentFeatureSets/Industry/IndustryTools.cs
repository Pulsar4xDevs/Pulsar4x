using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib.Industry
{
    public static class IndustryTools
    {
        public static void AddJob(Entity industryEntity, Guid plineID, IndustryJob job)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            AddJob(industryDB, plineID, job);
        }

        public static void AddJob(IndustryAbilityDB industryDB, Guid plineID, IndustryJob job)
        {
            lock(industryDB.ProductionLines[plineID])
            {
                var pline = industryDB.ProductionLines[plineID];
                pline.Jobs.Add(job);
            }
        }

        public static void ChangeJobPriority(Entity industryEntity, Guid prodLine, Guid jobID, int delta)
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
            }
        }

        public static void EditExsistingJob(Entity industryEntity, Guid prodLine, Guid jobID, bool RepeatJob = false, ushort NumberOrderd = 1, bool autoInstall = false)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                job.Auto = RepeatJob;
                job.NumberOrdered = NumberOrderd;
                /*if (job is ConstructJob)
                {
                    var cj = (ConstructJob)job;
                    cj.InstallOn = industryEntity;
                }*/

            }
        }

        public static void CancelExsistingJob(Entity industryEntity, Guid prodLine, Guid jobID)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                jobList.Remove(job);
            }
        }

        internal static void ConstructStuff(Entity industryEntity)
        {
            if(!industryEntity.TryGetDatablob<VolumeStorageDB>(out var stockpile))
            {
                throw new Exception("Tried to ConstructStuff on an entity with no VolumeStorageDB");
            }

            if(!industryEntity.Manager.FindEntityByGuid(industryEntity.FactionOwnerID, out Entity faction))
            {
                throw new Exception("Unable to find the faction entity");
            }

            if(!faction.TryGetDatablob<FactionInfoDB>(out var factionInfo))
            {
                throw new Exception("Unable to find FactionInfoDB");
            }

            if(!industryEntity.TryGetDatablob<IndustryAbilityDB>(out var industryDB))
            {
                throw new Exception("Unable to find IndustryAbilityDB");
            }

            foreach (var (prodLineID, prodLine) in industryDB.ProductionLines.ToArray())
            {
                var industryPointsRemaining = new Dictionary<Guid, int>(prodLine.IndustryTypeRates);

                foreach(var batchJob in prodLine.Jobs.ToArray())
                {
                    IConstrucableDesign designInfo = factionInfo.IndustryDesigns[batchJob.ItemGuid];
                    float pointsToUse = industryPointsRemaining[designInfo.IndustryTypeID];// * productionPercentage;

                    //total number of resources requred for a single job in this batch
                    var resourceSum = batchJob.ResourcesCosts.Sum(item => item.Value);
                    //how many construction points each resourcepoint is worth.
                    if (resourceSum == 0)
                        throw new Exception("resources can't cost 0");
                    float pointPerResource = (float)designInfo.IndustryPointCosts / resourceSum;
                    float startingPointsLeft = batchJob.ProductionPointsLeft;
                    float startingPointsToUse = pointsToUse;

                    if(batchJob.Status != IndustryJobStatus.Completed)
                    {
                        batchJob.Status = IndustryJobStatus.Queued;
                    }

                    while (
                        batchJob.NumberCompleted < batchJob.NumberOrdered &&
                        pointsToUse > 0)
                    {
                        batchJob.Status = IndustryJobStatus.Processing;
                        //gather availible resorces for this job.
                        //right now we take all the resources we can, for an individual item in the batch.
                        //even if we're taking more than we can use in this turn, we're using/storing it.
                        IDictionary<Guid, long> resourceCosts = batchJob.ResourcesRequiredRemaining;
                        //Note: this is editing batchjob.ResourcesRequired variable.
                        ConsumeResources(stockpile, ref resourceCosts);
                        //we calculate the difference between the design resources and the amount of resources we've squirreled away.

                        // this is the total of the resources that we don't have access to for this item.
                        var unusableResourceSum = resourceCosts.Sum(item => item.Value);
                        // this is the total resources that can be used on this item.
                        var useableResourcePoints = resourceSum - unusableResourceSum;

                        pointsToUse = Math.Min(industryPointsRemaining[designInfo.IndustryTypeID], batchJob.ProductionPointsLeft);
                        pointsToUse = Math.Min(pointsToUse, useableResourcePoints * pointPerResource);

                        if(pointsToUse < 0)
                            throw new Exception("Can't have negative production");

                        //construct only enough for the amount of resources we have.
                        batchJob.ProductionPointsLeft -= (int)Math.Floor(pointsToUse);
                        industryPointsRemaining[designInfo.IndustryTypeID] -= (int)Math.Floor(pointsToUse);

                        if(startingPointsLeft == batchJob.ProductionPointsLeft
                            && batchJob.ProductionPointsCost > startingPointsToUse)
                        {
                            // Didn't make any progress mark as missing resources
                            batchJob.Status = IndustryJobStatus.MissingResources;
                        }

                        if (batchJob.ProductionPointsLeft == 0 && unusableResourceSum == 0)
                        {
                            batchJob.Status = IndustryJobStatus.Completed;
                            designInfo.OnConstructionComplete(industryEntity, stockpile, prodLineID, batchJob, designInfo);
                        }
                    }
                }
            }
        }

        internal static void ConsumeResources(VolumeStorageDB fromCargo, ref IDictionary<Guid, long> toUse)
        {
            foreach (KeyValuePair<Guid, long> kvp in toUse.ToArray())
            {
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);//fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(kvp.Key);
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
                Guid cargoTypeID = cargoItem.CargoTypeID;
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
            if(!industryEntity.TryGetDatablob<VolumeStorageDB>(out var stockpile))
            {
                throw new Exception("Tried to ConstructStuff on an entity with no VolumeStorageDB");
            }
            if(!industryEntity.TryGetDatablob<IndustryAbilityDB>(out var industryDB))
            {
                throw new Exception("Unable to find IndustryAbilityDB");
            }

            var resReq = job.ResourcesRequiredRemaining;
            foreach (var kvp in resReq)
            {
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);
                if (cargoItem is null)
                {
                    if (industryEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().IndustryDesigns.TryGetValue(kvp.Key, out var design))
                    {
                        if (design != null)
                            cargoItem = (ICargoable)design;
                    }
                    else
                    {
                        continue;
                    }
                }
                var numStored = CargoExtensionMethods.GetUnitsStored(stockpile, cargoItem);
                var numReq = kvp.Value - numStored;
                if (numReq > 0)
                {
                    if (cargoItem is IConstrucableDesign)
                    {
                        IConstrucableDesign des = (IConstrucableDesign)cargoItem;
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
            (Guid lineID, int rate) bestLine = (Guid.Empty, 0);
            var plines = industrydb.ProductionLines;
            foreach (var line in plines)
            {
                if (!line.Value.IndustryTypeRates.TryGetValue(typID, out int rate))
                    rate = -1;
                if (rate > bestLine.rate)
                    bestLine = (line.Key, rate);
            }
            if(bestLine.lineID != Guid.Empty)
                AddJob(industrydb, bestLine.lineID, job);
        }
    }
}
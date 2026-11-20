using System;
using GameEngine.Engine.Orders;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.DataStructures;
using Pulsar4X.Factions;
using Pulsar4X.Engine;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry
{
    public static class IndustryTools
    {
        public const string ShipAssemblyIndustryType = "ship-assembly";

        public static bool IsShipyardLine(IndustryAbilityDB.ProductionLine line)
            => line?.IndustryTypeRates != null && line.IndustryTypeRates.ContainsKey(ShipAssemblyIndustryType);

        /// <summary>
        /// Shipyards build only ships. Component, material, and installation jobs belong on a factory.
        /// </summary>
        public static bool LineAccepts(IndustryAbilityDB.ProductionLine line, IConstructableDesign design)
        {
            if (line == null || design == null || line.IndustryTypeRates == null)
                return false;
            if (design is ShipRefitDesign)
                return IsShipyardLine(line);
            if (design is RecycleDesign)
                // A ship is taken apart in a slip, anything else on a factory line that builds its type.
                return line.IndustryTypeRates.ContainsKey(design.IndustryTypeID)
                    && IsShipyardLine(line) == (design.IndustryTypeID == ShipAssemblyIndustryType);
            if (!line.IndustryTypeRates.ContainsKey(design.IndustryTypeID))
                return false;
            if (IsShipyardLine(line) && design is not ShipDesign)
                return false;
            return true;
        }

        public static string RejectionReason(IndustryAbilityDB.ProductionLine line, IConstructableDesign design)
        {
            if (IsShipyardLine(line) && design is not ShipDesign)
                return "Shipyards build only ships.";
            return "This line cannot build that design.";
        }

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

        public static void EditExsistingJob(Entity industryEntity, string prodLine, string jobID, bool RepeatJob = false, ushort NumberOrderd = 1, bool autoInstall = false, long? stockThreshold = null)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                job.Auto = RepeatJob;
                job.NumberOrdered = NumberOrderd;

                // A new quantity at or below what is already built would leave the batch never finishing (the count only
                // ever meets the quantity exactly), so a repeat job would stop building for good. The batch is done
                // instead: a repeat job starts a new batch of the new size (an item part-built carries over); a one-shot
                // job is finished and leaves the line, or finishes the part-built item first.
                if (job.NumberCompleted >= job.NumberOrdered)
                {
                    bool partBuilt = job.ProductionPointsLeft < job.ProductionPointsCost
                        || job.ResourcesRequiredRemaining.Values.Sum() < job.ResourcesCosts.Values.Sum();
                    if (job.Auto)
                    {
                        job.NumberCompleted = 0;
                        job.StockBatchStarted = partBuilt;
                    }
                    else if (partBuilt)
                        job.NumberOrdered = (ushort)(job.NumberCompleted + 1);
                    else
                    {
                        jobList.Remove(job);
                        Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(industryEntity);
                        return;
                    }
                }

                // The edit states the whole job: no level means a plain repeat job. A batch already under way finishes first.
                if (stockThreshold != job.StockThreshold)
                {
                    if (stockThreshold == null)
                    {
                        job.StockThreshold = null;
                        job.StockBatchStarted = false;
                        if (job.Status == IndustryJobStatus.Idle)
                            job.Status = IndustryJobStatus.Queued;
                    }
                    else
                    {
                        job.StockThreshold = stockThreshold;
                        job.StockBatchStarted = job.NumberCompleted > 0
                            || job.ProductionPointsLeft < job.ProductionPointsCost
                            || job.Status == IndustryJobStatus.Processing;
                    }
                }
                if (job.StockThreshold != null && StockDesign(industryEntity, job) is { } design)
                    StockOrderIdles(industryEntity, job, design);
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

            // Crew plan phase 5: no industry points are spent while the player's balance is negative.
            if (Pulsar4X.Factions.FactionSpending.IsBlocked(industryEntity.Manager.Game, industryEntity.FactionOwnerID))
                return;

            // Infrastructure is the limiting factor on a colony's output: when the colony's
            // buildings exceed its infrastructure capacity, every production rate is scaled down.
            double infraEfficiency = InfrastructureProcessor.GetEfficiency(industryEntity);
            // Power: industry runs at its tier's share of demand (1 with no grid or no shortage).
            double powerEfficiency = Pulsar4X.Energy.ColonyPowerProcessor.Satisfaction(industryEntity, Pulsar4X.Energy.PowerTier.Industry)
                // Multi-faction phase 5: and at the Industry staffing tier's share (1 when fully staffed).
                * Pulsar4X.Colonies.Workforce.Share(industryEntity, Pulsar4X.Colonies.StaffTier.Industry)
                // Multi-faction phase 7a: × 0.9 below satisfaction 20, × 1.05 above 80.
                * Pulsar4X.Colonies.ColonyNeeds.OutputFactor(industryEntity);
            // What the lines draw now; if a job finishes or stalls today, demand is refreshed at the end.
            string drawBefore = Pulsar4X.Energy.ColonyPowerProcessor.ProductionDrawKey(industryDB);
            int queuedBefore = industryEntity.TryGetDataBlob<LocalConstructionDB>(out var siteQueue) ? siteQueue.BuildQueue.Count : 0;
            // Ship-refit points are one pool a day for the colony's shipyard lines, spent in line and job order.
            long refitPool = siteQueue == null ? 0
                : (long)Math.Floor(siteQueue.RefitPointsPerDay * infraEfficiency * powerEfficiency
                    * Pulsar4X.Ships.ShipCrew.WorkRate(industryEntity) + 1e-9);

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
                    if (batchJob.Refit != null)
                    {
                        AdvanceRefit(industryEntity, stockpile, prodLine, batchJob, ref refitPool);
                        continue;
                    }
                    if (batchJob.Recycle != null)
                    {
                        Recycling.Advance(industryEntity, stockpile, prodLine, batchJob, industryPointsRemaining);
                        continue;
                    }

                    IConstructableDesign designInfo = factionInfo.IndustryDesigns[batchJob.ItemGuid];
                    float industryPointsToUse = industryPointsRemaining[designInfo.IndustryTypeID];// * productionPercentage;

                    if(batchJob.Status != IndustryJobStatus.Completed)
                    {
                        batchJob.Status = IndustryJobStatus.Queued;
                    }

                    // A stock order idles while the colony's stock of its item is at or above its level; below it, one batch runs.
                    if (batchJob.StockThreshold != null && StockOrderIdles(industryEntity, batchJob, designInfo))
                        continue;

                    if(industryPointsToUse < 1) continue;

                    //total number of resources requred for a single job in this batch
                    var resourceSum = batchJob.ResourcesCosts.Sum(item => item.Value);
                    //how many construction points each resourcepoint is worth.
                    if (resourceSum == 0)
                        throw new Exception("resources can't cost 0");

                    float pointPerResource = (float)designInfo.IndustryPointCosts / (float)resourceSum;

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

                        // No inputs to work with at all: points only come from inputs, so nothing can be done however many
                        // points the line has. (This used to be guessed from "no progress and the cost is more than the
                        // line's points for the day", which left a job that fits in a day's points showing Queued.)
                        if (totalResourceStillReq > 0 && totalResourcesUsed == 0)
                        {
                            batchJob.Status = IndustryJobStatus.MissingResources;
                            break;
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

                        if(pointsToUse >= 1 || totalResourcesUsed > 0)
                        {
                            batchJob.Status = IndustryJobStatus.Processing;
                        }

                        if (batchJob.ProductionPointsLeft == 0 && totalResourceStillReq == 0)
                        {
                            batchJob.Status = IndustryJobStatus.Completed;
                            designInfo.OnConstructionComplete(industryEntity, stockpile, prodLineID, batchJob, designInfo);

                            // A stock order's batch is done (the repeat reset its count): it waits for the stock to drop again,
                            // or starts the next batch (and re-chains its feeders) when stock is already below the level.
                            if (batchJob.StockThreshold != null && batchJob.NumberCompleted == 0)
                            {
                                batchJob.StockBatchStarted = false;
                                StockOrderIdles(industryEntity, batchJob, designInfo);
                                break;
                            }
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
            else
                // Jobs took minerals out of the stores: a mine that was full may resume.
                Pulsar4X.Energy.ColonyPowerProcessor.RefreshIfIdleStateChanged(industryEntity);
        }

        /// <summary>
        /// A refit job spends the yard's ship-refit points like any job spends its type's points.
        /// A job that cannot run (ship out of range, part missing) spends none and says why.
        /// When its points are paid the parts move, the job completes and leaves the line.
        /// </summary>
        static void AdvanceRefit(Entity yard, CargoStorageDB storage, IndustryAbilityDB.ProductionLine line, IndustryJob job, ref long pool)
        {
            var refit = job.Refit!;
            string? blocker = LocalConstructionProcessor.RefitBlocker(yard, storage, refit);
            job.WaitReason = blocker ?? "";
            if (blocker != null)
            {
                job.Status = IndustryJobStatus.MissingResources;
                // With an industry head seated, parts nobody has queued yet are queued (once).
                if (refit.MountDesignIds.Count > 0)
                    AutoQueueRefitParts(yard);
                return;
            }

            job.Status = IndustryJobStatus.Queued;
            long spend = Math.Min(pool, job.ProductionPointsLeft);
            if (spend > 0)
            {
                job.ProductionPointsLeft -= spend;
                pool -= spend;
                job.Status = IndustryJobStatus.Processing;
            }
            if (job.ProductionPointsLeft > 0)
                return;

            if (!LocalConstructionProcessor.TryApplyRefit(yard, storage, refit, out var heldReason))
            {
                job.WaitReason = heldReason ?? ShipRefitJob.WaitingFor + "parts";
                job.Status = IndustryJobStatus.MissingResources;
                return;
            }

            job.Status = IndustryJobStatus.Completed;
            job.NumberCompleted = 1;
            line.Jobs.Remove(job);
            LocalConstructionProcessor.PublishRefitCompleted(yard, refit);
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
                // A component design counts both stacked under its design ID and as individual instances
                // (factory output is one entry per part); everything else is a plain stack.
                long amountUsedThisTick = Math.Min(fromCargo.GetUnitsStoredByDesign(cargoItem, false), kvp.Value);

                if (amountUsedThisTick > 0)
                {
                    long used = fromCargo.RemoveUnitsByDesign(cargoItem, amountUsedThisTick);
                    toUse[kvp.Key] -= used;
                }
            }
        }

        // ---- stock orders (civilian industry plan, slice 7) ----

        /// <summary>The design a job builds, from its faction's design list, or null when the faction no longer lists it.</summary>
        internal static IConstructableDesign? StockDesign(Entity colony, IndustryJob job)
        {
            var game = colony.Manager?.Game;
            if (game == null || !game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
                || !faction.TryGetDataBlob<FactionInfoDB>(out var info))
                return null;
            return info.IndustryDesigns.TryGetValue(job.ItemGuid, out var design) ? design : null;
        }

        /// <summary>Units of the design's item in the colony's stores (what a stock order compares with its level), or null if it is not a stored item.</summary>
        internal static long? StockOf(Entity colony, IConstructableDesign design)
        {
            if (design is not ICargoable item || !colony.TryGetDataBlob<CargoStorageDB>(out var storage))
                return null;
            return storage.GetUnitsStoredByDesign(item, false);
        }

        /// <summary>
        /// Why a stock order cannot be set, or null when it can. Used by the queue and edit commands.
        /// </summary>
        internal static string? StockOrderRefusal(Entity colony, IConstructableDesign design, bool repeat, long threshold)
        {
            if (threshold < 1)
                return "A stock level must be 1 or more.";
            if (!GameEngine.People.IndustrySeat.HasHead(colony))
                return "A stock order needs an industry head. Seat one on this colony first.";
            if (!repeat)
                return "A stock order needs a repeat job. Turn on Repeat first.";
            if (design is Pulsar4X.Ships.ShipDesign || design is not ICargoable)
                return "A stock order needs an item the colony stores. Ships have no stock level.";
            return null;
        }

        /// <summary>
        /// Called for a job with a stock level. Returns true when it should idle (Status set to Idle). When the head is
        /// gone it clears the level (the lazy backstop) and returns false. When no batch is running and stock is below
        /// the level it starts one: it marks the batch started and re-chains the feeders, in the head's name, like slice 3.
        /// </summary>
        internal static bool StockOrderIdles(Entity colony, IndustryJob job, IConstructableDesign design)
        {
            if (job.StockThreshold is not long level)
                return false;
            if (!GameEngine.People.IndustrySeat.HasHead(colony))
            {
                ClearStockOrders(colony, "the industry head left");
                return false;
            }
            if (job.StockBatchStarted)
                return false;
            if (StockOf(colony, design) is not long stock)
                return false;
            if (stock >= level)
            {
                job.Status = IndustryJobStatus.Idle;
                return true;
            }
            job.StockBatchStarted = true;
            if (job.Status == IndustryJobStatus.Idle)
                job.Status = IndustryJobStatus.Queued;
            AutoAddSubJobs(colony, job);
            return false;
        }

        /// <summary>
        /// Clears every stock level on the colony's jobs, so they carry on as plain repeat jobs. A later head does not
        /// bring them back. Writes one colony log line per job.
        /// </summary>
        internal static void ClearStockOrders(Entity colony, string why)
        {
            if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
                return;
            foreach (var (lineId, line) in industry.ProductionLines)
            {
                foreach (var job in line.Jobs)
                {
                    if (job.StockThreshold == null)
                        continue;
                    job.StockThreshold = null;
                    job.StockBatchStarted = false;
                    if (job.Status == IndustryJobStatus.Idle)
                        job.Status = IndustryJobStatus.Queued;
                    ShipLogBook.NoteIndustryChange(colony,
                        $"{job.Name} on {(string.IsNullOrEmpty(line.Name) ? lineId : line.Name)} lost its stock level because {why}; it now repeats as a plain job");
                }
            }
        }

        /// <summary>
        /// Queues a one-shot job on this colony's fastest accepting line for each buildable input the stockpile is
        /// short of, recursively, and writes one colony log line per job, in the industry head's name. This is the
        /// industry head's work: the caller checks <see cref="GameEngine.People.IndustrySeat.HasHead"/> first.
        /// </summary>
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

            foreach (var shortfall in InputShortfalls(industryEntity, job.ResourcesRequiredRemaining, stockpile, job))
            {
                if (shortfall.Builder is not { } des)
                    continue; // a raw resource: nothing here can build it
                if (shortfall.QueueQuantity == 0)
                    continue; // jobs already queued on this colony cover the shortfall, so queuing twice adds nothing
                IndustryJob newjob = new IndustryJob(des);
                newjob.InitialiseJob(shortfall.QueueQuantity, false);
                // A job no line here takes is not queued, so it gets no log line. Its inputs are still walked, as before.
                if (SetJobToFastest(industryDB, newjob))
                    ShipLogBook.NoteIndustryQueued(industryEntity, HeadName(industryEntity), shortfall.QueueQuantity, newjob.Name, job.Name);
                AutoAddSubJobs(industryEntity, newjob); //recursivly add jobs.
            }
        }

        /// <summary>
        /// One input a job needs more of than the colony's stockpile holds. The same numbers drive
        /// <see cref="AutoAddSubJobs"/> and the missing-inputs API view, so a one-click "queue this input" matches chaining.
        /// </summary>
        internal readonly record struct InputShortfall(string ResourceId, ICargoable Item, long Required, long InStock, long Queued = 0)
        {
            /// <summary>Units still needed: required minus stock. Always above 0 for a returned shortfall.</summary>
            public long Short => Required - InStock;

            /// <summary>Units still to make: the shortfall less what queued jobs will make (<see cref="Queued"/>, in units). 0 when they cover it.</summary>
            public long ToQueue => Math.Max(0, Short - Queued);

            /// <summary>Units one job item (one batch) makes: the recipe's <c>OutputAmount</c>, or 1 when it is 0 or the input cannot be built.</summary>
            public long OutputPerJob => OutputPerItem(Builder);

            /// <summary>The design that builds this input, or null for a raw resource that cannot be built.</summary>
            public IConstructableDesign? Builder => Item as IConstructableDesign;

            /// <summary>
            /// The quantity (batches, <c>NumberOrdered</c>) of a one-shot feeder job for what is still to make:
            /// <c>ceil(ToQueue / OutputPerJob)</c>, capped at what a job can hold (65535). 40 units short of a recipe
            /// that makes 2 per batch queues 20; 41 queues 21.
            /// </summary>
            public ushort QueueQuantity => (ushort)Math.Min((ToQueue + OutputPerJob - 1) / OutputPerJob, ushort.MaxValue);
        }

        /// <summary>
        /// Every input in <paramref name="required"/> (the current item's remaining resource needs, by id) that the
        /// stockpile holds less of. Inputs the faction has no cargo good or design for are skipped. Each carries how much
        /// the colony's queued jobs will still make of it (<see cref="QueuedOutput"/>), so feeding the same input twice
        /// only queues what is still short. <paramref name="parent"/> is the job asking: it never counts as its own supply.
        /// </summary>
        /// <summary>Units one batch of the design makes: its <c>OutputAmount</c>, with 0 (or no design) counted as 1.</summary>
        internal static long OutputPerItem(IConstructableDesign? design)
            => design == null ? 1 : Math.Max(1, (long)design.OutputAmount);

        internal static List<InputShortfall> InputShortfalls(Entity colony, IReadOnlyDictionary<string, long> required, CargoStorageDB? stockpile, IndustryJob? parent = null)
        {
            var found = new List<InputShortfall>();
            var info = colony.GetFactionOwner.GetDataBlob<FactionInfoDB>();
            foreach (var kvp in required)
            {
                ICargoable? cargoItem = info.Data.CargoGoods.GetAny(kvp.Key);
                if (cargoItem is null)
                {
                    if (info.IndustryDesigns.TryGetValue(kvp.Key, out var design) && design != null)
                        cargoItem = (ICargoable)design;
                    else
                        continue;
                }
                long stored = stockpile?.GetUnitsStoredByDesign(cargoItem, false) ?? 0;
                if (kvp.Value - stored > 0)
                    found.Add(new InputShortfall(kvp.Key, cargoItem, kvp.Value, stored,
                        cargoItem is IConstructableDesign builder ? QueuedOutput(colony, builder, parent) : 0));
            }
            return found;
        }

        /// <summary>
        /// Units of <paramref name="design"/>'s item the jobs already queued on the colony's lines will still make, in
        /// the same item units as the shortfall: per job, the batches left in its current batch (ordered less completed)
        /// times the recipe's <c>OutputAmount</c> (0 counts as 1). A repeat job counts only its current batch. A stock order that is idle (its batch not started) counts nothing, because it
        /// makes nothing until stock drops. <paramref name="except"/> (the parent job) and refit jobs are skipped.
        /// </summary>
        internal static long QueuedOutput(Entity colony, IConstructableDesign design, IndustryJob? except = null)
        {
            if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
                return 0;
            long units = 0;
            long perBatch = OutputPerItem(design);
            foreach (var line in industry.ProductionLines.Values)
            {
                foreach (var queued in line.Jobs)
                {
                    if (ReferenceEquals(queued, except) || queued.Refit != null || queued.ItemGuid != design.UniqueID)
                        continue;
                    if (queued.StockThreshold != null && !queued.StockBatchStarted)
                        continue;
                    units += Math.Max(0, queued.NumberOrdered - queued.NumberCompleted) * perBatch;
                }
            }
            return units;
        }

        /// <summary>
        /// The industry head's work for refits: every part the colony's waiting refit jobs still want (no stored part to
        /// claim) is queued on the fastest line that builds it, less what jobs already queued on the colony will make,
        /// so it never queues twice. Feeders are chained like <see cref="AutoAddSubJobs"/>. Does nothing without a
        /// seated head. Returns the number of jobs queued.
        /// </summary>
        internal static int AutoQueueRefitParts(Entity colony)
        {
            if (!GameEngine.People.IndustrySeat.HasHead(colony)
                || !colony.TryGetDataBlob<IndustryAbilityDB>(out var industryDB)
                || colony.GetFactionOwner == null
                || !colony.GetFactionOwner.TryGetDataBlob<FactionInfoDB>(out var info))
                return 0;

            // Wanted parts by design, over every refit on the colony's lines, with the first refit that wants each.
            // Parts already in stock are claimed first, so only what is truly missing is queued.
            colony.TryGetDataBlob<CargoStorageDB>(out var storage);
            var wanted = new Dictionary<string, (long Count, string ForJob)>();
            foreach (var (_, job) in ShipRefitJob.JobsOf(colony).ToList())
            {
                if (storage != null && job.Refit!.MountDesignIds.Count > 0)
                    LocalConstructionProcessor.ClaimStoredParts(colony, storage, job.Refit);
                foreach (var id in job.Refit!.MountDesignIds)
                {
                    wanted.TryGetValue(id, out var entry);
                    wanted[id] = (entry.Count + 1, entry.ForJob ?? job.Name);
                }
            }

            int queuedJobs = 0;
            foreach (var (id, (count, forJob)) in wanted)
            {
                if (!info.IndustryDesigns.TryGetValue(id, out var design) || design == null)
                    continue;
                long toMake = count - QueuedOutput(colony, design);
                if (toMake <= 0)
                    continue; // jobs already queued here cover it
                long perBatch = OutputPerItem(design);
                var newJob = new IndustryJob(design);
                newJob.InitialiseJob((ushort)Math.Min((toMake + perBatch - 1) / perBatch, ushort.MaxValue), false);
                if (!SetJobToFastest(industryDB, newJob))
                    continue; // no line here builds it
                queuedJobs++;
                ShipLogBook.NoteIndustryQueued(colony, HeadName(colony), newJob.NumberOrdered, newJob.Name, forJob);
                AutoAddSubJobs(colony, newJob);
            }
            return queuedJobs;
        }

        static string HeadName(Entity colony)
        {
            if (GameEngine.People.IndustrySeat.TryGetHead(colony, out var head)
                && head.TryGetDataBlob<Pulsar4X.People.CommanderDB>(out var db)
                && !string.IsNullOrEmpty(db.Name))
                return db.Name;
            return "head";
        }

        /// <summary>The colony line that builds this industry type fastest, or null when none takes it. Shipyards take only ships.</summary>
        internal static string? FastestLineId(IndustryAbilityDB industrydb, string typID)
        {
            (string lineID, int rate) bestLine = (String.Empty, 0);
            foreach (var line in industrydb.ProductionLines)
            {
                if (!line.Value.IndustryTypeRates.TryGetValue(typID, out int rate))
                    rate = -1;
                // A shipyard may still list component-construction from an older design.
                // Missing parts belong on a factory, not in a slip.
                if (IsShipyardLine(line.Value) && typID != ShipAssemblyIndustryType)
                    rate = -1;
                if (rate > bestLine.rate)
                    bestLine = (line.Key, rate);
            }
            return bestLine.lineID == String.Empty ? null : bestLine.lineID;
        }

        internal static bool SetJobToFastest(IndustryAbilityDB industrydb, IndustryJob job)
        {
            var lineId = FastestLineId(industrydb, job.TypeID);
            if (lineId == null)
                return false;
            AddJob(industrydb, lineId, job);
            return true;
        }
    }
}

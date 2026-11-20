using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Industry
{
    public class IndustryJob : JobBase
    {
        [JsonProperty]
        internal string TypeID;
        public IndustryJobStatus Status { get; internal set; } = IndustryJobStatus.Queued;

        /// <summary>
        /// Build energy per industry point of the item, kWh: the design's EnergyCost over its
        /// IndustryPointCosts, copied at queue time. 0 (a job from an old save) builds free.
        /// </summary>
        [JsonProperty]
        public double EnergyPerPoint { get; internal set; }

        /// <summary>Build energy spent on this job so far, kWh, across every item built.</summary>
        [JsonProperty]
        public double EnergyUsed { get; internal set; }

        /// <summary>Build energy of the whole order, kWh (items ordered x energy per item).</summary>
        public double EnergyOrdered => EnergyPerPoint * ProductionPointsCost * NumberOrdered;

        static double PerPoint(IConstructableDesign design)
            => design.IndustryPointCosts > 0 && double.IsFinite(design.EnergyCost)
                ? Math.Max(0, design.EnergyCost) / design.IndustryPointCosts
                : 0;

        [JsonConstructor]
        private IndustryJob()
        {
        }

        public IndustryJob(FactionInfoDB factionInfo, string itemID)
        {
            ItemGuid = itemID;
            var design = factionInfo.IndustryDesigns[itemID];
            TypeID = design.IndustryTypeID;
            Name = design.Name;
            if(design.ResourceCosts != null)
            {
                ResourcesRequiredRemaining = new Dictionary<string, long>(design.ResourceCosts);
            }
            else
            {
                ResourcesRequiredRemaining = new ();
            }
            ResourcesCosts = design.ResourceCosts;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            EnergyPerPoint = PerPoint(design);
            NumberOrdered = 1;
        }

        internal IndustryJob(IConstructableDesign design)
        {
            ItemGuid = design.UniqueID;
            TypeID = design.IndustryTypeID;
            Name = design.Name;
            ResourcesRequiredRemaining = new Dictionary<string, long>(design.ResourceCosts);
            ResourcesCosts = design.ResourceCosts;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            EnergyPerPoint = PerPoint(design);
            NumberOrdered = 1;
        }

        /// <summary>
        /// Stock order (civilian industry plan, slice 7): on a repeat job, the colony's stock level below which a
        /// batch runs. While the colony stock of the item is at or above it, the job is <see cref="IndustryJobStatus.Idle"/>.
        /// Null means a plain repeat job. Only an industry head being seated allows it; it is cleared when the head leaves.
        /// </summary>
        [JsonProperty]
        public long? StockThreshold { get; internal set; }

        /// <summary>True from the moment a stock order starts a batch until that batch completes.</summary>
        [JsonProperty]
        internal bool StockBatchStarted { get; set; }

        public Entity? InstallOn { get; set; } = null;

        /// <summary>Set on a shipyard refit job: the ship and the parts it moves. Null on every other job.</summary>
        public ShipRefitJob? Refit { get; set; }

        /// <summary>Set on a recycle job: what it takes apart and what each item returns. Null on every other job.</summary>
        public RecycleJob? Recycle { get; set; }

        /// <summary>Why a refit job waits (ship out of range, part missing, no room), else empty. Set each daily pass.</summary>
        public string WaitReason { get; internal set; } = "";

        /// <summary>A refit as a normal industry job: one item, the yard's ship-refit points, no resources.</summary>
        internal static IndustryJob ForRefit(ShipRefitJob refit, string name)
        {
            var job = new IndustryJob(new ShipRefitDesign(name, refit.PointCost)) { Refit = refit };
            job.NumberOrdered = 1;
            return job;
        }

        /// <summary>A recycle as a normal industry job: the line's points of the item's type, no resources in.</summary>
        internal static IndustryJob ForRecycle(RecycleJob recycle, RecycleDesign design)
            => new IndustryJob(design) { Recycle = recycle };

        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }
}
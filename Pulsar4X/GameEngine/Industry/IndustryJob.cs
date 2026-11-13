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

        public Entity? InstallOn { get; set; } = null;

        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }
}
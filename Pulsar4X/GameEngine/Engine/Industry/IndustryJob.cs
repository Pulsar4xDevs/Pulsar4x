using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine.Industry
{
    public class IndustryJob : JobBase
    {
        internal string TypeID;
        public IndustryJobStatus Status { get; internal set; } = IndustryJobStatus.Queued;

        public IndustryJob(FactionInfoDB factionInfo, string itemID)
        {
            ItemGuid = itemID;
            var design = factionInfo.IndustryDesigns[itemID];
            TypeID = design.IndustryTypeID;
            Name = design.Name;
            ResourcesRequiredRemaining = new Dictionary<string, long>(design.ResourceCosts);
            ResourcesCosts = design.ResourceCosts;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            NumberOrdered = 1;
        }

        internal IndustryJob(IConstrucableDesign design)
        {
            ItemGuid = design.UniqueID;
            TypeID = design.IndustryTypeID;
            Name = design.Name;
            ResourcesRequiredRemaining = new Dictionary<string, long>(design.ResourceCosts);
            ResourcesCosts = design.ResourceCosts;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            NumberOrdered = 1;
        }

        public Entity InstallOn { get; set; } = null;

        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }
}
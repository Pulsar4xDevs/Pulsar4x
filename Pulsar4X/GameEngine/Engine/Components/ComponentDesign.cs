using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Industry;
using Pulsar4X.DataStructures;
using System.Diagnostics;
using Pulsar4X.Storage;

namespace Pulsar4X.Components
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class ComponentDesign : ICargoable, IConstructableDesign
    {
        public ConstructableGuiHints GuiHints { get; set; }
        public int ID { get; private set; } = Game.GetEntityID();
        public string UniqueID { get; internal set; }
        public string Name { get; internal set; } //player defined name. ie "5t 2kn Thruster".
        public string ComponentType { get; internal set; } = "";

        public string TemplateName; //ie the name in staticData. ie "Newtonion Thruster".
        public string TemplateID; //ie the UniqueID in staticData. ie "newtonion-thruster".
        public List<(string propName, Type valueType, object propValue)> TemplatePropertyValues = new ();
        
        
        public bool IsValid {get; set; } = true;

        public string CargoTypeID { get; internal set; }
        public long MassPerUnit { get; internal set; }

        public double VolumePerUnit { get; internal set; }

        public double Density { get; internal set; }

        public long ResearchCostValue;
        public string TechID;
        
        public string Description;
        //public int Volume_m3 = 1;
        public float DestructionPercent;
        public int CrewReq;
        public long IndustryPointCosts { get; set; }
        public string IndustryTypeID { get; set; }
        public ushort OutputAmount
        {
            get { return 1; }
        }


        public int CreditCost;

        //public int ResearchCostValue;
        public Dictionary<string, long> ResourceCosts { get; internal set; } = new Dictionary<string, long>();

        public ComponentMountType ComponentMountType;
        //public List<ComponentDesignAtbData> ComponentDesignAttributes;

        [JsonProperty]
        public Dictionary<Type, IComponentDesignAttribute> AttributesByType = new();
        public float AspectRatio = 1f;

        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(designInfo.ResourceCosts);
            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;

            var design = (ComponentDesign)designInfo;

            // A site-only design cannot sit in cargo. The factory already spent the
            // recipe and the build points, so the follow-up is the mount cost.
            if (batchJob.InstallOn != null
                && LocalConstructionJob.IsSiteOnly(design)
                && industryEntity.TryGetDataBlob<LocalConstructionDB>(out var siteConstruction)
                && siteConstruction.PointsPerDay > 0)
            {
                var assemble = new LocalConstructionJob(design);
                assemble.ResourcesRemaining.Clear();
                assemble.PointCost = LocalConstructionJob.MountCost(design.MassPerUnit);
                siteConstruction.BuildQueue.Enqueue(assemble);
            }
            else
            {
                var instance = new ComponentInstance(design);
                long stored = storage.AddCargoByUnit(instance, 1);

                // A checked box queues a mount of this crate. Colony construction pays it later.
                if (stored > 0
                    && batchJob.InstallOn != null
                    && LocalConstructionJob.IsCrate(design)
                    && industryEntity.TryGetDataBlob<LocalConstructionDB>(out var construction)
                    && construction.PointsPerDay > 0)
                {
                    construction.BuildQueue.Enqueue(new LocalConstructionJob(design, instance));
                }
            }

            if (batchJob.NumberCompleted == batchJob.NumberOrdered)
            {
                industryDB.ProductionLines[productionLine].Jobs.Remove(batchJob);
                if (batchJob.Auto)
                {
                    batchJob.NumberCompleted = 0;
                    industryDB.ProductionLines[productionLine].Jobs.Add(batchJob);
                }
            }
        }

        public bool HasAttribute<T>()
            where T : IComponentDesignAttribute
        {
            return AttributesByType.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Will throw an exception if it doesn't have the type of attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAttribute<T>()
            where T : IComponentDesignAttribute
        {
            return (T)AttributesByType[typeof(T)];
        }

        public bool TryGetAttribute<T>(out T attribute)
            where T : IComponentDesignAttribute
        {
            if (HasAttribute<T>())
            {
                attribute = (T)AttributesByType[typeof(T)];
                return true;
            }
            attribute = default(T);
            return false;
        }

        [JsonIgnore]
        private string DebuggerDisplay => $"({UniqueID}) {Name}";
    }
}
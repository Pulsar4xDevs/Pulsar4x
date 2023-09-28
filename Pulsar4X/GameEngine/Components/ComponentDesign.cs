using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;

namespace Pulsar4X.Components
{
    public class ComponentDesign : ICargoable, IConstructableDesign
    {
        public ConstructableGuiHints GuiHints { get; set; }
        public int ID { get; private set; } = Game.GetEntityID();
        public string UniqueID { get; internal set; }
        public string Name { get; internal set; } //player defined name. ie "5t 2kn Thruster".
        public string ComponentType { get; internal set; } = "";

        public bool IsValid {get; set; } = true;

        public string CargoTypeID { get; internal set; }
        public long MassPerUnit { get; internal set; }

        public double VolumePerUnit { get; internal set; }

        public double Density { get; internal set; }

        public long ResearchCostValue;
        public string TechID;
        public string TypeName; //ie the name in staticData. ie "Newtonion Thruster".
        public string Description;
        //public int Volume_m3 = 1;
        public int HTK;
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

        [JsonIgnore]
        public Dictionary<Type, IComponentDesignAttribute> AttributesByType = new();
        public float AspectRatio = 1f;

        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(designInfo.ResourceCosts);
            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;

            if (batchJob.InstallOn != null)
            {
               ComponentInstance specificComponent = new((ComponentDesign)designInfo);
               if (batchJob.InstallOn == industryEntity || storage.HasSpecificEntity(batchJob.InstallOn.GetDataBlob<CargoAbleTypeDB>()))
               {
                   batchJob.InstallOn.AddComponent(specificComponent);
                   ReCalcProcessor.ReCalcAbilities(batchJob.InstallOn);
               }
            }
            else
            {
                storage.AddCargoByUnit((ComponentDesign)designInfo, 1);
               //StorageSpaceProcessor.AddCargo(storage, (ComponentDesign)designInfo, 1);
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
    }
}
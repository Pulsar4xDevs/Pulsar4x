using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Blueprints;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Atb;

namespace Pulsar4X.Engine.Designs
{
    public class OrdnanceDesign : ICargoable, IConstructableDesign, ISerializable
    {
        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.IsOrdinance;
        public int ID { get; private set; } = Game.GetEntityID();
        public string UniqueID { get; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public bool IsValid {get; set; } = true;
        public string CargoTypeID { get; }
        public int DesignVersion = 0;
        public bool IsObsolete = false;
        public long MassPerUnit { get; set; }
        public double VolumePerUnit { get; set; }

        /// <summary>
        /// Wet Density;
        /// </summary>
        public double Density { get; }
        public double WetMass { get; }
        public double DryMass { get; }
        public double ExaustVelocity { get; }
        public double BurnRate { get; }

        public double Volume;
        public List<(ComponentDesign design, int count)> Components;
        public (ArmorBlueprint type, float thickness) Armor;
        public Dictionary<string, long> ResourceCosts { get; internal set; } = new Dictionary<string, long>();
        public Dictionary<string, long> MineralCosts = new Dictionary<string, long>();
        public Dictionary<string, long> MaterialCosts = new Dictionary<string, long>();
        public Dictionary<string, long> ComponentCosts = new Dictionary<string, long>();
        public Dictionary<string, long> ShipInstanceCost = new Dictionary<string, long>();
        public int CrewReq;
        public long IndustryPointCosts { get; }

        //TODO: this is one of those places where moddata has bled into hardcode...
        //the guid here is from IndustryTypeData.json "Ordinance Construction"
        public string IndustryTypeID { get; } = "ordnance-construction"; //new Guid("5ADBF620-3740-4FD7-98BE-E8670D58945F");
        public ushort OutputAmount
        {
            get { return 1; }
        }

        public void OnConstructionComplete(Entity industryEntity, VolumeStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            batchJob.NumberCompleted++;
            batchJob.ResourcesRequiredRemaining = new Dictionary<string, long>(designInfo.ResourceCosts);
            batchJob.ProductionPointsLeft = designInfo.IndustryPointCosts;

        }

        public int CreditCost;
        public EntityDamageProfileDB DamageProfileDB;

        [JsonConstructor]
        internal OrdnanceDesign()
        {
        }

        public OrdnanceDesign(FactionInfoDB faction, string name, double fuelAmountKG,  List<(ComponentDesign design, int count)> components)
        {
            faction.MissileDesigns.Add(UniqueID, this);
            faction.IndustryDesigns[UniqueID] = this;
            Name = name;
            Components = components;

            //TODO! we're leaking softcode into hard code here! this is the "ordnance" cargo type, tells us to store this missile in "ordnance" type cargo.
            CargoTypeID = "ordnance-storage"; //new Guid("055E2026-20A4-4CFA-A8CA-A01915A48B5E");
            BurnRate = 0;
            string? fuelType = null;
            double fuelMass = fuelAmountKG;
            double mass = 0;
            double vol = 0;
            foreach (var component in components)
            {
                //If the mounttype does not include missiles, it will just ignore the component and wont add it.
                if((component.design.ComponentMountType & ComponentMountType.Missile) == ComponentMountType.Missile)
                {
                    mass += component.design.MassPerUnit * component.count;
                    vol += component.design.VolumePerUnit * component.count;
                    CreditCost += component.design.CreditCost;

                    if (ComponentCosts.ContainsKey(component.design.UniqueID))
                    {
                        ComponentCosts[component.design.UniqueID] = ComponentCosts[component.design.UniqueID] + component.count;
                    }
                    else
                    {
                        ComponentCosts.Add(component.design.UniqueID, component.count);
                    }

                    if (component.design.TryGetAttribute<NewtonionThrustAtb>(out NewtonionThrustAtb thrAtb))
                    {
                        //thrusters should all be of the same type.
                        ExaustVelocity = thrAtb.ExhaustVelocity;
                        BurnRate += thrAtb.FuelBurnRate;
                        fuelType = thrAtb.FuelType;
                    }
                }
            }


            WetMass = mass + fuelMass;
            DryMass = mass;
            Density = WetMass / 1000;

            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = (int)mass;
            MassPerUnit = (int)WetMass;
            VolumePerUnit = vol;
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(WetMass), WetMass);
            info.AddValue(nameof(DryMass), DryMass);
            info.AddValue(nameof(Density), Density);
        }
    }
}
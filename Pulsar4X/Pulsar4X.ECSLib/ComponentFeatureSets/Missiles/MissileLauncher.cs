using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileLauncherAtb : IComponentDesignAttribute
    {
        public int LauncherSize;
        public double ReloadRate;
        public double LaunchForce;
        
        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MissileLaunchersAbilityDB : BaseDataBlob
    {
        private MissileLauncherAtb[] Launchers;
        private ShipDesign[] LoadedMissiles;
        private Entity[] Targets;
        
        
        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
    
    
    
    
       public class OrdnanceDesign : ICargoable, IConstrucableDesign, ISerializable
    {
        public ConstructableGuiHints GuiHints { get; } = ConstructableGuiHints.CanBeLaunched;
        public Guid ID { get; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid CargoTypeID { get; }
        public int DesignVersion = 0;
        public bool IsObsolete = false;
        public int Mass { get; }
        public double Volume;
        public List<(ComponentDesign design, int count)> Components;
        public (ArmorSD type, float thickness) Armor;
        public Dictionary<Guid, int> ResourceCosts { get; internal set; } = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> MineralCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> MaterialCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> ComponentCosts = new Dictionary<Guid, int>();
        public Dictionary<Guid, int> ShipInstanceCost = new Dictionary<Guid, int>();
        public int CrewReq;
        public int IndustryPointCosts { get; }
        
        //TODO: this is one of those places where moddata has bled into hardcode...
        //the guid here is from IndustryTypeData.json "Ordinance Construction"
        public Guid IndustryTypeID { get; } = new Guid("5ADBF620-3740-4FD7-98BE-E8670D58945F");
        public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, Guid productionLine, IndustryJob batchJob, IConstrucableDesign designInfo)
        { 
            var industrydb = industryEntity.GetDataBlob<IndustryAbilityDB>();
            
        }

        public int CreditCost;
        public EntityDamageProfileDB DamageProfileDB;

        [JsonConstructor]
        internal OrdnanceDesign()
        {
        }

        public OrdnanceDesign(FactionInfoDB faction, string name, List<(ComponentDesign design, int count)> components, (ArmorSD armorType, float thickness) armor)
        {
            faction.MissileDesigns.Add(ID, this);
            faction.IndustryDesigns[ID] = this;
            Name = name;
            Components = components;
            Armor = armor;

            
            foreach (var component in components)
            {
                //If the mounttype does not include missiles, it will just ignore the component and wont add it. 
                if((component.design.ComponentMountType & ComponentMountType.Missile) == ComponentMountType.Missile)
                {
                    Mass += component.design.Mass * component.count;
                    CreditCost += component.design.CreditCost;

                    if (ComponentCosts.ContainsKey(component.design.ID))
                    {
                        ComponentCosts[component.design.ID] = ComponentCosts[component.design.ID] + component.count;
                    }
                    else
                    {
                        ComponentCosts.Add(component.design.ID, component.count);
                    }
                }
            }
            
            
            MineralCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            MaterialCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            ComponentCosts.ToList().ForEach(x => ResourceCosts[x.Key] = x.Value);
            IndustryPointCosts = Mass;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
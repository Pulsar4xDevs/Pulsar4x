using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileLauncherAtb : IComponentDesignAttribute, IFireWeaponInstr
    {
        public double LauncherSize;
        public double ReloadRate;
        public double LaunchForce;
        public OrdnanceDesign AssignedOrdnance { get; private set; }

        public MissileLauncherAtb()
        {
        }
        public MissileLauncherAtb(double maxMissileWeight, double reloadRate, double launchForce)
        {
            LauncherSize = maxMissileWeight;
            ReloadRate = reloadRate;
            LaunchForce = launchForce;
        }

        public bool CanLoadOrdnance(OrdnanceDesign ordnanceDesign)
        {
            //need to check ordnance type, size etc.
            return true;
        }

        public bool AssignOrdnance(OrdnanceDesign ordnanceDesign)
        {
            if (CanLoadOrdnance(ordnanceDesign))
            {
                AssignedOrdnance = ordnanceDesign;
                return true;
            }
            else return false;
        }

        public bool TryGetOrdnance(out OrdnanceDesign ordnanceDesign)
        {
            ordnanceDesign = AssignedOrdnance;
            return AssignedOrdnance != null;
        }

        public void FireWeapon(Entity launchingEntity, Entity tgtEntity)
        {
            MissileProcessor.LaunchMissile(launchingEntity, tgtEntity, LaunchForce, AssignedOrdnance);
        }
        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!componentInstance.HasAblity<WeaponState>())
            {
                var wpnState = new WeaponState(componentInstance, this);
                wpnState.WeaponType = "Missile Launcher";
                wpnState.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
                wpnState.WeaponStats[0] = ("Max Size:", LauncherSize, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.Milli));
                wpnState.WeaponStats[1] = ("Launch Force:", LaunchForce, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Force, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[2] = ("Rate Of Fire:", ReloadRate, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Number, ValueTypeStruct.ValueSizes.BaseUnit));
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
            /*
            if(!parentEntity.HasDataBlob<MissileLaunchersAbilityDB>())
            {
                var mla = new MissileLaunchersAbilityDB();
                parentEntity.SetDataBlob(mla);
            }*/
        }
    }

    public class MissileLaunchersAbilityDB : BaseDataBlob
    {
        public MissileLauncherAtb[] Launchers;
        public ShipDesign[] LoadedMissiles;


        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
    
    
    
    

}
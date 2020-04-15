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
        public double LauncherSize;
        public double ReloadRate;
        public double LaunchForce;


        public MissileLauncherAtb()
        {
        }
        public MissileLauncherAtb(double maxMissileWeight, double reloadRate, double launchForce)
        {
            LauncherSize = maxMissileWeight;
            ReloadRate = reloadRate;
            LaunchForce = launchForce;
        }
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!componentInstance.HasAblity<WeaponState>())
                componentInstance.SetAbilityState<WeaponState>(new WeaponState());
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
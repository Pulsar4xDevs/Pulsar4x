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


        public MissileLauncherAtb()
        {
        }
        public MissileLauncherAtb(double maxMissileWeight, double reloadRate, double launchForce)
        {
        }
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
    
    
    
    

}
using Newtonsoft.Json;
using System;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;


namespace Pulsar4X.ECSLib
{
    public class WeaponState : ComponentAbilityState
    {
        [JsonProperty]
        public DateTime CoolDown { get; internal set; }
        [JsonProperty]
        public bool ReadyToFire { get; internal set; }

        public string WeaponType = "";

        public ComponentInstance WeaponComponentInstance { get; set; }
        public (string name, double value, ValueTypeStruct valueType)[] WeaponStats;

        public OrdnanceDesign AssignedOrdnanceDesign = null;
        public int InernalMagCurAmount = 0;
        


        public WeaponState()
        {
            
            
        }

        public WeaponState(WeaponState db)
        {
            CoolDown = db.CoolDown;
            ReadyToFire = db.ReadyToFire;
            
        }
        
    }
}

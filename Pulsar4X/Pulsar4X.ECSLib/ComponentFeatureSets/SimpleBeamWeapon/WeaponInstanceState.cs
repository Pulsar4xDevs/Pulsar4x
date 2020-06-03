using Newtonsoft.Json;
using System;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;


namespace Pulsar4X.ECSLib
{
    public class WeaponState : ComponentTreeHeirarchyAbilityState
    {
        [JsonProperty]
        public DateTime CoolDown { get; internal set; }
        [JsonProperty]
        public bool ReadyToFire { get; internal set; }

        public string WeaponType = "";
        

        public ComponentInstance WeaponComponentInstance { get; set; }
        public (string name, double value, ValueTypeStruct valueType)[] WeaponStats;

        public OrdnanceDesign AssignedOrdnanceDesign = null;
        public int InternalMagCurAmount = 0;
        

        public WeaponState(ComponentInstance componentInstance) : base(componentInstance)
        {
            //weapon starts loaded
            InternalMagCurAmount = componentInstance.Design.GetAttribute<GenericWeaponAtb>().InternalMagSize;
        }

        public WeaponState(WeaponState db): base(db.ComponentInstance)
        {
            CoolDown = db.CoolDown;
            ReadyToFire = db.ReadyToFire;
            WeaponComponentInstance = db.WeaponComponentInstance;
            WeaponStats = db.WeaponStats;
            AssignedOrdnanceDesign = db.AssignedOrdnanceDesign;
            InternalMagCurAmount = db.InternalMagCurAmount;

        }
        
    }
}

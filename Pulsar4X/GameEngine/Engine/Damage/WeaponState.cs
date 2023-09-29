using Newtonsoft.Json;
using System;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;
using Pulsar4X.Atb;

namespace Pulsar4X.Engine.Damage
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

        //public OrdnanceDesign AssignedOrdnanceDesign {get; internal set;}
        public int InternalMagCurAmount = 0;
        public IFireWeaponInstr FireWeaponInstructions;

        public WeaponState(ComponentInstance componentInstance, IFireWeaponInstr weaponInstr) : base(componentInstance)
        {
            FireWeaponInstructions = weaponInstr;
            //weapon starts loaded, max value from component design.
            InternalMagCurAmount = componentInstance.Design.GetAttribute<GenericWeaponAtb>().InternalMagSize;
        }

        public WeaponState(WeaponState db): base(db.ComponentInstance)
        {
            CoolDown = db.CoolDown;
            ReadyToFire = db.ReadyToFire;
            WeaponComponentInstance = db.WeaponComponentInstance;
            WeaponStats = db.WeaponStats;
            //AssignedOrdnanceDesign = db.AssignedOrdnanceDesign;
            InternalMagCurAmount = db.InternalMagCurAmount;

        }
        
    }
}

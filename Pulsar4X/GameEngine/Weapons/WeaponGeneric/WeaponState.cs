using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using Pulsar4X.Components;
using Pulsar4X.Interfaces;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Weapons
{
    public class WeaponState : ComponentTreeHeirarchyAbilityState
    {
        
        [JsonProperty]
        public ComponentInstance WeaponComponentInstance { get; set; }
        [JsonProperty]
        public IFireWeaponInstr FireWeaponInstructions;
        
        [JsonProperty]
        public DateTime CoolDown { get; internal set; }
        [JsonProperty]
        public bool ReadyToFire { get; internal set; }
        
        
        [JsonProperty]
        public string WeaponType = "";
        [JsonIgnore]
        public (string name, double value, ValueTypeStruct valueType)[] WeaponStats;
        [JsonProperty]
        public int InternalMagCurAmount = 0;
        //public OrdnanceDesign AssignedOrdnanceDesign {get; internal set;}
        


        [JsonConstructor]
        private WeaponState(){}
        
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
        
        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            FireWeaponInstructions.SetWeaponState(this);
        }

    }
}

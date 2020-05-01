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
        public int InernalMagCurAmount = 0;

        public ComponentInstance Master
        {
            get
            {
                return ParentState.GetRootInstance();
            }

            set
            {
                if (value.HasAblity<FireControlAbilityState>() )
                {
                    this.SetParent(value.GetAbilityState<FireControlAbilityState>());
                }
                else if (value == null)
                {
                    this.SetParent(null);
                }

                
            }
        }

        public WeaponState(ComponentInstance componentInstance) : base(componentInstance)
        {
            
            
        }

        public WeaponState(WeaponState db): base(db.ComponentInstance)
        {
            CoolDown = db.CoolDown;
            ReadyToFire = db.ReadyToFire;
            Master = db.Master;
            
        }
        
    }
}

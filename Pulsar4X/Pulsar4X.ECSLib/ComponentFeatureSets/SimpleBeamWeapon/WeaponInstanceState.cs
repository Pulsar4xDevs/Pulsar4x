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
        public (string name, double value, ValueTypeStruct valueType)[] WeaponStats;

        public OrdnanceDesign OrdnanceDesign = null;
        public int InernalMagCurAmount = 0;
        
        [JsonProperty]
        private ComponentInstance _fireControl;
        public ComponentInstance FireControl
        {
            get
            {
                return _fireControl;
            }

            set
            {
                if (value == null)
                    _fireControl = null;
                else if (value.HasAblity<FireControlAbilityState>())
                    _fireControl = value;
                else
                    _fireControl = null;
            }
        }

        public WeaponState()
        {
            FireControl = null;
        }

        public WeaponState(WeaponState db)
        {
            CoolDown = db.CoolDown;
            ReadyToFire = db.ReadyToFire;
            FireControl = db.FireControl;
        }
        
    }
}

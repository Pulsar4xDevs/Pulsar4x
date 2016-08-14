using Newtonsoft.Json;
using System;


namespace Pulsar4X.ECSLib
{
    public class WeaponStateDB : BaseDataBlob
    {
        [JsonProperty]
        public TimeSpan CoolDown { get; internal set; }
        [JsonProperty]
        private Entity _fireControl;
        public Entity FireControl
        {
            get
            {
                return _fireControl;
            }
            set
            {
                if (value == null)
                    _fireControl = null;
                else if (value.HasDataBlob<FireControlInstanceAbilityDB>())
                    _fireControl = value;
                else
                    _fireControl = null;
            }
        }

        public WeaponStateDB()
        {
            FireControl = null;
        }

        public WeaponStateDB(WeaponStateDB db)
        {
            CoolDown = db.CoolDown;
            FireControl = db.FireControl;
        }

        public override object Clone()
        {
            return new WeaponStateDB(this);
        }
    }
}

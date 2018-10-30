using Newtonsoft.Json;
using System;


namespace Pulsar4X.ECSLib
{
    public class WeaponInstanceStateDB : BaseDataBlob
    {
        [JsonProperty]
        public DateTime CoolDown { get; internal set; }
        [JsonProperty]
        public bool ReadyToFire { get; internal set; }
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
                else if (value.HasDataBlob<FireControlInstanceStateDB>())
                    _fireControl = value;
                else
                    _fireControl = null;
            }
        }

        public WeaponInstanceStateDB()
        {
            FireControl = null;
        }

        public WeaponInstanceStateDB(WeaponInstanceStateDB db)
        {
            CoolDown = db.CoolDown;
            FireControl = db.FireControl;
        }

        public override object Clone()
        {
            return new WeaponInstanceStateDB(this);
        }
    }
}

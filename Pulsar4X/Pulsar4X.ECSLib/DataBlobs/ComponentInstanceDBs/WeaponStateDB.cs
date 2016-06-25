using Newtonsoft.Json;
using System;


namespace Pulsar4X.ECSLib
{
    public class WeaponStateDB : BaseDataBlob
    {
        [JsonProperty]
        public TimeSpan CoolDown { get; internal set; }
        [JsonProperty]
        public Entity FireControl { get; internal set; }

        public WeaponStateDB() { }

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

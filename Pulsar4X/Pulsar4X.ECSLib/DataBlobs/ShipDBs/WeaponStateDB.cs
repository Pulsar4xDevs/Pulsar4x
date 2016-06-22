using Newtonsoft.Json;
using System;


namespace Pulsar4X.ECSLib
{
    public class WeaponStateDB : BaseDataBlob
    {
        [JsonProperty]
        public TimeSpan CoolDown { get; internal set; }
        [JsonProperty]
        public Entity Target { get; internal set; }

        public WeaponStateDB() { }

        public WeaponStateDB(WeaponStateDB db)
        {
            CoolDown = db.CoolDown;
            Target = db.Target;
        }

        public override object Clone()
        {
            return new WeaponStateDB(this);
        }
    }
}

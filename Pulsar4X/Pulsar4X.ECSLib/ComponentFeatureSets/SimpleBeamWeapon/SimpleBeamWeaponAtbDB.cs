using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class SimpleBeamWeaponAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        public double MaxRange { get; internal set; }
        [JsonProperty]
        public int DamageAmount { get; internal set; }
        [JsonProperty]
        public int ReloadRate { get; internal set; }

        public SimpleBeamWeaponAtbDB() { }

        public SimpleBeamWeaponAtbDB(double maxRange, double damageAmount, double reloadRate)
        {
            MaxRange = maxRange;
            DamageAmount = (int)damageAmount;
            ReloadRate = (int)reloadRate;
        }

        public SimpleBeamWeaponAtbDB(SimpleBeamWeaponAtbDB db)
        {
            MaxRange = db.MaxRange;
            DamageAmount = db.DamageAmount;
            ReloadRate = db.ReloadRate;
        }

        public override object Clone()
        {
            return new SimpleBeamWeaponAtbDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var instancesDB = parentEntity.GetDataBlob<ComponentInstancesDB>();
            if (!parentEntity.HasDataBlob<FireControlAbilityDB>())
            {
                var fcdb = new FireControlAbilityDB();
                parentEntity.SetDataBlob(fcdb);
            }
           
            if (!componentInstance.HasAblity<WeaponState>())
            {
                var wpnState = new WeaponState();
                wpnState.WeaponType = "Beam";
                wpnState.WeaponStats = new (string name, double value, ValueTypeStruct valueType)[3];
                wpnState.WeaponStats[0] = ("Max Range:", MaxRange, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Distance, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[1] = ("Damage:", DamageAmount, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Power, ValueTypeStruct.ValueSizes.BaseUnit));
                wpnState.WeaponStats[2] = ("Rate Of Fire:", ReloadRate, new ValueTypeStruct(ValueTypeStruct.ValueTypes.Number, ValueTypeStruct.ValueSizes.BaseUnit));
                componentInstance.SetAbilityState<WeaponState>(wpnState);
            }
            

        }

    }
}

using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Weapons
{
    /// <summary>
    /// Other than giving an entity a datablob that says it can do fire control,
    /// it just links component instsances for convenience.
    /// </summary>
    public class FireControlAbilityDB : BaseDataBlob
    {
        [JsonIgnore]
        public List<ComponentInstance> FireControlInstances = new List<ComponentInstance>();
        [JsonIgnore]
        public List<ComponentInstance> WeaponInstances = new List<ComponentInstance>();

        [JsonConstructor]
        public FireControlAbilityDB()
        {
        }

        FireControlAbilityDB(FireControlAbilityDB db)
        {

        }

        public override object Clone()
        {
            return new FireControlAbilityDB(this);
        }

        // JSON deserialization callback.
        //[OnDeserialized]
        //private void Deserialized(StreamingContext context)
        //{
        //    try
        //    {
        //        var instancesDB = OwningEntity.GetDataBlob<ComponentInstancesDB>();
        //        if (instancesDB.TryGetComponentsByAttribute<BeamFireControlAtbDB>(out var fireControlInstances))
        //        {
        //            foreach (var fc in fireControlInstances)
        //            {
        //                FireControlInstances.Add(fc);
        //            }
        //        }
        //        if (instancesDB.TryGetComponentsByAttribute<GenericBeamWeaponAtb>(out var weaponInstances))
        //        {
        //            foreach (var gun in weaponInstances)
        //            {
        //                WeaponInstances.Add(gun);
        //            }
        //        }
        //    } catch (Exception ex) { }
        //}
    }
}

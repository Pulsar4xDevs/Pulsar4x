using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FireControlAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public List<Entity> FireControlComponents = new List<Entity>();

        [JsonIgnore]
        public List<FireControlInstanceStateDB> FireControlInsances = new List<FireControlInstanceStateDB>();

        [JsonProperty]
        public List<Entity> WeaponComponents = new List<Entity>();

        [JsonIgnore]
        public List<WeaponInstanceStateDB> WeaponInstanceStates = new List<WeaponInstanceStateDB>();

        public FireControlAbilityDB() { }

        public void AddFirecontrolToParentEntity(Entity fireControl)
        {
            FireControlInsances.Add(fireControl.GetDataBlob<FireControlInstanceStateDB>());
            FireControlComponents.Add(fireControl);
        }
        public void AddWeaponToParentEntity(Entity weapon)
        {
            WeaponInstanceStates.Add(weapon.GetDataBlob<WeaponInstanceStateDB>());
            WeaponComponents.Add(weapon);
        }

        public FireControlAbilityDB(FireControlAbilityDB toClone)
        {
            FireControlComponents = new List<Entity>(toClone.FireControlComponents);
            FireControlInsances = new List<FireControlInstanceStateDB>(toClone.FireControlInsances);
            WeaponComponents = new List<Entity>(toClone.WeaponComponents);
            WeaponInstanceStates = new List<WeaponInstanceStateDB>(toClone.WeaponInstanceStates);
        }

        public override object Clone()
        {
            return new FireControlAbilityDB(this);
        }

        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            //TODO: maybe we can get the ComponentInstanceDB from the context, and rebuild everything, and not have anything saved to json.
            FireControlInsances = new List<FireControlInstanceStateDB>();
            foreach (var fc in FireControlComponents)
            {
                FireControlInsances.Add(fc.GetDataBlob<FireControlInstanceStateDB>());
            }
            WeaponInstanceStates = new List<WeaponInstanceStateDB>();
            foreach (var weapon in WeaponComponents)
            {
                WeaponInstanceStates.Add(weapon.GetDataBlob<WeaponInstanceStateDB>());
            }
        }
    }

    /// <summary>
    /// Fire control instance ability db.
    /// This goes on each fire control component entity
    /// maybe this should be just a non db class and reside as a list on the ship firecontrolDB
    /// </summary>
    public class FireControlInstanceStateDB : BaseDataBlob
    {
        public Entity Target { get; internal set; }

        public List<Entity> AssignedWeapons { get; internal set; } = new List<Entity>();

        public bool IsEngaging { get; internal set; } = false;

        public FireControlInstanceStateDB() { }

        public FireControlInstanceStateDB(FireControlInstanceStateDB db)
        {
            Target = db.Target;
            AssignedWeapons = new List<Entity>(db.AssignedWeapons);
            IsEngaging = db.IsEngaging;
        }

        public override object Clone()
        {
            return new FireControlInstanceStateDB(this);
        }
    }
}

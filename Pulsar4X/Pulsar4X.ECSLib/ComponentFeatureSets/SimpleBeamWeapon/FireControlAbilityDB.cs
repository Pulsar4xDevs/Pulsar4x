using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FireControlAbilityDB : BaseDataBlob
    {
        public List<Entity> FireControlComponents = new List<Entity>();

        [JsonIgnore]
        public List<FireControlInstanceAbilityDB> FireControlInsances = new List<FireControlInstanceAbilityDB>();

        public List<Entity> WeaponComponents = new List<Entity>();

        [JsonIgnore]
        public List<WeaponStateDB> WeaponInstanceStates = new List<WeaponStateDB>();


        public override object Clone()
        {
            throw new NotImplementedException();

        }
    }

    /// <summary>
    /// Fire control instance ability db.
    /// This goes on each fire control component entity
    /// maybe this should be just a non db class and reside as a list on the ship firecontrolDB
    /// </summary>
    public class FireControlInstanceAbilityDB : BaseDataBlob
    {
        public Entity Target { get; set; }

        public List<Entity> AssignedWeapons { get; internal set; } = new List<Entity>();

        public bool IsEngaging { get; internal set; } = false;

        public FireControlInstanceAbilityDB() { }

        public FireControlInstanceAbilityDB(FireControlInstanceAbilityDB db)
        {
            Target = db.Target;
            AssignedWeapons = new List<Entity>(db.AssignedWeapons);
            IsEngaging = db.IsEngaging;
        }

        public override object Clone()
        {
            return new FireControlInstanceAbilityDB(this);
        }
    }
}

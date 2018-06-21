using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
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

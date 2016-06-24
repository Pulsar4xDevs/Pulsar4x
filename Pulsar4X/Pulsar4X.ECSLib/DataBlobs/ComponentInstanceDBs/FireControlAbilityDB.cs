using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class FireControlAbilityDB : BaseDataBlob
    {
        public Entity Target { get; internal set; }

        public List<Entity> AssignedWeapons { get; internal set; } = new List<Entity>();

        public bool IsEngaging { get; internal set; } = false;

        public FireControlAbilityDB() { }

        public FireControlAbilityDB(FireControlAbilityDB db)
        {
            Target = db.Target;
            AssignedWeapons = new List<Entity>(db.AssignedWeapons);
            IsEngaging = db.IsEngaging;
        }

        public override object Clone()
        {
            return new FireControlAbilityDB(this);
        }
    }
}

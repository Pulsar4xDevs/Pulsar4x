using Pulsar4X.Engine;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Datablobs
{
    public class FactionSystemInfoDB : BaseDataBlob
    {
        internal StarSystem StarSystem;
        internal HashSet<Entity> OwnedEntitesInSystem = new HashSet<Entity>();
        internal HashSet<Entity> KnownSystemBodies = new HashSet<Entity>();

        public FactionSystemInfoDB() { }

        public FactionSystemInfoDB(StarSystem starSystem)
        {
            this.StarSystem = starSystem;
            //TODO move this to a processor and require sensors of some kind.
            //will also need to figure out how much a faction knows about the body.
            foreach (var body in starSystem.GetAllEntitiesWithDataBlob<SystemBodyInfoDB>())
            {
                KnownSystemBodies.Add(body);
            }
        }

        public FactionSystemInfoDB(FactionSystemInfoDB db)
        {
            StarSystem = db.StarSystem;
            OwnedEntitesInSystem = new HashSet<Entity>(db.OwnedEntitesInSystem);
            KnownSystemBodies = new HashSet<Entity>(db.KnownSystemBodies);
        }

        public override object Clone()
        {
            return new FactionSystemInfoDB(this);
        }
    }
}
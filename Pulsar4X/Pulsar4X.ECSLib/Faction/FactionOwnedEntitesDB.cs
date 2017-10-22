using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionOwnedEntitesDB : BaseDataBlob
    {

        internal Dictionary<Guid, Entity> OwnedEntites = new Dictionary<Guid, Entity>();


        public FactionOwnedEntitesDB()
        {
        }

        FactionOwnedEntitesDB(FactionOwnedEntitesDB db)
        {
            OwnedEntites = new Dictionary<Guid, Entity>(db.OwnedEntites);
        }

        public override object Clone()
        {
            return new FactionOwnedEntitesDB(this);
        }
    }
}

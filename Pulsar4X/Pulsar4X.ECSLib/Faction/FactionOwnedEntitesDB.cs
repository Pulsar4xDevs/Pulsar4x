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

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}

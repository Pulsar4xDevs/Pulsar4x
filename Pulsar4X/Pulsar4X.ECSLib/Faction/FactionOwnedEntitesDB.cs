using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FactionOwnedEntitesDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
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

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in OwnedEntites)
            {
                hash = Misc.ValueHash(item.Key, hash);
                //hash *= Misc.ValueHash(item.Value.Guid);
            }

            return hash;
        }
    }
}

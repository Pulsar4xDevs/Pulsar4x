using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class AuthDB : BaseDataBlob
    {
        [JsonProperty]
        public string Hash { get; internal set; }

        public AuthDB()
        {
        }

        public AuthDB(string hash)
        {
            Hash = hash; ;
        }

        public AuthDB(AuthDB db)
        {
            Hash = db.Hash;
        }

        public override object Clone()
        {
            return new AuthDB(this);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.DataBlobs
{
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class BaseDataBlob
    {
        [JsonIgnore]
        public virtual EntityManager ContainingManager { get; set; }
        [JsonIgnore]
        public virtual int EntityID { get; set; }
        public virtual Guid EntityGuid { get; set; }

        [JsonIgnore]
        public readonly object LockObject = new object();
    }
}

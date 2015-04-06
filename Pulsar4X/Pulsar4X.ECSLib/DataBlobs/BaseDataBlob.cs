using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
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

using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class BaseDataBlob : ICloneable
    {
        public virtual Entity OwningEntity { get; set; }

        [JsonIgnore]
        public readonly object LockObject = new object();

        public abstract object Clone();
    }
}

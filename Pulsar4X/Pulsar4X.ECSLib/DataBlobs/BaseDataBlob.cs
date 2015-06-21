using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        [CanBeNull]
        public virtual Entity OwningEntity { get; internal set; }

        public abstract object Clone();
    }
}

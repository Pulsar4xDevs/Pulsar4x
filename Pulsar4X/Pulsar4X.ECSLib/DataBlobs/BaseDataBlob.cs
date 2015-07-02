using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        [NotNull]
        public virtual Entity OwningEntity { get; internal set; }

        public abstract object Clone();
    }
}

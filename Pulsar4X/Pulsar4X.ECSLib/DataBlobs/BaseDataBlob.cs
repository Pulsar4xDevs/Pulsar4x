using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        /// <summary>
        /// This is the Entity which Owns/Conatains/IsParentOf this datablob
        /// </summary>
        [NotNull]
        public virtual Entity OwningEntity { get; internal set; } = Entity.InvalidEntity;

        public abstract object Clone();
    }
}

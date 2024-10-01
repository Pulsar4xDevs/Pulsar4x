using Newtonsoft.Json;
using System;
using Pulsar4X.Engine;
using System.Collections.Generic;

namespace Pulsar4X.Datablobs
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        /// <summary>
        /// This is the Entity which Owns/Conatains/IsParentOf this datablob
        /// </summary>
        [NotNull]
        public virtual Entity? OwningEntity { get; internal set; } = Entity.InvalidEntity;

        /// <summary>
        /// while not what we're going for with the whole datablobs being only data, this could help make some things easier.
        /// have to be aware that it may require datablobs to be set in a given order if your overide requires a blob that's not yet on the entity
        /// </summary>
        internal virtual void OnSetToEntity()
        {
        }

        /// <summary>
        /// When this is called the entity may already be invalid
        /// </summary>
        internal virtual void OnRemovedFromEntity() { }

        // FIXME: changed this to not return null but this seems bad
        public virtual object Clone() { return new object(); }

        public static List<Type> GetDependencies() => new List<Type>();

        public bool Equals(BaseDataBlob? other)
        {
            if(other is null) return false;
            if(this.GetType() != other.GetType()) return false;

            return true;
        }
    }

    public interface IAbilityDescription
    {
        string AbilityName();
        string AbilityDescription();

    }
}

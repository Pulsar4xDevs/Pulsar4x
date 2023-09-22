using Newtonsoft.Json;
using System;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        /// <summary>
        /// This is the Entity which Owns/Conatains/IsParentOf this datablob
        /// </summary>
        [NotNull]
        public virtual Entity OwningEntity { get; internal set; } = Entity.InvalidEntity;

        /// <summary>
        /// while not what we're going for with the whole datablobs being only data, this could help make some things easier.
        /// have to be aware that it may require datablobs to be set in a given order if your overide requires a blob that's not yet on the entity
        /// </summary>
        internal virtual void OnSetToEntity()
        {
        }
        public abstract object Clone();

    }

    public interface IGetValuesHash
    {
        int GetValueCompareHash(int hash = 17);
    }

    public interface IAbilityDescription
    {
        string AbilityName();
        string AbilityDescription();

    }
}

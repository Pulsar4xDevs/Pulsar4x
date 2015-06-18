using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [JsonConverter(typeof(EntityConverter))]
    public class Entity
    {
        [PublicAPI]
        public Guid Guid { get; private set; }

        [CanBeNull]
        [PublicAPI]
        public EntityManager Manager { get; private set; }

        [NotNull]
        [PublicAPI]
        public ComparableBitArray DataBlobMask { get; private set; }

        [NotNull]
        [PublicAPI]
        public ReadOnlyCollection<BaseDataBlob> DataBlobs { get { return new ReadOnlyCollection<BaseDataBlob>(DataBlobList.Where(dataBlob => dataBlob != null).ToList()); } }
        internal readonly List<BaseDataBlob> DataBlobList;

        /// <summary>
        /// Static entity reference to an invalid entity.
        /// 
        /// Functions must never return a null entity. Instead, return InvalidEntity.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static readonly Entity InvalidEntity = new Entity();

        #region Entity Constructors

        /// <summary>
        /// Creates an unregistered entity with the optionally provided dataBlobs.
        /// </summary>
        [PublicAPI]
        public Entity(IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            Guid = Guid.Empty;
            Manager = null;
            DataBlobMask = EntityManager.BlankDataBlobMask();
            DataBlobList = EntityManager.BlankDataBlobList();

            if (dataBlobs == null)
            {
                return;
            }

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                int typeIndex;
                EntityManager.TryGetTypeIndex(dataBlob.GetType(), out typeIndex);

                SetDataBlob(dataBlob);
            }
        }

        internal Entity([NotNull] EntityManager entityManager, IEnumerable<BaseDataBlob> dataBlobs = null) : this(Guid.Empty, entityManager, dataBlobs)
        {
        }

        internal Entity(Guid entityGuid, [NotNull] EntityManager entityManager, IEnumerable<BaseDataBlob> dataBlobs = null) : this(dataBlobs)
        {
            Guid = entityGuid;
            Register(entityManager);
        }

        #endregion

        #region Public API Functions
        /// <summary>
        /// Used to determine if an entity is valid.
        /// 
        /// Entities are considered valid if they are not the static InvalidEntity and are properly registered to a manager.
        /// </summary>
        [PublicAPI]
        public bool IsValid
        {
            get { return this != InvalidEntity && Manager != null && Manager.IsValidEntity(this); }
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(int typeIndex)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [PublicAPI]
        public T GetDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return (T)DataBlobList[typeIndex];
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than directly accessing the DataBlob list.
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        [PublicAPI]
        public T GetDataBlob<T>(int typeIndex) where T : BaseDataBlob
        {
            return (T)DataBlobList[typeIndex];
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly slower than SetDataBlob(dataBlob, typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        [PublicAPI]
        public void SetDataBlob<T>([NotNull] T dataBlob) where T : BaseDataBlob
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }

            int typeIndex;
            EntityManager.TryGetTypeIndex(dataBlob.GetType(), out typeIndex);

            DataBlobList[typeIndex] = dataBlob;
            DataBlobMask[typeIndex] = true;
            dataBlob.OwningEntity = this;
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly faster than SetDataBlob(dataBlob);
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public void SetDataBlob([NotNull] BaseDataBlob dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }

            DataBlobList[typeIndex] = dataBlob;
            DataBlobMask[typeIndex] = true;
            dataBlob.OwningEntity = this;
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly slower than RemoveDataBlob(typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        [PublicAPI]
        public void RemoveDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            
            DataBlobList[typeIndex] = null;
            DataBlobMask[typeIndex] = false;
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly faster than the generic RemoveDataBlob(); function.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public void RemoveDataBlob(int typeIndex)
        {
            DataBlobList[typeIndex] = null;
            DataBlobMask[typeIndex] = false;
        }

        /// <summary>
        /// Registers this entity with the provided manager.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to register the static InvalidEntity, or if this Entity has already been registered.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the provided manager is null.</exception>
        [PublicAPI]
        public void Register([NotNull] EntityManager manager)
        {
            if (this == InvalidEntity)
            {
                throw new InvalidOperationException("Cannot register the static invalid entity.");
            }
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (Manager != null)
            {
                throw new InvalidOperationException("Cannot register an entity twice.");
            }

            Entity checkEntity;
            while (Guid == Guid.Empty || manager.FindEntityByGuid(Guid, out checkEntity))
            {
                Guid = Guid.NewGuid();
            }
            manager.Register(this);
            Manager = manager;

        }

        /// <summary>
        /// Checks if this entity has a DataBlob of type T.
        /// </summary>
        /// <typeparam name="T">Type of datablob to check for.</typeparam>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Checks if this entity has a DataBlob of the type indicated by the provided typeIndex.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when typeIndex is not a valid typeIndex.</exception>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob(int typeIndex)
        {
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Clones the entity, including datablobs.
        /// </summary>
        /// <returns>The cloned entity.</returns>
        [PublicAPI]
        public Entity Clone()
        {
            List<BaseDataBlob> clonedDataBlobs = 
                (from dataBlob in DataBlobList
                                   where dataBlob != null
                                   select (BaseDataBlob)dataBlob.Clone()).ToList();

            return new Entity(clonedDataBlobs);
        }

        /// <summary>
        /// Clones the entity, including datablobs, and registers the clone with the provided EntityManager.
        /// </summary>
        /// <returns>The cloned entity.</returns>
        [PublicAPI]
        public Entity Clone(EntityManager manager)
        {
            Entity clone = Clone();
            clone.Register(manager);
            return clone;
        }

        #endregion

        /// <summary>
        /// Simple override to display entities as their Guid.
        /// 
        /// Used mostly in debugging.
        /// </summary>
        public override string ToString()
        {
            return Guid.ToString();
        }

        /// <summary>
        /// Used to transfer an entity between managers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to transfer the static InvalidEntity.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the provided manager is null.</exception>
        internal void Transfer([NotNull] EntityManager newManager)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot transfer an invalid entity. Try registering it instead.");
            }
            if (newManager == null)
            {
                throw new ArgumentNullException("newManager");
            }
            // ReSharper disable once PossibleNullReferenceException
            // IsValid verifies that Manager is not null.
            Manager.TransferEntity(this, newManager);
        }

        /// <summary>
        /// EntityConverter responsible for deserializng Entity objects that are not part of an EntityManager.
        /// EntityManagers serialize Entities directly.
        /// </summary>
        private class EntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Entity);
            }

            /// <summary>
            /// Returns a Entity object that represents the entity.
            /// If the Entity's manager has already deserialized the entity, then the EntityManager's reference is returned.
            /// If not, then we create the entity in the global manager, and when the EntityManager containing this entity deserializes,
            /// it will transfer the entity to itself.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Entity entity;

                // Parse the Guid from the reader.
                Guid entityGuid = Guid.Parse(reader.Value.ToString());

                // Lookup the entity using a global Guid lookup.
                if (entityGuid == Guid.Empty)
                    return Entity.InvalidEntity;
                if (SaveGame.CurrentGame.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                    return entity;

                // If no entity was found, create a new entity in the global manager.
                entity = new Entity(entityGuid, SaveGame.CurrentGame.GlobalManager);
                return entity;
            }

            /// <summary>
            /// Serializes the Entity objects. Entities are serialized as simple Guids in this method.
            /// Datablobs are saved during EntityManager serialization.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Entity entity = (Entity)value;

                serializer.Serialize(writer, entity.Guid);
            }
        }
    }
}

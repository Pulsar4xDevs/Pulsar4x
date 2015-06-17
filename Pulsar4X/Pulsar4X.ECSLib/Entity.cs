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
        public Guid Guid { get; internal set; }

        [CanBeNull]
        [PublicAPI]
        public EntityManager Manager { get; private set; }

        [NotNull]
        [PublicAPI]
        public ComparableBitArray DataBlobMask { get; private set; }

        [NotNull]
        [PublicAPI]
        public ReadOnlyCollection<BaseDataBlob> DataBlobs { get { return new ReadOnlyCollection<BaseDataBlob>(DataBlobList); } }
        internal readonly List<BaseDataBlob> DataBlobList;

        [NotNull]
        [PublicAPI]
        public static Entity InvalidEntity
        {
            get
            {
                lock (LockObj)
                {
                    if (_invalidEntity == null)
                        _invalidEntity = new Entity();
                    return _invalidEntity;
                }
            }
        }
        private static Entity _invalidEntity;

        private static readonly object LockObj = new object();

        [PublicAPI]
        public Entity(IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            Guid = Guid.Empty;
            Manager = null;
            DataBlobMask = EntityManager.BlankDataBlobMask();

            DataBlobList = new List<BaseDataBlob>(EntityManager.DataBlobTypes.Count);

            for (int i = 0; i < EntityManager.DataBlobTypes.Count; i++)
            {
                DataBlobList.Add(null);
            }

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

        [PublicAPI]
        public void SetDataBlob<T>([NotNull] T dataBlob) where T : BaseDataBlob
        {
            int typeIndex;
            EntityManager.TryGetTypeIndex(dataBlob.GetType(), out typeIndex);

            SetDataBlob(dataBlob, typeIndex);
        }

        [PublicAPI]
        public void SetDataBlob([NotNull] BaseDataBlob dataBlob, int typeIndex)
        {
            DataBlobList[typeIndex] = dataBlob;
            DataBlobMask[typeIndex] = true;
            dataBlob.OwningEntity = this;
        }

        [PublicAPI]
        public void RemoveDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            
            DataBlobList[typeIndex] = null;
            DataBlobMask[typeIndex] = false;
        }

        [PublicAPI]
        public void RemoveDataBlob(int typeIndex)
        {
            DataBlobList[typeIndex] = null;
            DataBlobMask[typeIndex] = false;
        }

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

        public override string ToString()
        {
            return Guid.ToString();
        }

        [PublicAPI]
        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return DataBlobMask[typeIndex];
        }

        [PublicAPI]
        public Entity Clone()
        {
            List<BaseDataBlob> clonedDataBlobs = 
                (from dataBlob in DataBlobList
                                   where dataBlob != null
                                   select (BaseDataBlob)dataBlob.Clone()).ToList();

            return new Entity(clonedDataBlobs);
        }

        [PublicAPI]
        public Entity Clone(EntityManager manager)
        {
            Entity clone = Clone();
            clone.Register(manager);
            return clone;
        }
    }

    internal class EntityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Entity);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Entity entity;
            Guid entityGuid = Guid.Parse(reader.Value.ToString());
     
            if (entityGuid == Guid.Empty)
                return Entity.InvalidEntity;
            if (SaveGame.CurrentGame.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                return entity;

            entity = new Entity(entityGuid, SaveGame.CurrentGame.GlobalManager);
            return entity;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Entity entity = (Entity)value;
            serializer.Serialize(writer, entity.Guid);
        }
    }
}

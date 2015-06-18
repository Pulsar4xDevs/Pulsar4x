using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class GuidNotFoundException : Exception
    {
    }

    [JsonConverter(typeof(EntityManagerConverter))]
    public class EntityManager
    {
        private class EntityManagerConverter : JsonConverter
        {
            /// <summary>
            /// Serializes the EntityManager by manually serializing the Entities in the _localEntityDictionary.
            /// Manual entity serialization is done because the Entity class has a JsonConverter to serialize Entities as Guids only.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                EntityManager manager = (EntityManager)value;
                writer.WriteStartObject(); // Start of our EntityManager object.
                writer.WritePropertyName("Entities"); // Write Entities list PropertyName
                writer.WriteStartArray(); // Start the Entities array.
                foreach (Entity entity in manager._localEntityDictionary.Values)
                {
                    writer.WriteStartObject(); // Start the Entity.
                    writer.WritePropertyName("Guid"); // Write the Guid PropertyName
                    serializer.Serialize(writer, entity.Guid); // Write the Entity's guid.

                    foreach (BaseDataBlob dataBlob in entity.DataBlobList.Where(dataBlob => dataBlob != null))
                    {
                        writer.WritePropertyName(dataBlob.GetType().Name); // Write the PropertyName of the dataBlob as the dataBlob's type.
                        serializer.Serialize(writer, dataBlob); // Serialize the dataBlob in this property.
                    }
                    writer.WriteEndObject(); // End then Entity.
                }
                writer.WriteEndArray(); // End the Entities array.
                writer.WriteEndObject(); // End the EntityManager object.
            }

            /// <summary>
            /// Reconstructs the EntityManager from the JsonRead.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                EntityManager manager = new EntityManager(SaveGame.CurrentGame);
                reader.Read(); // PropertyName Entities
                reader.Read(); // StartArray
                reader.Read(); // StartObject OR EndArray
                while (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read(); // PropertyName Guid
                    reader.Read(); // ACTUAL GUID
                    Guid entityGuid = serializer.Deserialize<Guid>(reader); // Deserialize the Guid

                    // Attempt a global Guid lookup of the Guid.
                    Entity entity;
                    if (manager.FindEntityByGuid(entityGuid, out entity))
                    {
                        // An Entity reference for this Guid was previously created during deserialization.
                        // Transfer the Entity to this manager, and populate it's dataBlobs.
                        entity.Transfer(manager);
                    }
                    else
                    {
                        // No previous entity found. Create our own with the Guid and register it to this manager.
                        entity = new Entity(entityGuid, manager);
                    }

                    reader.Read(); // PropertyName DATABLOB
                    while (reader.TokenType == JsonToken.PropertyName)
                    {
                        Type dataBlobType = Type.GetType("Pulsar4X.ECSLib." +(string)reader.Value);
                        reader.Read(); // StartObject
                        BaseDataBlob dataBlob = (BaseDataBlob)serializer.Deserialize(reader, dataBlobType);
                        entity.SetDataBlob(dataBlob);

                        reader.Read(); // PropertyName OR EndObject
                    }
                    reader.Read(); //StartObject OR EndArray
                }
                reader.Read(); //EndObject

                return manager;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(EntityManager);
            }
        }

        internal static Dictionary<Type, int> DataBlobTypes;

        private readonly Game _game;
        [JsonProperty]
        private readonly Dictionary<Guid, Entity> _localEntityDictionary = new Dictionary<Guid, Entity>();
        private readonly object _entityLock = new object();

        /// <summary>
        /// Lock to ensure only one transfer happens at a time.
        /// Two concurrent transfers can create a deadlock.
        /// 
        /// Note: This lock is static and shared between all EntityManagers,
        /// regardless of what game they are in. Transfers should be fast and rare
        /// enought that this lock should not adversly affect other games.
        /// </summary>
        private static readonly object TransferLock = new object();

        internal EntityManager(Game game)
        {
            // Initialize our static variables.
            if (DataBlobTypes == null)
            {
                DataBlobTypes = new Dictionary<Type, int>();

                int i = 0;
                // Use reflection to Find all types that implement BaseDataBlob
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract))
                {
                    DataBlobTypes.Add(type, i);
                    i++;
                }
            }
            _game = game;
        }

        private EntityManager()
        { }

        /// <summary>
        /// Verifies that the supplied entity is valid in this manager.
        /// </summary>
        /// <returns>True is the entity is considered valid.</returns>
        internal bool IsValidEntity([NotNull] Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            lock (_entityLock)
            {
                return entity.Manager == this && _localEntityDictionary.ContainsKey(entity.Guid);
            }
        }

        /// <summary>
        /// Returns a list of entities that have datablob type T.
        /// <para></para>
        /// Returns a blank list if no entities have that datablob.
        /// <para></para>
        /// DO NOT ASSUME THE ORDER OF THE RETURNED LIST!
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        [NotNull]
        [PublicAPI]
        public List<Entity> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndex] = true;

            return GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        /// <summary>
        /// Returns a list of entities that contain all dataBlobs defined by the dataBlobMask.
        /// <para></para>
        /// Returns a blank list if no entities have all needed DataBlobs
        /// <para></para>
        /// DO NOT ASSUME THE ORDER OF THE RETURNED LIST!
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobMask is null.</exception>
        /// <exception cref="ArgumentException">Thrown when passed a malformed (incorrect length) dataBlobMask.</exception>
        [NotNull]
        [PublicAPI]
        public List<Entity> GetAllEntitiesWithDataBlobs([NotNull] ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask == null)
            {
                throw new ArgumentNullException("dataBlobMask");
            }

            if (dataBlobMask.Length != DataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<Entity>();

            lock (_entityLock)
            {
                entities.AddRange(_localEntityDictionary.Values.Where(entity => (entity.DataBlobMask & dataBlobMask) == dataBlobMask));

                return entities;
            }
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        [NotNull]
        [PublicAPI]
        public Entity GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(GetTypeIndex<T>());
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when passed an invalid typeIndex</exception>
        [NotNull]
        [PublicAPI]
        public Entity GetFirstEntityWithDataBlob(int typeIndex)
        {
            lock (_entityLock)
            {
                foreach (Entity entity in _localEntityDictionary.Values)
                {
                    if (entity.DataBlobMask.SetBits.Contains(typeIndex))
                    {
                        return entity;
                    }
                }
            }
            return Entity.InvalidEntity;
        }

        /// <summary>
        /// Returns a blank DataBlob mask with the correct number of entries.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static ComparableBitArray BlankDataBlobMask()
        {
            return new ComparableBitArray(DataBlobTypes.Count);
        }

        /// <summary>
        /// Returns a blank list used for storing datablobs by typeIndex.
        /// </summary>
        /// <returns></returns>
        internal static List<BaseDataBlob> BlankDataBlobList()
        {
            var blankList = new List<BaseDataBlob>(DataBlobTypes.Count);
            for (int i = 0; i < DataBlobTypes.Count; i++)
            {
                blankList.Add(null);
            }
            return blankList;
        }

        /// <summary>
        /// Transfers an entity to the specified manager.
        /// 
        /// NOT FOR USE OUTSIDE ENTITY.CS
        /// 
        /// Use entity.Transfer(newManager);
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        internal void TransferEntity([NotNull] Entity entity, [NotNull] EntityManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Entity is not valid in this manager.");
            }

            // Since we lock both our entity lock, and the other manager's entity lock,
            // we must prevent deadlocks. This is the only place where we take out two locks,
            // so we take out this static lock first.
            lock (TransferLock)
            {
                lock (_entityLock)
                {
                    lock (manager._entityLock)
                    {
                        _game.GuidDictionaryLock.EnterWriteLock();
                        try
                        {
                            _localEntityDictionary.Remove(entity.Guid);
                            manager._localEntityDictionary.Add(entity.Guid, entity);
                            _game.GlobalGuidDictionary[entity.Guid] = manager;
                        }
                        finally
                        {
                            _game.GuidDictionaryLock.ExitWriteLock();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find the entity with the associated Guid.
        /// </summary>
        /// <returns>True if entityID is found.</returns>
        [PublicAPI]
        public bool FindEntityByGuid(Guid entityGuid, out Entity entity)
        {
            _game.GuidDictionaryLock.EnterReadLock();
            try
            {
                EntityManager manager;

                if (!_game.GlobalGuidDictionary.TryGetValue(entityGuid, out manager))
                {
                    entity = Entity.InvalidEntity;
                    return false;
                }

                if (!manager._localEntityDictionary.TryGetValue(entityGuid, out entity))
                {
                    // Can only be reached if memory corruption or somehow the _guidLock thread synchronization fails.
                    // Entity must be removed from the local manager, but not the global list. Should not be possible.
                    throw new GuidNotFoundException();
                }
            }
            finally
            {
                _game.GuidDictionaryLock.ExitReadLock();
            }
            return true;
        }

        /// <summary>
        /// Gets the associated entityID of the specified Guid.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        [PublicAPI]
        public bool TryGetEntityByGuid(Guid entityGuid, out Entity entity)
        {
            lock (_entityLock)
            {
                _game.GuidDictionaryLock.EnterReadLock();
                try
                {
                    if (_localEntityDictionary.TryGetValue(entityGuid, out entity))
                    {
                        return true;
                    }
                    entity = Entity.InvalidEntity;
                    return false;
                }
                finally
                {
                    _game.GuidDictionaryLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Returns the true if the specified type is a valid DataBlobType.
        /// <para></para>
        /// typeIndex parameter is set to the typeIndex of the dataBlobType if found.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobType is null.</exception>
        [PublicAPI]
        public static bool TryGetTypeIndex(Type dataBlobType, out int typeIndex)
        {
            return DataBlobTypes.TryGetValue(dataBlobType, out typeIndex);
        }

        /// <summary>
        /// Faster than TryGetDataBlobTypeIndex and uses generics for type safety.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob, or is Abstract</exception>
        [PublicAPI]
        public static int GetTypeIndex<T>() where T : BaseDataBlob
        {
            return DataBlobTypes[typeof(T)];
        }

        public void Register(Entity entity)
        {
            lock (_entityLock)
            {
                _localEntityDictionary.Add(entity.Guid, entity);
                _game.GuidDictionaryLock.EnterWriteLock();
                try
                {
                    _game.GlobalGuidDictionary.Add(entity.Guid, this);
                }
                finally
                {
                    _game.GuidDictionaryLock.ExitWriteLock();
                }
            }
        }
    }
}
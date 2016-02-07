using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    [PublicAPI]
    public class GuidNotFoundException : Exception
    {
        [PublicAPI]
        public Guid MissingGuid { get; private set; }

        [PublicAPI]
        public GuidNotFoundException(Guid missingGuid)
        {
            MissingGuid = missingGuid;
        }
    }

    public class EntityManager : ISerializable
    {
        [CanBeNull]
        private readonly Game _game;
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly List<List<BaseDataBlob>> _dataBlobMap = new List<List<BaseDataBlob>>();
        private readonly Dictionary<Guid, Entity> _localEntityDictionary = new Dictionary<Guid, Entity>();

        private int _nextID;

        internal readonly List<ComparableBitArray> EntityMasks = new List<ComparableBitArray>();

        private static readonly Dictionary<Type, int> InternalDataBlobTypes = InitializeDataBlobTypes();
        [PublicAPI]
        public static ReadOnlyDictionary<Type, int> DataBlobTypes = new ReadOnlyDictionary<Type, int>(InternalDataBlobTypes);

        [PublicAPI]
        public ReadOnlyCollection<Entity> Entities => new ReadOnlyCollection<Entity>(_entities);

        #region Constructors

        internal EntityManager(Game game)
        {
            _game = game;

            for (int i = 0; i < DataBlobTypes.Keys.Count; i++)
            {
                _dataBlobMap.Add(new List<BaseDataBlob>());
            }
        }

        private static Dictionary<Type, int> InitializeDataBlobTypes()
        {
            var dbTypes = new Dictionary<Type, int>();

            int i = 0;
            // Use reflection to Find all types that implement BaseDataBlob
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract))
            {
                dbTypes.Add(type, i);
                i++;
            }
            return dbTypes;
        }

        #endregion

        #region Entity Management Functions

        internal int SetupEntity(Entity entity)
        {
            // Find an entity slot.
            int entityID;

            for (entityID = _nextID; entityID < _entities.Count; entityID++)
            {
                if (_entities[entityID] == null)
                {
                    break;
                }
            }
            _nextID = entityID + 1;

            if (entityID == _entities.Count)
            {
                _entities.Add(entity);
                EntityMasks.Add(BlankDataBlobMask());
                foreach (List<BaseDataBlob> dataBlobList in _dataBlobMap)
                {
                    dataBlobList.Add(null);
                }
            }
            else
            {
                _entities[entityID] = entity;
                EntityMasks[entityID] = BlankDataBlobMask();
                foreach (List<BaseDataBlob> dataBlobList in _dataBlobMap)
                {
                    dataBlobList[entityID] = null;
                }
            }

            // Setup the global dictionary.
            if (_game != null)
            {
                _game.GuidDictionaryLock.EnterWriteLock();
                try
                {
                    _game.GlobalGuidDictionary.Add(entity.Guid, this);
                    _localEntityDictionary.Add(entity.Guid, entity);
                }
                finally
                {
                    _game.GuidDictionaryLock.ExitWriteLock();
                }
            }
            else
            {
                // THis is a "fake" manager, that does not link to other managers.
                _localEntityDictionary.Add(entity.Guid, entity);
            }

            return entityID;
        }

        /// <summary>
        /// Verifies that the supplied entity is valid in this manager.
        /// </summary>
        /// <returns>True is the entity is considered valid.</returns>
        internal bool IsValidEntity([CanBeNull] Entity entity)
        {
            if (entity == null)
            {
                return false;
            }

            return IsValidID(entity.ID) && _entities[entity.ID] == entity;
        }

        private bool IsValidID(int entityID)
        {
            return entityID >= 0 && entityID < _entities.Count;
        }

        internal void RemoveEntity(Entity entity)
        {
            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Provided Entity is not valid in this manager.");
            }
            int entityID = entity.ID;
            _entities[entityID] = null;
            EntityMasks[entityID] = null;

            _nextID = entityID;

            for (int i = 0; i < DataBlobTypes.Count; i++)
            {
                if (_dataBlobMap[i][entityID] != null)
                {
                    _dataBlobMap[i][entityID].OwningEntity = Entity.InvalidEntity;
                    _dataBlobMap[i][entityID] = null;
                }
            }

            if (_game != null)
            {
                _game.GuidDictionaryLock.EnterWriteLock();
                try
                {
                    _localEntityDictionary.Remove(entity.Guid);
                    _game.GlobalGuidDictionary.Remove(entity.Guid);
                }
                finally
                {
                    _game.GuidDictionaryLock.ExitWriteLock();
                }
            }
            else
            {
                // This is a "fake" manager that does not link to other managers.
                _localEntityDictionary.Remove(entity.Guid);
            }
        }

        internal List<BaseDataBlob> GetAllDataBlobs(int id)
        {
            var dataBlobs = new List<BaseDataBlob>();
            for (int i = 0; i < DataBlobTypes.Count; i++)
            {
                BaseDataBlob dataBlob = _dataBlobMap[i][id];
                if (dataBlob != null)
                {
                    dataBlobs.Add(dataBlob);
                }
            }

            return dataBlobs;
        }

        internal T GetDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[GetTypeIndex<T>()][entityID];
        }

        internal T GetDataBlob<T>(int entityID, int typeIndex) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[typeIndex][entityID];
        }

        internal void SetDataBlob(int entityID, BaseDataBlob dataBlob)
        {
            int typeIndex;
            TryGetTypeIndex(dataBlob.GetType(), out typeIndex);

            _dataBlobMap[typeIndex][entityID] = dataBlob;
            EntityMasks[entityID][typeIndex] = true;
            dataBlob.OwningEntity = _entities[entityID];
        }

        internal void SetDataBlob(int entityID, BaseDataBlob dataBlob, int typeIndex)
        {
            _dataBlobMap[typeIndex][entityID] = dataBlob;
            EntityMasks[entityID][typeIndex] = true;
            dataBlob.OwningEntity = _entities[entityID];
        }

        internal void RemoveDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();
            _dataBlobMap[typeIndex][entityID].OwningEntity = null;
            _dataBlobMap[typeIndex][entityID] = null;
            EntityMasks[entityID][typeIndex] = false;
        }

        internal void RemoveDataBlob(int entityID, int typeIndex)
        {
            _dataBlobMap[typeIndex][entityID].OwningEntity = null;
            _dataBlobMap[typeIndex][entityID] = null;
            EntityMasks[entityID][typeIndex] = false;
        }

        #endregion

        #region Public API Functions

        [PublicAPI]
        public void ExportEntity([NotNull] Entity entity, [NotNull] Stream outputStream, bool compress = false)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (!IsValidEntity(entity))
            {
                throw new InvalidOperationException("This EntityManager cannot serialize this entity. Entity not found in this manager.");
            }

            var DefaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.None };
            DefaultSerializer.Formatting = compress ? Formatting.None : Formatting.Indented;

            using (var intermediateStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(intermediateStream, Encoding.UTF8, 1024, true))
                {
                    using (JsonWriter writer = new JsonTextWriter(streamWriter))
                    {
                        DefaultSerializer.Serialize(writer, entity.Clone());
                    }
                }

                // Reset the MemoryStream's position to 0. CopyTo copies from Position to the end.
                intermediateStream.Position = 0;

                if (compress)
                {
                    using (var compressionStream = new GZipStream(outputStream, CompressionLevel.Optimal))
                    {
                        intermediateStream.CopyTo(compressionStream);
                    }
                }
                else
                {
                    intermediateStream.CopyTo(outputStream);
                }
            }
        }

        public Entity ImportEntity(MemoryStream inputMemoryStream)
        {
            string jsonString;
            var entity = new ProtoEntity();

            // Check if our stream is compressed.
            using (var bufferedStream = new BufferedStream(inputMemoryStream))
            {
                if (SaveGame.HasGZipHeader(bufferedStream))
                {
                    // File is compressed. Decompress using GZip.
                    using (GZipStream compressionStream = new GZipStream(bufferedStream, CompressionMode.Decompress))
                    {
                        // Decompress into a MemoryStream.
                        using (MemoryStream intermediateStream = new MemoryStream())
                        {
                            // Decompress the file into an intermediate MemoryStream.
                            compressionStream.CopyTo(intermediateStream);

                            // Reset the position of the MemoryStream so it can be read from the beginning.
                            intermediateStream.Position = 0;

                            entity = PopulateEntity(intermediateStream);
                        }
                    }
                }
                else
                {
                    entity = PopulateEntity(bufferedStream);
                }
            }
            return Entity.Create(this, entity);
        }

        private ProtoEntity PopulateEntity(Stream stream)
        {
            // Populate the game from the uncompressed MemoryStream.
            JsonSerializer DefaultSerializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented, ContractResolver = new ForceUseISerializable(), PreserveReferencesHandling = PreserveReferencesHandling.Objects };

            using (StreamReader sr = new StreamReader(stream))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    return DefaultSerializer.Deserialize<ProtoEntity>(reader);
                }
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
                throw new ArgumentNullException(nameof(dataBlobMask));
            }

            if (dataBlobMask.Length != DataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<Entity>();

            entities.AddRange(_localEntityDictionary.Values.Where(entity => (entity.DataBlobMask & dataBlobMask) == dataBlobMask));

            return entities;
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
        [NotNull]
        [PublicAPI]
        public Entity GetFirstEntityWithDataBlob(int typeIndex)
        {
            foreach (Entity entity in _entities)
            {
                if (IsValidEntity(entity) && entity.DataBlobMask.SetBits.Contains(typeIndex))
                {
                    return entity;
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
        [PublicAPI]
        public static List<BaseDataBlob> BlankDataBlobList()
        {
            var blankList = new List<BaseDataBlob>(DataBlobTypes.Count);
            for (int i = 0; i < DataBlobTypes.Count; i++)
            {
                blankList.Add(null);
            }
            return blankList;
        }

        /// <summary>
        /// Attempts to find the entity with the associated Guid.
        /// </summary>
        /// <returns>True if entityID is found.</returns>
        /// <exception cref="GuidNotFoundException">Guid was found in Global list, but not locally. Should not be possible.</exception>
        [PublicAPI]
        public bool FindEntityByGuid(Guid entityGuid, out Entity entity)
        {
            if (_game == null)
            {
                // This is a "fake" manager not connected to other managers.
                // This manager can only perform local Guid lookups.
                return _localEntityDictionary.TryGetValue(entityGuid, out entity);
            }
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
                    throw new GuidNotFoundException(entityGuid);
                }
                return true;
            }
            finally
            {
                _game.GuidDictionaryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the entity with the associated Guid. this version doesn't use out. 
        /// </summary>
        /// <returns>The Entity if found</returns>
        /// <exception cref="GuidNotFoundException">Guid was not found in Global list, orlocally</exception>
        [PublicAPI]
        public Entity GetEntityByGuid(Guid entityGuid)
        {
            Entity entity;
            if (_game != null)
            {
                if (_localEntityDictionary.TryGetValue(entityGuid, out entity))
                {
                    return entity;
                }
                if (_game.GlobalGuidDictionary.ContainsKey(entityGuid))
                {
                    return _game.GlobalGuidDictionary[entityGuid].GetEntityByGuid(entityGuid);
                }
                throw new GuidNotFoundException(entityGuid);
            }
            // This is a "fake" manager that does not link to other managers.
            if (_localEntityDictionary.TryGetValue(entityGuid, out entity))
            {
                return entity;
            }
            throw new GuidNotFoundException(entityGuid);
        }

        /// <summary>
        /// Gets the associated entityID of the specified Guid. (this manager only, not global)
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        [PublicAPI]
        public bool TryGetEntityByGuid(Guid entityGuid, out Entity entity)
        {
            if (_game != null)
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
            // This is a "fake" manager that does not link to other managers.
            if (_localEntityDictionary.TryGetValue(entityGuid, out entity))
            {
                return true;
            }
            entity = Entity.InvalidEntity;
            return false;
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

        #endregion

        #region ISerializable interface

        // ReSharper disable once UnusedParameter.Local
        public EntityManager(SerializationInfo info, StreamingContext context) : this(SaveGame.CurrentGame)
        {
            var entities = (List<ProtoEntity>)info.GetValue("Entities", typeof(List<ProtoEntity>));

            foreach (ProtoEntity protoEntity in entities)
            {
                Entity entity;
                if (FindEntityByGuid(protoEntity.Guid, out entity))
                {
                    // Entity has already been deserialized as a reference. It currently exists on the global manager.
                    entity.Transfer(this);
                    foreach (BaseDataBlob dataBlob in protoEntity.DataBlobs.Where(dataBlob => dataBlob != null))
                    {
                        entity.SetDataBlob(dataBlob);
                    }
                }
                else
                {
                    // Entity has not been previously deserialized.
                    Entity.Create(this, protoEntity);
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            List<ProtoEntity> storedEntities = (from entity in _entities
                                                where entity != null
                                                select entity.Clone()).ToList();

            info.AddValue("Entities", storedEntities);
        }

        /// <summary>
        /// OnSerialized callback, called by the JSON serializer. Used to report saving progress back to the application.
        /// </summary>
        /// <param name="context"></param>
        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Fake managers cannot be serialized.");
            }

            SaveGame.ManagersProcessed++;
            SaveGame.Progress?.Report((double)SaveGame.ManagersProcessed / (_game.NumSystems + 1));
        }

        /// <summary>
        /// OnDeserialized callback, called by the JSON loader. Used to report loading progress back to the application.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Fake managers cannot be deserialized.");
            }

            SaveGame.ManagersProcessed++;
            SaveGame.Progress?.Report((double)SaveGame.ManagersProcessed / (_game.NumSystems + 1));
        }

        #endregion
    }
}
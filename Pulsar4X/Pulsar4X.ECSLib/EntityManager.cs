using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class GuidNotFoundException : Exception
    {
        public GuidNotFoundException()
        {

        }

        public GuidNotFoundException(string message)
            : base(message)
        {

        }

        public GuidNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public class EntityManager : ISerializable
    {
        private static Dictionary<Type, int> _dataBlobTypes;
        private static Dictionary<Guid, EntityManager> _globalGuidDictionary;
        private static ReaderWriterLockSlim _guidLock;

        private readonly List<List<BaseDataBlob>> _dataBlobMap;
        private readonly List<int> _entities;
        private readonly List<ComparableBitArray> _entityMasks;
        private readonly Dictionary<Guid, int> _localGuidDictionary;
        private readonly List<Guid> _localGuids;

        public EntityManager()
        {
            // Initialize our static variables.
            if (_dataBlobTypes == null)
            {
                _dataBlobTypes = new Dictionary<Type, int>();

                int i = 0;
                // Use reflection to setup all our dataBlobMap.
                // Find all types that implement BaseDataBlob
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract))
                {
                    _dataBlobTypes.Add(type, i);
                    i++;
                }

                _globalGuidDictionary = new Dictionary<Guid, EntityManager>();
                _guidLock = new ReaderWriterLockSlim();
            }

            // Initialize our instance variables.
            _dataBlobMap = new List<List<BaseDataBlob>>(_dataBlobTypes.Count);
            _entities = new List<int>();
            _entityMasks = new List<ComparableBitArray>();
            _localGuidDictionary = new Dictionary<Guid, int>();
            _localGuids = new List<Guid>();

            // Fill out the first level of the datablob map.
            for (int i = 0; i < _dataBlobTypes.Count; i++)
            {
                _dataBlobMap.Add(new List<BaseDataBlob>());
            }
            Clear();
        }

        /// <summary>
        /// Verifies that the supplied entityID is valid in this manager.
        /// </summary>
        /// <returns>True is the entityID is considered valid.</returns>
        public bool IsValidEntity(int entityID)
        {
            if (entityID < 0 || entityID >= _entities.Count)
            {
                return false;
            }
            return _entities[entityID] == entityID;
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(entityID, typeIndex)
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid entityID is passed.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public T GetDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();
            return GetDataBlob<T>(entityID, typeIndex);
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Fastest direct lookup available.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        /// <exception cref="InvalidCastException">Thrown when typeIndex does not match m_dataBlobTypes entry for Type T</exception>
        public T GetDataBlob<T>(int entityID, int typeIndex) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[typeIndex][entityID];
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid entityID is passed.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public void SetDataBlob<T>(int entityID, T dataBlob) where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();
            SetDataBlob(entityID, dataBlob, typeIndex);
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        private void SetDataBlob(int entityID, BaseDataBlob dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Do not use SetDataBlob to remove a datablob. Use RemoveDataBlob.");
            }

            _dataBlobMap[typeIndex][entityID] = dataBlob;
            _entityMasks[entityID][typeIndex] = true;

            dataBlob.EntityID = entityID;
            dataBlob.EntityGuid = _localGuids[entityID];
            dataBlob.ContainingManager = this;
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Slower than RemoveDataBlob(entityID, typeIndex).
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid entityID is passed.</exception>
        public void RemoveDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();
            RemoveDataBlob(entityID, typeIndex);
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Fastest DataBlob removal available.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        public void RemoveDataBlob(int entityID, int typeIndex)
        {
            if (!IsValidEntity(entityID))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            _dataBlobMap[typeIndex][entityID] = null;
            _entityMasks[entityID][typeIndex] = false;
        }

        /// <summary>
        /// Returns a list of all DataBlobs with type T.
        /// <para></para>
        /// Returns a blank list if no DataBlobs of type T found.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public List<T> GetAllDataBlobsOfType<T>() where T : BaseDataBlob
        {
            var dataBlobs = new List<T>();
            foreach (BaseDataBlob dataBlob in _dataBlobMap[GetTypeIndex<T>()])
            {
                if (dataBlob != null)
                {
                    dataBlobs.Add((T)dataBlob);
                }
            }

            return dataBlobs;
        }

        /// <summary>
        /// Returns a list of all DataBlobs for a given entity.
        /// <para></para>
        /// Returns a blank list if entityID has no DataBlobs.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        public List<BaseDataBlob> GetAllDataBlobsOfEntity(int entityID)
        {
            if (!IsValidEntity(entityID))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            var entityDBs = new List<BaseDataBlob>();
            ComparableBitArray entityMask = _entityMasks[entityID];

            for (int typeIndex = 0; typeIndex < _dataBlobTypes.Count; typeIndex++)
            {
                if (entityMask[typeIndex])
                {
                    entityDBs.Add(GetDataBlob<BaseDataBlob>(entityID, typeIndex));
                }
            }

            return entityDBs;
        }

        /// <summary>
        /// Returns a list of entityID id's for entities that have datablob type T.
        /// <para></para>
        /// Returns a blank list if no DataBlobs of type T exist.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public List<int> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndex] = true;

            return GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        /// <summary>
        /// Returns a list of entityID id's for entities that contain all dataBlobs defined by
        /// the dataBlobMask.
        /// <para></para>
        /// Returns a blank list if no entities have all needed DataBlobs
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed a malformed (incorrect length) dataBlobMask.</exception>
        /// <exception cref="NullReferenceException">Thrown when dataBlobMask is null.</exception>
        public List<int> GetAllEntitiesWithDataBlobs(ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask.Length != _dataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<int>();

            for (int entityID = 0; entityID < _entityMasks.Count; entityID++)
            {
                if ((_entityMasks[entityID] & dataBlobMask) == dataBlobMask)
                {
                    entities.Add(entityID);
                }
            }

            return entities;
        }

        /// <summary>
        /// Optimized convenience function to get entities that contain two types of DataBlobs, along with the associated DataBlobs.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Tuple<T1, T2>> GetEntitiesAndDataBlobs<T1, T2>()
            where T1 : BaseDataBlob
            where T2 : BaseDataBlob
        {
            int typeIndexT1 = GetTypeIndex<T1>();
            int typeIndexT2 = GetTypeIndex<T2>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndexT1] = true;
            dataBlobMask[typeIndexT2] = true;

            List<int> entities = GetAllEntitiesWithDataBlobs(dataBlobMask);

            var entitiesAndDataBlobs = new Dictionary<int, Tuple<T1, T2>>();

            foreach (int entityID in entities)
            {
                T1 dataBlobT1 = (T1)_dataBlobMap[typeIndexT1][entityID];
                T2 dataBlobT2 = (T2)_dataBlobMap[typeIndexT2][entityID];

                var dataBlobs = new Tuple<T1, T2>(dataBlobT1, dataBlobT2);

                entitiesAndDataBlobs.Add(entityID, dataBlobs);
            }

            return entitiesAndDataBlobs;
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public int GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(GetTypeIndex<T>());
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns -1 if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when passed an invalid typeIndex</exception>
        public int GetFirstEntityWithDataBlob(int typeIndex)
        {
            List<BaseDataBlob> dataBlobType = _dataBlobMap[typeIndex];
            for (int i = 0; i < _entities.Count; i++)
            {
                if (dataBlobType[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns a blank DataBlob mask with the correct number of entries.
        /// </summary>
        public static ComparableBitArray BlankDataBlobMask()
        {
            return new ComparableBitArray(_dataBlobTypes.Count);
        }

        /// <summary>
        /// Creates an entityID with an entityID slot.
        /// </summary>
        /// <returns>entityID ID of the new entity.</returns>
        public int CreateEntity()
        {
            _guidLock.EnterWriteLock();
            try
            {
                return CreateEntity(Guid.NewGuid());
            }
            finally
            {
                _guidLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds an entityID with the pre-existing datablobs to this EntityManager.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobs is null.</exception>
        public int CreateEntity(List<BaseDataBlob> dataBlobs)
        {
            _guidLock.EnterWriteLock();
            try
            {
                return CreateEntity(dataBlobs, Guid.NewGuid());
            }
            finally
            {
                _guidLock.ExitWriteLock();
            }
        }

        private int CreateEntity(List<BaseDataBlob> dataBlobs, Guid entityGuid)
        {
            if (dataBlobs == null)
            {
                throw new ArgumentNullException("dataBlobs", "dataBlobs cannot be null. To create a blank entityID use CreateEntity().");
            }

            int entityID = CreateEntity(entityGuid);

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                int typeIndex;
                TryGetTypeIndex(dataBlob.GetType(), out typeIndex);
                SetDataBlob(entityID, dataBlob, typeIndex);
            }

            return entityID;
        }

        private int CreateEntity(Guid entityGuid)
        {
            int entityID;
            for (entityID = 0; entityID < _entities.Count; entityID++)
            {
                if (entityID != _entities[entityID])
                {
                    // Space open.
                    break;
                }
            }

            // Mark space claimed by making the index match the value.
            // Entities[7] == 7; on claimed spot.
            // Entities[7] == -1; on unclaimed spot.
            if (entityID == _entities.Count)
            {
                _entities.Add(entityID);
                _localGuids.Add(entityGuid);

                _entityMasks.Add(new ComparableBitArray(_dataBlobTypes.Count));

                // Make sure the entityDBMaps have enough space for this entity.
                foreach (List<BaseDataBlob> entityDBMap in _dataBlobMap)
                {
                    entityDBMap.Add(null);
                }
            }
            else
            {
                _entities[entityID] = entityID;
                _localGuids[entityID] = entityGuid;

                _entityMasks[entityID] = new ComparableBitArray(_dataBlobTypes.Count);

                // Make sure the EntityDBMaps are null for this entity.
                // This should be done by RemoveentityID, but let's just be safe.
                for (int typeIndex = 0; typeIndex < _dataBlobTypes.Count; typeIndex++)
                {
                    _dataBlobMap[typeIndex][entityID] = null;
                }

            }

            // Add the GUID to the lookup lists.
            _globalGuidDictionary.Add(entityGuid, this);
            _localGuidDictionary.Add(entityGuid, entityID);
            return entityID;
        }

        /// <summary>
        /// Removes this entityID from this entityID manager.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        public void RemoveEntity(int entityID)
        {
            // Make sure we only attempt to remove valid entities.
            if (!IsValidEntity(entityID))
            {
                throw new ArgumentException("Invalid Entity.");
            }

            _guidLock.EnterWriteLock();
            try
            {
                Guid entityGuid = _localGuids[entityID];
                RemoveEntity(entityID, entityGuid);
            }
            finally
            {
                _guidLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the entityID from this entityID manager.
        /// </summary>
        private void RemoveEntity(int entityID, Guid entityGuid)
        {
            // Remove the GUID from all lists.
            _globalGuidDictionary.Remove(entityGuid);
            _localGuidDictionary.Remove(entityGuid);

            // Mark the entityID as invalid.
            _entities[entityID] = -1;
            _localGuids[entityID] = Guid.Empty;

            // Destroy references to datablobs.
            foreach (List<BaseDataBlob> dataBlobType in _dataBlobMap)
            {
                dataBlobType[entityID] = null;
            }

            _entityMasks[entityID] = BlankDataBlobMask();
        }

        /// <summary>
        /// Transfers an entityID to the specified manager.
        /// </summary>
        /// <returns>New entityID in new manager.</returns>
        /// <exception cref="ArgumentException">Thrown when passed an invalid entity.</exception>
        public int TransferEntity(int entityID, EntityManager manager)
        {
            List<BaseDataBlob> dataBlobs = GetAllDataBlobsOfEntity(entityID);

            _guidLock.EnterWriteLock();
            try
            {
                Guid entityGuid = _localGuids[entityID];
                RemoveEntity(entityID, entityGuid);
                return manager.CreateEntity(dataBlobs, entityGuid);
            }
            finally
            {
                _guidLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the associated EntityManager and entityID of the specified Guid.
        /// </summary>
        /// <returns>True if entityID is found.</returns>
        public static bool FindEntityByGuid(Guid entityGuid, out EntityManager manager, out int entityID)
        {
            entityID = -1;

            _guidLock.EnterReadLock();

            try
            {
                if (!_globalGuidDictionary.TryGetValue(entityGuid, out manager))
                {
                    return false;
                }

                entityID = manager._localGuidDictionary[entityGuid];
                return true;
            }
            finally
            {
                _guidLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the associated entityID of the specified Guid.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        public bool TryGetEntityByGuid(Guid entityGuid, out int entityID)
        {
            _guidLock.EnterReadLock();
            try
            {
                return _localGuidDictionary.TryGetValue(entityGuid, out entityID);
            }
            finally
            {
                _guidLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the associated Guid of the specified entityID.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        public bool TryGetGuidByEntity(int entityID, out Guid entityGuid)
        {
            entityGuid = Guid.Empty;

            if (!IsValidEntity(entityID))
            {
                return false;
            }

            _guidLock.EnterReadLock();
            try
            {
                entityGuid = _localGuids[entityID];
                return true;
            }
            finally
            {
                _guidLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Completely clears all entities.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        public void Clear(bool global = false)
        {
            for (int entityID = 0; entityID < _entities.Count; entityID++)
            {
                if (IsValidEntity(entityID))
                {
                    RemoveEntity(entityID);
                }
            }
            if (!global)
            {
                return;
            }

            _guidLock.EnterWriteLock();
            try
            {
                _globalGuidDictionary = new Dictionary<Guid, EntityManager>();
            }
            finally
            {
                _guidLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the true if the specified type is a valid DataBlobType.
        /// <para></para>
        /// typeIndex parameter is set to the typeIndex of the dataBlobType if found.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when dataBlobType is null.</exception>
        public static bool TryGetTypeIndex(Type dataBlobType, out int typeIndex)
        {
            return _dataBlobTypes.TryGetValue(dataBlobType, out typeIndex);
        }

        /// <summary>
        /// Faster than TryGetDataBlobTypeIndex and uses generics for type safety.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob, or is Abstract</exception>
        public static int GetTypeIndex<T>() where T : BaseDataBlob
        {
            return _dataBlobTypes[typeof(T)];
        }

        #region ISerializable Methods

        public EntityManager(SerializationInfo info, StreamingContext context) : this()
        {
            var entities = (List<Guid>)info.GetValue("Entities", typeof(List<Guid>));

            foreach (Guid entity in entities)
            {
                CreateEntity(entity);
            }

            foreach (KeyValuePair<Type, int> dataBlobTypeInfo in _dataBlobTypes)
            {
                Type dataBlobType = dataBlobTypeInfo.Key;
                Type listType = typeof(List<>).MakeGenericType(dataBlobType);
                dynamic dataBlobs = Activator.CreateInstance(listType);

                try
                {
                    dataBlobs = info.GetValue(dataBlobType.Name, listType);
                }
                catch (System.Runtime.Serialization.SerializationException e)
                {
                    if (e.Message == "Member '" + dataBlobType.Name + "' was not found.")
                    {
                        // Harmless.
                    }
                    else
                    {
                        // Not harmless. Rethrow.
                        throw;
                    }
                }

                foreach (dynamic dataBlob in dataBlobs)
                {
                    SetDataBlob(_localGuidDictionary[dataBlob.EntityGuid], dataBlob);
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Serialize DataBlobs.
            foreach (KeyValuePair<Type, int> typeKVP in _dataBlobTypes)
            {
                Type dataBlobType = typeKVP.Key;
                int typeIndex = typeKVP.Value;

                // Here be dragons.
                MethodInfo castMethod = GetType().GetMethod("Cast", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(dataBlobType);

                IEnumerable<object> enumerable = from dataBlob in _dataBlobMap[typeIndex]
                                                where dataBlob != null
                                                select castMethod.Invoke(null, new object[] { dataBlob });
                IEnumerable<object> dataBlobObjects = enumerable as IList<object> ?? enumerable.ToList();
                if (dataBlobObjects.Any())
                {
                    info.AddValue(dataBlobType.Name, dataBlobObjects.ToList());
                }
            }
            // Serialize Entities
            var defraggedGuids = new List<Guid>(_localGuids.Where(guid => guid != Guid.Empty));

            info.AddValue("Entities", defraggedGuids);
        }

        private static T Cast<T>(object o) where T : BaseDataBlob
        {
            return (T)o;
        }

        #endregion
    }
}
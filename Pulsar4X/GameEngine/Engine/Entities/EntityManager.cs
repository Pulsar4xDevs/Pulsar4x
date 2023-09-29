using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Sensors;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.Engine
{
    [JsonConverter(typeof(EntityManagerConverter))]
    public class EntityManager
    {
        [CanBeNull]
        public string ManagerGuid { get; internal set; }

        [JsonIgnore]
        public Game Game { get;  internal set; }
        protected readonly List<Entity> _entities = new List<Entity>();
        private readonly List<List<BaseDataBlob>> _dataBlobMap = new List<List<BaseDataBlob>>();
        private readonly Dictionary<string, Entity> _localEntityDictionary = new ();
        private readonly Dictionary<string, EntityManager> _globalEntityDictionary;
        private readonly ReaderWriterLockSlim _globalGuidDictionaryLock;
        public int NumberOfEntites { get { return _entities.Count; } }
        public int NumberOfGlobalEntites { get { return _globalEntityDictionary.Count; } }
        private int _nextID;

        internal readonly List<ComparableBitArray> EntityMasks = new List<ComparableBitArray>();

        private static readonly Dictionary<Type, int> InternalDataBlobTypes = InitializeDataBlobTypes();
        [PublicAPI]
        public static ReadOnlyDictionary<Type, int> DataBlobTypes = new ReadOnlyDictionary<Type, int>(InternalDataBlobTypes);

        public DateTime StarSysDateTime => ManagerSubpulses.StarSysDateTime;

        internal ReadOnlyCollection<Entity> Entities => _entities.AsReadOnly();

        internal List<AEntityChangeListener> EntityListners = new ();

        internal Dictionary<string, SystemSensorContacts> FactionSensorContacts = new ();
        public SystemSensorContacts GetSensorContacts(string factionGuid)
        {
            if (!FactionSensorContacts.ContainsKey(factionGuid))
                return new SystemSensorContacts(this, GetGlobalEntityByGuid(factionGuid));
            return FactionSensorContacts[factionGuid];
        }
        Dictionary<string, List<Entity>> EntitesByFaction = new ();
        public List<Entity> GetEntitiesByFaction(string factionGuid)
        {
            if (EntitesByFaction.ContainsKey(factionGuid))
                return EntitesByFaction[factionGuid];
            else
                return new List<Entity>();
        }
        [JsonProperty]
        public ManagerSubPulse ManagerSubpulses { get; internal set; }

        /// <summary>
        /// Static reference to an invalid manager.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static readonly EntityManager InvalidManager = new EntityManager();

        #region Constructors
        protected EntityManager() { }
        internal EntityManager(Game game, bool isGlobalManager = false)
        {
            Game = game;
            ManagerGuid = Guid.NewGuid().ToString();
            game.GlobalManagerDictionary.Add(ManagerGuid, this);
            if (isGlobalManager)
            {
                _globalEntityDictionary = new Dictionary<string, EntityManager>();
                _globalGuidDictionaryLock = new ReaderWriterLockSlim();
            }
            else
            {
                _globalEntityDictionary = game.GlobalManager._globalEntityDictionary;
                _globalGuidDictionaryLock = game.GlobalManager._globalGuidDictionaryLock;
            }
            for (int i = 0; i < InternalDataBlobTypes.Keys.Count; i++)
            {
                _dataBlobMap.Add(new List<BaseDataBlob>());
            }
            ManagerSubpulses = new ManagerSubPulse(this, game.ProcessorManager);
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

        /// <summary>
        /// Used to add the provided entity to this entity manager.
        /// Sets up the entity slot and assigns it to the entity while preserving
        /// entity object references.
        /// </summary>
        internal void SetupEntity(Entity entity, IEnumerable<BaseDataBlob> dataBlobs = null)
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
            if (Game != null)
            {
                _globalGuidDictionaryLock.EnterWriteLock();
                try
                {
                    _globalEntityDictionary.Add(entity.Guid, this);
                    _localEntityDictionary.Add(entity.Guid, entity);
                }
                finally
                {
                    _globalGuidDictionaryLock.ExitWriteLock();
                }
            }
            else
            {
                // This is a "fake" manager, that does not link to other managers.
                _localEntityDictionary.Add(entity.Guid, entity);
            }

            entity.ID = entityID;
            entity.SetMask();

            //the below chunk of code was moved from Entity constructor. this allows the entity to be fully populated and helps with entityChangeLisnters.
            if(dataBlobs != null)
            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                if (dataBlob != null)
                {
                    SetDataBlob(entityID, dataBlob, false);
                }
            }

            UpdateListeners(_entities[entityID], null, EntityChangeData.EntityChangeType.EntityAdded);

            if (entity.FactionOwnerID != null)
            {
                if (!EntitesByFaction.ContainsKey(entity.FactionOwnerID))
                    EntitesByFaction.Add(entity.FactionOwnerID, new List<Entity>());
                EntitesByFaction[entity.FactionOwnerID].Add(entity);
            }
                //return entityID; //commented this out since we're now setting the entity.ID in here instead of returning the ID to be set by the entity. this was due to UpdateListners needing a valid entity.
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
            return entityID >= 0 && entityID < _entities.Count && _entities[entityID] != null;
        }

        internal void RemoveEntity(Entity entity)
        {
            if (!IsValidEntity(entity))
            {
                throw new ArgumentException("Provided Entity is not valid in this manager.");
            }

            // Event logevent = new Event(game.TimePulse.GameGlobalDateTime, "Entity Removed From Manager");
            // logevent.Entity = entity;
            // if(entity.FactionOwnerID != Guid.Empty)
            //     logevent.Faction = GetGlobalEntityByGuid(entity.FactionOwnerID);
            // logevent.SystemGuid = ManagerGuid;
            // logevent.EventType = EventType.EntityDestroyed;
            // if (entity.IsValid && entity.HasDataBlob<NameDB>())
            //     logevent.EntityName = entity.GetDataBlob<NameDB>().OwnersName;


            // StaticRefLib.EventLog.AddEvent(logevent);

            int entityID = entity.ID;
            _entities[entityID] = null;
            EntityMasks[entityID] = null;

            _nextID = entityID;

            for (int i = 0; i < InternalDataBlobTypes.Count; i++)
            {
                if (_dataBlobMap[i][entityID] != null)
                {
                    _dataBlobMap[i][entityID].OwningEntity = Entity.InvalidEntity;
                    _dataBlobMap[i][entityID] = null;
                }
            }

            if (Game != null)
            {
                UpdateListeners(entity, null, EntityChangeData.EntityChangeType.EntityRemoved);
                _globalGuidDictionaryLock.EnterWriteLock();
                try
                {
                    _localEntityDictionary.Remove(entity.Guid);
                    _globalEntityDictionary.Remove(entity.Guid);
                }
                finally
                {
                    _globalGuidDictionaryLock.ExitWriteLock();
                }

            }
            else
            {
                // This is a "fake" manager that does not link to other managers.
                _localEntityDictionary.Remove(entity.Guid);
            }

            EntitesByFaction[entity.FactionOwnerID].Remove(entity);
            foreach (var factionContacts in FactionSensorContacts.Values)
            {
                factionContacts.RemoveContact(entity.Guid);
            }

        }

        public List<BaseDataBlob> GetAllDataBlobsForEntity(int entityID)
        {
            var dataBlobs = new List<BaseDataBlob>();
            for (int i = 0; i < InternalDataBlobTypes.Count; i++)
            {
                BaseDataBlob dataBlob = _dataBlobMap[i][entityID];
                if (dataBlob != null)
                {
                    dataBlobs.Add(dataBlob);
                }
            }

            return dataBlobs;
        }

        public List<T> GetAllDataBlobsOfType<T>(int typeIndex) where T : BaseDataBlob
        {
            var dataBlobs = new List<T>();
            foreach (var item in _dataBlobMap[typeIndex])
            {
                if (item != null)
                {
                    T datablob = (T)item;
                    dataBlobs.Add(datablob);
                }
            }

            /*
            for (int i = 0; i < InternalDataBlobTypes.Count; i++)
            {
                if (_dataBlobMap[i].Count -1 >= id)
                {
                    T dataBlob = (T)_dataBlobMap[i][id];
                    if (dataBlob != null)
                    {
                        dataBlobs.Add(dataBlob);
                    }
                }
            }*/

            return dataBlobs;
        }

        public List<T> GetAllDataBlobsOfType<T>() where T : BaseDataBlob
        {
            return GetAllDataBlobsOfType<T>(GetTypeIndex<T>());
        }

        internal T GetDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[GetTypeIndex<T>()][entityID];
        }

        internal T GetDataBlob<T>(int entityID, int typeIndex) where T : BaseDataBlob
        {
            return (T)_dataBlobMap[typeIndex][entityID];
        }

        internal void SetDataBlob(int entityID, BaseDataBlob dataBlob, bool updateListners = true)
        {
            int typeIndex;
            TryGetTypeIndex(dataBlob.GetType(), out typeIndex);
            SetDataBlob(entityID, dataBlob, typeIndex, updateListners);
        }

        internal void SetDataBlob(int entityID, BaseDataBlob dataBlob, int typeIndex, bool updateListners = true)
        {
            _dataBlobMap[typeIndex][entityID] = dataBlob;
            EntityMasks[entityID][typeIndex] = true;
            dataBlob.OwningEntity = _entities[entityID];
            dataBlob.OnSetToEntity();
            dataBlob.OwningEntity.Manager.ManagerSubpulses.AddSystemInterupt(dataBlob);
            if(updateListners)
                UpdateListeners(_entities[entityID], dataBlob, EntityChangeData.EntityChangeType.DBAdded);
        }

        internal void RemoveDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();
            RemoveDataBlob(entityID, typeIndex);
        }

        internal void RemoveDataBlob(int entityID, int typeIndex)
        {
            BaseDataBlob db = _dataBlobMap[typeIndex][entityID];
            _dataBlobMap[typeIndex][entityID].OwningEntity = null;
            _dataBlobMap[typeIndex][entityID] = null;
            EntityMasks[entityID][typeIndex] = false;
            UpdateListeners(_entities[entityID], db, EntityChangeData.EntityChangeType.DBRemoved);
        }

        #endregion

        private void UpdateListeners(Entity entity, BaseDataBlob db, EntityChangeData.EntityChangeType change)
        {
            //listners to this work on thier own threads and are not affected by this one.
            if (EntityListners.Count > 0)
            {
                var changeData = new EntityChangeData() {
                    Entity = entity,
                    Datablob = db,
                    ChangeType = change
                };
                foreach (var listner in EntityListners)
                {
                    listner.AddChange(changeData);
                }
            }


            //this one works on the active (ie this) thread
            entity.InvokeChangeEvent(change, db);
        }

        #region Public API Functions

        /// <summary>
        /// Don't assume entites are not null
        /// </summary>
        /// <returns></returns>
        public List<Entity> GetAllEntites()
        {
            return new List<Entity>(_entities);
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
        public List<Entity> GetAllEntitiesWithDataBlob<T>(AuthenticationToken authToken) where T : BaseDataBlob
        {
            List<Entity> allEntities = GetAllEntitiesWithDataBlob<T>();
            var authorizedEntities = new List<Entity>();

            foreach (Entity entity in allEntities)
            {
                if (AccessControl.IsAuthorized(Game, authToken, entity))
                {
                    authorizedEntities.Add(entity);
                }
            }

            return authorizedEntities;
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
        public List<Entity> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = GetTypeIndex<T>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndex] = true;

            return GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        /// <summary>
        /// Returns a list of entities that have datablob type T.
        /// <para></para>
        /// Returns a blank list if no entities have that datablob.
        /// <para></para>
        /// DO NOT ASSUME THE ORDER OF THE RETURNED LIST!
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        public List<Entity> GetAllEntitiesWithDataBlob<T>(int typeIndex) where T : BaseDataBlob
        {
            //int typeIndex = GetTypeIndex<T>();

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
        public List<Entity> GetAllEntitiesWithDataBlobs(AuthenticationToken authToken, [NotNull] ComparableBitArray dataBlobMask)
        {
            List<Entity> allEntities = GetAllEntitiesWithDataBlobs(dataBlobMask);
            var authorizedEntities = new List<Entity>();

            foreach (Entity entity in allEntities)
            {
                if (AccessControl.IsAuthorized(Game, authToken, entity))
                {
                    authorizedEntities.Add(entity);
                }
            }

            return authorizedEntities;
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
        internal List<Entity> GetAllEntitiesWithDataBlobs([NotNull] ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask == null)
            {
                throw new ArgumentNullException(nameof(dataBlobMask));
            }

            if (dataBlobMask.Length != InternalDataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<Entity>();

            for (int entityID = 0; entityID < EntityMasks.Count; entityID++)
            {
                ComparableBitArray entityMask = EntityMasks[entityID];
                if (entityMask == null)
                {
                    continue;
                }
                if ((entityMask & dataBlobMask) == dataBlobMask)
                {
                    entities.Add(_entities[entityID]);
                }
            }

            return entities;
        }


        [NotNull]
        public List<Entity> GetAllEntitiesWithOUTDataBlobs(AuthenticationToken authToken, [NotNull] ComparableBitArray dataBlobMask)
        {
            List<Entity> allEntities = GetAllEntitiesWithOUTDataBlobs(dataBlobMask);
            var authorizedEntities = new List<Entity>();

            foreach (Entity entity in allEntities)
            {
                if (AccessControl.IsAuthorized(Game, authToken, entity))
                {
                    authorizedEntities.Add(entity);
                }
            }

            return authorizedEntities;
        }

        internal virtual List<Entity> GetAllEntitiesWithOUTDataBlobs([NotNull] ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask == null)
            {
                throw new ArgumentNullException(nameof(dataBlobMask));
            }

            if (dataBlobMask.Length != InternalDataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            var entities = new List<Entity>();

            for (int entityID = 0; entityID < EntityMasks.Count; entityID++)
            {
                ComparableBitArray entityMask = EntityMasks[entityID];
                if (entityMask == null)
                {
                    continue;
                }

                if ((entityMask & dataBlobMask) == BlankDataBlobMask())
                {
                    entities.Add(_entities[entityID]);
                }
            }

            return entities;
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns Entity.InvalidEntity if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        [NotNull]
        public Entity GetFirstEntityWithDataBlob<T>(AuthenticationToken authToken) where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(authToken, GetTypeIndex<T>());
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns Entity.InvalidEntity if no entities have the specified DataBlobType.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob.</exception>
        [NotNull]
        public Entity GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(GetTypeIndex<T>());
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns Entity.InvalidEntity if no entities have the specified DataBlobType.
        /// </summary>
        [NotNull]
        public Entity GetFirstEntityWithDataBlob(AuthenticationToken authToken, int typeIndex)
        {
            Entity entity = GetFirstEntityWithDataBlob(typeIndex);

            if (AccessControl.IsAuthorized(Game, authToken, entity))
            {
                return entity;
            }
            return Entity.InvalidEntity;
        }

        /// <summary>
        /// Returns the first entityID found with the specified DataBlobType.
        /// <para></para>
        /// Returns Entity.InvalidEntity if no entities have the specified DataBlobType.
        /// </summary>
        [NotNull]
        internal Entity GetFirstEntityWithDataBlob(int typeIndex)
        {
            foreach (Entity entity in _entities)
            {
                if (entity != null && entity.DataBlobMask.SetBits.Contains(typeIndex))
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
            return new ComparableBitArray(InternalDataBlobTypes.Count);
        }

        /// <summary>
        /// Returns a blank list used for storing datablobs by typeIndex.
        /// </summary>
        /// <returns></returns>
        [PublicAPI]
        public static List<BaseDataBlob> BlankDataBlobList()
        {
            var blankList = new List<BaseDataBlob>(InternalDataBlobTypes.Count);
            for (int i = 0; i < InternalDataBlobTypes.Count; i++)
            {
                blankList.Add(null);
            }
            return blankList;
        }

        /// <summary>
        /// Checks if the global dictionary contains the requested entity guid.
        /// </summary>
        /// <returns><c>true</c>, if entity does exist globaly <c>false</c> otherwise.</returns>
        /// <param name="entityGuid">Entity GUID.</param>
        [PublicAPI]
        public bool EntityExistsGlobaly(string entityGuid)
        {
            bool exsits;
            if (Game == null)
            {
                exsits = EntityExistsLocaly(entityGuid);
            }
            else
            {
                _globalGuidDictionaryLock.EnterReadLock();
                exsits = _globalEntityDictionary.ContainsKey(entityGuid);
                _globalGuidDictionaryLock.ExitReadLock();
            }
            return exsits;
        }

        /// <summary>
        /// Does the entity exsist localy.
        /// </summary>
        /// <returns><c>true</c>, if entity exsist localy <c>false</c> otherwise.</returns>
        /// <param name="entityGuid">Entity GUID.</param>
        [PublicAPI]
        public bool EntityExistsLocaly(string entityGuid)
        {
            if (_localEntityDictionary.ContainsKey(entityGuid))
                return true;
            return false;

        }

        /// <summary>
        /// Attempts to find the entity with the associated ID. Checks globally.
        /// </summary>
        /// <returns>True if entityID is found.</returns>
        /// <exception cref="GuidNotFoundException">ID was found in Global list, but not locally. Should not be possible.</exception>
        [PublicAPI]
        public bool FindEntityByGuid(string entityGuid, out Entity entity)
        {
            if (Game == null)
            {
                // This is a "fake" manager not connected to other managers.
                // This manager can only perform local ID lookups.
                return _localEntityDictionary.TryGetValue(entityGuid, out entity);
            }
            _globalGuidDictionaryLock.EnterReadLock();
            try
            {
                EntityManager manager;

                if (!_globalEntityDictionary.TryGetValue(entityGuid, out manager))
                {
                    entity = Entity.InvalidEntity;
                    return false;
                }

                if (!manager._localEntityDictionary.TryGetValue(entityGuid, out entity))
                {
                    // Can only be reached if memory corruption or somehow the _guidLock thread synchronization fails.
                    // Entity must be removed from the local manager, but not the global list. Should not be possible.
                    throw new Exception(entityGuid);
                }
                return true;
            }
            finally
            {
                _globalGuidDictionaryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the entity with the associated ID, this checks globaly
        /// </summary>
        /// <param name="entityGuid"></param>
        /// <returns>Entity if found</returns>
        /// <exception cref="GuidNotFoundException">ID was not found</exception>
        [PublicAPI]
        public Entity GetGlobalEntityByGuid(string entityGuid)
        {
            Entity entity;
            if (!FindEntityByGuid(entityGuid, out entity))
                throw new Exception(entityGuid);
            return entity;
        }

        /// <summary>
        /// Gets the entity with the associated ID. Checks only this EntityManager.
        /// </summary>
        /// <returns>The Entity if found</returns>
        /// <exception cref="GuidNotFoundException">ID was not found in Global list, orlocally</exception>
        [PublicAPI]
        public Entity GetLocalEntityByGuid(string entityGuid)
        {
            Entity entity;
            if (!TryGetEntityByGuid(entityGuid, out entity))
            {
                throw new Exception(entityGuid);
            }
            return entity;
        }

        /// <summary>
        /// Gets the associated entity of the specified ID. Checks only this EntityManager.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        [PublicAPI]
        public bool TryGetEntityByGuid(string entityGuid, out Entity entity)
        {
            if (Game != null)
            {
                _globalGuidDictionaryLock.EnterReadLock();
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
                    _globalGuidDictionaryLock.ExitReadLock();
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
            return InternalDataBlobTypes.TryGetValue(dataBlobType, out typeIndex);
        }

        /// <summary>
        /// Faster than TryGetDataBlobTypeIndex and uses generics for type safety.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown when T is not derived from BaseDataBlob, or is Abstract</exception>
        [PublicAPI]
        public static int GetTypeIndex<T>() where T : BaseDataBlob
        {
            return InternalDataBlobTypes[typeof(T)];
        }

        #endregion

        #region ISerializable interface

        // ReSharper disable once UnusedParameter.Local
        // public EntityManager(SerializationInfo info, StreamingContext context) : this((Game)context.Context)
        // {
        //     var entities = (List<ProtoEntity>)info.GetValue("Entities", typeof(List<ProtoEntity>));
        //     ManagerSubpulses = (ManagerSubPulse)info.GetValue("ManagerSubpulses", typeof(ManagerSubPulse));
        //     ManagerSubpulses.PostLoadInit(context, this);
        //     foreach (ProtoEntity protoEntity in entities)
        //     {
        //         Entity entity;
        //         if (FindEntityByGuid(protoEntity.Guid, out entity))
        //         {
        //             // Entity has already been deserialized as a reference. It currently exists on the global manager.
        //             entity.Transfer(this);
        //             foreach (BaseDataBlob dataBlob in protoEntity.DataBlobs.Where(dataBlob => dataBlob != null))
        //             {
        //                 entity.SetDataBlob(dataBlob);
        //             }
        //         }
        //         else
        //         {
        //             // Entity has not been previously deserialized. TODO: check whether the faction guid will deserialise after this or if we need to read it and input it into the constructor here.
        //             Entity.Create(this, String.Empty, protoEntity);
        //         }
        //     }
        // }

        // public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        // {
        //     List<ProtoEntity> storedEntities = (from entity in _entities
        //                                         where entity != null
        //                                         select entity.Clone()).ToList();

        //     info.AddValue("Entities", storedEntities);
        //     info.AddValue("ManagerSubpulses", ManagerSubpulses);
        // }

        // /// <summary>
        // /// OnSerialized callback, called by the JSON serializer. Used to report saving progress back to the application.
        // /// </summary>
        // /// <param name="context"></param>
        // [OnSerialized]
        // private void OnSerialized(StreamingContext context)
        // {
        //     if (Game == null)
        //     {
        //         throw new InvalidOperationException("Fake managers cannot be serialized.");
        //     }
        // }

        // /// <summary>
        // /// OnDeserialized callback, called by the JSON loader. Used to report loading progress back to the application.
        // /// </summary>
        // [OnDeserialized]
        // private void OnDeserialized(StreamingContext context)
        // {
        //     if (Game == null)
        //     {
        //         throw new InvalidOperationException("Fake managers cannot be deserialized.");
        //     }
        // }

        #endregion

        public void Clear()
        {
            for (int index = 0; index < _entities.Count; index++)
            {
                Entity entity = _entities[index];
                entity?.Destroy();
            }
        }
    }

    public class EntityManagerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(EntityManager);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var gameProperty = serializer.Context.Context as Game;

            // By default if we are deserializing an EntityManager directly it is the global manager
            var manager = new EntityManager(gameProperty, true);

            manager.ManagerSubpulses = jsonObject["Subpulses"].ToObject<ManagerSubPulse>(serializer);

            List<Entity> entities = jsonObject["Entities"].ToObject<List<Entity>>(serializer);

            foreach (var protoEntity in entities)
            {
                protoEntity.Transfer(manager);
                // if (manager.FindEntityByGuid(protoEntity.Guid, out var entity))
                // {
                //     // Entity has already been deserialized as a reference. It currently exists on the global manager.
                //     entity.Transfer(manager);
                //     foreach (BaseDataBlob dataBlob in protoEntity.DataBlobs.Where(dataBlob => dataBlob != null))
                //     {
                //         entity.SetDataBlob(dataBlob);
                //     }
                // }
                // else
                // {
                //     // Entity has not been previously deserialized. TODO: check whether the faction guid will deserialise after this or if we need to read it and input it into the constructor here.
                //     Entity.Create(manager, String.Empty, protoEntity);
                // }
            }

            return manager;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var manager = (EntityManager)value;

            // List<ProtoEntity> storedEntities = (from entity in manager.Entities
            //                                     where entity != null
            //                                     select entity.Clone()).ToList();

            JObject obj = new JObject
            {
                { "Entities", JObject.FromObject(manager.Entities) },
                { "Subpulses", JObject.FromObject(manager.ManagerSubpulses) }
            };
            obj.WriteTo(writer);
        }
    }
}
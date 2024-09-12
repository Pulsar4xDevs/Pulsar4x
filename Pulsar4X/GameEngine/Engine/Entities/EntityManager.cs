using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Auth;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Extensions;
using System.Reflection;
using Pulsar4X.Events;
using Pulsar4X.Messaging;

namespace Pulsar4X.Engine
{
    public class EntityManager
    {
        [JsonProperty]
        public string ManagerID { get; internal set; }

        [JsonIgnore]
        public Game Game { get;  internal set; }

        /// <summary>
        /// The Entities Dictionary holds all the entities this manager has. The Key
        /// is the Entities Id.
        /// </summary>
        [JsonProperty("Entities")]
        private SafeDictionary<int, Entity> _entities = new ();

        [JsonIgnore]
        public int EntityCount => _entities.Count;

        /// <summary>
        /// The DataBlobStores hold all the datablobs for the entities this manager has.
        /// The Type key and subsequent Dictionary are lazily instantiated as DataBlobs
        /// are added to the manager. This helps keep the managers as light and as fast
        /// as possible. To lookup a DataBlob for an Entity you need the Type of DataBlob
        /// you want and the Entities Id.
        /// </summary>
        [JsonProperty("DatablobStores")]
        private SafeDictionary<Type, SafeDictionary<int, BaseDataBlob>> _datablobStores = new ();

        [JsonIgnore]
        public DateTime StarSysDateTime => ManagerSubpulses.StarSysDateTime;

        private object _lockObj = new object();

        internal List<Entity> _entitiesTaggedForRemoval = new List<Entity>();

        [JsonProperty]
        public ManagerSubPulse ManagerSubpulses { get; internal set; }

        [JsonProperty("FactionSensorContacts")]
        private Dictionary<int, SystemSensorContacts> _factionSensorContacts = new ();

        /// <summary>
        /// List of neutral entities per faction that the given
        /// faction knows about.
        /// </summary>
        [JsonProperty]
        private Dictionary<int, List<int>> _factionNeutralContacts = new ();

        /// <summary>
        /// Static reference to an invalid manager.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static readonly EntityManager InvalidManager = new EntityManager();

        /// <summary>
        /// Used to filter the entities that we should consider valid targets
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public delegate bool FilterEntities(Entity entity);


        #region Constructors
        internal EntityManager() { }

        internal void Initialize(Game game, bool postLoad = false)
        {
            SelfInitialize(game);
            SetEntities();
            InitializeManagerSubPulse(game, postLoad);


            SetEntities();
        }

        private void InitializeManagerSubPulse(Game game, bool postLoad = false)
        {
            if (postLoad)
            {
                ManagerSubpulses.PostLoadInit(this);
                return;
            }
            ManagerSubpulses ??= new ManagerSubPulse();
            ManagerSubpulses.Initialize(this, game.ProcessorManager);
        }

        private void SetEntities()
        {
            // Make sure all the entities have the manager set
            foreach (var (id, entity) in _entities)
            {
                entity.Manager = this;
            }

            // Make sure the owning entity is set on all datablobs
            foreach (var (type, blobDict) in _datablobStores)
            {
                foreach (var (id, blob) in blobDict)
                {
                    blob.OwningEntity = _entities[id];
                }
            }
        }

        private void SelfInitialize(Game game)
        {
            Game = game;

            if (ManagerID.IsNullOrEmpty())
            {
                ManagerID = Guid.NewGuid().ToString();
            }

            if (!game.GlobalManagerDictionary.ContainsKey(ManagerID))
            {
                game.GlobalManagerDictionary.Add(ManagerID, this);
            }
        }

        #endregion

        #region Entity Management Functions

        public void AddEntity(Entity entity, IEnumerable<BaseDataBlob>? dataBlobs = null)
        {
            if (_entities.ContainsKey(entity.Id))
                throw new ArgumentException($"Entity with ID {entity.Id} already exists");

            entity.Manager = this;

            // Add the entity
            _entities[entity.Id] = entity;

            // Add any specified datablobs
            if (dataBlobs != null)
            {
                foreach (var blob in dataBlobs)
                {
                    SetDataBlob(entity.Id, blob);
                }
            }

            if (!entity.AreAllDependenciesPresent())
                throw new InvalidOperationException("This entity does not have all of the required DataBlob dependencies.");

            entity.IsValid = true;

            // Update listeners
            MessagePublisher.Instance.Publish(
                Message.Create(
                    MessageTypes.EntityAdded,
                    entity.Id,
                    ManagerID
                ));
        }

        public Entity CreateAndAddEntity(ProtoEntity protoEntity)
        {
            var entity = Entity.Create();
            AddEntity(entity, protoEntity.DataBlobs);

            return entity;
        }

        public void Transfer(Entity entity)
        {
            // Don't allow an entity to transer to the manager it's already in
            if(entity.Manager == this) return;

            var dataBlobs = new List<BaseDataBlob>();
            if(entity.Manager != null)
            {
                dataBlobs = entity.Manager.GetAllDataBlobsForEntity(entity.Id);
                entity.Manager.TagEntityForRemoval(entity);
            }

            AddEntity(entity, dataBlobs);
        }


        /// <summary>
        /// Verifies that the supplied entity is valid in this manager.
        /// </summary>
        /// <returns>True is the entity is considered valid.</returns>
        internal bool IsValidEntity([CanBeNull] Entity entity)
        {
            return entity != null && _entities.ContainsKey(entity.Id);
        }

        private bool IsValidID(int entityID)
        {
            return _entities.ContainsKey(entityID);
        }

        internal void TagEntityForRemoval(Entity entity)
        {
            //check we've not already tagged this.
            if (!_entitiesTaggedForRemoval.Contains(entity))
            {
                //do we really need to check this?
                //if so, do we really need to throw an exception?
                if (!IsValidEntity(entity))
                {
                    throw new ArgumentException("Provided Entity is not valid in this manager.");
                }
                entity.IsValid = false;
                ManagerSubpulses.RemoveEntity(entity);
                _entitiesTaggedForRemoval.Add(entity);
                MessagePublisher.Instance.Publish(
                    Message.Create(
                        MessageTypes.EntityRemoved,
                        entity.Id,
                        ManagerID
                    ));
            }
        }

        /// <summary>
        /// This should happen at the beginning of a managers time pulse,
        /// eg entites get removed at the start of the next pulse.
        /// </summary>
        internal void RemoveTaggedEntitys()
        {
            foreach (var entity in _entitiesTaggedForRemoval)
            {
                foreach (var (type, dictionary) in _datablobStores)
                {
                    dictionary.Remove(entity.Id);
                }
                foreach (var (key, value) in _factionSensorContacts)
                {
                    value.RemoveContact(entity.Id);
                }

                if(entity.FactionOwnerID == Game.NeutralFactionId)
                {
                    foreach(var (factionId, factionContactList) in _factionNeutralContacts)
                    {
                        factionContactList.Remove(entity.Id);
                    }
                }

                //remove each of the datablobs.
                foreach (var type in GetAllDataBlobTypesForEntity(entity.Id))
                {
                    if (_datablobStores.ContainsKey(type))
                    {
                        _datablobStores[type].Remove(entity.Id);
                    }
                }
                //actualy remove it from the manager here.
                if (!_entities.Remove(entity.Id))
                {
                    throw new KeyNotFoundException($"Entity with ID {entity.Id} not found in manager.");
                }
                
                Event e = Event.Create(EventType.EntityDestroyed, StarSysDateTime, "Entity Removed From Manager", entity.FactionOwnerID, ManagerID, entity.Id);
                EventManager.Instance.Publish(e);

            }
            _entitiesTaggedForRemoval = new List<Entity>();
        }

        public List<BaseDataBlob> GetAllDataBlobsForEntity(int entityID)
        {
            var dataBlobs = new List<BaseDataBlob>();
            foreach(var storeEntry in _datablobStores)
            {
                if(storeEntry.Value.ContainsKey(entityID))
                {
                    dataBlobs.Add(storeEntry.Value[entityID]);
                }
            }

            return dataBlobs;
        }

        public List<Type> GetAllDataBlobTypesForEntity(int entityId)
        {
            var list = new List<Type>();
            foreach(var storeEntry in _datablobStores)
            {
                if(storeEntry.Value.ContainsKey(entityId))
                {
                    list.Add(storeEntry.Key);
                }
            }

            return list;
        }

        public List<T> GetAllDataBlobsOfType<T>() where T : BaseDataBlob
        {
            Type blobType = typeof(T);

            // Check if there are datablobs of the specified type
            if (_datablobStores.TryGetValue(blobType, out var blobStore))
            {
                // Convert the SafeDictionary values to a list of the desired type
                return blobStore.Values.Cast<T>().ToList();
            }

            return new List<T>();  // Return an empty list if no datablobs of the specified type exist
        }

        internal T GetDataBlob<T>(int entityID) where T : BaseDataBlob
        {
            Type blobType = typeof(T);

            if(!_datablobStores.ContainsKey(blobType) || !_datablobStores[blobType].ContainsKey(entityID))
                throw new KeyNotFoundException($"BlobType {blobType} not found in Manager: {ManagerID}");

            return (T)_datablobStores[blobType][entityID];
        }

        internal BaseDataBlob GetDataBlob(int entityID, Type type)
        {
            return _datablobStores[type][entityID];
        }

        internal bool HasDataBlob<T>(int entityID) where T: BaseDataBlob
        {
            Type blobType = typeof(T);
            return _datablobStores.ContainsKey(blobType) && _datablobStores[blobType].ContainsKey(entityID);
        }

        internal bool HasDataBlob(int entityID, Type type)
        {
            return _datablobStores[type].ContainsKey(entityID);
        }

        internal void SetDataBlob<T>(int entityId, T dataBlob, bool updateListeners = true) where T : BaseDataBlob
        {
            if (dataBlob is null)
                throw new ArgumentNullException(nameof(dataBlob));
            if(!_entities.ContainsKey(entityId))
                throw new ArgumentException("Entity ID does not exist");

            Type type = dataBlob.GetType();
            if (!_datablobStores.ContainsKey(type))
                _datablobStores[type] = new SafeDictionary<int, BaseDataBlob>();

            _datablobStores[type][entityId] = dataBlob;
            dataBlob.OwningEntity = _entities[entityId];
            dataBlob.OnSetToEntity();
            ManagerSubpulses.AddSystemInterupt(dataBlob);

            if(updateListeners)
            {
                var message = Message.Create(
                        MessageTypes.DBAdded,
                        entityId,
                        ManagerID,
                        null,
                        dataBlob);

                MessagePublisher.Instance.Publish(message);
            }
        }

        public void RemoveDatablob<T>(int entityId) where T : BaseDataBlob
        {
            var type = typeof(T);
            if (_datablobStores.ContainsKey(type))
            {
                var blob = _datablobStores[type][entityId];
                blob.OwningEntity = null;
                _datablobStores[type].Remove(entityId);

                var message = Message.Create(
                        MessageTypes.DBRemoved,
                        entityId,
                        ManagerID,
                        null,
                        blob);

                MessagePublisher.Instance.Publish(message);
            }
        }

        #endregion

        #region Public API Functions

        /// <summary>
        /// Don't assume entites are not null
        /// </summary>
        /// <returns></returns>
        public List<Entity> GetAllEntites()
        {
            return new List<Entity>(_entities.Values);
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
        [NotNull]
        public List<Entity> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            var type = typeof(T);
            if(_datablobStores.TryGetValue(type, out var blobStore))
            {
                return _entities.Values.Where(e => blobStore.ContainsKey(e.Id)).ToList();
            }

            return new List<Entity>();
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
        public List<Entity> GetAllEntitiesWithDataBlobs(AuthenticationToken authToken, [NotNull] IEnumerable<Type> datablobTypes)
        {
            List<Entity> allEntities = GetAllEntitiesWithDataBlobs(datablobTypes);
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

        public List<Entity> GetAllEntitiesWithDataBlobs(IEnumerable<Type> datablobTypes)
        {
            var matchingEntities = new List<Entity>();

            foreach (var entity in _entities.Values)
            {
                bool hasAllBlobs = true;

                foreach (var blobType in datablobTypes)
                {
                    if (!_datablobStores.TryGetValue(blobType, out var blobStore) || !blobStore.ContainsKey(entity.Id))
                    {
                        hasAllBlobs = false;
                        break;
                    }
                }

                if (hasAllBlobs)
                {
                    matchingEntities.Add(entity);
                }
            }

            return matchingEntities;
        }

        /// <summary>
        /// Gets the associated entity of the specified ID. Checks only this EntityManager.
        /// <para></para>
        /// Does not throw exceptions.
        /// </summary>
        /// <returns>True if entityID exists in this manager.</returns>
        [PublicAPI]
        public bool TryGetEntityById(int entityId, out Entity entity)
        {
            if(_entities.ContainsKey(entityId))
            {
                entity = _entities[entityId];
                return true;
            }

            entity = Entity.InvalidEntity;
            return false;
        }

        public Entity GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            var type = typeof(T);
            return _entities[_datablobStores[type].Keys.First()];
        }

        public SystemSensorContacts GetSensorContacts(int factionId)
        {
            if (!_factionSensorContacts.ContainsKey(factionId))
            {
                _factionSensorContacts.Add(factionId, new SystemSensorContacts(Game.Factions[factionId]));
            }

            return _factionSensorContacts[factionId];
        }

        public void HideNeutralEntityFromFaction(int factionId, int entityId)
        {
            SetupDefaultNeutralEntitiesForFaction(factionId);

            _factionNeutralContacts[factionId].Remove(entityId);

            MessagePublisher.Instance.Publish(
                Message.Create(
                    MessageTypes.EntityHidden,
                    entityId,
                    ManagerID,
                    factionId));
        }

        public void ShowNeutralEntityToFaction(int factionId, int entityId)
        {
            SetupDefaultNeutralEntitiesForFaction(factionId);

            _factionNeutralContacts[factionId].Add(entityId);

            MessagePublisher.Instance.Publish(
                Message.Create(
                    MessageTypes.EntityRevealed,
                    entityId,
                    ManagerID,
                    factionId));
        }

        public void SetupDefaultNeutralEntitiesForFaction(int factionId)
        {
            if(!_factionNeutralContacts.ContainsKey(factionId))
            {
                _factionNeutralContacts[factionId] = new List<int>();
                var defaultVisible = GetAllEntitiesWithDataBlob<VisibleByDefaultDB>();
                foreach(var entity in defaultVisible)
                {
                    _factionNeutralContacts[factionId].Add(entity.Id);
                }
            }
        }

        /// <summary>
        /// Gets the Entity with the specified Id from any EntityManager
        /// in the game. This can be much slower than looking up the
        /// Entity directly from the EntityManger that contains the Entity.
        /// </summary>
        /// <param name="entityId">The Id of the Entity to retrieve.</param>
        /// <returns>The Entity if one exists, InvalidEntity if it doesn't exist.</returns>
        public Entity GetGlobalEntityById(int entityId)
        {
            Entity entity = Entity.InvalidEntity;

            if(Game.GlobalManager.TryGetEntityById(entityId, out entity))
            {
                return entity;
            }

            foreach(var manager in Game.Systems)
            {
                if(manager.TryGetEntityById(entityId, out entity))
                {
                    return entity;
                }
            }

            return Entity.InvalidEntity;
        }

        public bool TryGetGlobalEntityById(int entityId, out Entity entity)
        {
            if(Game.GlobalManager.TryGetEntityById(entityId, out entity))
            {
                return true;
            }

            foreach(var manager in Game.Systems)
            {
                if(manager.TryGetEntityById(entityId, out entity))
                {
                    return true;
                }
            }

            entity = Entity.InvalidEntity;
            return false;
        }

        public List<Entity> GetFilteredEntities(EntityFilter entityFilter, int factionId)
        {
            return GetFilteredEntities(entityFilter, factionId, null, FilterLogic.And);
        }

        public List<Entity> GetFilteredEntities(EntityFilter entityFilter, int factionId, FilterEntities filter)
        {
            return GetFilteredEntities(entityFilter, factionId, null, FilterLogic.And, filter);
        }

        public List<Entity> GetFilteredEntities(EntityFilter entityFilter, int factionId, Type? datablobFilter = null)
        {
            return GetFilteredEntities(entityFilter, factionId, datablobFilter == null ? null : new List<Type>() { datablobFilter });
        }

        public List<Entity> GetFilteredEntities(EntityFilter entityFilter, int factionId, List<Type>? datablobFilter = null, FilterLogic filterLogic = FilterLogic.And, FilterEntities? filter = null)
        {
            if(factionId == Game.GameMasterFaction.Id) return _entities.Values.ToList();

            return _entities.Values.Where(entity =>
                ((entityFilter.HasFlag(EntityFilter.Friendly) && entity.FactionOwnerID == factionId) ||
                (entityFilter.HasFlag(EntityFilter.Neutral) && entity.FactionOwnerID == Game.NeutralFactionId && EvaluateNeutralEntity(entity, factionId)) ||
                (entityFilter.HasFlag(EntityFilter.Hostile) && entity.FactionOwnerID != factionId && entity.FactionOwnerID != Game.NeutralFactionId && EvaluateSensorContact(entity, factionId))) &&
                (datablobFilter == null || datablobFilter.Count == 0 || EvaluateDataBlobs(entity, datablobFilter, filterLogic)) &&
                (filter == null || filter(entity)))
                .ToList();
        }

        private bool EvaluateNeutralEntity(Entity entity, int factionId)
        {
            return (_factionNeutralContacts.ContainsKey(factionId) && _factionNeutralContacts[factionId].Contains(entity.Id)) ||
                EvaluateSensorContact(entity, factionId);
        }

        private bool EvaluateDataBlobs(Entity entity, List<Type> dataTypes, FilterLogic logic)
        {
            var results = dataTypes.Select(type => entity.HasDataBlob(type)).ToList();

            return logic == FilterLogic.And ? results.All(x => x) : results.Any(x => x);
        }

        private bool EvaluateSensorContact(Entity entity, int factionId)
        {
            return _factionSensorContacts.ContainsKey(factionId) && _factionSensorContacts[factionId].SensorContactExists(entity.Id);
        }

        private bool AreAllDataBlobDependenciesPresent(Type type, int entityId, HashSet<Type> visitedTypes, int depth)
        {
            // We don't want to check this on the intial type that is passed in
            if(depth > 0)
            {
                if(visitedTypes.Contains(type))
                    return true;

                if(!_datablobStores.ContainsKey(type) || !_datablobStores[type].ContainsKey(entityId))
                    return false;

                visitedTypes.Add(type);
            }

            // Get the dependencies for the current type.
            var method = type.GetMethod("GetDependencies", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (method == null)
                return true;
                //throw new InvalidOperationException($"{type.Name} does not implement the GetDependencies method.");

            var dependencies = method.Invoke(null, null) as List<Type>;

            if (dependencies == null)
                return true; // No dependencies.

            // Check each dependency.
            foreach (var dependency in dependencies)
            {
                // Recursively ensure dependencies of the dependency.
                if (!AreAllDataBlobDependenciesPresent(dependency, entityId, visitedTypes, depth + 1))
                    return false;
            }

            return true;
        }

        #endregion
    }
}
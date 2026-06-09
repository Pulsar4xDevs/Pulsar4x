using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using System.Linq;
using Pulsar4X.Colonies;
using Pulsar4X.Names;
using Pulsar4X.Movement;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Maintains client side state for a StarSystem
    /// </summary>
    public class SystemState
    {
        public delegate void SystemStateEntityEventHandler(SystemState systemState, Entity entity);
        public delegate void SystemStateEntityIdEventHandler(SystemState systemState, int entityId);
        public delegate void SystemStateEntityUpdateHandler(SystemState systemState, int entityId, Message messages);
        public event SystemStateEntityEventHandler? OnEntityAdded;
        public event SystemStateEntityIdEventHandler? OnEntityRemoved;
        public event SystemStateEntityUpdateHandler? OnEntityUpdated;

        private int _factionId;

        /// <summary>
        /// The actual star system that SystemState proxies.
        /// </summary>
        /// <remarks>
        /// Prefer using native SystemState methods instead of directly accessing the StarSystem.
        /// </remarks>
        internal StarSystem StarSystem { get; private set; }

        internal SystemSensorContacts? SystemContacts { get; private set; }
        // ConcurrentQueue<Message> _sensorChanges = new ConcurrentQueue<Message>();
        // internal List<Message> SensorChanges = new List<Message>();

        private class ChangeBuffer
        {
            public ConcurrentQueue<int> EntitiesToAdd = new();
            public ConcurrentQueue<(int, Message)> EntitiesToUpdate = new();
            public ConcurrentQueue<int> EntitiesToBin = new();
        }

        // Double buffering the changes to minimize critical section during events.
        private ChangeBuffer _clientSide = new();
        private ChangeBuffer _serverSide = new();
        private readonly object _bufferSwapLock = new();

        // public List<Message> SystemChanges = new List<Message>();

        // Backing fields for the entity dictionaries.
        // Updated in PreFrameSetup based on queued changes.
        private Dictionary<int, EntityState> _allEntities = [];
        private Dictionary<int, EntityState> _entitiesWithNames = [];
        private Dictionary<int, EntityState> _entitiesWithPosition = [];
        private Dictionary<int, EntityState> _entitiesWithColonies = [];

        /// <summary>
        /// A snapshot of all entities in the system that the faction is currently aware of for the current frame.
        /// </summary>
        public IReadOnlyDictionary<int, EntityState> AllEntities => _allEntities;

        /// <summary>
        /// A snapshot of all entities with a name component in the system that the faction is currently aware of for the current frame.
        /// </summary>
        public IReadOnlyDictionary<int, EntityState> EntityStatesWithNames => _entitiesWithNames;

        /// <summary>
        /// A snapshot of all entities with a position component in the system that the faction is currently aware of for the current frame.
        /// </summary>
        public IReadOnlyDictionary<int, EntityState> EntityStatesWithPosition => _entitiesWithPosition;

        /// <summary>
        /// A snapshot of all entities with a colony component in the system that the faction is currently aware of for the current frame.
        /// </summary>
        public IReadOnlyDictionary<int, EntityState> EntityStatesColonies => _entitiesWithColonies;

        public CameraState? SavedCameraState = null;

        public SystemState(StarSystem system, int factionId)
        {
            StarSystem = system;
            StarSystem.SetupDefaultNeutralEntitiesForFaction(factionId);
            SystemContacts = system.GetSensorContacts(factionId);
            // _sensorChanges = SystemContacts.Changes.Subscribe();
            _factionId = factionId;

            var entities = StarSystem.GetFilteredEntities(EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile, factionId);
            foreach (var entity in entities)
            {
                SetupEntity(entity, entity.FactionOwnerID);
            }

            Func<Message, bool> filterById = msg => msg.EntityId != null && msg.SystemId != null && msg.SystemId.Equals(StarSystem.ManagerID);

            MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, OnEntityAddedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, OnEntityRemovedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRevealed, OnEntityAddedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityHidden, OnEntityRemovedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnEntityUpdatedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnEntityUpdatedMessage, filterById);
        }

        private void SetupEntity(Entity entity, int factionId)
        {
            var entityState = new EntityState(entity, entity.Id, factionId);

            if (!_allEntities.ContainsKey(entity.Id))
                _allEntities.Add(entity.Id, entityState);

            if (!EntityStatesWithNames.ContainsKey(entity.Id) && entity.TryGetDataBlob<NameDB>(out var nameDB))
            {
                entityState.Name = nameDB.GetName(factionId); // TODO: doesn't update when if/when the entity is renamed
                _entitiesWithNames.Add(entity.Id, entityState);
            }
            if (!EntityStatesWithPosition.ContainsKey(entity.Id) && entity.TryGetDataBlob<PositionDB>(out var positionDB))
            {
                entityState.Position = positionDB;
                _entitiesWithPosition.Add(entity.Id, entityState);
            }
            if (!EntityStatesColonies.ContainsKey(entity.Id) && entity.HasDataBlob<ColonyInfoDB>())
            {
                _entitiesWithColonies.Add(entity.Id, entityState);
            }
        }

        Task OnEntityAddedMessage(Message message)
        {
            if (message.EntityId == null) return Task.CompletedTask;

            lock (_bufferSwapLock)
            {
                _serverSide.EntitiesToAdd.Enqueue(message.EntityId.Value);
            }
            return Task.CompletedTask;
        }

        Task OnEntityRemovedMessage(Message message)
        {
            if (message.EntityId == null) return Task.CompletedTask;

            lock (_bufferSwapLock)
            {
                _serverSide.EntitiesToBin.Enqueue(message.EntityId.Value);
            }
            return Task.CompletedTask;
        }

        Task OnEntityUpdatedMessage(Message message)
        {
            if (message.EntityId == null) return Task.CompletedTask;

            lock (_bufferSwapLock)
            {
                _serverSide.EntitiesToUpdate.Enqueue((message.EntityId.Value, message));
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public void Update()
        {
            lock (_bufferSwapLock)
            {
                var temp = _serverSide;
                _serverSide = _clientSide;
                _clientSide = temp;
            }

            // Deal with additions
            while (_clientSide.EntitiesToAdd.TryDequeue(out var entityToAdd))
            {
                // FIXME: need to remove the call to the game engine internals
                if (StarSystem.TryGetEntityById(entityToAdd, out var entity))
                {
                    SetupEntity(entity, entity.FactionOwnerID);
                    OnEntityAdded?.Invoke(this, entity);
                }
            }

            // Run entity update before entity removals to ensure they process.
            // Possibly not strictly necessary, but seems safer for now.
            foreach (var entity in _allEntities.Values)
            {
                entity.Update();
            }

            // Deal with removals
            while (_clientSide.EntitiesToBin.TryDequeue(out var entityToRemove))
            {
                if (_allEntities.TryGetValue(entityToRemove, out var entityState))
                {
                    entityState.Unsubscribe();
                }
                _allEntities.Remove(entityToRemove);
                _entitiesWithPosition.Remove(entityToRemove);
                _entitiesWithNames.Remove(entityToRemove);
                _entitiesWithColonies.Remove(entityToRemove);
                OnEntityRemoved?.Invoke(this, entityToRemove);
            }
            // SensorChanges.Clear();
            // SystemChanges.Clear();

            while (_clientSide.EntitiesToUpdate.TryDequeue(out var entityToUpdate))
            {
                OnEntityUpdated?.Invoke(this, entityToUpdate.Item1, entityToUpdate.Item2);
            }
        }

        public void PostFrameCleanup()
        {
            // TODO: not sure we need this?
            // foreach(var item in AllEntities.Values)
            // {
            //     if(item.IsDestroyed)
            //     {
            //         if(!EntitiesToBin.Contains(item.Entity.Id))
            //             EntitiesToBin.Add(item.Entity.Id);
            //     }
            // }

            foreach (var item in AllEntities.Values)
            {
                item.PostFrameCleanup();
            }
        }

        public List<EntityState> GetFilteredEntities(EntityFilter entityFilter, int factionId, Type? datablobFilter = null)
        {
            return GetFilteredEntities(entityFilter, factionId, datablobFilter == null ? null : new List<Type>() { datablobFilter });
        }

        public List<EntityState> GetFilteredEntities(EntityFilter entityFilter, int factionId, List<Type>? datablobFilter = null, FilterLogic filterLogic = FilterLogic.And)
        {
            return AllEntities.Values.Where(entityState =>
                ((entityFilter.HasFlag(EntityFilter.Friendly) && entityState.FactionId == factionId) ||
                (entityFilter.HasFlag(EntityFilter.Neutral) && entityState.FactionId == Game.NeutralFactionId) ||
                (entityFilter.HasFlag(EntityFilter.Hostile) && entityState.FactionId != factionId && entityState.FactionId != Game.NeutralFactionId)) &&
                (datablobFilter == null || datablobFilter.Count == 0 || EvaluateDataBlobs(entityState, datablobFilter, filterLogic)))
                .ToList();
        }

        public EntityState? GetEntityById(int id)
        {
            if (!AllEntities.ContainsKey(id))
                return null;

            return AllEntities[id];
        }

        private bool EvaluateDataBlobs(EntityState entityState, List<Type> dataTypes, FilterLogic logic)
        {
            var results = dataTypes.Select(type => entityState.HasDataBlob(type)).ToList();

            return logic == FilterLogic.And ? results.All(x => x) : results.Any(x => x);
        }
    }
}

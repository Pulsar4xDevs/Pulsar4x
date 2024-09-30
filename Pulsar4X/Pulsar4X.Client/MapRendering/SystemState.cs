using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using System.Linq;

namespace Pulsar4X.SDL2UI
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
        internal StarSystem StarSystem;
        internal SystemSensorContacts? SystemContacts;
        ConcurrentQueue<Message> _sensorChanges = new ConcurrentQueue<Message>();
        internal List<Message> SensorChanges = new List<Message>();
        public ConcurrentQueue<int> EntitiesToAdd = new ();
        public ConcurrentQueue<(int, Message)> EntitiesToUpdate = new ();
        public SafeList<int> EntitiesToBin = new ();
        public List<Message> SystemChanges = new List<Message>();
        public SafeDictionary<int, EntityState> AllEntities = new ();
        public SafeDictionary<int, EntityState> EntityStatesWithNames = new ();
        public SafeDictionary<int, EntityState> EntityStatesWithPosition = new ();
        public SafeDictionary<int, EntityState> EntityStatesColonies = new ();

        public readonly object Lock = new object();

        public SystemState(StarSystem system, int factionId)
        {
            StarSystem = system;
            StarSystem.SetupDefaultNeutralEntitiesForFaction(factionId);
            SystemContacts = system.GetSensorContacts(factionId);
            _sensorChanges = SystemContacts.Changes.Subscribe();
            _factionId = factionId;

            var entities = StarSystem.GetFilteredEntities(EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile, factionId);
            foreach(var entity in entities)
            {
                SetupEntity(entity, entity.FactionOwnerID);
            }

            Func<Message, bool> filterById = msg => msg.EntityId != null && msg.SystemId != null && msg.SystemId.Equals(StarSystem.ManagerID);

            MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, OnEntityAddedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, OnEntityRemovedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRevealed, OnEntityAddedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnEntityUpdatedMessage, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnEntityUpdatedMessage, filterById);
        }

        private void SetupEntity(Entity entity, int factionId)
        {
            var entityState = new EntityState(entity, entity.Id, factionId);

            if(!AllEntities.ContainsKey(entity.Id))
                AllEntities.Add(entity.Id, entityState);

            if (!EntityStatesWithNames.ContainsKey(entity.Id) && entity.TryGetDatablob<NameDB>(out var nameDB))
            {
                entityState.Name = nameDB.GetName(factionId); // TODO: doesn't update when if/when the entity is renamed
                EntityStatesWithNames.Add(entity.Id, entityState);
            }
            if (!EntityStatesWithPosition.ContainsKey(entity.Id) && entity.TryGetDatablob<PositionDB>(out var positionDB))
            {
                entityState.Position = positionDB;
                EntityStatesWithPosition.Add(entity.Id, entityState);
            }
            if (!EntityStatesColonies.ContainsKey(entity.Id) && entity.HasDataBlob<ColonyInfoDB>())
            {
                EntityStatesColonies.Add(entity.Id, entityState);
            }
        }

        Task OnEntityAddedMessage(Message message)
        {
            lock(Lock)
            {
                if(message.EntityId == null) return Task.CompletedTask;
                EntitiesToAdd.Enqueue(message.EntityId.Value);
                return Task.CompletedTask;
            }
        }

        Task OnEntityRemovedMessage(Message message)
        {
            lock(Lock)
            {
                if(message.EntityId == null) return Task.CompletedTask;
                if(!EntitiesToBin.Contains(message.EntityId.Value))
                    EntitiesToBin.Add(message.EntityId.Value);

                return Task.CompletedTask;
            }
        }

        Task OnEntityUpdatedMessage(Message message)
        {
            if(message.EntityId == null) return Task.CompletedTask;
            EntitiesToUpdate.Enqueue((message.EntityId.Value, message));
            return Task.CompletedTask;
        }

        public void PreFrameSetup()
        {
            lock(Lock)
            {
                // Deal with additions
                while(EntitiesToAdd.TryDequeue(out var entityToAdd))
                {
                    // FIXME: need to remove the call to the game engine internals
                    if(StarSystem.TryGetEntityById(entityToAdd, out var entity))
                    {
                        SetupEntity(entity, entity.FactionOwnerID);
                        OnEntityAdded?.Invoke(this, entity);
                    }
                }

                // Deal with removals
                foreach (var entityToRemove in EntitiesToBin)
                {
                    AllEntities.Remove(entityToRemove);
                    EntityStatesWithPosition.Remove(entityToRemove);
                    EntityStatesWithNames.Remove(entityToRemove);
                    EntityStatesColonies.Remove(entityToRemove);
                    OnEntityRemoved?.Invoke(this, entityToRemove);
                }
                EntitiesToBin.Clear();
                SensorChanges.Clear();
                SystemChanges.Clear();
            }

            while(EntitiesToUpdate.TryDequeue(out var entityToUpdate))
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

        private bool EvaluateDataBlobs(EntityState entityState, List<Type> dataTypes, FilterLogic logic)
        {
            var results = dataTypes.Select(type => entityState.HasDataBlob(type)).ToList();

            return logic == FilterLogic.And ? results.All(x => x) : results.Any(x => x);
        }
    }
}

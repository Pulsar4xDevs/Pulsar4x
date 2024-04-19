using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;

namespace Pulsar4X.SDL2UI
{
    /// <summary>
    /// System state.
    /// *Notes*
    /// Currently Entity has an Entity.ChangeEvent
    /// each individual EntityState listens to this and changes the IsDestroyed flag if needed.
    /// Should that be done here instead? TODO: profile this to see which is faster, if either.
    ///
    /// </summary>
    public class SystemState
    {
        public delegate void SystemStateEntityEventHandler(SystemState systemState, Entity entity);
        public delegate void SystemStateEntityIdEventHandler(SystemState systemState, int entityId);
        public delegate void SystemStateEntityUpdateHandler(SystemState systemState, int entityId, Message messages);
        public event SystemStateEntityEventHandler OnEntityAdded;
        public event SystemStateEntityIdEventHandler OnEntityRemoved;
        public event SystemStateEntityUpdateHandler OnEntityUpdated;

        private Entity _faction;
        internal StarSystem StarSystem;
        internal SystemSensorContacts? SystemContacts;
        ConcurrentQueue<Message> _sensorChanges = new ConcurrentQueue<Message>();
        internal List<Message> SensorChanges = new List<Message>();
        ManagerSubPulse PulseMgr;

        public SafeList<int> EntitiesToAdd = new ();
        public SafeDictionary<int, SafeList<Message>> EntitiesToUpdate = new ();
        public SafeList<int> EntitiesToBin = new ();
        public List<Message> SystemChanges = new List<Message>();
        public SafeDictionary<int, EntityState> EntityStatesWithNames = new ();
        public SafeDictionary<int, EntityState> EntityStatesWithPosition = new ();
        public SafeDictionary<int, EntityState> EntityStatesColonies = new ();

        public SystemState(StarSystem system, Entity faction)
        {
            StarSystem = system;
            SystemContacts = system.GetSensorContacts(faction.Id);
            _sensorChanges = SystemContacts.Changes.Subscribe();
            PulseMgr = system.ManagerSubpulses;
            _faction = faction;

            if(_faction.Id == system.Game.GameMasterFaction.Id)
            {
                foreach(var entity in system.GetAllEntites())
                {
                    SetupEntity(entity, _faction);
                }

                Func<Message, bool> filterById = msg => msg.EntityId != null && msg.SystemId != null && msg.SystemId.Equals(StarSystem.ManagerGuid);

                MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, OnEntityAddedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, OnEntityRemovedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.EntityRevealed, OnEntityAddedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnEntityUpdatedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnEntityUpdatedMessage, filterById);
            }
            else
            {
                var factionEntities = StarSystem.GetEntitiesByFaction(faction.Id);
                foreach (Entity entityItem in factionEntities)
                {
                    SetupEntity(entityItem, faction);
                }

                Func<Message, bool> filterById = msg => msg.EntityId != null && msg.SystemId != null && msg.SystemId.Equals(StarSystem.ManagerGuid);

                MessagePublisher.Instance.Subscribe(MessageTypes.EntityAdded, OnEntityAddedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, OnEntityRemovedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.EntityRevealed, OnEntityAddedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnEntityUpdatedMessage, filterById);
                MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnEntityUpdatedMessage, filterById);

                foreach (SensorContact sensorContact in SystemContacts.GetAllContacts())
                {
                    var entityState = new EntityState(sensorContact) { Name = "Unknown" };
                    if(!EntityStatesWithNames.ContainsKey(sensorContact.ActualEntityId))
                        EntityStatesWithNames.Add(sensorContact.ActualEntityId, entityState);

                    if(!EntityStatesWithPosition.ContainsKey(sensorContact.ActualEntityId))
                        EntityStatesWithPosition.Add(sensorContact.ActualEntityId, entityState);
                }

                foreach(var entityId in StarSystem.GetNonOwnedEntititesForFaction(faction.Id))
                {
                    if(StarSystem.TryGetEntityById(entityId, out var entity))
                    {
                        SetupEntity(entity, faction);
                    }
                }
            }
        }

        private void SetupEntity(Entity entityItem, Entity faction)
        {
            var entityState = new EntityState(entityItem);
            // Add Data to State if Available
            if (!EntityStatesWithNames.ContainsKey(entityItem.Id) && entityItem.TryGetDatablob<NameDB>(out var nameDB))
            {
                entityState.Name = nameDB.GetName(faction);
                EntityStatesWithNames.Add(entityItem.Id, entityState);
            }
            if (!EntityStatesWithPosition.ContainsKey(entityItem.Id) && entityItem.TryGetDatablob<PositionDB>(out var positionDB))
            {
                entityState.Position = positionDB;
                EntityStatesWithPosition.Add(entityItem.Id, entityState);
            }
            if (!EntityStatesColonies.ContainsKey(entityItem.Id) && entityItem.HasDataBlob<ColonyInfoDB>())
            {
                EntityStatesColonies.Add(entityItem.Id, entityState);
            }
        }

        async Task OnEntityAddedMessage(Message message)
        {
            await Task.Run(() =>
            {
                if(message.EntityId == null) return;
                EntitiesToAdd.Add(message.EntityId.Value);
            });
        }

        async Task OnEntityRemovedMessage(Message message)
        {
            await Task.Run(() =>
            {
                if(message.EntityId == null) return;
                EntitiesToBin.Add(message.EntityId.Value);
            });
        }

        async Task OnEntityUpdatedMessage(Message message)
        {
            await Task.Run(() =>
            {
                if(message.EntityId == null) return;
                if(!EntitiesToUpdate.ContainsKey(message.EntityId.Value))
                {
                    EntitiesToUpdate[message.EntityId.Value] = new ();
                }
                EntitiesToUpdate[message.EntityId.Value].Add(message);
            });
        }

        /// <summary>
        /// Populates the EntitesToBin list and changes.
        /// Call this before any UI work done.
        /// </summary>
        public void PreFrameSetup()
        {
            foreach(var entityToAdd in EntitiesToAdd)
            {
                if(StarSystem.TryGetEntityById(entityToAdd, out var entity))
                {
                    SetupEntity(entity, _faction);
                    OnEntityAdded?.Invoke(this, entity);
                }
            }

            foreach(var (entityId, messages) in EntitiesToUpdate)
            {
                foreach(var message in messages)
                {
                    OnEntityUpdated?.Invoke(this, entityId, message);
                }
            }
            EntitiesToUpdate.Clear();

            foreach (var item in EntityStatesWithPosition.Values)
            {
                if (item.IsDestroyed) //items get flagged via an event triggered by worker threads.
                {
                    if(!EntitiesToBin.Contains(item.Entity.Id))
                        EntitiesToBin.Add(item.Entity.Id);
                }
            }
        }

        /// <summary>
        /// clears the EntitysToBin list.
        /// Call this afer all UI work is done. (each ui object needs to handle it's own cleanup using EntitesToBin list as a reference before this is called)
        /// </summary>
        public void PostFrameCleanup()
        {
            foreach (var entityToRemove in EntitiesToBin)
            {
                EntityStatesWithPosition.Remove(entityToRemove);
                OnEntityRemoved?.Invoke(this, entityToRemove);
            }
            EntitiesToBin = new SafeList<int>();
            SensorChanges = new List<Message>();
            SystemChanges = new List<Message>();
            foreach (var item in EntityStatesWithPosition.Values)
            {
                item.PostFrameCleanup();
            }
        }
    }
}

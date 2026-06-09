using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using System.Diagnostics.CodeAnalysis;
using Pulsar4X.Colonies;
using Pulsar4X.Industry;
using Pulsar4X.Sensors;
using Pulsar4X.Ships;
using Pulsar4X.Technology;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Pulsar4X.Client
{
    public class EntityState
    {
        public Entity Entity;
        public int Id { get; private set; }
        public int FactionId { get; private set; }
        public string Name { get; set; } = "Unknown";

        public IPosition? Position;
        public IKepler? OrbitIcon;
        public OrbitOrderIcon? DebugOrbitOrder;
        public bool IsDestroyed = false; //currently IsDestroyed = true if moved from one system to another, may need to revisit this.
        private SafeDictionary<Type, BaseDataBlob> DataBlobs = new();

        private class ChangeBuffer
        {
            public ConcurrentQueue<Message> ChangeMessages = new();
        }

        private ChangeBuffer _clientSide = new();
        private ChangeBuffer _serverSide = new();
        private readonly object _bufferSwapLock = new();

        private List<Message> _changesThisFrame = new();
        public IReadOnlyList<Message> Changes => _changesThisFrame;

        public CommandReferences? CmdRef;
        internal string? StarSystemId;
        internal UserOrbitSettings.OrbitBodyType BodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        public EntityState(Entity entity, int id, int factionId)
        {
            Entity = entity;
            Id = id;
            FactionId = factionId;

            if (entity.Manager != null)
            {
                foreach (var db in entity.Manager.GetAllDataBlobsForEntity(entity.Id))
                {
                    DataBlobs.Add(db.GetType(), db);
                }

                StarSystem starSys = (StarSystem)entity.Manager;
                StarSystemId = starSys.ID;
            }

            SetupEventListeners();
            SetBodyType();
        }

        public Entity? GetParent()
        {
            if (HasDataBlob(typeof(PositionDB)))
                return ((PositionDB)DataBlobs[typeof(PositionDB)]).Parent;

            return null;
        }

        public bool IsPlanetOrMoon()
        {
            return this.BodyType == UserOrbitSettings.OrbitBodyType.Planet || this.BodyType == UserOrbitSettings.OrbitBodyType.DwarfPlanet || this.BodyType == UserOrbitSettings.OrbitBodyType.Moon;
        }

        public bool IsSmallBody()
        {
            return this.BodyType == UserOrbitSettings.OrbitBodyType.Asteroid || this.BodyType == UserOrbitSettings.OrbitBodyType.Comet;
        }

        public bool IsStar()
        {
            return this.BodyType == UserOrbitSettings.OrbitBodyType.Star;
        }

        public EntityState(SensorContact sensorContact)
        {
            // TODO: re-implement this
            Entity = sensorContact.ActualEntity;
            Position = sensorContact.Position;

            //Name = sensorContact.GetDataBlob<NameDB>().GetName(_uiState.Faction);
            if (Entity.Manager != null)
            {
                StarSystem starSys = (StarSystem)Entity.Manager;
                StarSystemId = starSys.ID;
            }
            SetupEventListeners();
            SetBodyType();
        }

        public bool CanResearch
        {
            get
            {
                return DataBlobs.ContainsKey(typeof(EntityResearchDB));
            }
        }
        public bool CanConstruct
        {
            get
            {
                return DataBlobs.ContainsKey(typeof(IndustryAbilityDB));
            }
        }

        void SetBodyType()
        {
            BodyType = Utils.EntityBodyType(Entity);
        }

        private void SetupEventListeners()
        {
            Func<Message, bool> filterById = msg => msg.EntityId == Id;

            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, EnqueueToBuffer, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, EnqueueToBuffer, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, EnqueueToBuffer, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityHidden, EnqueueToBuffer, filterById);
        }

        /// <summary>
        /// Unsubscribes from all message events. Must be called when this EntityState
        /// is removed from a SystemState to prevent memory leaks and ghost updates.
        /// </summary>
        public void Unsubscribe()
        {
            MessagePublisher.Instance.Unsubscribe(MessageTypes.EntityRemoved, EnqueueToBuffer);
            MessagePublisher.Instance.Unsubscribe(MessageTypes.DBAdded, EnqueueToBuffer);
            MessagePublisher.Instance.Unsubscribe(MessageTypes.DBRemoved, EnqueueToBuffer);
            MessagePublisher.Instance.Unsubscribe(MessageTypes.EntityHidden, EnqueueToBuffer);
        }

        // Enqueues the change messages to the server side buffer.
        Task EnqueueToBuffer(Message message)
        {
            lock (_bufferSwapLock)
            {
                _serverSide.ChangeMessages.Enqueue(message);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public void Update()
        {
            _changesThisFrame.Clear();

            lock (_bufferSwapLock)
            {
                (_clientSide, _serverSide) = (_serverSide, _clientSide);
            }

            while (_clientSide.ChangeMessages.TryDequeue(out var message))
            {
                switch (message.MessageType)
                {
                    case MessageTypes.EntityRemoved:
                    case MessageTypes.EntityHidden:
                        HandleOnEntityRemoved(message);
                        break;
                    case MessageTypes.DBAdded:
                        HandleOnDBAdded(message);
                        break;
                    case MessageTypes.DBRemoved:
                        HandleOnDBRemoved(message);
                        break;
                }
            }
        }

        private void HandleOnEntityRemoved(Message message)
        {
            DataBlobs.Clear();
            IsDestroyed = true;
        }

        private void HandleOnDBAdded(Message message)
        {
            if (message.DataBlob is null) return;

            DataBlobs[message.DataBlob.GetType()] = message.DataBlob;
            _changesThisFrame.Add(message);
        }

        private void HandleOnDBRemoved(Message message)
        {
            if (message.DataBlob is null) return;

            DataBlobs.Remove(message.DataBlob.GetType());
            _changesThisFrame.Add(message);
        }

        public void PostFrameCleanup()
        { }

        public bool HasDataBlob(Type? type)
        {
            return type == null ? false : DataBlobs.ContainsKey(type);
        }

        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            return HasDataBlob(typeof(T));
        }

        public T GetDataBlob<T>() where T : BaseDataBlob
        {
            return (T)DataBlobs[typeof(T)];
        }

        public BaseDataBlob GetDataBlob(Type type)
        {
            return DataBlobs[type];
        }

        public bool TryGetDataBlob<T>([NotNullWhen(true)] out T? value) where T : BaseDataBlob
        {
            if (HasDataBlob<T>())
            {
                value = GetDataBlob<T>();
                return true;
            }

            value = null;
            return false;
        }
    }
}

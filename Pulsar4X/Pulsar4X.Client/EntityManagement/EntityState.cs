using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Messaging;
using System.Threading.Tasks;
using Pulsar4X.DataStructures;
using System.Diagnostics.CodeAnalysis;

namespace Pulsar4X.SDL2UI
{
    public class EntityState
    {
        public Entity Entity;
        public int Id { get; private set; }
        public int FactionId { get; private set; }
        public string Name { get; set; } = "Unknown";

        public IPosition? Position;
        public NameIcon? NameIcon;
        public IKepler? OrbitIcon;
        public OrbitOrderWidget? DebugOrbitOrder;
        public bool IsDestroyed = false; //currently IsDestroyed = true if moved from one system to another, may need to revisit this.
        private SafeDictionary<Type, BaseDataBlob> DataBlobs = new ();
        public SafeList<Message> Changes = new ();
        public SafeList<Message> _changesNextFrame = new ();
        public CommandReferences? CmdRef;
        internal string? StarSystemId;
        internal UserOrbitSettings.OrbitBodyType BodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        public EntityState(Entity entity, int id, int factionId)
        {
            Entity = entity;
            Id = id;
            FactionId = factionId;

            if(entity.Manager != null)
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
            if(HasDataBlob(typeof(PositionDB)))
                return ((PositionDB)DataBlobs[typeof(PositionDB)]).Parent;

            return null;
        }

        public bool IsPlanetOrMoon()
        {
            return this.BodyType == UserOrbitSettings.OrbitBodyType.Planet || this.BodyType == UserOrbitSettings.OrbitBodyType.Moon;
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
            if(Entity.Manager != null)
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
                return DataBlobs.ContainsKey(typeof(EntityResearchDB)) ;
            }
        }
        public bool CanConstruct
        {
            get
            {
                return DataBlobs.ContainsKey(typeof(IndustryAbilityDB)) ;
            }
        }

        void SetBodyType()
        {
            if (HasDataBlob<SystemBodyInfoDB>())
            {
                switch (GetDataBlob<SystemBodyInfoDB>().BodyType)
                {
                    case DataStructures.BodyType.Asteroid:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Asteroid;
                            break;
                        }
                    case DataStructures.BodyType.Comet:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Comet;
                            break;
                        }
                    case DataStructures.BodyType.DwarfPlanet:
                    case DataStructures.BodyType.GasDwarf:
                    case DataStructures.BodyType.GasGiant:
                    case DataStructures.BodyType.IceGiant:
                    case DataStructures.BodyType.Terrestrial:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Planet;
                            break;
                        }

                    case DataStructures.BodyType.Moon:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Moon;
                            break;
                        }
                    default:
                        break;
                }

            }
            if (HasDataBlob<StarInfoDB>())
                BodyType = UserOrbitSettings.OrbitBodyType.Star;
            if (HasDataBlob<ColonyInfoDB>())
                BodyType = UserOrbitSettings.OrbitBodyType.Colony;
            if (HasDataBlob<ShipInfoDB>())
                BodyType = UserOrbitSettings.OrbitBodyType.Ship;
        }

        private void SetupEventListeners()
        {
            Func<Message, bool> filterById = msg => msg.EntityId == Id;

            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, OnEntityRemoved, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBAdded, OnDBAdded, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.DBRemoved, OnDBRemoved, filterById);
            MessagePublisher.Instance.Subscribe(MessageTypes.EntityHidden, OnEntityRemoved, filterById);
        }

        Task OnEntityRemoved(Message message)
        {
            DataBlobs.Clear();
            IsDestroyed = true;
            return Task.CompletedTask;
        }

        Task OnDBAdded(Message message)
        {
            if(message.DataBlob != null)
            {
                DataBlobs[message.DataBlob.GetType()] = message.DataBlob;
                _changesNextFrame.Add(message);
            }
            return Task.CompletedTask;
        }

        Task OnDBRemoved(Message message)
        {
            if(message.DataBlob != null)
            {
                DataBlobs.Remove(message.DataBlob.GetType());
                _changesNextFrame.Add(message);
            }
            return Task.CompletedTask;
        }

        public void PostFrameCleanup()
        {
            Changes = _changesNextFrame;
            _changesNextFrame.Clear();
        }

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
            if(HasDataBlob<T>())
            {
                value = GetDataBlob<T>();
                return true;
            }

            value = null;
            return false;
        }
    }
}

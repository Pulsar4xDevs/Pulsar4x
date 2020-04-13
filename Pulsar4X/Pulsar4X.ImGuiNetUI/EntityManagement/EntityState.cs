using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.SDL2UI
{
    public class EntityState
    {
        public Entity Entity;
        public string Name = "Unknown";

        public IPosition Position;
        public NameIcon NameIcon;
        public OrbitIconBase OrbitIcon;
        public OrbitOrderWiget DebugOrbitOrder;
        public bool IsDestroyed = false; //currently IsDestroyed = true if moved from one system to another, may need to revisit this. 
        public Dictionary<Type, BaseDataBlob> DataBlobs = new Dictionary<Type, BaseDataBlob>();
        public List<EntityChangeData> Changes = new List<EntityChangeData>();
        public List<EntityChangeData> _changesNextFrame = new List<EntityChangeData>();
        public CommandReferences CmdRef;
        internal Guid StarSysGuid;
        internal UserOrbitSettings.OrbitBodyType BodyType = UserOrbitSettings.OrbitBodyType.Unknown;
        public EntityState(Entity entity)
        {
            Entity = entity;
            foreach (var db in entity.DataBlobs)
            {
                DataBlobs.Add(db.GetType(), db);
            }
            Position = entity.GetDataBlob<PositionDB>();

            //Name = entity.GetDataBlob<NameDB>().GetName(_state.Faction);
            StarSystem starSys = (StarSystem)entity.Manager;
            StarSysGuid = starSys.Guid;
            entity.ChangeEvent += On_entityChangeEvent;

            SetBodyType();


        }

        public int GetRank()
        {
            if (this.IsStar()) 
            {
                return 0;
            }
            else if(this.GetParent() == null) 
            {
                return 0;
            }
            else
            {
                EntityState _parententityState = new EntityState(this.GetParent());
                return _parententityState.GetRank() + 1;
            }
        
        }

        public Entity GetParent()
        {
            return Entity.GetDataBlob<PositionDB>().Parent;
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
            Entity = sensorContact.ActualEntity;
            Position = sensorContact.Position;

            //Name = sensorContact.GetDataBlob<NameDB>().GetName(_state.Faction);
            StarSystem starSys = (StarSystem)Entity.Manager;
            StarSysGuid = starSys.Guid;
            sensorContact.ActualEntity.ChangeEvent += On_entityChangeEvent;
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
            if (Entity.HasDataBlob<SystemBodyInfoDB>())
            {
                switch (Entity.GetDataBlob<SystemBodyInfoDB>().BodyType)
                {
                    case ECSLib.BodyType.Asteroid:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Asteroid;
                            break;
                        }
                    case ECSLib.BodyType.Comet:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Comet;
                            break;
                        }
                    case ECSLib.BodyType.DwarfPlanet:
                    case ECSLib.BodyType.GasDwarf:
                    case ECSLib.BodyType.GasGiant:
                    case ECSLib.BodyType.IceGiant:
                    case ECSLib.BodyType.Terrestrial:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Planet;
                            break;
                        }

                    case ECSLib.BodyType.Moon:
                        {
                            BodyType = UserOrbitSettings.OrbitBodyType.Moon;
                            break;
                        }
                    default:
                        break;
                }

            }
            if (Entity.HasDataBlob<StarInfoDB>())
                BodyType = UserOrbitSettings.OrbitBodyType.Star;
            if (Entity.HasDataBlob<ShipInfoDB>())
                BodyType = UserOrbitSettings.OrbitBodyType.Ship;
        }

        //maybe this should be done in the SystemState?
        void On_entityChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            _changesNextFrame.Add(new EntityChangeData() { ChangeType = changeType, Datablob = db, Entity = Entity });
            switch (changeType)
            {
                case EntityChangeData.EntityChangeType.DBAdded:
                    DataBlobs[db.GetType()] = db;
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    DataBlobs.Remove(db.GetType());
                    break;
                case EntityChangeData.EntityChangeType.EntityRemoved:
                    DataBlobs.Clear();
                    IsDestroyed = true;
                    break;
                default:
                    break;
            }
        }

        public void PostFrameCleanup()
        {
            Changes = _changesNextFrame;
            _changesNextFrame = new List<EntityChangeData>();
        }

        
    }
}

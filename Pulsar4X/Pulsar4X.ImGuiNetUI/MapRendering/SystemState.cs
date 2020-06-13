using System;
using Pulsar4X.ECSLib;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
        private Entity _faction;
        internal StarSystem StarSystem;
        internal SystemSensorContacts SystemContacts;
        ConcurrentQueue<EntityChangeData> _sensorChanges = new ConcurrentQueue<EntityChangeData>();
        internal List<EntityChangeData> SensorChanges = new List<EntityChangeData>();
        ManagerSubPulse PulseMgr;
        AEntityChangeListner _changeListner;
        public List<Guid> EntitysToBin = new List<Guid>();
        public List<Guid> EntitiesAdded = new List<Guid>();
        public List<EntityChangeData> SystemChanges = new List<EntityChangeData>();
        public Dictionary<Guid, EntityState> EntityStatesWithNames = new Dictionary<Guid, EntityState>();
        public Dictionary<Guid, EntityState> EntityStatesWithPosition = new Dictionary<Guid, EntityState>();
        public Dictionary<Guid, EntityState> EntityStatesColonies = new Dictionary<Guid, EntityState>();

        public SystemState(StarSystem system, Entity faction)
        {
            StarSystem = system;
            SystemContacts = system.GetSensorContacts(faction.Guid);
            _sensorChanges = SystemContacts.Changes.Subscribe();
            PulseMgr = system.ManagerSubpulses;
            _faction = faction;
            foreach (Entity entityItem in StarSystem.GetEntitiesByFaction(faction.Guid))
            {
                var entityState = new EntityState(entityItem);// { Name = "Unknown" };
                if (entityItem.HasDataBlob<NameDB>())
                {
                    entityState.Name = entityItem.GetDataBlob<NameDB>().GetName(faction);
                    EntityStatesWithNames.Add(entityItem.Guid, entityState);
                }                    
                if (entityItem.HasDataBlob<PositionDB>())
                {
                 EntityStatesWithPosition.Add(entityItem.Guid, entityState);
                }
                if( entityItem.HasDataBlob<ColonyInfoDB>())
                {
                    EntityStatesColonies.Add(entityItem.Guid, entityState);
                }
            }

            var listnerblobs = new List<int>();
            listnerblobs.Add(EntityManager.DataBlobTypes[typeof(PositionDB)]);
            AEntityChangeListner changeListner = new EntityChangeListner(StarSystem, faction, listnerblobs);//, listnerblobs);
            _changeListner = changeListner;

            foreach (SensorContact sensorContact in SystemContacts.GetAllContacts())
            {
                var entityState = new EntityState(sensorContact) { Name = "Unknown" };
                EntityStatesWithNames.Add(sensorContact.ActualEntityGuid, entityState);
                EntityStatesWithPosition.Add(sensorContact.ActualEntityGuid, entityState);
            }

        }


        public static SystemState GetMasterState(StarSystem starSystem)
        {
            return new SystemState(starSystem);
        }

        private SystemState(StarSystem system)
        {
            StarSystem = system;
            //SystemContacts = system.FactionSensorContacts[faction.ID];
            //_sensorChanges = SystemContacts.Changes.Subscribe();
            PulseMgr = system.ManagerSubpulses;

            foreach (var entityItem in system.GetAllEntitiesWithDataBlob<NameDB>())
            {

                var entityState = new EntityState(entityItem);// { Name = "Unknown" };
                entityState.Name = entityItem.GetDataBlob<NameDB>().DefaultName;
                EntityStatesWithNames.Add(entityItem.Guid, entityState);
                if (entityItem.HasDataBlob<PositionDB>())
                {
                    EntityStatesWithPosition.Add(entityItem.Guid, entityState);
                }
                else if (entityItem.HasDataBlob<ColonyInfoDB>())
                {
                    EntityStatesColonies.Add(entityItem.Guid, entityState);
                }
            }

            var listnerblobs = new List<int>();
            listnerblobs.Add(EntityManager.DataBlobTypes[typeof(PositionDB)]);
            AEntityChangeListner changeListner = new EntityChangeListnerSM(StarSystem);//, listnerblobs);
            _changeListner = changeListner;
            /*
            foreach (SensorContact sensorContact in SystemContacts.GetAllContacts())
            {
                var entityState = new EntityState(sensorContact) { Name = "Unknown" };
                EntityStates.Add(sensorContact.ActualEntity.ID, entityState);
            }*/
        }

        void HandleUpdates(EntityChangeData change)
        {

                switch (change.ChangeType)
                {
                    case EntityChangeData.EntityChangeType.EntityAdded:
                        var entityItem = change.Entity;
                        EntitiesAdded.Add(change.Entity.Guid);
                        var entityState = new EntityState(entityItem);// { Name = "Unknown" };
                        if (entityItem.HasDataBlob<NameDB>())
                        {
                            entityState.Name = entityItem.GetDataBlob<NameDB>().GetName(_faction.Guid);
                            EntityStatesWithNames.Add(entityItem.Guid, entityState);
                        }
                        if (entityItem.HasDataBlob<PositionDB>())
                        {
                            EntityStatesWithPosition.Add(entityItem.Guid, entityState);
                        }
                        else if( entityItem.HasDataBlob<ColonyInfoDB>())
                        {
                            EntityStatesColonies.Add(entityItem.Guid, entityState);
                        }
                        break;
                    //if an entity moves from one system to another, then this should be triggered, 
                    //currently Entity.ChangeEvent probibly does too, but we might have to tweak this. maybe add another enum? 
                    case EntityChangeData.EntityChangeType.EntityRemoved:
                        EntitysToBin.Add(change.Entity.Guid);
                        break;
                }

        }

        /// <summary>
        /// Populates the EntitesToBin list and changes. 
        /// Call this before any UI work done.
        /// </summary>
        public void PreFrameSetup()
        {
            while (_changeListner.TryDequeue(out EntityChangeData change))
            {
                SystemChanges.Add(change);
                HandleUpdates(change);
            }
            while (_sensorChanges.TryDequeue(out EntityChangeData change))
            {
                SensorChanges.Add(change);
                HandleUpdates(change);
            }


            foreach (var item in EntityStatesWithPosition.Values)
            {
                if (item.IsDestroyed) //items get flagged via an event triggered by worker threads. 
                {
                    if(!EntitysToBin.Contains(item.Entity.Guid))
                        EntitysToBin.Add(item.Entity.Guid);
                }
            }

        }

        /// <summary>
        /// clears the EntitysToBin list.
        /// Call this afer all UI work is done. (each ui object needs to handle it's own cleanup using EntitesToBin list as a reference before this is called)
        /// </summary>
        public void PostFrameCleanup()
        {
            foreach (var itemGuid in EntitysToBin)
            {
                EntityStatesWithPosition.Remove(itemGuid);
            }
            EntitysToBin = new List<Guid>();
            EntitiesAdded = new List<Guid>();
            SensorChanges = new List<EntityChangeData>();
            SystemChanges = new List<EntityChangeData>();
            foreach (var item in EntityStatesWithPosition.Values)
            {
                item.PostFrameCleanup();
            }
        }
    }
}

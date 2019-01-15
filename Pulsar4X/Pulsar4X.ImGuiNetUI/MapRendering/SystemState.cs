using System;
using Pulsar4X.ECSLib;
using System.Collections.Generic;

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
        internal StarSystem StarSystem;
        SystemSensorContacts SystemContacts;
        ManagerSubPulse PulseMgr;
        EntityChangeListner _changeListner;
        public List<Guid> EntitysToBin = new List<Guid>();
        public List<Guid> EntitiesAdded = new List<Guid>();
        public Dictionary<Guid, EntityState> EntityStates = new Dictionary<Guid, EntityState>();

        public SystemState(StarSystem system, Entity faction)
        {
            StarSystem = system;
            SystemContacts = system.FactionSensorContacts[faction.Guid];
            PulseMgr = system.ManagerSubpulses;

            foreach (Entity entityItem in StarSystem.GetEntitiesByFaction(faction.Guid))
            {
                if (entityItem.HasDataBlob<PositionDB>())
                {
                    var entityState = new EntityState(entityItem) { Name = "Unknown" };
                    EntityStates.Add(entityItem.Guid, entityState);
                }
            }

            var listnerblobs = new List<int>();
            listnerblobs.Add(EntityManager.DataBlobTypes[typeof(PositionDB)]);
            EntityChangeListner changeListner = new EntityChangeListner(StarSystem, faction, listnerblobs);
            _changeListner = changeListner;

            foreach (SensorContact sensorContact in SystemContacts.GetAllContacts())
            {
                var entityState = new EntityState(sensorContact) { Name = "Unknown" };
                EntityStates.Add(sensorContact.ActualEntity.Guid, entityState);
            }

        }


        void HandleUpdates()
        {
            while (_changeListner.TryDequeue(out EntityChangeData change))
            {

                switch (change.ChangeType)
                {
                    case EntityChangeData.EntityChangeType.EntityAdded:
                        if (change.Entity.HasDataBlob<PositionDB>())
                        {
                            var entityState = new EntityState(change.Entity) { Name = "Unknown" };
                            EntityStates.Add(change.Entity.Guid, entityState);
                            EntitiesAdded.Add(change.Entity.Guid);
                        }
                        break;
                    //if an entity moves from one system to another, then this should be triggered, 
                    //currently Entity.ChangeEvent probibly does too, but we might have to tweak this. maybe add another enum? 
                    case EntityChangeData.EntityChangeType.EntityRemoved:
                        EntitysToBin.Add(change.Entity.Guid);
                        break;
                }

            }

        }

        /// <summary>
        /// Populates the EntitesToBin list. 
        /// Call this before any UI work done.
        /// </summary>
        public void SortItemsToBin()
        {
            HandleUpdates();
            foreach (var item in EntityStates.Values)
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
        public void EmptyRecycleBin()
        {
            foreach (var itemGuid in EntitysToBin)
            {
                EntityStates.Remove(itemGuid);
            }
            EntitysToBin = new List<Guid>();
            EntitiesAdded = new List<Guid>();
        }
    }
}

using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Pulsar4X.Extensions;

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
        AEntityChangeListener _changeListner;
        public List<int> EntitysToBin = new ();
        public List<int> EntitiesAdded = new ();
        public List<EntityChangeData> SystemChanges = new List<EntityChangeData>();
        public Dictionary<int, EntityState> EntityStatesWithNames = new ();
        public Dictionary<int, EntityState> EntityStatesWithPosition = new ();
        public Dictionary<int, EntityState> EntityStatesColonies = new ();

        public SystemState(StarSystem system, Entity faction)
        {
            StarSystem = system;
            SystemContacts = system.GetSensorContacts(faction.Id);
            _sensorChanges = SystemContacts.Changes.Subscribe();
            PulseMgr = system.ManagerSubpulses;
            _faction = faction;

            var factionEntities = StarSystem.GetEntitiesByFaction(faction.Id);
            foreach (Entity entityItem in factionEntities)
            {
                SetupEntity(entityItem, faction);
            }

            // foreach(var neutralEntity in StarSystem.GetNeutralEntities())
            // {
            //     SetupEntity(neutralEntity, faction);
            // }

            AEntityChangeListener changeListner = new EntityChangeListener(StarSystem, faction, new List<Type>() { typeof(PositionDB) });//, listnerblobs);
            _changeListner = changeListner;

            foreach (SensorContact sensorContact in SystemContacts.GetAllContacts())
            {
                var entityState = new EntityState(sensorContact) { Name = "Unknown" };
                if(!EntityStatesWithNames.ContainsKey(sensorContact.ActualEntityId))
                    EntityStatesWithNames.Add(sensorContact.ActualEntityId, entityState);

                if(!EntityStatesWithPosition.ContainsKey(sensorContact.ActualEntityId))
                    EntityStatesWithPosition.Add(sensorContact.ActualEntityId, entityState);
            }

        }

        private void SetupEntity(Entity entityItem, Entity faction)
        {
            var entityState = new EntityState(entityItem);
            // Add Data to State if Available
            if (!EntityStatesWithNames.ContainsKey(entityItem.Id) && entityItem.HasDataBlob<NameDB>())
            {
                entityState.Name = entityItem.GetDataBlob<NameDB>().GetName(faction);
                EntityStatesWithNames.Add(entityItem.Id, entityState);
            }
            if (!EntityStatesWithPosition.ContainsKey(entityItem.Id) && entityItem.HasDataBlob<PositionDB>())
            {
                entityState.Position = entityItem.GetDataBlob<PositionDB>();
                EntityStatesWithPosition.Add(entityItem.Id, entityState);
            }
            if (!EntityStatesColonies.ContainsKey(entityItem.Id) && entityItem.HasDataBlob<ColonyInfoDB>())
            {
                EntityStatesColonies.Add(entityItem.Id, entityState);
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
            _faction = system.Game.GameMasterFaction;
            foreach (var entityItem in system.GetAllEntitiesWithDataBlob<NameDB>())
            {

                var entityState = new EntityState(entityItem);// { Name = "Unknown" };
                entityState.Name = entityItem.GetDataBlob<NameDB>().DefaultName;
                EntityStatesWithNames.Add(entityItem.Id, entityState);
                if (entityItem.HasDataBlob<PositionDB>())
                {
                    EntityStatesWithPosition.Add(entityItem.Id, entityState);
                }
                else if (entityItem.HasDataBlob<ColonyInfoDB>())
                {
                    EntityStatesColonies.Add(entityItem.Id, entityState);
                }
            }

            AEntityChangeListener changeListner = new EntityChangeListenerSM(StarSystem);//, listnerblobs);
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
                        EntitiesAdded.Add(change.Entity.Id);
                        var entityState = new EntityState(entityItem);// { Name = "Unknown" };
                        if (entityItem.HasDataBlob<NameDB>())
                        {
                            entityState.Name = entityItem.GetDataBlob<NameDB>().GetName(_faction.Id);
                            EntityStatesWithNames.Add(entityItem.Id, entityState);
                        }
                        if (entityItem.HasDataBlob<PositionDB>())
                        {
                            EntityStatesWithPosition.Add(entityItem.Id, entityState);
                        }
                        else if( entityItem.HasDataBlob<ColonyInfoDB>())
                        {
                            EntityStatesColonies.Add(entityItem.Id, entityState);
                        }
                        break;
                    //if an entity moves from one system to another, then this should be triggered,
                    //currently Entity.ChangeEvent probibly does too, but we might have to tweak this. maybe add another enum?
                    case EntityChangeData.EntityChangeType.EntityRemoved:
                        EntitysToBin.Add(change.Entity.Id);
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
                    if(!EntitysToBin.Contains(item.Entity.Id))
                        EntitysToBin.Add(item.Entity.Id);
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
            EntitysToBin = new List<int>();
            EntitiesAdded = new List<int>();
            SensorChanges = new List<EntityChangeData>();
            SystemChanges = new List<EntityChangeData>();
            foreach (var item in EntityStatesWithPosition.Values)
            {
                item.PostFrameCleanup();
            }
        }
    }
}

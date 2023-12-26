using System.Collections.Generic;
using System.Collections.Concurrent;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using System;

namespace Pulsar4X.Engine
{

    public abstract class AEntityChangeListener
    {
        private object _lockObj = new object();
        public bool HasProcessed { get; private set; } = false;
        
        protected ConcurrentQueue<EntityChangeData> EntityChanges { get; } = new ConcurrentQueue<EntityChangeData>();
        internal ConcurrentHashSet<Entity> ListningToEntites { get; } = new ConcurrentHashSet<Entity>();
        //internal List<int> IncludeDBTypeIndexFilter = new List<int>();
        //internal List<int> ExcludeDBTypeIndexFilter = new List<int>();
        
        //concurrent queue because we will have multiple systems/entity managers/ threads writing to this.
        internal ConcurrentQueue<EntityChangeData> stepChanges { get; } = new ConcurrentQueue<EntityChangeData>();

        internal AEntityChangeListener(EntityManager manager)
        {
            manager.EntityListeners.Add(this);
        }

        public void TagAsProcessed(bool hasProcessed)
        {
            lock (_lockObj)
                HasProcessed = hasProcessed;
        }

        internal virtual void AddChange(EntityChangeData changeData)
        {
            if (changeData.ChangeType != EntityChangeData.EntityChangeType.EntityAdded)
            {
                if (ListningToEntites.Contains(changeData.Entity))
                {
                    TagAsProcessed(false);
                    EntityChanges.Enqueue(changeData);
                }
            }
            else
            {
                ListningToEntites.Add(changeData.Entity);
                EntityChanges.Enqueue(changeData);
            }
            if (changeData.ChangeType == EntityChangeData.EntityChangeType.EntityRemoved)
            {
                ListningToEntites.Remove(changeData.Entity);
            }
        }

        public bool HasUpdates()
        {
            return (EntityChanges.Count > 0);
        }


        private ConcurrentHashSet<Entity> entityNew = new ConcurrentHashSet<Entity>(); 
        internal void OnGlobalStep()
        {
            foreach (var change in stepChanges)
            {
                if(change.ChangeType is EntityChangeData.EntityChangeType.EntityAdded)
                {
                    entityNew.Add(change.Entity);
                }

                if (change.ChangeType is EntityChangeData.EntityChangeType.EntityRemoved)
                {
                    if (entityNew.Contains(change.Entity))
                        entityNew.Remove(change.Entity);
                }
            }
        }

        public bool TryDequeue(out EntityChangeData changeData)
        {


            /*
            if (EntityChanges.TryDequeue(out changeData))
            {
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.EntityRemoved)
                    ListningToEntites.Remove(changeData.Entity);
                if (changeData.ChangeType == EntityChangeData.EntityChangeType.EntityAdded)
                {

                    if (changeData.Entity.HasDataBlob<OwnedDB>() && changeData.Entity.GetDataBlob<OwnedDB>().OwnedByFaction != ListenForFaction)
                    {
                        if(
                    }

                }
                return true;
            }



            return
            false; */

            return EntityChanges.TryDequeue(out changeData);
        }


    }

    public class EntityChangeListenerSM : AEntityChangeListener
    {
        public EntityChangeListenerSM(EntityManager manager) : base(manager)
        { }
    }



    // public class NetEntityChangeListener : EntityChangeListener
    // {
    //     internal List<EntityChangeListener> ManagerListeners = new List<EntityChangeListener>(); //TODO: shoudl we rewrite this so we just have one concurrent queue and put all the changes into that (from each of the managers)
    //     public NetEntityChangeListener(EntityManager manager, Entity faction) : base(manager, faction, new List<int>())
    //     {
    //         var knownSystems = faction.GetDataBlob<FactionInfoDB>().KnownSystems;
    //         foreach (var starSysGuid in knownSystems)
    //         {
    //             StarSystem starSys = manager.Game.Systems[starSysGuid];
    //             EntityManager starSysManager = starSys;
    //             ManagerListeners.Add(new EntityChangeListener(starSysManager, faction, new List<int>()));
    //         }
    //     }
    // }

    public class EntityChangeListener : AEntityChangeListener
    {
        internal List<Type> IncludeDBTypeIndexFilter = new ();

        internal Entity ListenForFaction { get; }
        private FactionOwnerDB _ownerDB;
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.EntityChangeListnerDB"/> class.
        /// </summary>
        /// <param name="factionEntity">will listen for any entites added or removed that are owned by this entity</param>
        public EntityChangeListener(EntityManager manager, Entity factionEntity, List<Type> datablobFilter) : base(manager)
        {
            ListenForFaction = factionEntity;
            _ownerDB = ListenForFaction.GetDataBlob<FactionOwnerDB>();
            IncludeDBTypeIndexFilter = datablobFilter;

            bool include = false;
            foreach (var entityitem in manager.GetEntitiesByFaction(ListenForFaction.Id))
            {
                foreach (var item in IncludeDBTypeIndexFilter)
                {
                    if (entityitem.HasDataBlob(item))
                        include = true;
                    else
                    {
                        include = false;
                        break;
                    }
                }
                if (include)
                    ListningToEntites.Add(entityitem);
            }
        }



        internal override void AddChange(EntityChangeData changeData)
        {
            switch (changeData.ChangeType)
            {
                case EntityChangeData.EntityChangeType.EntityAdded:
                    OnEntityAdded(changeData);
                    break;
                case EntityChangeData.EntityChangeType.EntityRemoved:
                    OnEntityRemoved(changeData);
                    break;
                case EntityChangeData.EntityChangeType.DBAdded:
                    OnDBAdded(changeData);
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    OnDBRemoved(changeData);
                    break;
                default:
                    break;
            }
        }

        private void OnEntityAdded(EntityChangeData changeData)
        {
            bool include = true;
            foreach (var includeitem in IncludeDBTypeIndexFilter)
            {
                //debug
                // var someentity = changeData.Entity.Manager.GetFirstEntityWithDataBlob(includeitem);
                // var db = someentity.GetDataBlob<BaseDataBlob>(includeitem);
                //end debug

                if (changeData.Entity.HasDataBlob(includeitem))
                {


                    include = true;
                }
                else
                {
                    include = false;
                    break;
                }
            }
            if (include && ListenForFaction.Id == changeData.Entity.FactionOwnerID) //note: this will miss a lot of new entittes, since this code gets called before ownership is set on a new entity. it will get caught when a datablob is set though.
            {
                ListningToEntites.Add(changeData.Entity);
                EntityChanges.Enqueue(changeData);
            }
        }

        private void OnEntityRemoved(EntityChangeData changeData)
        {
            if (ListningToEntites.Contains( changeData.Entity))
            {
                ListningToEntites.Add(changeData.Entity);
                while (!EntityChanges.IsEmpty)
                {
                    //wait
                    BlockingCollection<EntityChangeData> foo = new BlockingCollection<EntityChangeData>();
                    
                }
                EntityChanges.Enqueue(changeData);
            }
        }

        private void OnDBAdded(EntityChangeData changeData)
        {
            if (ListningToEntites.Contains(changeData.Entity))
            {
                ListningToEntites.Add(changeData.Entity);
                EntityChanges.Enqueue(changeData);
            }
            else
            {
                if (IncludeDBTypeIndexFilter.Contains(changeData.Datablob.GetType()))
                {
                    bool include = false;
                    foreach (var includeitem in IncludeDBTypeIndexFilter)
                    {
                        if (!changeData.Entity.HasDataBlob(includeitem))
                        {
                            include = false;
                            break;
                        }
                        else
                        {
                            include = true;
                        }
                    }
                    if (include && _ownerDB.OwnedEntities.ContainsKey(changeData.Entity.Id))
                    {
                        ListningToEntites.Add(changeData.Entity);
                        EntityChangeData addedChange = new EntityChangeData()
                        {
                            ChangeType = EntityChangeData.EntityChangeType.EntityAdded,
                            Entity = changeData.Entity
                        };
                        EntityChanges.Enqueue(addedChange);
                        //EntityChanges.Enqueue(changeData);
                    }
                }
            }
        }

        private void OnDBRemoved(EntityChangeData changeData)
        {
            if (ListningToEntites.Contains(changeData.Entity))
            {
                if (IncludeDBTypeIndexFilter.Contains(changeData.Datablob.GetType()))
                {
                    ListningToEntites.Remove(changeData.Entity);
                }
                EntityChanges.Enqueue(changeData);
            }
        }
    }
}

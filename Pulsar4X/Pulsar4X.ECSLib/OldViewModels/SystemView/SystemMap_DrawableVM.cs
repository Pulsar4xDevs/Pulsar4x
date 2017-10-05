using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.ECSLib
{
    public class SystemMap_DrawableVM : ViewModelBase
    {
        

        public ObservableCollection<Entity> IconableEntitys { get; } = new ObservableCollection<Entity>();
        private HashSet<Entity> _iconableEntites = new HashSet<Entity>();

        public ManagerSubPulse SystemSubpulse { get; private set; }
        private EntityChangeListnerDB _listnerDB;
        public void Initialise(GameVM gameVM, StarSystem starSys)
        {
            _listnerDB = new EntityChangeListnerDB();
            Entity ChangeListnerEntity = new Entity(starSys.SystemManager);
            ChangeListnerEntity.SetDataBlob(_listnerDB);
            ChangeListnerEntity.SetDataBlob(new OwnedDB(gameVM.CurrentFaction));
            EntityChangedListnerProcessor.SetListners(starSys.SystemManager, ChangeListnerEntity);

            IconableEntitys.Clear();
            _iconableEntites.Clear();

            //add non owned entites that have position
            foreach (var entity in starSys.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>())
            {
                if (!entity.HasDataBlob<OwnedDB>())
                    AddIconableEntity(entity);
            }

            foreach (var entity in _listnerDB.ListningToEntites)
            {
                if (entity.HasDataBlob<PositionDB>())
                {
                    AddIconableEntity(entity);
                }
            }
            SystemSubpulse = starSys.SystemManager.ManagerSubpulses;
            starSys.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>(gameVM.CurrentAuthToken);

            OnPropertyChanged(nameof(IconableEntitys));
        }

        private void AddIconableEntity(Entity entity)
        {
            if (!_iconableEntites.Contains(entity))
            {
                _iconableEntites.Add(entity);
                IconableEntitys.Add(entity);
            }
        }
        private void RemoveIconableEntity(Entity entity)
        {
            if (_iconableEntites.Contains(entity))
            {
                IconableEntitys.Remove(entity);
                _iconableEntites.Remove(entity);
            }
        }

        public bool UpdatesReady { get { 
                return _listnerDB.EntityChanges.Count > 0; } }

        public List<EntityChangeData> GetUpdates()
        {
            
            EntityChangedListnerProcessor.PreHandling(_listnerDB);

            var changes = new List<EntityChangeData>();



            lock (_listnerDB.EntityChanges)
            {
                foreach (var change in _listnerDB.EntityChanges)
                {
                    switch( change.ChangeType)
                    {
                        case EntityChangeData.EntityChangeType.EntityAdded:
                            if (change.Entity.HasDataBlob<PositionDB>())
                            {
                                AddIconableEntity(change.Entity);   
                            }
                            break;
                        case EntityChangeData.EntityChangeType.EntityRemoved:
                            RemoveIconableEntity(change.Entity);
                            break;

                        case EntityChangeData.EntityChangeType.DBAdded:
                            if (change.Datablob is PositionDB)
                                AddIconableEntity(change.Entity);
                            else
                                changes.Add(change);
                            break;
                        case EntityChangeData.EntityChangeType.DBRemoved:
                            if (change.Datablob is PositionDB)
                                RemoveIconableEntity(change.Entity);
                            else
                                changes.Add(change);
                            break;
                    }
                }
                EntityChangedListnerProcessor.PostHandling(_listnerDB);
            }
            return changes;
        }

    }




}

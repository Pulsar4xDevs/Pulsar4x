using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ECSLib
{
    public class SystemInfoVM : ViewModelBase
    {
        private GameVM _gameVM;
        public DictionaryVM<StarSystem, string> StarSystems { get; } = new DictionaryVM<StarSystem, string>(DisplayMode.Value);
        public DictionaryVM<Entity, string> Entities { get; } = new DictionaryVM<Entity, string>(DisplayMode.Value);
        public DictionaryVM<string, TreeHierarchyDB> TreeBlobs { get; } = new DictionaryVM<string, TreeHierarchyDB>(DisplayMode.Key);

        public List<Entity> SortedList { get; } = new List<Entity>();

        private EntityBlobPair _eBTreePair;
        public EntityBlobPair EBTreePair
        {
            get { return _eBTreePair; }
            set { _eBTreePair = value; OnPropertyChanged(); }
        }

        public SystemInfoVM(GameVM gameVM)
        {
            _gameVM = gameVM;

            StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
            Entities.SelectionChangedEvent += Entities_SelectionChangedEvent;
            TreeBlobs.SelectionChangedEvent += TreeBlobs_SelectionChangedEvent;

            PopulateStarSystemList();             
        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            PopulateEntitesList();
        }

        private void Entities_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            PopulateTreeBlobs();
        }

        private void TreeBlobs_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            if(Entities.SelectedKey != null && TreeBlobs.SelectedValue != null)
                EBTreePair = new EntityBlobPair { Entity = Entities.SelectedKey, Blob = TreeBlobs.SelectedValue };
        }

        private void PopulateStarSystemList()
        {
            StarSystems.Clear();
            List<StarSystem> starSystems = _gameVM.Game.GetSystems(_gameVM.CurrentAuthToken);
            foreach (var item in starSystems)
            {
                StarSystems.Add(item, item.NameDB.DefaultName);
            }
            StarSystems.SelectedIndex = 0;
        }

        private void PopulateEntitesList()
        {
            Entities.Clear();
            if (StarSystems.Count > 0)
            { 
                foreach (var entity in StarSystems.SelectedKey.SystemManager.GetAllEntitiesWithDataBlob<OrbitDB>(_gameVM.CurrentAuthToken))
                {
                    string entityname = entity.Guid.ToString();
                    if (entity.HasDataBlob<NameDB>())
                        entityname = entity.GetDataBlob<NameDB>().DefaultName;
                    Entities.Add(entity, entityname);
                }
                Entities.SelectedIndex = 0;
            }
        }

        private void PopulateTreeBlobs()
        {
            TreeBlobs.Clear();
            if (Entities.Count > 0)
            {
                foreach (var blob in Entities.SelectedKey.DataBlobs.OfType<TreeHierarchyDB>())
                {
                    TreeBlobs.Add(blob.GetType().ToString(), blob);
                }
                TreeBlobs.SelectedIndex = 0;
            }
        }
    }

    public struct EntityBlobPair
    {
        public Entity Entity;
        public TreeHierarchyDB Blob;
    }
}

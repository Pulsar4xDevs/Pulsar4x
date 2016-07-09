using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class SystemInfoVM : ViewModelBase
    {
        private GameVM _gameVM;
        public DictionaryVM<StarSystem, string> StarSystems { get; } = new DictionaryVM<StarSystem, string>(DisplayMode.Value);
        public DictionaryVM<Entity, string> Entities { get; } = new DictionaryVM<Entity, string>(DisplayMode.Value);
        public DictionaryVM<string, TreeHierarchyDB> TreeBlobs { get; } = new DictionaryVM<string, TreeHierarchyDB>(DisplayMode.Key);

        private EntityBlobPair _eBTreePair;
        public EntityBlobPair EBTreePair
        {
            get { return _eBTreePair; }
            set { _eBTreePair = value; OnPropertyChanged(); }
        }

        public SystemInfoVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            foreach (var item in _gameVM.Game.GetSystems(_gameVM.CurrentAuthToken))
            {
                StarSystems.Add(item, item.NameDB.DefaultName);
            }
            StarSystems.SelectedIndex = 0;
            foreach (var entity in StarSystems.SelectedKey.SystemManager.GetAllEntitiesWithDataBlob<OrbitDB>(_gameVM.CurrentAuthToken))
            {               
                string entityname = entity.Guid.ToString();
                if (entity.HasDataBlob<NameDB>())
                    entityname = entity.GetDataBlob<NameDB>().DefaultName;
                Entities.Add(entity, entityname);               
            }
            Entities.SelectedIndex = 0;
            foreach (var blob in Entities.SelectedKey.DataBlobs.OfType<TreeHierarchyDB>())
            {           
                TreeBlobs.Add(blob.GetType().ToString(), blob);                
            }
            TreeBlobs.SelectedIndex = 0;
            EBTreePair = new EntityBlobPair { Entity = Entities.SelectedKey, Blob = TreeBlobs.SelectedValue };
            OnPropertyChanged(nameof(StarSystems));
            OnPropertyChanged(nameof(Entities));
            OnPropertyChanged(nameof(TreeBlobs));
        }
    }

    public struct EntityBlobPair
    {
        public Entity Entity;
        public TreeHierarchyDB Blob;
    }
}

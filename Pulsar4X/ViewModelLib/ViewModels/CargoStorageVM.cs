using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Pulsar4X.ViewModel
{
    public class CargoStorageVM : ViewModelBase
    {
        private CargoStorageDB _storageDB;
        private StaticDataStore _dataStore;
        private GameVM _gameVM;

        public  ObservableCollection<CargoStorageByTypeVM> CargoStore { get; } = new ObservableCollection<CargoStorageByTypeVM>();
        public CargoStorageVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _dataStore = _gameVM.Game.StaticData;
        }
        public void Initialise(Entity entity)
        {
            _storageDB = entity.GetDataBlob<CargoStorageDB>();
            foreach (var item in _storageDB.CargoCapicity)
            {
                CargoStorageByTypeVM storeType = new CargoStorageByTypeVM(_gameVM);
                storeType.Initalise(_storageDB, item.Key);
                CargoStore.Add(storeType);
            }
            _storageDB.CargoCapicity.CollectionChanged += _storageDB_CollectionChanged;
        }

        private void _storageDB_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                CargoStorageByTypeVM storeType = new CargoStorageByTypeVM(_gameVM);
                KeyValuePair<Guid, object> kvp = (KeyValuePair<Guid, object>)e.NewItems[0];
                storeType.Initalise(_storageDB, kvp.Key);
                CargoStore.Add(storeType);               
            }
        }


    }

    public class CargoStorageByTypeVM : INotifyPropertyChanged
    {
        private CargoStorageDB _storageDB;
        private StaticDataStore _dataStore;
        private Guid _typeID;
        private GameVM _gameVM;
        public string TypeName { get; set; }
        public long MaxWeight { get { return _storageDB?.CargoCapicity[_typeID] ?? 0; } }
        public float NetWeight { get { return StorageSpaceProcessor.NetWeight(_storageDB, _typeID); } }
        public float RemainingWeight { get { return StorageSpaceProcessor.RemainingCapacity(_storageDB, _typeID); } }
        private string _typeName;
        public string HeaderText { get { return _typeName; } set { _typeName = value; OnPropertyChanged(); } }
        public ObservableCollection<CargoItemVM> TypeStore { get; } = new RangeEnabledObservableCollection<CargoItemVM>();

        public CargoStorageByTypeVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _dataStore = _gameVM.Game.StaticData;
        }

        public void Initalise(CargoStorageDB storageDB, Guid storageTypeID)
        {
            _storageDB = storageDB;
            _typeID = storageTypeID;

            CargoTypeSD cargoType = _dataStore.CargoTypes[_typeID];
            TypeName = cargoType.Name;
            foreach (var itemKVP in StorageSpaceProcessor.GetResourcesOfCargoType(storageDB, _typeID))
            {                             
                CargoItemVM cargoItem = new CargoItemVM(_gameVM, _storageDB, itemKVP.Key);
                TypeStore.Add(cargoItem);
            }

            //foreach (var entityObj in StorageSpaceProcessor.GetEntitesOfCargoType(storageDB, storageType))
            //{
            //    ICargoable cargoableitem = entityObj.GetDataBlob<CargoAbleTypeDB>();
            //    CargoItemVM cargoItem = new CargoItemVM(_gameVM, _storageDB, cargoableitem);
            //    TypeStore.Add(cargoItem);
            //}
            HeaderText = cargoType.Name + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;
            _storageDB.MinsAndMatsByCargoType[_typeID].CollectionChanged += _storageDB_CollectionChanged;
        }

        private void _storageDB_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    OnItemAdded((KeyValuePair<ICargoable, long>)item);
                }

            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {

                foreach (var item in e.OldItems)
                {
                    OnItemRemoved((KeyValuePair<ICargoable, long>)item);
                }
            }
        }

        private void OnItemAdded(KeyValuePair<ICargoable, long> newItem)
        {
            if (newItem.Key.CargoTypeID == _typeID)
            {
                CargoItemVM cargoItem = new CargoItemVM(_gameVM, _storageDB, newItem.Key);
                TypeStore.Add(cargoItem);
            }
        }

        private void OnItemRemoved(KeyValuePair<ICargoable, long> removedItem)
        { //is there a better way to do this?
            foreach (var item in TypeStore.ToArray())
            {
                if (item.ItemID == removedItem.Key.ID)
                    TypeStore.Remove(item);
            }    
        }

        private void ManagerSubpulses_SystemDateChangedEvent(DateTime newDate)
        {
            HeaderText = TypeName + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
            OnPropertyChanged(nameof(MaxWeight));
            OnPropertyChanged(nameof(NetWeight));
            OnPropertyChanged(nameof(RemainingWeight));
            OnPropertyChanged(nameof(TypeStore));
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CargoItemVM : INotifyPropertyChanged
    {
        CargoStorageDB _storageDB;
        internal Guid ItemID { get; private set; }
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public long Amount { get { return StorageSpaceProcessor.GetAmountOf(_storageDB, ItemID); } }
        public float ItemWeight { get; set; } = 0;
        public float TotalWeight { get { return (ItemWeight * Amount); } } 

        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, ICargoable item)
        {
            ItemID = item.ID;
            ItemName = item.Name;
            _storageDB = storageDB;
            ItemWeight = item.Mass;
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;
            if (item is MineralSD)
                ItemTypeName = "Raw Mineral";
            else if (item is ProcessedMaterialSD)
                ItemTypeName = "Processed Material";
            else if (item is CargoAbleTypeDB)
            {
                CargoAbleTypeDB itemdb = (CargoAbleTypeDB)item;
                Entity itemEntity = itemdb.OwningEntity;
                ItemTypeName = itemEntity.GetDataBlob<ComponentInstanceInfoDB>()?.
                    DesignEntity.GetDataBlob<NameDB>().GetName(itemEntity.GetDataBlob<OwnedDB>().
                    ObjectOwner) ?? "Unknown Construct Type";
            }
        }

        private void ManagerSubpulses_SystemDateChangedEvent(DateTime newDate)
        {
            OnPropertyChanged(nameof(Amount));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

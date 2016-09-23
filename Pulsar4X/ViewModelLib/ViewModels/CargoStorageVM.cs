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
            _storageDB.StoredEntities.CollectionChanged += StoredEntities_CollectionChanged;
        }

        private void StoredEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

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
        public Guid TypeID { get; private set; }
        private GameVM _gameVM;
        public string TypeName { get; set; }
        public long MaxWeight { get { return _storageDB?.CargoCapicity[TypeID] ?? 0; } }
        public float NetWeight { get { return StorageSpaceProcessor.NetWeight(_storageDB, TypeID); } }
        public float RemainingWeight { get { return StorageSpaceProcessor.RemainingCapacity(_storageDB, TypeID); } }
        private string _typeName;
        public string HeaderText { get { return _typeName; } set { _typeName = value; OnPropertyChanged(); } }
        public ObservableCollection<CargoItemVM> TypeStore { get; } = new ObservableCollection<CargoItemVM>();
        public ObservableCollection<ComponentSpecificDesignVM> DesignStore { get; } = new ObservableCollection<ComponentSpecificDesignVM>();
        public bool HasComponents { get { if (DesignStore.Count > 0) return true; else return false; } }
        public CargoStorageByTypeVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _dataStore = _gameVM.Game.StaticData;
        }

        public void Initalise(CargoStorageDB storageDB, Guid storageTypeID)
        {
            _storageDB = storageDB;
            TypeID = storageTypeID;

            CargoTypeSD cargoType = _dataStore.CargoTypes[TypeID];
            TypeName = cargoType.Name;
            foreach (var itemKVP in StorageSpaceProcessor.GetResourcesOfCargoType(storageDB, TypeID))
            {                             
                CargoItemVM cargoItem = new CargoItemVM(_gameVM, _storageDB, itemKVP.Key);
                TypeStore.Add(cargoItem);
            }
            if (_storageDB.StoredEntities.ContainsKey(TypeID))
            {
                InitEntities();
            }

            HeaderText = cargoType.Name + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;
            _storageDB.MinsAndMatsByCargoType[TypeID].CollectionChanged += _storageDB_CollectionChanged;
            _storageDB.StoredEntities.CollectionChanged += StoredEntities_CollectionChanged;
        }

        private void InitEntities()
        {
            foreach (var item in _storageDB.StoredEntities[TypeID])
            {
                ComponentSpecificDesignVM design = new ComponentSpecificDesignVM(item.Key, item.Value);
                DesignStore.Add(design);
            }
            _storageDB.StoredEntities[TypeID].CollectionChanged += CargoStorageByTypeVM_CollectionChanged;
            OnPropertyChanged(nameof(HasComponents));
        }

        private void StoredEntities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (KeyValuePair<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>> newitem in e.NewItems)
                {        
                    if (TypeID == newitem.Key)
                    {
                        InitEntities();
                    }                   
                }
            }
        }

        private void CargoStorageByTypeVM_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    KeyValuePair<Entity, PrIwObsList<Entity>> kvp = (KeyValuePair<Entity, PrIwObsList<Entity>>)item;
                    ComponentSpecificDesignVM design = new ComponentSpecificDesignVM(kvp.Key, kvp.Value);
                    DesignStore.Add(design);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    Entity key = (Entity)item;
                    foreach (var vmitem in DesignStore.ToArray())
                    {
                        if (vmitem.EntityID == key.Guid)
                        {
                            DesignStore.Remove(vmitem);
                            break;
                        }
                    }
                }
            }


            OnPropertyChanged(nameof(HasComponents));
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
                    OnItemRemoved((ICargoable)item);
                }
            }
        }

        private void OnItemAdded(KeyValuePair<ICargoable, long> newItem)
        {
            if (newItem.Key.CargoTypeID == TypeID)
            {
                CargoItemVM cargoItem = new CargoItemVM(_gameVM, _storageDB, newItem.Key);
                TypeStore.Add(cargoItem);
            }
        }

        private void OnItemRemoved(ICargoable removedItem)
        { //is there a better way to do this?
            foreach (var item in TypeStore.ToArray())
            {
                if (item.ItemID == removedItem.ID)
                {
                    TypeStore.Remove(item);
                    break;
                }
            }    
        }

        private void ManagerSubpulses_SystemDateChangedEvent(DateTime newDate)
        {
            HeaderText = TypeName + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
            OnPropertyChanged(nameof(MaxWeight));
            OnPropertyChanged(nameof(NetWeight));
            OnPropertyChanged(nameof(RemainingWeight));
            //OnPropertyChanged(nameof(TypeStore));
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

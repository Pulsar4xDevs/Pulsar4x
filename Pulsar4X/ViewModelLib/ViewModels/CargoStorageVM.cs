using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
    public class CargoStorageVM : ViewModelBase
    {
        private CargoStorageDB _storageDB;
        private StaticDataStore _dataStore;
        private GameVM _gameVM;
        public  RangeEnabledObservableCollection<CargoStorageByTypeVM> CargoStore { get; } = new RangeEnabledObservableCollection<CargoStorageByTypeVM>();
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
        public RangeEnabledObservableCollection<CargoItemVM> TypeStore { get; } = new RangeEnabledObservableCollection<CargoItemVM>();

        public CargoStorageByTypeVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _dataStore = _gameVM.Game.StaticData;
        }

        public void Initalise(CargoStorageDB storageDB, Guid storageType)
        {
            _storageDB = storageDB;
            _typeID = storageType;

            CargoTypeSD cargoType = _dataStore.CargoTypes[storageType];
            TypeName = cargoType.Name;
            foreach (var itemKVP in StorageSpaceProcessor.GetResourcesOfCargoType(storageDB, storageType))
            {
                ICargoable cargoableitem = (ICargoable)_dataStore.FindDataObjectUsingID(itemKVP.Key);
                
                if (cargoableitem is MineralSD)
                {
                    MineralSD mineral = (MineralSD)cargoableitem;
                    CargoItemVM cargoItem = new CargoItemVM(_gameVM, storageDB, mineral);

                    TypeStore.Add(cargoItem);
                }
                else if (cargoableitem is ProcessedMaterialSD)
                {
                    ProcessedMaterialSD material = (ProcessedMaterialSD)cargoableitem;
                    CargoItemVM cargoItem = new CargoItemVM(_gameVM, storageDB, material);
                    TypeStore.Add(cargoItem);
                }  
            }

            foreach (var entityObj in StorageSpaceProcessor.GetEntitesOfCargoType(storageDB, storageType))
            {
                ICargoable cargoableitem = entityObj.GetDataBlob<CargoAbleTypeDB>();

                if (cargoableitem is MineralSD)
                {
                    CargoItemVM cargoItem = new CargoItemVM(_gameVM, storageDB, entityObj);

                    TypeStore.Add(cargoItem);
                }
            }
            HeaderText = cargoType.Name + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;
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
        Guid _itemID;
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public long Amount { get { return StorageSpaceProcessor.GetAmountOf(_storageDB, _itemID); } }
        public float ItemWeight { get; set; } = 0;
        public float TotalWeight { get { return (ItemWeight * Amount); } } 

        private CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, ICargoable item)
        {
            _itemID = item.ID;
            _storageDB = storageDB;
            ItemWeight = item.Mass;
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;                    
        }

        private void ManagerSubpulses_SystemDateChangedEvent(DateTime newDate)
        {
            OnPropertyChanged(nameof(Amount));
        }

        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, MineralSD item):this(gameVM, storageDB, (ICargoable)item)
        {
            ItemName = item.Name;
            ItemTypeName = "Raw Mineral";   
        }

        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, ProcessedMaterialSD item) : this(gameVM, storageDB, (ICargoable)item)
        {
            ItemName = item.Name;
            ItemTypeName = "Processed Material";    
        }
        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, Entity item) 
        {
            _storageDB = storageDB;
            ItemName = item.GetDataBlob<NameDB>().GetName(item.GetDataBlob<OwnedDB>().ObjectOwner);
            ItemTypeName = item.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity.GetDataBlob<NameDB>().GetName(item.GetDataBlob<OwnedDB>().ObjectOwner);
            ItemWeight = (float)item.GetDataBlob<MassVolumeDB>().Mass;
            _storageDB.OwningEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += ManagerSubpulses_SystemDateChangedEvent;

        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

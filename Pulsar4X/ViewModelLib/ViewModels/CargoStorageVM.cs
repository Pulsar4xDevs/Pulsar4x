using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

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
    public class CargoStorageByTypeVM
    {
        private StaticDataStore _dataStore;
        private GameVM _gameVM;
        public int MaxWeight { get; set; } = 0;
        public float NetWeight { get; set; } = 0;
        public float RemainingWeight { get; set; } = 0;
        public string HeaderText { get; set; } = "";
        public RangeEnabledObservableCollection<CargoItemVM> TypeStore { get; } = new RangeEnabledObservableCollection<CargoItemVM>();

        public CargoStorageByTypeVM(GameVM gameVM)
        {
            _gameVM = gameVM;
            _dataStore = _gameVM.Game.StaticData;
        }

        public void Initalise(CargoStorageDB storageDB, Guid storageType)
        {

            MaxWeight = storageDB.CargoCapicity[storageType];
            CargoTypeSD cargoType = _dataStore.CargoTypes[storageType];
            foreach (var itemKVP in storageDB.GetResourcesOfCargoType(storageType))
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

            foreach (var entityObj in storageDB.GetEntiesOfCargoType(storageType))
            {
                ICargoable cargoableitem = entityObj.GetDataBlob<CargoAbleTypeDB>();

                if (cargoableitem is MineralSD)
                {
                    CargoItemVM cargoItem = new CargoItemVM(_gameVM, storageDB, entityObj);

                    TypeStore.Add(cargoItem);
                }
            }
            HeaderText = cargoType.Name + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
        }
    }
    public class CargoItemVM : ViewModelBase
    {
        CargoStorageDB _storageDB;
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public int Amount { get; set; } = 0;
        public float ItemWeight { get; set; } = 0;
        public float TotalWeight { get { return (ItemWeight * Amount); } } 

        private CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, ICargoable item)
        {
            _storageDB = storageDB;
            ItemWeight = item.Mass;
            gameVM.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;                    
        }

        private void GameLoop_GameGlobalDateChangedEvent(DateTime newDate)
        {
            OnPropertyChanged(nameof(Amount));
        }

        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, MineralSD item):this(gameVM, storageDB, (ICargoable)item)
        {
            ItemName = "Raw Mineral";
            Amount = _storageDB.MinsAndMatsByCargoType[item.CargoTypeID][item.ID];
            
        }

        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, ProcessedMaterialSD item) : this(gameVM, storageDB, (ICargoable)item)
        {
            ItemName = "Processed Material";
            Amount = _storageDB.MinsAndMatsByCargoType[item.CargoTypeID][item.ID];
            
        }
        public CargoItemVM(GameVM gameVM, CargoStorageDB storageDB, Entity item) 
        {
            _storageDB = storageDB;
            ItemName = item.GetDataBlob<NameDB>().GetName(item.GetDataBlob<OwnedDB>().ObjectOwner);
            ItemTypeName = item.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity.GetDataBlob<NameDB>().GetName(item.GetDataBlob<OwnedDB>().ObjectOwner);
            Amount = 1;
            ItemWeight = (float)item.GetDataBlob<MassVolumeDB>().Mass;
            gameVM.Game.GameLoop.GameGlobalDateChangedEvent += GameLoop_GameGlobalDateChangedEvent;

        }
    }

}

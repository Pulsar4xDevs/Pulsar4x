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
                    CargoItemVM cargoItem = new CargoItemVM()
                    {
                        ItemName = mineral.Name,
                        ItemTypeName = "Raw Mineral",
                        Amount = itemKVP.Value,
                        ItemWeight = cargoableitem.Mass,
                        TotalWeight = cargoableitem.Mass * itemKVP.Value
                    };
                    TypeStore.Add(cargoItem);
                }
                else if (cargoableitem is ProcessedMaterialSD)
                {
                    ProcessedMaterialSD material = (ProcessedMaterialSD)cargoableitem;
                    CargoItemVM cargoItem = new CargoItemVM()
                    {
                        ItemName = material.Name,
                        ItemTypeName = "Procesesd Material",                  
                        Amount = itemKVP.Value,
                        ItemWeight = cargoableitem.Mass,
                        TotalWeight = cargoableitem.Mass * itemKVP.Value
                    };
                    TypeStore.Add(cargoItem);
                }
                
            }


            foreach (var entityObj in storageDB.GetEntiesOfCargoType(storageType))
            {
                ICargoable cargoableitem = entityObj.GetDataBlob<CargoAbleTypeDB>();

                if (cargoableitem is MineralSD)
                {
                    CargoItemVM cargoItem = new CargoItemVM()
                    {
                        ItemName = entityObj.GetDataBlob<NameDB>().GetName(entityObj.GetDataBlob<OwnedDB>().ObjectOwner),
                        ItemTypeName = entityObj.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity.GetDataBlob<NameDB>().GetName(entityObj.GetDataBlob<OwnedDB>().ObjectOwner),
                        //Amount = entityObj.Value,
                        ItemWeight = cargoableitem.Mass,
                        //TotalWeight = cargoitem.Mass * entityObj.Value
                    };
                    TypeStore.Add(cargoItem);
                }
            }
            HeaderText = cargoType.Name + ": " + NetWeight.ToString() + " of " + MaxWeight.ToString() + " used, " + RemainingWeight.ToString() + " remaining";
        }
    }
    public class CargoItemVM
    {
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public int Amount { get; set; }
        public float ItemWeight { get; set; }
        public float TotalWeight { get; set; }       
    }

}

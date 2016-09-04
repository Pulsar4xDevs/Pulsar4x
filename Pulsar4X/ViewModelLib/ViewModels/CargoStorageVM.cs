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
        public RangeEnabledObservableCollection<CargoItemVM> CargoStore { get; } = new RangeEnabledObservableCollection<CargoItemVM>();

        public CargoStorageVM(GameVM gamevm, Entity entity)
        {
            _gameVM = gamevm;
            _dataStore = gamevm.Game.StaticData;
            _storageDB = entity.GetDataBlob<CargoStorageDB>();

            foreach (var storageType in _storageDB.MinsAndMatsByCargoType)
            {
                foreach (var itemKVP in storageType.Value)
                {                                       
                    ICargoable cargoableitem = (ICargoable)_dataStore.FindDataObjectUsingID(itemKVP.Key);
                   
                    if (cargoableitem is MineralSD)
                    {
                        MineralSD mineral = (MineralSD)cargoableitem;
                        CargoItemVM cargoItem = new CargoItemVM()
                        {
                            ItemName = mineral.Name,
                            ItemTypeName = "Raw Mineral",
                            CargoTypeName = _dataStore.CargoTypes[cargoableitem.CargoTypeID].Name,
                            Amount = itemKVP.Value,
                            ItemWeight = cargoableitem.Mass,
                            TotalWeight = cargoableitem.Mass * itemKVP.Value
                        };
                        CargoStore.Add(cargoItem);
                    }
                    else if (cargoableitem is ProcessedMaterialSD)
                    {
                        ProcessedMaterialSD material = (ProcessedMaterialSD)cargoableitem;
                        CargoItemVM cargoItem = new CargoItemVM()
                        {
                            ItemName = material.Name,
                            ItemTypeName = "Procesesd Material",
                            CargoTypeName = _dataStore.CargoTypes[cargoableitem.CargoTypeID].Name,
                            Amount = itemKVP.Value,
                            ItemWeight = cargoableitem.Mass,
                            TotalWeight = cargoableitem.Mass * itemKVP.Value
                        };
                        CargoStore.Add(cargoItem);
                    }
                }
                
            }

            foreach (var storageType in _storageDB.StoredEntities)
            {
                foreach (var entityObj in storageType.Value)
                {
                    ICargoable cargoableitem = entityObj.GetDataBlob<CargoAbleTypeDB>();

                    if (cargoableitem is MineralSD)
                    {
                        
                        CargoItemVM cargoItem = new CargoItemVM()
                        {
                            ItemName = entityObj.GetDataBlob<NameDB>().GetName(entityObj.GetDataBlob<OwnedDB>().ObjectOwner),
                            ItemTypeName = "Entity",
                            CargoTypeName = _dataStore.CargoTypes[cargoableitem.CargoTypeID].Name,
                            //Amount = entityObj.Value,
                            ItemWeight = cargoableitem.Mass,
                            //TotalWeight = cargoitem.Mass * entityObj.Value

                        };
                        CargoStore.Add(cargoItem);
                    }
                }
            }
        }
    }

    public class CargoItemVM
    {
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public string CargoTypeName { get; set; }
        public int Amount { get; set; }
        public float ItemWeight { get; set; }
        public float TotalWeight { get; set; }
        

    }

}

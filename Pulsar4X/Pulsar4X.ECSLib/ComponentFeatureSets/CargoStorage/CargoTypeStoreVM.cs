using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class CargoTypeStoreVM
    {
        StaticDataStore _staticData;
        public string StorageTypeName { get; set; }
        public string StorageTypeDescription { get; set; }

        CargoTypeStore _typeStore;

        Dictionary<Guid, CargoItemVM> _cargoItemsDict = new Dictionary<Guid, CargoItemVM>();
        public ObservableCollection<CargoItemVM> CargoItems { get; } = new ObservableCollection<CargoItemVM>();

        private long _maxCapacity;
        public string MaxCapacity { get; set; }
        private long _freeCapacity;
        public string FreeCapacity { get; set; }

        internal CargoTypeStoreVM(StaticDataStore staticDataStore, Guid typeGuid, CargoTypeStore typeStore)
        {

            _staticData = staticDataStore;
            _typeStore = typeStore;

            var _cargoTypeSD = staticDataStore.CargoTypes[typeGuid];
            StorageTypeName = _cargoTypeSD.Name;
            StorageTypeDescription = _cargoTypeSD.Description;

            Update();
        }


        internal void Update()
        {
            if(_maxCapacity != _typeStore.MaxCapacity) {
                _maxCapacity = _typeStore.MaxCapacity;
                MaxCapacity = _maxCapacity.ToString();

            }
            if(_freeCapacity != _typeStore.FreeCapacity) {
                _freeCapacity = _typeStore.FreeCapacity;
                FreeCapacity = _freeCapacity.ToString();
            }

            foreach(var kvp in _typeStore.ItemsAndAmounts) {
                if(!_cargoItemsDict.ContainsKey(kvp.Key)) {
                    var newCargoItemVM = new CargoItemVM(_staticData.GetICargoable(kvp.Key));
                    _cargoItemsDict.Add(kvp.Key, newCargoItemVM);
                    CargoItems.Add(newCargoItemVM);
                }
                _cargoItemsDict[kvp.Key].Update(kvp.Value);
            }

            foreach(var key in _cargoItemsDict.Keys.ToArray()) {
                if(!_typeStore.ItemsAndAmounts.ContainsKey(key)) {
                    CargoItems.Remove(_cargoItemsDict[key]);
                    _cargoItemsDict.Remove(key);
                }
            }
        }
    }
}

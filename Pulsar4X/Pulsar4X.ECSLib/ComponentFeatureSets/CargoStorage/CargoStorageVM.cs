using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageVM : IDBViewmodel
    {
        CargoStorageDB _storageDatablob;
        StaticDataStore _staticData;

        Dictionary<Guid, CargoTypeStoreVM> _cargoStoresDict = new Dictionary<Guid, CargoTypeStoreVM>();
        public ObservableCollection<CargoTypeStoreVM> CargoStores { get; } = new ObservableCollection<CargoTypeStoreVM>();

        internal CargoStorageVM(StaticDataStore staticData)
        {
            _staticData = staticData;
        }

        public void Update()
        {
            foreach(var kvp in _storageDatablob.StoredCargos) {
                if(!_cargoStoresDict.ContainsKey(kvp.Key)) {
                    var newCargoTypeStoreVM = new CargoTypeStoreVM(_staticData, kvp.Key, kvp.Value);
                    _cargoStoresDict.Add(kvp.Key, newCargoTypeStoreVM);
                    CargoStores.Add(newCargoTypeStoreVM);
                }
                _cargoStoresDict[kvp.Key].Update();
            }

            foreach(var key in _cargoStoresDict.Keys.ToArray()) {
                if(!_storageDatablob.StoredCargos.ContainsKey(key)) {
                    CargoStores.Remove(_cargoStoresDict[key]);
                    _cargoStoresDict.Remove(key);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class CargoTypeStoreVM : INotifyPropertyChanged
    {
        StaticDataStore _staticData;
        public string StorageTypeName { get; set; }
        public string StorageTypeDescription { get; set; }

        private string _headerText;
        public string HeaderText{get { return _headerText; } set { _headerText = value; OnPropertyChanged(); } }

        CargoTypeStore _typeStore;

        Dictionary<Guid, CargoItemVM> _cargoItemsDict = new Dictionary<Guid, CargoItemVM>();
        public ObservableCollection<CargoItemVM> CargoItems { get; } = new ObservableCollection<CargoItemVM>();
        public CargoItemVM GetItemVM(Guid guid)
        {
            return _cargoItemsDict[guid]; 
        }
        public CargoItemVM GetOrAddItemVM(ICargoable cargoableItem)
        {
            if (!_cargoItemsDict.ContainsKey(cargoableItem.ID))
            {
                var itemVM = new CargoItemVM(cargoableItem);
                _cargoItemsDict.Add(cargoableItem.ID, itemVM);
                CargoItems.Add(itemVM);
            }
            return _cargoItemsDict[cargoableItem.ID];
        }
        public bool ContainsItem(Guid guid)
        {
            return _cargoItemsDict.ContainsKey(guid); 
        }
        private long _maxCapacity;
        public string MaxCapacity { get; set; }
        private long _freeCapacity;

        public event PropertyChangedEventHandler PropertyChanged;

        public string FreeCapacity { get; set; }

        public CargoTypeStoreVM(StaticDataStore staticDataStore, Guid typeGuid, CargoTypeStore typeStore)
        {

            _staticData = staticDataStore;
            _typeStore = typeStore;

            var _cargoTypeSD = staticDataStore.CargoTypes[typeGuid];
            StorageTypeName = _cargoTypeSD.Name;
            StorageTypeDescription = _cargoTypeSD.Description;

            Update();
        }


        public void Update()
        {
            if(_maxCapacity != _typeStore.MaxCapacity) {
                _maxCapacity = _typeStore.MaxCapacity;
                MaxCapacity = _maxCapacity.ToString();

            }
            if(_freeCapacity != _typeStore.FreeCapacity) {
                _freeCapacity = _typeStore.FreeCapacity;
                FreeCapacity = _freeCapacity.ToString();
            }

            HeaderText = StorageTypeName + " " + FreeCapacity + "/" + MaxCapacity + " Free";
            
            foreach(var kvp in _typeStore.ItemsAndAmounts.ToArray())
            {
                if(!_cargoItemsDict.ContainsKey(kvp.Key))
                { //if the key from the DB's dictionary is not in our dictionary here
                    var newCargoItemVM = new CargoItemVM(_staticData.GetICargoable(kvp.Key)); //then create a new CargoItemVM
                    _cargoItemsDict.Add(kvp.Key, newCargoItemVM); //add it to the dictionary
                    CargoItems.Add(newCargoItemVM); //then add it to the observable collection
                }
                _cargoItemsDict[kvp.Key].Update(kvp.Value); //since the object in the observable collection is the same object as the one in the dictionary, update the object via the dictionary
            }

            foreach(var key in _cargoItemsDict.Keys.ToArray()) {
                if(!_typeStore.ItemsAndAmounts.ContainsKey(key)) {
                    CargoItems.Remove(_cargoItemsDict[key]);
                    _cargoItemsDict.Remove(key);
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

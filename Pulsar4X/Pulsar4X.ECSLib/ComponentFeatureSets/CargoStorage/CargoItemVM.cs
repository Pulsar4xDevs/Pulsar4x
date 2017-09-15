using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class CargoItemVM : INotifyPropertyChanged
    {

        private ICargoable _cargoableItem;
        internal Guid ItemGuid
        {
            get
            {
                return _cargoableItem.ID;
            }
        }
        private long _itemCount = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ItemName { get { return _cargoableItem.Name; } }
        public string ItemWeightPerUnit { get { return _cargoableItem.Mass.ToString(); } }
        private string _noOfItems;
        public string NumberOfItems {
            get { return _noOfItems; } set { _noOfItems = value; OnPropertyChanged(); }
        }

        private string _totalWeight;

        public string TotalWeight
        {
            get { return _totalWeight; }
            set
            {
                _totalWeight = value;
                OnPropertyChanged();
            }
        }


        internal CargoItemVM(ICargoable cargoableItem)
        {
            _cargoableItem = cargoableItem;
        }

        private void setTotalWeight()
        {
            double totalWeight = _itemCount * _cargoableItem.Mass;
            if(totalWeight > 1000) {
                totalWeight = totalWeight * 0.001;
                TotalWeight = totalWeight.ToString("0.###") + "T";
            }
            else { TotalWeight = totalWeight.ToString("0.###") + "Kg"; } //add KT and MT?
        }

        internal void Update(long itemCount)
        {
            
            if(_itemCount != itemCount) {
                _itemCount = itemCount;
                NumberOfItems = _itemCount.ToString();
                setTotalWeight();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

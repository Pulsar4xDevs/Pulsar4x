using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class CargoItemVM : INotifyPropertyChanged
    {

        public ICargoable CargoableItem;
        internal Guid ItemGuid
        {
            get
            {
                return CargoableItem.ID;
            }
        }
        private long _itemCount = 0;
        public long ItemStoredCount { get { return _itemCount; } }
        public long ItemsTotal      { get { return _itemCount + ItemIncomingAmount - ItemOutgoingAmount; } } 
        public long ItemIncomingAmount = 0;
        public long ItemOutgoingAmount = 0;
        public event PropertyChangedEventHandler PropertyChanged;

        public string ItemName { get { return CargoableItem.Name; } }
        public string ItemMassPerUnit { get { return CargoableItem.Density.ToString(); } }
        private string _noOfItems;
        public string NumberOfItems {
            get { return _noOfItems; } set { _noOfItems = value; OnPropertyChanged(); }
        }
        public string GetIncomingMass()
        {
            return Stringify.Mass(ItemIncomingAmount * CargoableItem.Density, "0.###"); 
        }
        public string GetOutgoingMass()
        {
            return Stringify.Mass(ItemOutgoingAmount * CargoableItem.Density, "0.###");
        }
        public string GetStoredMass()
        {
            return Stringify.Mass(_itemCount * CargoableItem.Density, "0.###");
        }

        private string _totalMass;

        public string TotalMass
        {
            get { return _totalMass; }
            set
            {
                _totalMass = value;
                OnPropertyChanged();
            }
        }


        public CargoItemVM(ICargoable cargoableItem)
        {
            CargoableItem = cargoableItem;
        }

        private void setTotalMass()
        {
            double totalMass = _itemCount * CargoableItem.Density;
            TotalMass = Stringify.Mass(totalMass, "0.###");
        }

        public void Update(long itemCount)
        {
            
            if(_itemCount != itemCount) {
                _itemCount = itemCount;
                NumberOfItems = _itemCount.ToString();
                setTotalMass();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

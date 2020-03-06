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
        public string ItemWeightPerUnit { get { return CargoableItem.Mass.ToString(); } }
        private string _noOfItems;
        public string NumberOfItems {
            get { return _noOfItems; } set { _noOfItems = value; OnPropertyChanged(); }
        }
        public string GetIncomingWeight()
        {
            return Misc.StringifyMass(ItemIncomingAmount * CargoableItem.Mass, "0.###"); 
        }
        public string GetOutgoungWeight()
        {
            return Misc.StringifyMass(ItemOutgoingAmount * CargoableItem.Mass, "0.###");
        }
        public string GetStoredWeight()
        {
            return Misc.StringifyMass(_itemCount * CargoableItem.Mass, "0.###");
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


        public CargoItemVM(ICargoable cargoableItem)
        {
            CargoableItem = cargoableItem;
        }

        private void setTotalWeight()
        {
            double totalWeight = _itemCount * CargoableItem.Mass;
            TotalWeight = Misc.StringifyMass(totalWeight, "0.###");
        }

        public void Update(long itemCount)
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

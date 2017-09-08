using System;
namespace Pulsar4X.ECSLib
{
    public class CargoItemVM
    {

        private ICargoable _cargoableItem;
        internal Guid ItemGuid
        {
            get
            {
                return _cargoableItem.ID;
            }
        }
        private long _itemCount;

        public string ItemName { get { return _cargoableItem.Name; } }
        public string ItemWeightPerUnit { get { return _cargoableItem.Mass.ToString(); } }
        public string NumberOfItems { get; set; }
        public string TotalWeight { get; set; }


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

    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class CargoOrdersVM : INotifyPropertyChanged, IDBViewmodel
    {
        public string CurrentEntityName
        {
            get
            {
                return _currentEntity.GetDataBlob<NameDB>().DefaultName;
            }
        }
        public string LoadFromEntityName
        {
            get
            {
                return _currentEntity.GetDataBlob<NameDB>().DefaultName;
            }
        }

        long cargoAmount;
        public long CargoAmount
        {
            get
            {
                return cargoAmount;
            }
            set
            {
                if(value != cargoAmount) {
                    cargoAmount = value;
                    OnPropertyChanged();
                }
            }
        }


        Entity _faction { get; set; }
        Entity _currentEntity { get; set; }
        Entity _loadFromEntity { get; set; }
        List<Entity> _selectableEntites { get; set; }
        List<ICargoable> _selectableCargos { get; set; }
        ICargoable CargoItem { get; set; }

        public CargoOrdersVM()
        {

        }
        public void initalize(Entity factionEntity, Entity entity)
        {
            _faction = factionEntity;
            _currentEntity = entity;
        }

        public void SendNewLoadCargoOrder(DateTime currentDatetime)
        {
            var newOrder = new CargoLoadOrder();
            newOrder.CreatedDate = currentDatetime;
            newOrder.RequestingFactionGuid = _faction.Guid;
            newOrder.LoadCargoFromEntityGuid = _loadFromEntity.Guid;
            newOrder.TargetEntityGuid = _faction.Guid;
            newOrder.ItemToTransfer = CargoItem.ID;
            newOrder.TotalAmountToTransfer = CargoAmount;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }

}

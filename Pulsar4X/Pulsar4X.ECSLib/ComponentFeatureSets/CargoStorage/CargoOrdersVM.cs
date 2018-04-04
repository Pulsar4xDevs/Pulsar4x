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
        IOrderHandler _orderHandler;
        
        Entity _faction { get; set; }
        Entity _currentEntity { get; set; }

        Entity _loadFromEntity { get; set; }
        public string CurrentEntityName { get; set; }

        public string LoadFromEntityName { get; set; }

        List<Entity> _selectableEntites { get; set; }

        public CargoStorageVM LoadFromCargoVM { get; set; } 

        List<ICargoable> _selectableCargos { get; set; } 

        ICargoable SelectedCargoItem { get; set; }
        long AmountToTransfer { get; set; }
        public CommandReferences CmdRef { get; set; }

        public CargoOrdersVM(Game game)
        {
            _orderHandler = game.OrderHandler;
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
            newOrder.EntityCommandingGuid = _faction.Guid;
            newOrder.ItemToTransfer = SelectedCargoItem.ID;
            newOrder.TotalAmountToTransfer = AmountToTransfer;
            _orderHandler.HandleOrder(newOrder);
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

using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class ShipOrderVM : IViewModel
    {
        public DictionaryVM<StarSystem, string> StarSystems;
        public DictionaryVM<Entity, string> ShipList;

        public DictionaryVM<Entity, string> TargetList;

        public DictionaryVM<BaseOrder, string> OrdersPossible;
        public DictionaryVM<BaseOrder, string> OrderList;

        public Entity SelectedShip;
        public BaseOrder SelectedPossibleOrder;
        public BaseOrder SelectedOrder;
        public Entity SelectedTarget;

        public Boolean TargetShown;

        public StarSystem SelectedSystem;

        private GameVM _gameVM;
        public GameVM GameVM { get { return _gameVM; } }

        public ShipOrderVM(GameVM game)
        {
            _gameVM = game;

            ShipList = new DictionaryVM<Entity, string>();
            TargetList = new DictionaryVM<Entity, string>();
            OrdersPossible = new DictionaryVM<BaseOrder, string>();
            OrderList = new DictionaryVM<BaseOrder, string>();

            FactionInfoDB finfo = _gameVM.CurrentFaction.GetDataBlob<FactionInfoDB>();
            StarSystems = new DictionaryVM<StarSystem, string>();
            foreach (SystemVM system in _gameVM.StarSystems)
            {
                if(finfo.KnownSystems.Contains(system.StarSystem.Guid))
                {
                    StarSystems.Add(system.StarSystem, system.StarSystem.NameDB.GetName(_gameVM.CurrentFaction));
                }
            }

            StarSystems.SelectedIndex = 0;

            SelectedSystem = StarSystems.SelectedKey;

            TargetShown = false;

            RefreshShips();

        }

        public static ShipOrderVM Create(GameVM game)
        {
            
            return new ShipOrderVM(game);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            SelectedSystem = StarSystems.SelectedKey;

            RefreshShips();
        }

        private void Ship_SelectionChangedEvent(int oldSelection, int newSelection)
        {


            RefreshOrders();
        }

        // Updates the list of ships to give orders to and targets when the system changes
        private void RefreshShips()
        {
            ShipList.Clear();
            foreach(Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            ShipList.SelectedIndex = 0;

            SelectedShip = ShipList.SelectedKey;

            TargetList.Clear();
            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                TargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            TargetList.SelectedIndex = 0;

            SelectedTarget = TargetList.SelectedKey;

            RefreshOrders();

            return;
        }

        private void RefreshOrders()
        {
            

            OrdersPossible.Clear();

            if (SelectedShip.HasDataBlob<PropulsionDB>())
                OrdersPossible.Add(new MoveOrder(), "Move to");

            OrdersPossible.SelectedIndex = 0;

            SelectedPossibleOrder = OrdersPossible.SelectedKey;

            if (OrdersPossible.SelectedKey.OrderType == orderType.MOVETO)
                TargetShown = true;
            else
                TargetShown = false;

            List<BaseOrder> orders = new List<BaseOrder>(SelectedShip.GetDataBlob<ShipInfoDB>().Orders);

            OrderList.Clear();

            foreach(BaseOrder order in orders)
            {
                string orderDescription = "";

                switch(order.OrderType)
                {
                    case orderType.MOVETO:
                        MoveOrder moveOrder = (MoveOrder)order;
                        orderDescription += "Move to ";
                        orderDescription += moveOrder.Target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction);
                        break;
                    default:
                        break;
                }


                OrderList.Add(order, orderDescription);
            }

            OnPropertyChanged();


            return;
        }

        public void OnAddOrder()
        {
            // Check if Ship, Target, and Order are set
            if (SelectedShip == null  || SelectedTarget == null || SelectedPossibleOrder == null) 
                return;
            switch(SelectedPossibleOrder.OrderType)
            {
                case orderType.MOVETO:
                    _gameVM.CurrentPlayer.Orders.MoveOrder(SelectedShip, SelectedTarget);
                    break;
                case orderType.INVALIDORDER:
                    break;
                default:
                    break;
            }

            _gameVM.CurrentPlayer.ProcessOrders();

            RefreshOrders();
        }

        private ICommand _addOrder;
        public ICommand AddOrder
        {
            get
            {
                return _addOrder ?? (_addOrder = new CommandHandler(OnAddOrder, true));
            }
        }


    }
}

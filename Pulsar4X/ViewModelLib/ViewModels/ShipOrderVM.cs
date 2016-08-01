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

        private DictionaryVM<StarSystem, string> _starSystems = new DictionaryVM<StarSystem, string>();
        public DictionaryVM<StarSystem, string> StarSystems 
        { 
            get 
            { 
                return _starSystems; 
            } 
            set 
            { 
                _starSystems = value;
                _starSystems.SelectedIndex = 0;
                RefreshShips(0, 0); 
            } 
        } //these must be properties

        private DictionaryVM<Entity, string> _shipList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> ShipList 
        { 
            get 
            { 
                return _shipList; 
            } 
            set 
            { 
                _shipList = value;
                _shipList.SelectedIndex = 0;
                RefreshOrders(0,0); 
            } 
        }

        private DictionaryVM<Entity, string> _moveTargetList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> MoveTargetList 
        { 
            get 
            { 
                return _moveTargetList; 
            } 
            set 
            {
                _moveTargetList = value;
                _moveTargetList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedMoveTarget));
            } 
        } //not fields!

        private DictionaryVM<Entity, string> _attackTargetList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> AttackTargetList
        {
            get
            {
                return _attackTargetList;
            }
            set
            {
                _attackTargetList = value;
                _attackTargetList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedAttackTarget));
            }
        } //not fields!

        private DictionaryVM<BaseOrder, string> _moveOrdersPossible = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> MoveOrdersPossible 
        { 
            get
            { 
                return _moveOrdersPossible; 
            } 
            set
            {
                _moveOrdersPossible = value;
                _moveOrdersPossible.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedPossibleMoveOrder));
            }
        }
        private DictionaryVM<BaseOrder, string> _moveOrderList = new DictionaryVM<BaseOrder, string>();
        public DictionaryVM<BaseOrder, string> MoveOrderList 
        {
            get
            { 
                return _moveOrderList;
            }
            set
            {
                _moveOrderList = value;
                _moveOrderList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedMoveOrder));
            }
        }

        private DictionaryVM<Entity, string> _fireControlList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> FireControlList
        {
            get
            {
                return _fireControlList;
            }
            set
            {
                _fireControlList = value;
                _fireControlList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedFireControl));
            }
        }

        private DictionaryVM<Entity, string> _attachedBeamList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> AttachedBeamList
        {
            get
            {
                return _attachedBeamList;
            }
            set
            {
                _attachedBeamList = value;
                _attachedBeamList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedAttachedBeam));
            }
        }

        private DictionaryVM<Entity, string> _freeBeamList = new DictionaryVM<Entity, string>();
        public DictionaryVM<Entity, string> FreeBeamList
        {
            get
            {
                return _freeBeamList;
            }
            set
            {
                _freeBeamList = value;
                _freeBeamList.SelectedIndex = 0;
                OnPropertyChanged(nameof(SelectedFreeBeam));
            }
        }

        private Entity _targetedEntity;

        public StarSystem SelectedSystem { get { return _starSystems.SelectedKey; }}
        public Entity SelectedShip { get { return _shipList.SelectedKey; }}
        public BaseOrder SelectedPossibleMoveOrder { get { return _moveOrdersPossible.SelectedKey; } }
        public BaseOrder SelectedMoveOrder { get { return _moveOrderList.SelectedKey; } }
        public Entity SelectedMoveTarget { get { return _moveTargetList.SelectedKey; } }
        public Entity SelectedAttackTarget { get { return _attackTargetList.SelectedKey; } }
        public Entity SelectedFireControl { get { return _fireControlList.SelectedKey; } }
        public Entity SelectedAttachedBeam { get { return _attachedBeamList.SelectedKey; } }
        public Entity SelectedFreeBeam { get { return _freeBeamList.SelectedKey; } }
        public string TargetedEntity { get { return _targetedEntity.GetDataBlob<NameDB>().DefaultName; } }

        public Boolean TargetShown { get; internal set; }
        public int TargetAreaWidth { get; internal set; }

        

        public string ShipSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Length()).ToString("N2");
            }
        }

        public string XSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.X).ToString("N2");
            }
        }

        public string YSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.ToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Y).ToString("N2");
            }
        }

        public string XPos
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PositionDB>().X.ToString("N5");
            }
        }

        public string YPos
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PositionDB>().Y.ToString("N5");
            }
        }

        public string MoveTargetDistance
        {
            get
            {
                if (SelectedShip == null)
                    return "N/A";
                if (SelectedMoveTarget == null)
                    return "N/A";

                Vector4 delta = SelectedShip.GetDataBlob<PositionDB>().AbsolutePosition - SelectedMoveTarget.GetDataBlob<PositionDB>().AbsolutePosition;
                return Distance.ToKm(delta.Length()).ToString("N2") ;
            }
        }

        private GameVM _gameVM;
        public GameVM GameVM { get { return _gameVM; } }

        public ShipOrderVM(GameVM game)
        {
            _gameVM = game;

            FactionInfoDB finfo = _gameVM.CurrentFaction.GetDataBlob<FactionInfoDB>();
            foreach (SystemVM system in _gameVM.StarSystems)
            {
                if(finfo.KnownSystems.Contains(system.StarSystem.Guid))
                {
                    _starSystems.Add(system.StarSystem, system.StarSystem.NameDB.GetName(_gameVM.CurrentFaction));
                }
            }

            _starSystems.SelectedIndex = 0;

            TargetShown = false;
            TargetAreaWidth = 2;

            RefreshShips(0, 0);

            //PropertyChanged += ShipOrderVM_PropertyChanged;
            SelectedSystem.SystemSubpulses.SystemDateChangedEvent += UpdateInterface_SystemDateChangedEvent;

            _starSystems.SelectionChangedEvent += RefreshShips;
            _shipList.SelectionChangedEvent += RefreshOrders;
            _shipList.SelectionChangedEvent += RefreshFireControlList;
            _moveOrdersPossible.SelectionChangedEvent += RefreshTarget;
            _moveTargetList.SelectionChangedEvent += RefreshTargetDistance;

            OnPropertyChanged(nameof(StarSystems));
            OnPropertyChanged(nameof(SelectedSystem));
        }


        // Not 100% on events, but hopefully this will do
        public void UpdateInterface_SystemDateChangedEvent(DateTime newDate)
        {
            OnPropertyChanged(nameof(ShipSpeed));
            OnPropertyChanged(nameof(XSpeed));
            OnPropertyChanged(nameof(YSpeed));
            OnPropertyChanged(nameof(XPos));
            OnPropertyChanged(nameof(YPos));
            OnPropertyChanged(nameof(MoveTargetDistance));
            RefreshOrderList(0, 0);
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
            OnPropertyChanged(nameof(StarSystems));
            RefreshShips(0, 0);

        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            RefreshShips(0, 0);
        }

        // Updates the list of ships to give orders to and targets when the system changes
        public void RefreshShips(int a, int b)
        {
            if (SelectedSystem == null)
                return;
            _shipList.Clear();
            foreach(Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _shipList.SelectedIndex = 0;

            RefreshTarget(0, 0);
            RefreshFireControlList(0, 0);

            OnPropertyChanged(nameof(ShipList));
            OnPropertyChanged(nameof(MoveTargetList));

            OnPropertyChanged(nameof(SelectedShip));
            OnPropertyChanged(nameof(SelectedMoveTarget));

            return;
        }

        public void RefreshTarget(int a, int b)
        {

            if (_starSystems.SelectedIndex == -1) //if b is not a valid selection
                return;

            int targetIndex = _moveTargetList.SelectedIndex;

            _moveTargetList.Clear();
            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                if(target != SelectedShip)
                    _moveTargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _moveTargetList.SelectedIndex = targetIndex;

            if (SelectedPossibleMoveOrder == null)
                TargetShown = false;
            else if (SelectedPossibleMoveOrder.OrderType == orderType.MOVETO)
                TargetShown = true;
            else
                TargetShown = false;

            if (TargetShown)
                TargetAreaWidth = 200;
            else
                TargetAreaWidth = 2;

            OnPropertyChanged(nameof(TargetShown));
            OnPropertyChanged(nameof(TargetAreaWidth));
        }

        public void RefreshTargetDistance(int a, int b)
        {
            OnPropertyChanged(nameof(MoveTargetDistance));
        }

        public void RefreshOrders(int a, int b)
        {
            if (SelectedShip == null)
                return;

            _moveOrdersPossible.Clear();

            if (SelectedShip.HasDataBlob<PropulsionDB>())
                _moveOrdersPossible.Add(new MoveOrder(), "Move to");

            _moveOrdersPossible.SelectedIndex = 0;

            RefreshOrderList(0, 0);



            OnPropertyChanged(nameof(SelectedMoveOrder));
            OnPropertyChanged(nameof(SelectedPossibleMoveOrder));

            OnPropertyChanged(nameof(ShipSpeed));
            OnPropertyChanged(nameof(XSpeed));
            OnPropertyChanged(nameof(YSpeed));
            OnPropertyChanged(nameof(XPos));
            OnPropertyChanged(nameof(YPos));


            return;
        }

        public void RefreshOrderList(int a, int b)
        {
            if (SelectedShip == null)
                return;
            List<BaseOrder> orders = new List<BaseOrder>(SelectedShip.GetDataBlob<ShipInfoDB>().Orders);

            _moveOrderList.Clear();

            foreach (BaseOrder order in orders)
            {
                string orderDescription = "";

                switch (order.OrderType)
                {
                    case orderType.MOVETO:
                        MoveOrder moveOrder = (MoveOrder)order;
                        orderDescription += "Move to ";
                        orderDescription += moveOrder.Target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction);
                        break;
                    default:
                        break;
                }
                _moveOrderList.Add(order, orderDescription);
            }

            OnPropertyChanged(nameof(MoveOrderList));
            OnPropertyChanged(nameof(MoveOrdersPossible));
        }

        public void RefreshFireControlList(int a, int b)
        {
            if (SelectedShip == null)
                return;

            if (!SelectedShip.HasDataBlob<BeamWeaponsDB>())
                return;

            _fireControlList.Clear();

            List<KeyValuePair<Entity, List<Entity>>> fcList = new List<KeyValuePair<Entity, List<Entity>>>(SelectedShip.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamFireControlAtbDB>()).ToList());

            foreach(KeyValuePair<Entity, List<Entity>> kvp in fcList)
            {
                    if (kvp.Key.HasDataBlob<BeamFireControlAtbDB>())
                        _fireControlList.Add(new KeyValuePair<Entity, string>(kvp.Key, kvp.Key.GetDataBlob<NameDB>().DefaultName));
                
            }

            _fireControlList.SelectedIndex = 0;

            OnPropertyChanged(nameof(FireControlList));

        }

        public void OnAddOrder()
        {
            // Check if Ship, Target, and Order are set
            if (SelectedShip == null  || SelectedMoveTarget == null || SelectedPossibleMoveOrder == null) 
                return;
            switch(SelectedPossibleMoveOrder.OrderType)
            {
                case orderType.MOVETO:
                    _gameVM.CurrentPlayer.Orders.MoveOrder(SelectedShip, SelectedMoveTarget);
                    break;
                case orderType.INVALIDORDER:
                    break;
                default:
                    break;
            }

            _gameVM.CurrentPlayer.ProcessOrders();

            RefreshOrders(0,0);
            
        }

        public void OnRemoveOrder()
        {


            if (SelectedShip == null)
                return;

            BaseOrder nextOrder;
            Queue<BaseOrder> orderList = SelectedShip.GetDataBlob<ShipInfoDB>().Orders;


            int totalOrders = orderList.Count;

            for (int i = 0; i < totalOrders; i++)
            {
                nextOrder = orderList.Dequeue();
                if(nextOrder != SelectedMoveOrder)
                    orderList.Enqueue(nextOrder);
            }

            
            RefreshOrders(0,0);
        }

        public void OnAddBeam()
        {

        }

        public void OnRemoveBeam()
        {

        }

        private ICommand _addOrder;
        public ICommand AddOrder
        {
            get
            {
                return _addOrder ?? (_addOrder = new CommandHandler(OnAddOrder, true));
            }
        }

        private ICommand _removeOrder;
        public ICommand RemoveOrder
        {
            get
            {
                return _removeOrder ?? (_removeOrder = new CommandHandler(OnRemoveOrder, true));
            }
        }

        private ICommand _addBeam;
        public ICommand AddBeam
        {
            get
            {
                return _addBeam ?? (_addBeam = new CommandHandler(OnAddBeam, true));
            }
        }

        private ICommand _removeBeam;
        public ICommand RemoveBeam
        {
            get
            {
                return _removeBeam ?? (_removeBeam = new CommandHandler(OnRemoveBeam, true));
            }
        }

    }
}

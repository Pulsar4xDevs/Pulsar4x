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
        }

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
        } 

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

        public StarSystem SelectedSystem { get { return _starSystems.SelectedKey; }}
        public Entity SelectedShip { get { return _shipList.SelectedKey; }}
        public BaseOrder SelectedPossibleMoveOrder { get { return _moveOrdersPossible.SelectedKey; } }
        public BaseOrder SelectedMoveOrder { get { return _moveOrderList.SelectedKey; } }
        public Entity SelectedMoveTarget { get { return _moveTargetList.SelectedKey; } }
        public Entity SelectedAttackTarget { get { return _attackTargetList.SelectedKey; } }
        public Entity SelectedFireControl { get { return _fireControlList.SelectedKey; } }
        public Entity SelectedAttachedBeam { get { return _attachedBeamList.SelectedKey; } }
        public Entity SelectedFreeBeam { get { return _freeBeamList.SelectedKey; } }

        private Entity _targetedEntity;
        public string TargetedEntity {
            get
            { if (_targetedEntity == null)
                    return "None";
              else
                    return _targetedEntity.GetDataBlob<NameDB>().DefaultName;
            }
        }

        public Boolean TargetShown { get; internal set; }
        public int TargetAreaWidth { get; internal set; }

        

        public string ShipSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Length()).ToString("N2");
            }
        }

        public string XSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.X).ToString("N2");
            }
        }

        public string YSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return Distance.AuToKm(SelectedShip.GetDataBlob<PropulsionDB>().CurrentSpeed.Y).ToString("N2");
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

        public string MaxSpeed
        {
            get
            {
                if (SelectedShip == null)
                    return "";
                return SelectedShip.GetDataBlob<PropulsionDB>().MaximumSpeed.ToString("N5");
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
                return Distance.AuToKm(delta.Length()).ToString("N2") ;
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
            SelectedSystem.SystemManager.ManagerSubpulses.SystemDateChangedEvent += UpdateInterface_SystemDateChangedEvent;

            _starSystems.SelectionChangedEvent += RefreshShips;
            _shipList.SelectionChangedEvent += RefreshOrders;
            _shipList.SelectionChangedEvent += RefreshFireControlList;
            _moveOrdersPossible.SelectionChangedEvent += RefreshTarget;
            _moveTargetList.SelectionChangedEvent += RefreshTargetDistance;
            _fireControlList.SelectionChangedEvent += RefreshBeamWeaponsList;
            _fireControlList.SelectionChangedEvent += RefreshFCTarget;

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
            OnPropertyChanged(nameof(MaxSpeed));
            OnPropertyChanged(nameof(MoveTargetDistance));
            RefreshOrderList(0, 0);
            RefreshFireControlList(0, 0);
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
            if (SelectedSystem == null || _starSystems.SelectedIndex == -1)
                return;

            _shipList.Clear();
            foreach(Entity ship in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<ShipInfoDB>(_gameVM.CurrentAuthToken))
            {
                if (ship.HasDataBlob<PropulsionDB>())
                    ShipList.Add(ship, ship.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
            }

            _shipList.SelectedIndex = 0;

            //RefreshTarget(0, 0);

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

            _moveTargetList.Clear();
            _attackTargetList.Clear();

            int moveTargetIndex = _moveTargetList.SelectedIndex;
            int attackTargetIndex = _attackTargetList.SelectedIndex;

            foreach (Entity target in SelectedSystem.SystemManager.GetAllEntitiesWithDataBlob<PositionDB>(_gameVM.CurrentAuthToken))
            {
                if(target != SelectedShip)
                {
                    _moveTargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));
                    if (target.HasDataBlob<SensorProfileDB>())
                        _attackTargetList.Add(target, target.GetDataBlob<NameDB>().GetName(_gameVM.CurrentFaction));

                }
                    
            }

            _moveTargetList.SelectedIndex = moveTargetIndex;
            _attackTargetList.SelectedIndex = attackTargetIndex;

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

        public void RefreshFCTarget(int a, int b)
        {
            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return; 

            _targetedEntity = SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().Target;
            OnPropertyChanged(TargetedEntity);
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
            OnPropertyChanged(nameof(MaxSpeed));

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
            _fireControlList.Clear();

            if (SelectedShip == null)
                return;

            if (!SelectedShip.HasDataBlob<BeamWeaponsDB>())
            {
                _fireControlList.Clear();
                return;
            }



            // The component instances all seem to think that their parent entity is Ensuing Calm, regardless of SelectedShip
            List<KeyValuePair<Entity, List<Entity>>> fcList = new List<KeyValuePair<Entity, List<Entity>>>(SelectedShip.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamFireControlAtbDB>()).ToList());

            foreach(KeyValuePair<Entity, List<Entity>> kvp in fcList)
            {
                int fcCount = 0;
                if (kvp.Key.HasDataBlob<BeamFireControlAtbDB>())
                foreach(Entity instance in kvp.Value)
                {
                    fcCount++;
                    _fireControlList.Add(instance, kvp.Key.GetDataBlob<NameDB>().DefaultName + fcCount);
                }
                        
                
            }

            _fireControlList.SelectedIndex = 0;

            

            RefreshBeamWeaponsList(0, 0);

//            OnPropertyChanged(nameof(FireControlList));

        }

        public void RefreshBeamWeaponsList(int a, int b)
        {
            _attachedBeamList.Clear();
            _freeBeamList.Clear();

            if (SelectedShip == null || _shipList.SelectedIndex == -1)
                return;

            if (_fireControlList.Count > 0 && _fireControlList.SelectedIndex != -1)
            {
                int beamCount = 0;
                foreach (Entity beam in SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().AssignedWeapons)
                {
                    beamCount++;
                    _attachedBeamList.Add(beam, beam.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity.GetDataBlob<NameDB>().DefaultName + " " + beamCount);
                }

            }
            else
                _attachedBeamList.Clear();

            List<KeyValuePair<Entity, List<Entity>>> beamList = new List<KeyValuePair<Entity, List<Entity>>>(SelectedShip.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamWeaponAtbDB>() || item.Key.HasDataBlob<SimpleBeamWeaponAtbDB>()).ToList());

            bool isBeamControlled = false;
            _freeBeamList.Clear();

            // Get a list of all beam weapons not currently controlled by a fire control
            // @todo: make sure you check all fire controls - currently only lists
            // beams not set to the current fire control
            foreach (KeyValuePair<Entity, List<Entity>> kvp in beamList)
            {
                int beamCount = 0;
                foreach (Entity instance in kvp.Value)
                {
                    if (instance.GetDataBlob<WeaponStateDB>().FireControl == null)
                        _freeBeamList.Add(new KeyValuePair<Entity, string>(instance, kvp.Key.GetDataBlob<NameDB>().DefaultName + " " + ++beamCount));

                }
            }

            OnPropertyChanged(nameof(AttachedBeamList));
            OnPropertyChanged(nameof(FreeBeamList));

        }

        private bool IsBeamInFireControlList(Entity beam)
        {
            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return false;

            List<KeyValuePair<Entity, List<Entity>>> fcList = new List<KeyValuePair<Entity, List<Entity>>>(SelectedShip.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<BeamFireControlAtbDB>()).ToList());

            foreach (KeyValuePair<Entity, List<Entity>> kvp in fcList)
            {
                foreach (Entity instance in kvp.Value)
                {
                    if (SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().AssignedWeapons.Contains(beam))
                        return true;
                }
            }

            return false;
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
            Entity beam = SelectedFreeBeam;

            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return;

            if (SelectedFreeBeam == null || _freeBeamList.SelectedIndex == -1)
                return;

            List<Entity> weaponList = SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().AssignedWeapons;
            weaponList.Add(SelectedFreeBeam);

            // @todo: set the fire control for the beam
            beam.GetDataBlob<WeaponStateDB>().FireControl = SelectedFireControl;

            RefreshBeamWeaponsList(0, 0);
        }

        public void OnRemoveBeam()
        {
            Entity beam = SelectedAttachedBeam;


            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return;

            if (SelectedAttachedBeam == null || _attachedBeamList.SelectedIndex == -1)
                return;

            List<Entity> weaponList = SelectedFireControl.GetDataBlob<FireControlInstanceAbilityDB>().AssignedWeapons;
            weaponList.Remove(SelectedAttachedBeam);

            // @todo: unset the fire control for the beam

            beam.GetDataBlob<WeaponStateDB>().FireControl = null;

            RefreshBeamWeaponsList(0, 0);
        }

        public void OnAddTarget()
        {
            Entity fc = SelectedFireControl;
            Entity target = SelectedAttackTarget;

            if (SelectedFireControl == null || _fireControlList.SelectedIndex == -1)
                return;

            if (SelectedAttackTarget == null || _attackTargetList.SelectedIndex == -1)
                return;

            fc.GetDataBlob<FireControlInstanceAbilityDB>().Target = target;
            // Get the currently selected ship and fire control and the currently selected list of targets
            // Add the currently selected target to the selected ship's target
            // Update GUI

            RefreshFireControlList(0, 0);
        }

        public void OnRemoveTarget()
        {
            // Get the currently selected ship fire control
            // Clear its selected target
            // Update GUI

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

        private ICommand _addTarget;
        public ICommand AddTarget
        {
            get
            {
                return _addTarget ?? (_addTarget = new CommandHandler(OnAddTarget, true));
            }
        }

        private ICommand _removeTarget;
        public ICommand RemoveTarget
        {
            get
            {
                return _removeTarget ?? (_removeTarget = new CommandHandler(OnRemoveTarget, true));
            }
        }

    }
}

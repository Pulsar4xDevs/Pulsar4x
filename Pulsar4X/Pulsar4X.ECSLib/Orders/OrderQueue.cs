using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    // Orders for ships
    public class OrderQueue
    {
        #region Properties

        List<BaseOrder> _orderList;
        //EntityManager _entityManager;

        #endregion 

        #region Constructors

        // @todo: make sure all types of orders are added 

        public OrderQueue()
        {
            _orderList = new List<BaseOrder>();
            //_entityManager = new EntityManager(new Game());
        }

        public OrderQueue(Game game)
        {
            _orderList = new List<BaseOrder>();
            //_entityManager = new EntityManager(game);
        }

        public OrderQueue(EntityManager em)
        {
            _orderList = new List<BaseOrder>();
            //_entityManager = em;
        }

        public OrderQueue(OrderQueue oq)
        {
            _orderList = _orderList.Select(item => (BaseOrder)item.Clone()).ToList();
            //_entityManager = oq._entityManager;
        }

        #endregion

        #region Public API Functions

        /// <summary>
        /// Returns the length of the order queue
        /// </summary>
        public int Length()
        {
            return (_orderList.ToArray().Length);
        }

        // Creates a new order at a colony to construct something
        public bool ConstructionOrder(Entity colony, string type, long amount)
        {
            throw new NotImplementedException();
        }

        // Creates a new order at a colony to research something
        public bool ResearchOrder(Entity colony, string type, long researchersAmount)
        {
            throw new NotImplementedException();
        }

        // Creates a new order at a colony to toggle a facility's function
        public bool ToggleFacility(Entity colony, string type)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to move to the given coordinates in a star system
        public bool MoveOrder(Entity ship, StarSystem system, double x, double y)
        {
            PositionDB position = new PositionDB(new Vector4(x, y, 0.0, 0.0), system.Guid);
            MoveOrder moveOrder = new MoveOrder(ship, position);

            _orderList.Add(moveOrder);

            return true;
        }

        // Creates a new order for a ship to move to the target Entity
        public bool MoveOrder(Entity ship, Entity target)
        {
            MoveOrder moveOrder = new MoveOrder(ship, target);


            _orderList.Add(moveOrder);

            return true;

        }

        // Creates a new order for a ship to jump to another system through the given jump point
        public bool JumpSystemOrder(Entity ship, Entity jumpPoint)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to attack the target with a beam weapon
        public bool BeamAttackOrder(Entity ship, Entity target, Entity weaponSystem)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to attack the target with a missile weapon
        public bool MissileAttackOrder(Entity ship, Entity target, Entity weaponSystem)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to survey the given systemBody
        public bool SurveyOrder(Entity ship, Entity systemBody)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to survey the given jump survey point
        public bool SurveyJumpOrder(Entity ship, Entity systemBody)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to move cargo from the origin to the target
        public bool MoveCargoOrder(Entity ship, Entity origin, Entity target, Guid cargo, double amount)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to load cargo from the origin
        public bool LoadCargo(Entity ship, Entity origin, string cargo, long amount)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to unload cargo to the target
        public bool UnloadCargo(Entity ship, Entity target, string cargo, long amount)
        {
            throw new NotImplementedException();
        }

        // Processes the next order in the order list.  Checks for validity, then returns the order for loading into the colony or ship.  If invalid, it 
        // returns null
        public BaseOrder ProcessOrder()
        {
            if (_orderList.Count == 0)
                return null;
            BaseOrder order = _orderList.First<BaseOrder>();
            // Check order for validity
            if (order == null)
                return null;

            _orderList.Remove(order);

            // Check order's IsValid function
            if (!order.isValid())
                return null;

            return order;

            //  throw new NotImplementedException();
        }
        
        //@todo: after testing, make this private
        public BaseOrder PeekNextOrder()
        {
            return _orderList.First<BaseOrder>();
        }

        public void ClearOrders()
        {
            if (_orderList.Count == 0)
                return;
            BaseOrder order = _orderList.First<BaseOrder>();
            while (order != null)
            {
                _orderList.Remove(order);
                order = _orderList.First<BaseOrder>();
            }
        }

        public void RemoveOrder(BaseOrder order)
        {
            _orderList.Remove(order);
        }

        public int NumOrders()
        {
            return _orderList.Count;
        }

        #endregion
    }
}

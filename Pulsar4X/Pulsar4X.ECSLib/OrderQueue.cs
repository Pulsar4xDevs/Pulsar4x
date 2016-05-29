using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class OrderQueue
    {
        #region Properties

        List<Entity> _orderList;
        EntityManager _entityManager;

        #endregion 

        #region Constructors

        // @todo: make sure all types of orders are added 

        public OrderQueue()
        {
            _orderList = new List<Entity>();
            _entityManager = new EntityManager(new Game());
        }

        public OrderQueue(Game game)
        {
            _orderList = new List<Entity>();
            _entityManager = new EntityManager(game);
        }

        public OrderQueue(EntityManager em)
        {
            _orderList = new List<Entity>();
            _entityManager = em;
        }

        public OrderQueue(OrderQueue oq)
        {
            _orderList = oq._orderList.Select(item => (Entity)item.Clone()).ToList();
            _entityManager = oq._entityManager;
        }

        #endregion

        #region Public API Functions

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

        // Creates a new order at a colony to mine something
        public bool MiningOrder(Entity colony, string type, long amount)
        {
            throw new NotImplementedException();
        }

        // Creates a new order at a colony to toggle a facility's function
        public bool ToggleFacility(Entity colony, string type)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to move to the given coordinates in a star system
        public bool MoveOrder(Entity ship, Entity System, long x, long y)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to move to the target Entity
        public bool MoveOrder(Entity ship, Entity target)
        {
            throw new NotImplementedException();

            MoveOrderDB moveDB = new MoveOrderDB(ship, target);

            Entity order = new Entity(_entityManager, new List<BaseDataBlob> { moveDB });
            _orderList.Add(order);
            
        }

        // Creates a new order for a ship to jump to another system through the given jump point
        public bool JumpSystemOrder(Entity ship, Entity jumpPoint)
        {
            throw new NotImplementedException();
        }

        // Creates a new order for a ship to attack the target Entity
        public bool AttackOrder(Entity ship, Entity target)
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
        public bool MoveCargoOrder(Entity ship, Entity origin, Entity target, string cargo, long amount)
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
        // returns the InvalidEntity
        public Entity ProcessOrder()
        {
            Entity order = _orderList.First<Entity>();
            _orderList.Remove(order);

            // Check order for validity
            if (order == null)
                return Entity.InvalidEntity;

            BaseOrderDB orderDB = order.GetDataBlob<BaseOrderDB>();

            // Check order's IsValid function
            if (!orderDB.isValid())
                return Entity.InvalidEntity;

            // @todo: more tests?

            return order;

//            throw new NotImplementedException();
        }

        #endregion
    }
}

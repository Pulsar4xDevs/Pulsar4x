using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class OrderQueue
    {
        #region Properties

        List<Entity> _orderList;

        #endregion 

        #region Constructors

        // @todo: make sure all types of orders are added 

        public OrderQueue()
        {
            _orderList = new List<Entity>();
        }

        public OrderQueue(OrderQueue oq)
        {
            _orderList = oq._orderList.Select(item => (Entity)item.Clone()).ToList();
        }

        #endregion

        #region Public API Functions

        public bool ConstructionOrder(Entity colony, string type, long amount)
        {
            throw new NotImplementedException();
        }

        public bool ResearchOrder(Entity colony, string type, long researchersAmount)
        {
            throw new NotImplementedException();
        }

        public bool MiningOrder(Entity colony, string type, long amount)
        {
            throw new NotImplementedException();
        }

        public bool ToggleConstruction(Entity colony, string type)
        {
            throw new NotImplementedException();
        }

        public bool MoveOrder(Entity ship, Entity System, long x, long y)
        {
            throw new NotImplementedException();
        }

        public bool AttackOrder(Entity ship, Entity target)
        {
            throw new NotImplementedException();
        }

        public bool SurveyOrder(Entity ship, Entity systemBody)
        {
            throw new NotImplementedException();
        }

        public bool MoveCargoOrder(Entity ship, Entity target, string cargo, long amount)
        {
            throw new NotImplementedException();
        }



        #endregion
    }
}

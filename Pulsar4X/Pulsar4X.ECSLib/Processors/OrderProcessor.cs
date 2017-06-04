using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;



namespace Pulsar4X.ECSLib
{
    public static class OrderProcessor
    {

        internal static void SetNextInterupt(DateTime estDateTime, BaseAction action )
        {
            action.EstTimeComplete = estDateTime;
            action.ThisEntity.Manager.ManagerSubpulses.AddEntityInterupt(estDateTime, PulseActionEnum.OrderProcess, action.ThisEntity); 
        }

        internal static void ProcessManagerOrders(EntityManager manager)
        {            
            while (manager.OrderQueue.Count > 0) //process all the orders in the manager's order queue.
            {
                BaseOrder nextOrder;
                if(manager.OrderQueue.TryDequeue(out nextOrder));// should I do anything if it's false? (ie threadlocked due to writing) ie wait?
                {                    
                    BaseAction action = nextOrder.CreateAction(manager.Game, nextOrder);
                    action.ThisEntity.GetDataBlob<OrderableDB>().ActionQueue.Add(action);
                }
            }            
        }

        internal static void ProcessActionList(DateTime toDate, EntityManager manager)
        {
            List<Entity> orderableEntities = manager.GetAllEntitiesWithDataBlob<OrderableDB>();
            foreach (var orderableEntity in orderableEntities)
            {
                ProcessActionList(toDate, orderableEntity);
            }          
        }

        internal static void ProcessActionList(DateTime toDate, Entity entity)
        {
            OrderableDB orderableDB = entity.GetDataBlob<OrderableDB>();
            List<BaseAction> actionList = orderableDB.ActionQueue;
            int mask = 1;

            int i = 0;
            while (i < actionList.Count())
            {
                var item = actionList[i];


                if ((mask & item.Lanes) == item.Lanes) //bitwise and
                {
                    if (item.IsBlocking)
                    {
                        mask |= item.Lanes; //bitwise or
                    }
                    item.OrderableProcessor.ProcessAction(toDate, item);
                }                      
                if(item.IsFinished)
                    actionList.RemoveAt(i);
                else 
                    i++;
            }            
        }

        public static bool IsTargetClose(Game game, Entity thisEntity, Entity targetEntity, BaseAction order, int reqiredDistance)
        {
            PositionDB thisPosition = thisEntity.GetDataBlob<PositionDB>();
            PositionDB targetPosition = targetEntity.GetDataBlob<PositionDB>();

            if (thisPosition.GetDistanceTo(targetPosition) <= reqiredDistance) //then we're within range.
            {
                return true;
            }
            return false;
        }
    }
}

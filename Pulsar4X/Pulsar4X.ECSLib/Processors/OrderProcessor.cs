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

        public static void Process(Game game)
        {
            Dictionary<Guid, StarSystem> systems = game.Systems;
            if (game.Settings.EnableMultiThreading ?? false)
            {
                // Process the orderqueue
                Parallel.ForEach(game.Players, player => player.ProcessOrders());
                Parallel.ForEach(systems, system => ProcessSystem(system.Value.SystemManager));
            }
            else
            {
                foreach (var player in game.Players)
                {
                    player.ProcessOrders();
                }

                foreach (var system in systems) //TODO thread this
                {

                    ProcessSystem(system.Value.SystemManager);
                }
            }
        }

/*
        public static void ProcessGanttOrder(Entity entity)
        {
            entity.GetDataBlob<OrderableDB>().OrdersQueue.ProcessCurrentNodes();
        }
*/

        internal static void ProcessActionList(EntityManager manager)
        {
            List<Entity> orderableEntities = manager.GetAllEntitiesWithDataBlob<OrderableDB>();
            foreach (var orderableEntity in orderableEntities)
            {
                ProcessActionList(orderableEntity);
            }          
        }

        internal static void ProcessActionList(Entity entity)
        {
            OrderableDB orderableDB = entity.GetDataBlob<OrderableDB>();
            List<BaseAction> actionList = orderableDB.ActionQueue;
            int mask = 1;

            int i = 0;
            while (i < actionList.Count())
            {
                var item = actionList[i];


                if ((mask & item.Lanes) == 0) //bitwise and
                {
                    if (item.IsBlocking)
                    {
                        mask |= item.Lanes; //bitwise or
                    }
                    item.OrderableProcessor.ProcessOrder(item);
                }                      
                if(item.IsFinished)
                    actionList.RemoveAt(i);
                else 
                    i++;
            }            
        }

        public static void ProcessSystem(EntityManager manager)
        {
            foreach (Entity ship in manager.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                ProcessShip(ship);
            }
        }

        public static void ProcessShip(Entity ship)
        {
            ShipInfoDB sInfo = ship.GetDataBlob<ShipInfoDB>();

            if (sInfo.Orders.Count == 0)
                return;

            if (sInfo.Orders.Peek().processOrder())
                sInfo.Orders.Dequeue();

            return;
        }

        public static void IsTargetClose(Game game, Entity thisEntity, Entity targetEntity, BaseAction order, int reqiredDistance)
        {
            PositionDB thisPosition = thisEntity.GetDataBlob<PositionDB>();
            PositionDB targetPosition = targetEntity.GetDataBlob<PositionDB>();

            if (thisPosition.GetDistanceTo(targetPosition) > reqiredDistance) //then we're too far away
            {
                OrderableDB thisOrderable = thisEntity.GetDataBlob<OrderableDB>();
                TranslateOrderableDB thisTranslateOderable = thisEntity.GetDataBlob<TranslateOrderableDB>();
                if (thisTranslateOderable.CurrentOrder.TargetEntityGuid == targetEntity.Guid) // it already has a move order there.
                {
                    //set this order to trigger after the translation order.
                    //thisTranslateOderable.CurrentOrder.NextOrders.Add(order);
                }
                else
                {
                    //create new translation order.
                    //TranslationOrder newTMove = new TranslationOrder(TranslationOrder.HelmOrderTypeEnum.InterceptTarget, thisEntity, targetEntity);
                    //TranslationOrder newTHold = new TranslationOrder(TranslationOrder.HelmOrderTypeEnum.HoldAt, thisEntity, targetEntity);

                    //newTMove.NextOrders.Add(newTHold);
                    //newTMove.NextOrders.Add(order);
                    //newTMove.Processor = new TranslationOrderProcessor();
                    //newTMove.Processor.FirstProcess(game, newTMove);
                    //newTHold.Processor.FirstProcess(game, newTHold);
                }
            }
        }
    }

    public class OrderableDB : BaseDataBlob
    {
        //[JsonProperty]
        //public GanttOrders.GanttList OrdersQueue = new GanttList();
        
        [JsonProperty]
        public List<BaseAction> ActionQueue { get; } = new List<BaseAction>();
        
        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            //OrdersQueue = new GanttOrders.GanttList(db.OrdersQueue);
            ActionQueue = new List<BaseAction>(db.ActionQueue);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }
}

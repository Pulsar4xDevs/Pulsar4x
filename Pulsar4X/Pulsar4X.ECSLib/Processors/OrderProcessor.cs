using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.ECSLib.GanttOrders;

namespace Pulsar4X.ECSLib
{
    static public class OrderProcessor
    {

        static public void Process(Game game)
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


        static public void ProcessGanttOrder(Entity entity)
        {
            entity.GetDataBlob<OrderableDB>().OrdersQueue.ProcessCurrentNodes();
        }


        static public void ProcessSystem(EntityManager manager)
        {
            foreach (Entity ship in manager.GetAllEntitiesWithDataBlob<ShipInfoDB>())
            {
                ProcessShip(ship);
            }
        }

        static public void ProcessShip(Entity ship)
        {
            ShipInfoDB sInfo = ship.GetDataBlob<ShipInfoDB>();

            if (sInfo.Orders.Count == 0)
                return;

            if (sInfo.Orders.Peek().processOrder())
                sInfo.Orders.Dequeue();

            return;
        }

        static public void IsTargetClose(Game game, Entity thisEntity, Entity targetEntity, Order order, int reqiredDistance)
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
        [JsonProperty]
        public GanttOrders.GanttList OrdersQueue = new GanttList();


        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            OrdersQueue = new GanttOrders.GanttList(db.OrdersQueue);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }
}

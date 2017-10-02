using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {

        internal List<EntityCommand> ActionList = new List<EntityCommand>();

        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            ActionList = new List<EntityCommand>(db.ActionList);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }

    public class OrderableProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        private Game _game;

        public OrderableProcessor(Game game)
        { _game = game; }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            OrderableDB orderableDB = entity.GetDataBlob<OrderableDB>();
            ProcessOrderList(_game, orderableDB.ActionList);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<OrderableDB>();
            foreach (var entity in entitysWithCargoTransfers)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }


        internal static void ProcessOrderList(Game game, List<EntityCommand> actionList)
        {
            //List<EntityCommand> actionList = orderableDB.ActionList;
            int mask = 1;

            int i = 0;
            while (i < actionList.Count)
            {
                var item = actionList[i];


                if ((mask & item.ActionLanes) == item.ActionLanes) //bitwise and
                {
                    if (item.IsBlocking)
                    {
                        mask |= item.ActionLanes; //bitwise or
                    }
                    if(!item.IsRunning)
                        item.ActionCommand(game);
                }
                if (item.IsFinished())
                    actionList.RemoveAt(i);
                else
                    i++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    
    public class OrderableProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(5);

        public Type GetParameterType => typeof(OrderableDB);

        private Game _game;

        public void Init(Game game)
        {
            _game = game;
        }

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    
    public class OrderableProcessor : IInstanceProcessor , IHotloopProcessor
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
            orderableDB.ProcessOrderList();
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<OrderableDB>();
            foreach (var entity in entitysWithCargoTransfers)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }

        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            OrderableDB orderableDB = entity.GetDataBlob<OrderableDB>();
            orderableDB.ProcessOrderList();
        }
    }
}

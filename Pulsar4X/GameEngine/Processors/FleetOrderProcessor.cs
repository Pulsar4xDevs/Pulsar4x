using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{
    public class FleetOrderProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromHours(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(1);

        public Type GetParameterType => typeof(FleetDB);

        public void Init(Game game)
        {

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            if(entity.TryGetDatablob<FleetDB>(out var fleetDB))
            {
                Process(fleetDB, deltaSeconds);
            }
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllDataBlobsOfType<FleetDB>();
            foreach (var db in entities)
            {
                Process(db, deltaSeconds);
            }
            return Math.Max(entities.Count, 1);
        }

        private void Process(FleetDB fleetDB, int deltaSeconds)
        {
            // We only care about fleets with standing orders
            if(fleetDB.StandingOrders.Count == 0) return;

            // Fleet must have a flagship to use standing orders
            if(fleetDB.FlagShipID == -1) return;

            // Make sure the fleet entity is orderable
            if(!fleetDB.OwningEntity.TryGetDatablob<OrderableDB>(out var orderableDB)) return;

            // Standing orders only process when the fleet has no existing orders
            if(orderableDB.ActionList.Count > 0) return;

            foreach(var order in fleetDB.StandingOrders)
            {
                if(!order.IsValid) continue;

                if(order.Condition.Evaluate(fleetDB.OwningEntity))
                {
                    // If the conditions are true, add the actions to the orderableDB action list
                    // and stop processing further standing orders
                    foreach(var action in order.Actions)
                    {
                        orderableDB.ActionList.Add(action.Clone());
                    }
                    break;
                }
            }
        }
    }
}
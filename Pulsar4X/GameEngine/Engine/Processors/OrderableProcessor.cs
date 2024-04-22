using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Engine
{

    public class OrderableProcessor : IInstanceProcessor, IHotloopProcessor
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
            DateTime atDateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            ProcessEntity(entity, atDateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithCargoTransfers = manager.GetAllEntitiesWithDataBlob<OrderableDB>();

            foreach (var entity in entitysWithCargoTransfers)
            {
                ProcessEntity(entity, deltaSeconds);
            }

            return entitysWithCargoTransfers.Count;
        }

        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if(entity.TryGetDatablob<OrderableDB>(out var orderableDB))
            {
                int mask = 0;

                foreach(var entityCommand in orderableDB.ActionList)
                {
                    if ((mask & ((int)entityCommand.ActionLanes)) == 0) //bitwise and
                    {
                        if (entityCommand.IsBlocking)
                        {
                            mask = mask | ((int)entityCommand.ActionLanes); //bitwise or
                        }
                        if (atDateTime >= entityCommand.ActionOnDate)
                        {
                            if(entityCommand.PauseOnAction &! entityCommand.IsRunning)
                            {
                                var e = Event.Create(EventType.OrdersHalt,
                                                        atDateTime,
                                                        "",
                                                        entityCommand.RequestingFactionGuid,
                                                        entityCommand.EntityCommanding.Manager.ManagerGuid,
                                                        entityCommand.EntityCommandingGuid);
                                EventManager.Instance.Publish(e);
                            }
                            entityCommand.Execute(atDateTime);
                        }
                    }
                }

                orderableDB.ActionList.RemoveAll(e => e.IsFinished());
            }
        }
    }
}

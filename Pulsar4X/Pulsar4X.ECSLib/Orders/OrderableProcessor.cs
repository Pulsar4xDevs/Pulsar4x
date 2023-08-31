using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
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
                lock (orderableDB.Lock)
                {
                    //var actionList = new List<EntityCommand>(_actionList);
                    int mask = 0;
                    var list = orderableDB.ActionList;

                    int i = 0;
                    while (i < list.Count)
                    {   var j = list.Count;
                        EntityCommand entityCommand = list[i];

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
                                    Event newEvent = new Event(atDateTime, "Command Halt");
                                    newEvent.EventType = EventType.OrdersHalt;
                                    newEvent.Entity = orderableDB.OwningEntity;
                                    newEvent.Faction = orderableDB.OwningEntity.GetFactionOwner;
                                    StaticRefLib.EventLog.AddEvent(newEvent);
                                }
                                entityCommand.ActionCommand(atDateTime);
                            }
                        }

                        if (entityCommand.IsFinished())
                        {
                            if(j != list.Count)
                                throw new Exception ("List Changed");
                            if(list[i] != entityCommand)
                                throw new Exception("How is this possible");
                            list.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }
        }
    }
}

using System;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Storage
{
    public class AddComponentToStorageOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithExternalEntity | ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => true;

        public override string Name => "Add Component To Storage Order";

        public override string Details => "Add a component to a cargo hold";

        internal override Entity EntityCommanding { get; }
        public ComponentInstance ComponentInstance { get; private set; }
        public int Amount { get; private set; }
        private bool hasExecuted = false;

        internal AddComponentToStorageOrder(Entity entity, ComponentInstance componentInstance, int amount = 1)
        {
            EntityCommanding = entity;
            ComponentInstance = componentInstance;
            Amount = amount;
        }

        public static AddComponentToStorageOrder Create(Entity entity, ComponentInstance componentInstance, int amount = 1)
        {
            var cmd = new AddComponentToStorageOrder(entity, componentInstance, amount)
            {
                RequestingFactionGuid = entity.FactionOwnerID,
                EntityCommandingGuid = entity.Id,
                CreatedDate = entity.Manager.ManagerSubpulses.StarSysDateTime,
                UseActionLanes = false
            };

            return cmd;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }

        internal override bool IsFinished()
        {
            return _isFinished = hasExecuted;
        }

        internal override void Execute(DateTime atDateTime)
        {
            CargoTransferProcessor.AddCargoItems(EntityCommanding, ComponentInstance, Amount);
            hasExecuted = true;
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }
    }
}
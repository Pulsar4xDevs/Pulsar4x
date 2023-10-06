using System;
using Pulsar4X.Components;

namespace Pulsar4X.Engine.Orders
{
    public class UninstallComponentInstanceOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithExternalEntity | ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => true;

        public override string Name => "Uninstall Component Instance";

        public override string Details => "Remove a component from it's parent entity";

        internal override Entity EntityCommanding { get; }
        public ComponentInstance ComponentInstance { get; private set; }
        public int Amount { get; private set; }
        private bool hasExecuted = false;

        internal UninstallComponentInstanceOrder(Entity entity, ComponentInstance componentInstance, int amount = 1)
        {
            EntityCommanding = entity;
            ComponentInstance = componentInstance;
            Amount = amount;
        }

        public static UninstallComponentInstanceOrder Create(Entity entity, ComponentInstance componentInstance, int amount = 1)
        {
            var cmd = new UninstallComponentInstanceOrder(entity, componentInstance, amount)
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

        public override bool IsFinished()
        {
            return hasExecuted;
        }

        internal override void Execute(DateTime atDateTime)
        {
            EntityCommanding.RemoveComponent(ComponentInstance);
            hasExecuted = true;
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }
    }
}
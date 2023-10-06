using System;
using Pulsar4X.Components;

namespace Pulsar4X.Engine.Orders
{
    public class InstallComponentInstanceOrder : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithExternalEntity | ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => true;

        public override string Name => "Install Component Instance";

        public override string Details => "Add a component to an entity";

        internal override Entity EntityCommanding { get; }
        public ComponentInstance ComponentInstance { get; private set; }
        private bool hasExecuted = false;

        internal InstallComponentInstanceOrder(Entity entity, ComponentInstance componentInstance)
        {
            EntityCommanding = entity;
            ComponentInstance = componentInstance;
        }

        public static InstallComponentInstanceOrder Create(Entity entity, ComponentInstance componentInstance)
        {
            var cmd = new InstallComponentInstanceOrder(entity, componentInstance)
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
            EntityCommanding.AddComponent(ComponentInstance);
            hasExecuted = true;
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }
    }
}
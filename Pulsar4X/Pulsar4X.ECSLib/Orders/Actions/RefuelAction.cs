using System;

namespace Pulsar4X.ECSLib
{
    public class RefuelAction : EntityCommand
    {
        public override string Name => "Refuel";
        public override string Details => "Refuel the fleet, must be at a location where supplies are availablle.";
         public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithEntitySameFleet;

        public override bool IsBlocking => true;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding
        {
            get { return _entityCommanding; }
        }

        public override bool IsFinished()
        {
            return false;
        }

        internal override void Execute(DateTime atDateTime)
        {
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }

        public RefuelAction() { }
        public RefuelAction(Entity commandingEntity)
        {
            _entityCommanding = commandingEntity;
        }

        public override EntityCommand Clone()
        {
            var command = new RefuelAction(EntityCommanding)
            {
                UseActionLanes = this.UseActionLanes,
                RequestingFactionGuid = this.RequestingFactionGuid,
                EntityCommandingGuid = this.EntityCommandingGuid,
                CreatedDate = this.CreatedDate,
                ActionOnDate = this.ActionOnDate,
                ActionedOnDate = this.ActionedOnDate,
                IsRunning = this.IsRunning
            };

            return command;
        }
    }
}
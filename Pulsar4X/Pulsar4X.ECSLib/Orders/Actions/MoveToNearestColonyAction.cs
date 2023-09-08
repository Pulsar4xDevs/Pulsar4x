using System;

namespace Pulsar4X.ECSLib
{
    public class MoveToNearestColonyAction : EntityCommand
    {
        public override string Name => "Move to Nearest Colony";
        public override string Details => "Moves the fleet to the nearest colony.";

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

        public MoveToNearestColonyAction() { }
        public MoveToNearestColonyAction(Entity commandingEntity)
        {
            _entityCommanding = commandingEntity;
        }

        public override EntityCommand Clone()
        {
            var command = new MoveToNearestColonyAction(EntityCommanding)
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
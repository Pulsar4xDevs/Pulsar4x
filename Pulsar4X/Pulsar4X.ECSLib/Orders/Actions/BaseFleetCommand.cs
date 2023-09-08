using System;

namespace Pulsar4X.ECSLib
{
    public class BaseFleetCommand : EntityCommand
    {
        public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.IneteractWithSelf;

        public override bool IsBlocking => true;

        public override string Name => "Base Fleet Command";

        public override string Details => "Inherit from this, don't use it directly.";

        internal override Entity EntityCommanding { get; }

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
    }
}
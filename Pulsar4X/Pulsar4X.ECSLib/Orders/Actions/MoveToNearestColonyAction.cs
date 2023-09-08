using System;

namespace Pulsar4X.ECSLib
{
    public class MoveToNearestColonyAction : BaseFleetCommand
    {
        public override string Name => "Move to Nearest Colony";
        public override string Details => "Moves the fleet to the nearest colony.";
        internal override void Execute(DateTime atDateTime)
        {
            base.Execute(atDateTime);
        }
    }
}
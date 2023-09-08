using System;

namespace Pulsar4X.ECSLib
{
    public class RefuelAction : BaseFleetCommand
    {
        public override string Name => "Refuel";
        public override string Details => "Refuel the fleet, must be at a location where supplies are availablle.";
        internal override void Execute(DateTime atDateTime)
        {
            base.Execute(atDateTime);
        }
    }
}
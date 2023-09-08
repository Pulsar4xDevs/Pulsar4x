using System;

namespace Pulsar4X.ECSLib
{
    public class ResupplyAction : BaseFleetCommand
    {
        public override string Name => "Resupply";
        public override string Details => "Resupply the fleet, must be at a location where supplies are availablle.";
        internal override void Execute(DateTime atDateTime)
        {
            base.Execute(atDateTime);
        }
    }
}
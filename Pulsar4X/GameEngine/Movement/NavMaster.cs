using System;
using System.Collections.Generic;

namespace Pulsar4X.Engine.Orders
{
    public class NavMaster : EntityCommand
    {
        public override string Name { get; } = "";

        public override string Details { get; } = "";

        public bool CycleCommand = false;
        List<EntityCommand> Orders = new List<EntityCommand>();
        List<EntityCommand> OrdersForOthers = new List<EntityCommand>();
        public override ActionLaneTypes ActionLanes { get; }
        public override bool IsBlocking { get; } = false;
        internal override Entity EntityCommanding { get; }
        internal override bool IsValidCommand(Game game)
        {
            return true;
        }

        internal override void Execute(DateTime atDateTime)
        {
            throw new NotImplementedException();
        }

        public override bool IsFinished()
        {
            throw new NotImplementedException();
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}
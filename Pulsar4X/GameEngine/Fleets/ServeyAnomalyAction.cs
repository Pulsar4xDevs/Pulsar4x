using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Fleets;

public class ServeyAnomalyAction : EntityCommand
{
    public override ActionLaneTypes ActionLanes { get; }
    public override bool IsBlocking { get; }
    public override string Name { get; }
    public override string Details { get; }
    internal override Entity EntityCommanding { get; }
    internal override bool IsValidCommand(Game game)
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        throw new NotImplementedException();
    }

    internal override bool IsFinished()
    {
        return _isFinished;
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}
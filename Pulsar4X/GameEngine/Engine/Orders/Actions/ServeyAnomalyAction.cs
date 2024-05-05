using System;

namespace Pulsar4X.Engine.Orders;

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

    public override bool IsFinished()
    {
        throw new NotImplementedException();
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}
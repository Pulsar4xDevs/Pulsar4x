using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Fleets;

public class ServeyAnomalyAction : EntityAction
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

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }
}
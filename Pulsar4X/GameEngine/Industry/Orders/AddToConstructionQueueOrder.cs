using System;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Industry.Orders;

public class AddToConstructionQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Add To Construction Queue";

    public override string Details => "Adds a component design to the local construction queue";

    internal override Entity EntityCommanding => _colonyEntity;

    private Entity _colonyEntity;
    private ComponentDesign _design;

    private AddToConstructionQueueOrder(Entity colonyEntity, ComponentDesign design)
    {
        _colonyEntity = colonyEntity;
        _design = design;
    }

    public static AddToConstructionQueueOrder Create(Entity colonyEntity, ComponentDesign design)
    {
        return new AddToConstructionQueueOrder(colonyEntity, design);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!_colonyEntity.TryGetDataBlob<LocalConstructionDB>(out var constructionDB))
            return;

        if (_design == null)
            return;

        var job = new LocalConstructionJob(_design);
        constructionDB.BuildQueue.Enqueue(job);
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return _colonyEntity != null && _design != null;
    }
}

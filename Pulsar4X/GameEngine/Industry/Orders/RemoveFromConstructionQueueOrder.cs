using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Industry.Orders;

public class RemoveFromConstructionQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Remove From Construction Queue";

    public override string Details => "Removes an item from the local construction queue";

    internal override Entity EntityCommanding => _colonyEntity;

    private Entity _colonyEntity;
    private LocalConstructionJob _job;

    private RemoveFromConstructionQueueOrder(Entity colonyEntity, LocalConstructionJob job)
    {
        _colonyEntity = colonyEntity;
        _job = job;
    }

    public static RemoveFromConstructionQueueOrder Create(Entity colonyEntity, LocalConstructionJob job)
    {
        return new RemoveFromConstructionQueueOrder(colonyEntity, job);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!_colonyEntity.TryGetDataBlob<LocalConstructionDB>(out var constructionDB))
            return;

        if (_job == null)
            return;

        // Convert queue to list, remove item, convert back to queue
        var queueList = constructionDB.BuildQueue.ToList();
        queueList.Remove(_job);
        constructionDB.BuildQueue = new Queue<LocalConstructionJob>(queueList);
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return _colonyEntity != null && _job != null;
    }
}

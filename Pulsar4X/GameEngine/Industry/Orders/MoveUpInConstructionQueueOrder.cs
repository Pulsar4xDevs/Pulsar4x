using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Industry.Orders;

public class MoveUpInConstructionQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Move Up In Construction Queue";

    public override string Details => "Moves an item up in the local construction queue";

    internal override Entity EntityCommanding => _colonyEntity;

    private Entity _colonyEntity;
    private LocalConstructionJob _job;

    private MoveUpInConstructionQueueOrder(Entity colonyEntity, LocalConstructionJob job)
    {
        _colonyEntity = colonyEntity;
        _job = job;
    }

    public static MoveUpInConstructionQueueOrder Create(Entity colonyEntity, LocalConstructionJob job)
    {
        return new MoveUpInConstructionQueueOrder(colonyEntity, job);
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

        // Convert queue to list
        var queueList = constructionDB.BuildQueue.ToList();

        // Find the item's index
        int index = queueList.IndexOf(_job);

        // If item is found and not already at the top, move it up
        if (index > 0)
        {
            queueList.RemoveAt(index);
            queueList.Insert(index - 1, _job);
            constructionDB.BuildQueue = new Queue<LocalConstructionJob>(queueList);
        }
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

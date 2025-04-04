using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Technology;

public class MoveDownInQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Move Down In Queue Order";

    public override string Details => "Instantly moves the given tech down in the given labs queue";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private string _techId;

    private MoveDownInQueueOrder(Entity labEntity, string techId)
    {
        _labEntity = labEntity;
        _techId = techId;
    }

    public static MoveDownInQueueOrder Create(Entity labEntity, string techId)
    {
        return new MoveDownInQueueOrder(labEntity, techId);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!_labEntity.TryGetDataBlob<ResearcherDB>(out var researcherDB))
            return;

        if(string.IsNullOrEmpty(_techId))
            return;

        researcherDB.TechQueue.TryMoveDown(_techId);

        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyMovedInQueue,
                    atDateTime,
                    "Technology moved down in queue",
                    _labEntity.FactionOwnerID,
                    _labEntity.Manager.ManagerID,
                    _labEntity.Id));
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}
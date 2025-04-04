using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Technology;

public class RemoveTechFromQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Remove Tech From Queue";

    public override string Details => "Instantly removes the given tech from the given labs queue";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private string _techId;

    private RemoveTechFromQueueOrder(Entity labEntity, string techId)
    {
        _labEntity = labEntity;
        _techId = techId;
    }

    public static RemoveTechFromQueueOrder Create(Entity labEntity, string techId)
    {
        return new RemoveTechFromQueueOrder(labEntity, techId);
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

        researcherDB.TechQueue.TryRemoveItem(_techId);

        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyRemovedFromQueue,
                    atDateTime,
                    "Technology removed from queue",
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
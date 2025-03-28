using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Technology;

public class AddTechToQueueOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Add Tech To Queue";

    public override string Details => "Instantly adds the given tech to the given labs queue";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private string _techId;

    private AddTechToQueueOrder(Entity labEntity, string techId)
    {
        _labEntity = labEntity;
        _techId = techId;
    }

    public static AddTechToQueueOrder Create(Entity labEntity, string techId)
    {
        return new AddTechToQueueOrder(labEntity, techId);
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

        researcherDB.TechQueue.Enqueue(_techId);

        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyQueued,
                    atDateTime,
                    "Technology added to queue",
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
using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.People;

namespace Pulsar4X.Technology;

public class UnassignScientistOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Unassign Scientist from Lab";

    public override string Details => "Instantly unassigns a scientist from a lab";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private int _scientistId;

    private UnassignScientistOrder(Entity labEntity, int scientistId)
    {
        _labEntity = labEntity;
        _scientistId = scientistId;
    }

    public static UnassignScientistOrder Create(Entity labEntity, int scientistId)
    {
        return new UnassignScientistOrder(labEntity, scientistId);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!_labEntity.TryGetDataBlob<ResearcherDB>(out var researcherDB))
            return;

        if(!_labEntity.Manager.TryGetGlobalEntityById(_scientistId, out var scientist))
            return;

        if(!scientist.TryGetDataBlob<CommanderDB>(out var commanderDB))
            return;

        // Clear the assignments
        commanderDB.AssignedTo = -1;
        researcherDB.ScientistId = -1;

        // From the lab perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyLabScientistUnassigned,
                    atDateTime,
                    "Lab was unassigned a scientist",
                    _labEntity.FactionOwnerID,
                    _labEntity.Manager.ManagerID,
                    _labEntity.Id));

        // From the scientist perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.ScientistUnassignedFromLab,
                    atDateTime,
                    "Scientist was unassigned from lab",
                    _labEntity.FactionOwnerID,
                    _labEntity.Manager.ManagerID,
                    _scientistId));
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
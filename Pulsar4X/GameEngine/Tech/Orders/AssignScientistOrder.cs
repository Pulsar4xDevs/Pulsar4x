using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;

namespace Pulsar4X.Technology;

public class AssignScientistOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Assign Scientist to Lab";

    public override string Details => "Instantly assigns a scientist to a lab";

    internal override Entity EntityCommanding => _labEntity;

    private Entity _labEntity;
    private int _scientistId;

    private AssignScientistOrder(Entity labEntity, int scientistId)
    {
        _labEntity = labEntity;
        _scientistId = scientistId;
    }

    public static AssignScientistOrder Create(Entity labEntity, int scientistId)
    {
        return new AssignScientistOrder(labEntity, scientistId);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!_labEntity.TryGetDatablob<ResearcherDB>(out var researcherDB))
            return;

        // TODO: need to check for previous assignments and unassign them
        researcherDB.ScientistId = _scientistId;

        // From the lab perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.TechnologyLabScientistAssigned,
                    atDateTime,
                    "Lab was assigned a scientist",
                    _labEntity.FactionOwnerID,
                    _labEntity.Manager.ManagerID,
                    _labEntity.Id));

        // From the scientist perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.ScientistAssignedToLab,
                    atDateTime,
                    "Scientist assigned to lab",
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
using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.People;

namespace Pulsar4X.Technology;

public class AssignScientistOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Assign Scientist to Lab";

    public override string Details => "Instantly assigns a scientist to a lab";

    internal override Entity EntityCommanding => _labEntity;
    protected override void BindCommanding(Entity entity) => _labEntity = entity;

    private Entity _labEntity;
    private int _scientistId;

    private AssignScientistOrder(Entity labEntity, int scientistId)
    {
        _labEntity = labEntity;
        _scientistId = scientistId;
        UseActionLanes = false;
    }

    public static AssignScientistOrder Create(Entity labEntity, int scientistId)
    {
        return new AssignScientistOrder(labEntity, scientistId);
    }

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        _isFinished = true;
        if(!_labEntity.TryGetDataBlob<ResearcherDB>(out var researcherDB))
            return;

        if(!_labEntity.Manager.TryGetGlobalEntityById(_scientistId, out var scientist))
            return;

        if(!scientist.TryGetDataBlob<CommanderDB>(out var commanderDB))
            return;

        int previousId = researcherDB.ScientistId;

        // Need to find the current assignment and unassign them
        if(commanderDB.AssignedTo >= 0)
        {
            if(_labEntity.Manager.TryGetGlobalEntityById(commanderDB.AssignedTo, out var previousLab))
            {
                bool sameChair = previousLab.Id == _labEntity.Id;
                var unassignOrder = UnassignScientistOrder.Create(previousLab, scientist.Id, silent: sameChair);
                _labEntity.Manager.Game.OrderHandler.HandleOrder(unassignOrder);
            }
        }

        // Assign the scientist
        researcherDB.ScientistId = _scientistId;
        commanderDB.AssignedTo = _labEntity.Id;

        if (previousId != _scientistId)
        {
            var book = ShipLogBook.LabBook(_labEntity);
            if (book != null)
            {
                if (previousId >= 0
                    && _labEntity.Manager.TryGetGlobalEntityById(previousId, out var previous))
                    ShipLogBook.NoteLeftPost(book, previous, atDateTime, "the lab");
                ShipLogBook.NoteTookPost(book, scientist, atDateTime, "the lab");
            }
        }

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
        return _isFinished;
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}
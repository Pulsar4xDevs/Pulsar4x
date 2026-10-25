using System;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.People;

namespace Pulsar4X.People.Orders;

public class AssignAdministratorOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Assign Administrator to Post";

    public override string Details => "Instantly assigns an administrator to an admin post";

    internal override Entity EntityCommanding => _adminEntity;
    protected override void BindCommanding(Entity entity) => _adminEntity = entity;

    private Entity _adminEntity;
    private int _administratorId;
    private string _postComponentName;

    private AssignAdministratorOrder(Entity adminEntity, int administratorId, string postComponentName)
    {
        _adminEntity = adminEntity;
        _administratorId = administratorId;
        _postComponentName = postComponentName;
        UseActionLanes = false;
    }

    public static AssignAdministratorOrder Create(Entity adminEntity, int administratorId, string postComponentName)
    {
        return new AssignAdministratorOrder(adminEntity, administratorId, postComponentName);
    }

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!_adminEntity.TryGetDataBlob<AdminSpaceDB>(out var adminSpaceDB))
            return;

        // Find the specific admin post by component name
        AdminSpaceAbilityState? post = null;
        foreach (var seat in adminSpaceDB.CommanderSeats)
        {
            if (seat.ComponentName == _postComponentName)
            {
                post = seat;
                break;
            }
        }

        if (post == null)
            return;

        if(!_adminEntity.Manager.TryGetGlobalEntityById(_administratorId, out var administrator))
            return;

        if(!administrator.TryGetDataBlob<CommanderDB>(out var commanderDB))
            return;

        int previousId = post.CommanderID;

        // Need to find the current assignment and unassign them
        if(commanderDB.AssignedTo >= 0)
        {
            if(_adminEntity.Manager.TryGetGlobalEntityById(commanderDB.AssignedTo, out var previousEntity))
            {
                bool sameChair = previousEntity.Id == _adminEntity.Id;
                var unassignOrder = UnassignAdministratorOrder.Create(
                    previousEntity, administrator.Id, _postComponentName, silent: sameChair);
                _adminEntity.Manager.Game.OrderHandler.HandleOrder(unassignOrder);
            }
        }

        // Assign the administrator
        post.CommanderID = _administratorId;
        post.Commander = commanderDB;
        commanderDB.AssignedTo = _adminEntity.Id;

        if (previousId != _administratorId)
        {
            if (previousId >= 0
                && _adminEntity.Manager.TryGetGlobalEntityById(previousId, out var previous))
                ShipLogBook.NoteLeftPost(_adminEntity, previous, atDateTime, "the post");
            ShipLogBook.NoteTookPost(_adminEntity, administrator, atDateTime, "the post");
        }

        // From the colony/post perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.ColonyAdministratorAssigned,
                    atDateTime,
                    "Admin post was assigned an administrator",
                    _adminEntity.FactionOwnerID,
                    _adminEntity.Manager.ManagerID,
                    _adminEntity.Id));

        // From the administrator perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.AdministratorAssignedToColony,
                    atDateTime,
                    "Administrator assigned to post",
                    _adminEntity.FactionOwnerID,
                    _adminEntity.Manager.ManagerID,
                    _administratorId));
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

using System;
using GameEngine.People;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.People;

namespace Pulsar4X.People.Orders;

public class UnassignAdministratorOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Unassign Administrator from Post";

    public override string Details => "Instantly unassigns an administrator from an admin post";

    internal override Entity EntityCommanding => _adminEntity;

    private Entity _adminEntity;
    private int _administratorId;
    private string _postComponentName;

    private UnassignAdministratorOrder(Entity adminEntity, int administratorId, string postComponentName)
    {
        _adminEntity = adminEntity;
        _administratorId = administratorId;
        _postComponentName = postComponentName;
    }

    public static UnassignAdministratorOrder Create(Entity adminEntity, int administratorId, string postComponentName)
    {
        return new UnassignAdministratorOrder(adminEntity, administratorId, postComponentName);
    }

    public override EntityCommand Clone()
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

        // Clear the assignments
        commanderDB.AssignedTo = -1;
        post.CommanderID = -1;
        post.Commander = null;

        // From the colony/post perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.ColonyAdministratorUnassigned,
                    atDateTime,
                    "Admin post was unassigned an administrator",
                    _adminEntity.FactionOwnerID,
                    _adminEntity.Manager.ManagerID,
                    _adminEntity.Id));

        // From the administrator perspective
        EventManager.Instance.Publish(
            Event.Create(
                    EventType.AdministratorUnassignedFromColony,
                    atDateTime,
                    "Administrator was unassigned from post",
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

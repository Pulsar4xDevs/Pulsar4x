using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.Extensions;

namespace Pulsar4X.Colonies;

public class CreateColonyOrder : EntityAction
{
    public Entity TargetSystemBody { get; private set; }
    public Entity Species { get; private set; }

    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Create Colony";

    public override string Details => $"Create Colony on {TargetSystemBody.ToString()}";

    internal override Entity EntityCommanding => _entityCommanding;

    public static CreateColonyOrder CreateCommand(Entity faction, Entity species, Entity targetBody)
    {
        var command = new CreateColonyOrder()
        {
            _entityCommanding = faction,
            EntityCommandingGuid = faction.Id,
            RequestingFactionGuid= faction.Id,
            TargetSystemBody = targetBody,
            Species = species
        };

        return command;
    }

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override bool IsFinished()
    {
        // Scoped to this faction: another faction's colony on the same body does not count.
        _isFinished = TargetSystemBody.TryGetFactionColonyOnBody(RequestingFactionGuid, out _);
        return _isFinished;
    }

    internal override void Execute(DateTime atDateTime)
    {
        var colony = ColonyFactory.CreateColony(_entityCommanding, Species, TargetSystemBody);
        var colonyName = colony.GetName(RequestingFactionGuid);
        EventManager.Instance.Publish(
            Event.Create(
                EventType.ColonyCreated,
                atDateTime,
                $"{colonyName} has been created",
                RequestingFactionGuid,
                _entityCommanding.Manager.ManagerID,
                colony.Id));
    }

    internal override bool IsValidCommand(Game game)
    {
        // A faction founds at most one colony per body. Other factions' colonies do not block it.
        return !TargetSystemBody.TryGetFactionColonyOnBody(RequestingFactionGuid, out _);
    }
}
using System;
using Pulsar4X.Events;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders;

public class CreateColonyOrder : EntityCommand
{
    public Entity TargetSystemBody { get; private set; }
    public Entity Species { get; private set; }

    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Create Colony";

    public override string Details => $"Create Colony on {TargetSystemBody.ToString()}";

    private Entity _entityCommanding;
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

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    public override bool IsFinished()
    {
        return TargetSystemBody.IsOrHasColony().Item1;
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
                _entityCommanding.Manager.ManagerGuid,
                colony.Id));
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}
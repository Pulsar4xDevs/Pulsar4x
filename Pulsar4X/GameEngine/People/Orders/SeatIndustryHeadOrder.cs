using System;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.People.Orders;

/// <summary>Instantly seats a hired civilian as a colony's industry head. See <see cref="IndustrySeat"/> for the rules.</summary>
public class SeatIndustryHeadOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Seat Industry Head";

    public override string Details => "Instantly seats an administrator as the colony's industry head";

    internal override Entity EntityCommanding => _colony;
    protected override void BindCommanding(Entity entity) => _colony = entity;

    private Entity _colony;
    private readonly int _administratorId;

    private SeatIndustryHeadOrder(Entity colony, int administratorId)
    {
        _colony = colony;
        _administratorId = administratorId;
        UseActionLanes = false;
    }

    public static SeatIndustryHeadOrder Create(Entity colony, int administratorId)
        => new SeatIndustryHeadOrder(colony, administratorId);

    public override EntityAction Clone() => throw new NotImplementedException();

    /// <summary>The refusal that <see cref="Execute"/> would hit, or null when the order is good.</summary>
    internal string? Refusal()
    {
        Entity? person = null;
        if (_colony.Manager != null && _colony.Manager.TryGetGlobalEntityById(_administratorId, out var found))
            person = found;
        return IndustrySeat.SeatRefusal(_colony, person, _administratorId);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_colony.Manager == null || !_colony.Manager.TryGetGlobalEntityById(_administratorId, out var person))
            return;
        IndustrySeat.Seat(_colony, person, atDateTime);
    }

    internal override bool IsFinished() => true;

    internal override bool IsValidCommand(Game game) => Refusal() == null;
}

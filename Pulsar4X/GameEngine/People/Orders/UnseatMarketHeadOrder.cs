using System;
using GameEngine.Engine.Orders;
using GameEngine.People;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.People.Orders;

/// <summary>Instantly empties a colony's market seat. The head becomes an unassigned civilian again.</summary>
public class UnseatMarketHeadOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Unseat Market Head";

    public override string Details => "Instantly removes the colony's market head";

    internal override Entity EntityCommanding => _colony;
    protected override void BindCommanding(Entity entity) => _colony = entity;

    private Entity _colony;

    private UnseatMarketHeadOrder(Entity colony)
    {
        _colony = colony;
        UseActionLanes = false;
    }

    public static UnseatMarketHeadOrder Create(Entity colony) => new UnseatMarketHeadOrder(colony);

    public override EntityAction Clone() => throw new NotImplementedException();

    internal override void Execute(DateTime atDateTime) => MarketSeat.Unseat(_colony, atDateTime);

    internal override bool IsFinished() => true;

    internal override bool IsValidCommand(Game game) => _colony.HasDataBlob<ColonyInfoDB>();
}

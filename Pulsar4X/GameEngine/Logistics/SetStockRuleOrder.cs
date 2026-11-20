using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Logistics;

/// <summary>Instantly sets or clears one warehouse stock rule on a colony. See <see cref="StockRules"/>.</summary>
public class SetStockRuleOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;
    public override bool IsBlocking => false;
    public override string Name => "Set Stock Rule";
    public override string Details => _min == null && _max == null
        ? $"Clears the stock rule for {_cargoId}"
        : $"Keeps {_cargoId} at least {_min?.ToString() ?? "-"}, sells above {_max?.ToString() ?? "-"}";

    internal override Entity EntityCommanding => _colony;
    protected override void BindCommanding(Entity entity) => _colony = entity;

    private Entity _colony;
    private readonly string _cargoId;
    private readonly long? _min;
    private readonly long? _max;

    private SetStockRuleOrder(Entity colony, string cargoId, long? min, long? max)
    {
        _colony = colony;
        _cargoId = cargoId;
        _min = min;
        _max = max;
        UseActionLanes = false;
    }

    /// <summary>Sets the rule. Null bounds are unset; both null clears it.</summary>
    public static SetStockRuleOrder Create(Entity colony, string cargoId, long? min, long? max)
        => new SetStockRuleOrder(colony, cargoId, min, max);

    /// <summary>Clears the item's rule.</summary>
    public static SetStockRuleOrder CreateClear(Entity colony, string cargoId)
        => new SetStockRuleOrder(colony, cargoId, null, null);

    public override EntityAction Clone() => new SetStockRuleOrder(_colony, _cargoId, _min, _max);

    internal string? Refusal() => StockRules.Refusal(_colony, _cargoId, _min, _max);

    internal override void Execute(DateTime atDateTime) => StockRules.Set(_colony, _cargoId, _min, _max);

    internal override bool IsFinished() => true;

    internal override bool IsValidCommand(Game game) => Refusal() == null;
}

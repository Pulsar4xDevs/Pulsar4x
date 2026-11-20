using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Industry.Orders;

/// <summary>
/// Instantly queues a recycle job on a colony's line: a ship in transfer range on a shipyard line, or components /
/// installations from the warehouse on a factory line that builds them. See <see cref="Recycling"/>.
/// </summary>
public class RecycleOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;
    public override bool IsBlocking => false;
    public override string Name => "Recycle";
    public override string Details => _ship != null ? "Queue scrapping a ship at a shipyard" : "Queue recycling stored components";

    internal override Entity EntityCommanding => _colony;
    protected override void BindCommanding(Entity entity) => _colony = entity;

    private Entity _colony;
    private readonly string _lineId;
    private readonly Entity? _ship;
    private readonly string? _designId;
    private readonly ushort _quantity;

    private RecycleOrder(Entity colony, string lineId, Entity? ship, string? designId, ushort quantity)
    {
        _colony = colony;
        _lineId = lineId;
        _ship = ship;
        _designId = designId;
        _quantity = quantity;
        RequestingFactionGuid = colony.FactionOwnerID;
        EntityCommandingGuid = colony.Id;
        CreatedDate = colony.StarSysDateTime;
        UseActionLanes = false;
    }

    /// <summary>Scrap <paramref name="ship"/> on the colony's shipyard line <paramref name="lineId"/>.</summary>
    public static RecycleOrder CreateShip(Entity colony, string lineId, Entity ship)
        => new RecycleOrder(colony, lineId, ship, null, 1);

    /// <summary>Recycle <paramref name="quantity"/> stored units of a component or installation design on <paramref name="lineId"/>.</summary>
    public static RecycleOrder CreateComponent(Entity colony, string lineId, string designId, ushort quantity)
        => new RecycleOrder(colony, lineId, null, designId, quantity);

    public override EntityAction Clone() => new RecycleOrder(_colony, _lineId, _ship, _designId, _quantity);

    internal string? Refusal()
        => _ship != null
            ? Recycling.ShipRejection(_colony, _lineId, _ship)
            : Recycling.ComponentRejection(_colony, _lineId, _designId ?? "", _quantity);

    internal override void Execute(DateTime atDateTime)
    {
        if (Refusal() != null)
            return;
        IndustryJob job = _ship != null
            ? Recycling.ShipJob(_colony, _ship)
            : Recycling.ComponentJob(_colony, Recycling.ComponentDesignOf(_colony, _designId!)!, _quantity);
        IndustryTools.AddJob(_colony, _lineId, job);
        ShipLogBook.NoteIndustryChange(_colony, $"queued {job.Name}" + (job.NumberOrdered > 1 ? $" x{job.NumberOrdered}" : ""));
    }

    internal override bool IsFinished() => true;

    internal override bool IsValidCommand(Game game) => Refusal() == null;
}

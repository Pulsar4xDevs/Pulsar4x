using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Events;
using Pulsar4X.Names;

namespace Pulsar4X.Industry.Orders;

public class QueueShipRefitOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Queue Ship Refit";

    public override string Details => "Queue a refit of a ship in transfer range";

    internal override Entity EntityCommanding => _yard;
    protected override void BindCommanding(Entity entity) => _yard = entity;

    private Entity _yard;
    private Entity _ship;
    private string _lineId;
    private List<ComponentInstance> _remove;
    private List<ComponentInstance> _mount;
    private string? _targetDesignId;

    private QueueShipRefitOrder(
        Entity yard, string lineId, Entity ship,
        List<ComponentInstance> remove, List<ComponentInstance> mount,
        string? targetDesignId)
    {
        _yard = yard;
        _ship = ship;
        _lineId = lineId;
        _remove = remove;
        _mount = mount;
        _targetDesignId = targetDesignId;
        RequestingFactionGuid = yard.FactionOwnerID;
        EntityCommandingGuid = yard.Id;
        CreatedDate = yard.StarSysDateTime;
        UseActionLanes = false;
    }

    public static QueueShipRefitOrder Create(
        Entity yard, string lineId, Entity ship,
        List<ComponentInstance> remove, List<ComponentInstance> mount,
        string? targetDesignId = null)
        => new QueueShipRefitOrder(yard, lineId, ship, remove, mount, targetDesignId);

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!_yard.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.ContainsKey(_lineId))
            return;

        var refit = new ShipRefitJob(_ship.Id, _remove, _mount);
        refit.TargetDesignId = _targetDesignId;
        string shipName = _ship.TryGetDataBlob<NameDB>(out var name)
            ? name.GetName(_yard.FactionOwnerID)
            : _ship.Id.ToString();
        // A normal industry job on the yard's line: it shares the line and its points like any other job.
        IndustryTools.AddJob(_yard, _lineId, IndustryJob.ForRefit(refit, JobName(shipName)));
        EventManager.Instance.Publish(
            Event.Create(
                EventType.ShipRefitBegan,
                _yard.StarSysDateTime,
                "Refit began on " + shipName,
                _yard.FactionOwnerID,
                _yard.Manager.ManagerID,
                _ship.Id));
    }

    string JobName(string shipName)
    {
        string name = "Refit " + shipName;
        if (!string.IsNullOrEmpty(_targetDesignId)
            && _yard.Manager?.Game is { } game
            && game.Factions.TryGetValue(_yard.FactionOwnerID, out var faction)
            && faction.TryGetDataBlob<Pulsar4X.Factions.FactionInfoDB>(out var info)
            && info.ShipDesigns.TryGetValue(_targetDesignId, out var design))
            name += " to " + design.Name;
        return name;
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (_yard == null || _ship == null)
            return false;
        return ShipRefitJob.Rejection(_yard, _lineId, _ship, _remove, _mount) == null;
    }
}

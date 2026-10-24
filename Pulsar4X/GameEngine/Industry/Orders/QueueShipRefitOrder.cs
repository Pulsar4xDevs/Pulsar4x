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
        if (!_yard.TryGetDataBlob<LocalConstructionDB>(out var construction))
            return;

        var job = new ShipRefitJob(_ship.Id, _lineId, _remove, _mount);
        job.TargetDesignId = _targetDesignId;
        construction.RefitQueue.Enqueue(job);
        string shipName = _ship.TryGetDataBlob<NameDB>(out var name)
            ? name.GetName(_yard.FactionOwnerID)
            : _ship.Id.ToString();
        EventManager.Instance.Publish(
            Event.Create(
                EventType.ShipRefitBegan,
                _yard.StarSysDateTime,
                "Refit began on " + shipName,
                _yard.FactionOwnerID,
                _yard.Manager.ManagerID,
                _ship.Id));
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

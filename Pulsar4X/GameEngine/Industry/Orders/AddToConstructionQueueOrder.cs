using System;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry.Orders;

public class AddToConstructionQueueOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

    public override bool IsBlocking => false;

    public override string Name => "Add To Construction Queue";

    public override string Details => _instance != null
        ? "Queue a mount of a stored component"
        : "Queue assembly of a site-only design";

    internal override Entity EntityCommanding => _actor;
    protected override void BindCommanding(Entity entity) => _actor = entity;

    private Entity _actor;
    private Entity? _targetColony;
    private ComponentDesign? _design;
    private ComponentInstance? _instance;

    private AddToConstructionQueueOrder(Entity actor, ComponentDesign? design, ComponentInstance? instance, Entity? targetColony)
    {
        _actor = actor;
        _targetColony = targetColony;
        _design = design;
        _instance = instance;
        RequestingFactionGuid = actor.FactionOwnerID;
        EntityCommandingGuid = actor.Id;
        CreatedDate = actor.StarSysDateTime;
        UseActionLanes = false;
    }

    public static AddToConstructionQueueOrder CreateMount(Entity actor, ComponentInstance instance)
        => new AddToConstructionQueueOrder(actor, instance.Design, instance, null);

    public static AddToConstructionQueueOrder CreateMount(Entity actor, ComponentInstance instance, Entity colony)
        => new AddToConstructionQueueOrder(actor, instance.Design, instance, colony);

    public static AddToConstructionQueueOrder CreateAssemble(Entity actor, ComponentDesign design)
        => new AddToConstructionQueueOrder(actor, design, null, null);

    public static AddToConstructionQueueOrder CreateAssemble(Entity actor, ComponentDesign design, Entity colony)
        => new AddToConstructionQueueOrder(actor, design, null, colony);

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!_actor.TryGetDataBlob<LocalConstructionDB>(out var constructionDB))
            return;

        LocalConstructionJob job = _instance != null
            ? new LocalConstructionJob(_instance.Design, _instance)
            : new LocalConstructionJob(_design!);
        if (_targetColony != null)
            job.TargetEntityId = _targetColony.Id;
        constructionDB.BuildQueue.Enqueue(job);
    }

    internal override bool IsFinished()
    {
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (_actor == null)
            return false;
        if (!_actor.TryGetDataBlob<LocalConstructionDB>(out var construction) || construction.PointsPerDay <= 0)
            return false;
        if (_targetColony != null)
        {
            if (!_targetColony.HasDataBlob<ColonyInfoDB>()
                || _targetColony.FactionOwnerID != _actor.FactionOwnerID
                || !LocalConstructionProcessor.InTransferRange(_actor, _targetColony))
                return false;
        }
        else if (!_actor.HasDataBlob<ColonyInfoDB>())
            return false;

        if (_instance != null)
        {
            if (!LocalConstructionJob.IsCrate(_instance.Design))
                return false;
            if (!_actor.TryGetDataBlob<CargoStorageDB>(out var storage)
                || storage.GetUnitsStored(_instance, false) < 1)
                return false;
            foreach (var job in construction.BuildQueue)
            {
                if (job.Instance != null && job.Instance.ID == _instance.ID)
                    return false;
            }
            return true;
        }

        return _design != null && LocalConstructionJob.IsSiteOnly(_design);
    }
}

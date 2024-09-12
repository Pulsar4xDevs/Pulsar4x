using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders;

public class MoveTowardsTargetOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;

    public override bool IsBlocking => true;

    public override string Name => "Move Towards";

    public override string Details => "";

    private Entity _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding;

    public Entity Target { get; private set; }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    public override bool IsFinished()
    {
        return !_entityCommanding.HasDataBlob<MoveTowardsTargetDB>();
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!IsRunning)
        {
            var moveTowardsTargetDB = new MoveTowardsTargetDB()
            {
                TargetId = Target.Id,
                Speed = 1000000 // TODO: calculate this
            };

            _entityCommanding.SetDataBlob(moveTowardsTargetDB);

            if(_entityCommanding.HasDataBlob<OrbitDB>())
                _entityCommanding.RemoveDataBlob<OrbitDB>();

            IsRunning = true;
        }
    }

    internal override bool IsValidCommand(Game game)
    {
        // TODO: need to check if the target is in the same system
        return true;
    }

    public static MoveTowardsTargetOrder CreateCommand(Entity entity, Entity target)
    {
        var order = new MoveTowardsTargetOrder()
        {
            RequestingFactionGuid = entity.FactionOwnerID,
            EntityCommandingGuid = entity.Id,
            _entityCommanding = entity,
            Target = target,
        };

        return order;
    }
}
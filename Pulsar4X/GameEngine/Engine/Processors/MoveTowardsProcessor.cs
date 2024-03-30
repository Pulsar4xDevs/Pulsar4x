using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine;

public class MoveTowardsProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromHours(1);

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(1);

    public Type GetParameterType => typeof(MoveTowardsTargetDB);

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        var moveTowardsTargetDB = entity.GetDataBlob<MoveTowardsTargetDB>();

        // TODO: need to gracefully handle this
        if(!entity.Manager.TryGetEntityById(moveTowardsTargetDB.TargetId, out var target))
            return;

        var currentPosition = entity.GetDataBlob<PositionDB>();
        var targetPosition = target.GetDataBlob<PositionDB>();
        var vectorToTarget = currentPosition.AbsolutePosition - targetPosition.AbsolutePosition;
        var distanceToTarget = vectorToTarget.Length();
        var direction = -vectorToTarget / distanceToTarget;

        float deltaTime = 3600; // TODO: calculate from timespan of last run
        var movementVector = direction * moveTowardsTargetDB.Speed * deltaTime;
        if(movementVector.Length() > distanceToTarget)
        {
            currentPosition.AbsolutePosition = targetPosition.AbsolutePosition;
            entity.RemoveDataBlob<MoveTowardsTargetDB>();
        }
        else
        {
            currentPosition.AbsolutePosition += movementVector;
        }
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var entitysWithMove = manager.GetAllEntitiesWithDataBlob<MoveTowardsTargetDB>();
        foreach (var entity in entitysWithMove)
        {
            ProcessEntity(entity, deltaSeconds);
        }
        return entitysWithMove.Count;
    }
}
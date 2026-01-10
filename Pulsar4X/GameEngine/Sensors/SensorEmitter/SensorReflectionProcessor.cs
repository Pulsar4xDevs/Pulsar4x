using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Movement;

namespace Pulsar4X.Sensors;

public class SensorReflectionProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency => TimeSpan.FromHours(1);

    public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(30);

    public Type GetParameterType => typeof(SensorProfileDB);

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if(!entity.TryGetDataBlob<PositionDB>(out var detectablePosDB))
        {
            return;
        }

        SensorProfileTools.UpdateReflectionProfile(entity.GetDataBlob<SensorProfileDB>(), entity.StarSysDateTime);
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var entities = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
        foreach(var entity in entities)
        {
            ProcessEntity(entity, deltaSeconds);
        }

        return entities.Count;
    }
}
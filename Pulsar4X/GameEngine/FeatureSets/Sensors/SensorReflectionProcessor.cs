using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine;

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
        if(!entity.TryGetDatablob<PositionDB>(out var detectablePosDB))
        {
            return;
        }

        entity.GetDataBlob<SensorProfileDB>().SetReflectionProfile(entity.StarSysDateTime);
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
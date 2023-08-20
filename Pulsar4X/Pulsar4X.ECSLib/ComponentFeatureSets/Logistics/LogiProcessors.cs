using System;

namespace Pulsar4X.ECSLib;

public class LogiProcessors : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromHours(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(0.25);

    public Type GetParameterType => typeof(LogiBaseDB);

    public void Init(Game game)
    {
            
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        LogisticsCycle.LogiBaseBidding(entity.GetDataBlob<LogiBaseDB>());
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var tradingBases = manager.GetAllDataBlobsOfType<LogiBaseDB>();
        var shippingEntities = manager.GetAllEntitiesWithDataBlob<LogiShipperDB>();
        foreach(LogiBaseDB tradeBase in tradingBases)
        {
            LogisticsCycle.LogiBaseBidding(tradeBase);
        }
        return tradingBases.Count;
    }
}
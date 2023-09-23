using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Components;

namespace Pulsar4X.Datablobs;

/// <summary>
/// Contains info on how an entitiy can be stored.
/// NOTE an entity with this datablob must also have a MassVolumeDB
/// </summary>
public class LogiBaseDB : BaseDataBlob
{
    public int Capacity { get; internal set; }

    public Dictionary<ICargoable, (int minVal, int maxVal)> DesiredLevels = new Dictionary<ICargoable, (int minVal, int MaxVal)>();
    public Dictionary<ICargoable,(int count, int demandSupplyWeight)> ListedItems = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>();

    public Dictionary<ICargoable,(Guid shipingEntity, double amountVolume)> ItemsWaitingPickup = new Dictionary<ICargoable, (Guid shipingEntity, double amountVolume)>();
    public Dictionary<ICargoable,(Guid shipingEntity, double amountVolume)> ItemsInTransit = new Dictionary<ICargoable, (Guid shipingEntity, double amountVolume)>();

    public List<(Entity ship, LogisticsCycle.CargoTask cargoTask)> TradeShipBids = new List<(Entity ship, LogisticsCycle.CargoTask cargoTask)>();

    public LogiBaseDB()
    {

    }

    internal override void OnSetToEntity()
    {

    }

    private LogiBaseDB(LogiBaseDB db)
    {
        ListedItems = new Dictionary<ICargoable, (int count, int demandSupplyWeight)>(db.ListedItems);
        ItemsWaitingPickup = new Dictionary<ICargoable, (Guid shipingEntity, double amountVolume)>(db.ItemsWaitingPickup);
        TradeShipBids = new List<(Entity ship, LogisticsCycle.CargoTask cargoTask)>(db.TradeShipBids);
    }
    public override object Clone()
    {
        return new LogiBaseDB(this);
    }
}

public class LogiBaseAtb : IComponentDesignAttribute
{
    public int LogisicCapacity { get; internal set; }
    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.TryGetDatablob(out LogiBaseDB lbdb))
        {
            parentEntity.SetDataBlob(lbdb = new LogiBaseDB());
        }
        var instancesDB = parentEntity.GetDataBlob<ComponentInstancesDB>();
        if (instancesDB.TryGetComponentsByAttribute<LogiBaseAtb>(out var instances))
        {
            int totalCap = 0;
            foreach (var instance in instances)
            {
                var designInfo = instance.Design.GetAttribute<LogiBaseAtb>();
                totalCap += designInfo.LogisicCapacity;
            }
            lbdb.Capacity = totalCap;
        }
    }
    public LogiBaseAtb() { }

    public LogiBaseAtb(double logisticCap)
    {
        LogisicCapacity = (int)logisticCap;
    }
    public string AtbName()
    {
        return "Logistics Base";
    }

    public string AtbDescription()
    {
        return "An office which handles the export and import of goods";
    }
}





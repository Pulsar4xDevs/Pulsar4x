using System;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;

namespace Pulsar4X.Atb;

public class LogiBaseAtb : IComponentDesignAttribute
{
    public int LogisicCapacity { get; internal set; }
    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.TryGetDatablob(out LogiBaseDB? lbdb))
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

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if(parentEntity.TryGetDatablob<LogiBaseDB>(out var logiBaseDB)
            && parentEntity.TryGetDatablob<ComponentInstancesDB>(out var componentInstancesDB))
        {
            logiBaseDB.Capacity -= LogisicCapacity;

            if(logiBaseDB.Capacity == 0)
            {
                parentEntity.RemoveDataBlob<LogiBaseDB>();
            }
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
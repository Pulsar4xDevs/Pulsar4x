using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;
using Pulsar4X.Factions;

namespace Pulsar4X.Storage;

public class CargoTransferAtb : IComponentDesignAttribute
{
    /// <summary>
    /// Gets or sets the transfer rate.
    /// </summary>
    /// <value>The transfer rate in Kg/s</value>
    public int TransferRate_kgs { get; internal set; }
    /// <summary>
    /// Gets or sets the transfer range.
    /// </summary>
    /// <value>DeltaV in m/s, Low Earth Orbit is about 10000m/s</value>
    public double TransferRange_ms { get; internal set; }

    public CargoTransferAtb(double rate_kgs, double rangeDV_ms)
    {
        TransferRate_kgs = (int)rate_kgs;
        TransferRange_ms = rangeDV_ms;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.HasDataBlob<CargoStorageDB>())
        {
            var newdb = new CargoStorageDB();
            parentEntity.SetDataBlob(newdb);
        }
        
        StorageSpaceProcessor.RecalcVolumeCapacityAndRates(parentEntity);
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        StorageSpaceProcessor.RecalcVolumeCapacityAndRates(parentEntity);
    }

    public string AtbName()
    {
        return "Cargo Transfer Rate";
    }

    public string AtbDescription()
    {
        return "Adds " + TransferRate_kgs + " kg per hour at " + TransferRange_ms + " m/s Dv";
    }
}
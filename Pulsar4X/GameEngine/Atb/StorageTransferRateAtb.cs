using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;

namespace Pulsar4X.Atb;

public class StorageTransferRateAtb : IComponentDesignAttribute
{
    /// <summary>
    /// Gets or sets the transfer rate.
    /// </summary>
    /// <value>The transfer rate in Kg/h</value>
    public int TransferRate_kgh { get; internal set; }
    /// <summary>
    /// Gets or sets the transfer range.
    /// </summary>
    /// <value>DeltaV in m/s, Low Earth Orbit is about 10000m/s</value>
    public double TransferRange_ms { get; internal set; }

    public StorageTransferRateAtb(double rate_kgh, double rangeDV_ms)
    {
        TransferRate_kgh = (int)rate_kgh;
        TransferRange_ms = rangeDV_ms;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.HasDataBlob<VolumeStorageDB>())
        {
            var newdb = new VolumeStorageDB();
            parentEntity.SetDataBlob(newdb);
        }
        var cargoLibrary = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
        StorageSpaceProcessor.RecalcVolumeCapacityAndRates(parentEntity, cargoLibrary);
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {

    }

    public string AtbName()
    {
        return "Cargo Transfer Rate";
    }

    public string AtbDescription()
    {
        return "Adds " + TransferRate_kgh + " kg per hour at " + TransferRange_ms + " m/s Dv";
    }
}
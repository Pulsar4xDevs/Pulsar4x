using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Components;

namespace Pulsar4X.Atb;
public class VolumeStorageAtb : IComponentDesignAttribute
{
    public string StoreTypeID;
    public double MaxVolume;

    public VolumeStorageAtb(string storeTypeID, double maxVolume)
    {
        StoreTypeID = storeTypeID;
        MaxVolume = maxVolume;
    }

    public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
    {
        if (!parentEntity.HasDataBlob<VolumeStorageDB>())
        {
            var newdb = new VolumeStorageDB(StoreTypeID, MaxVolume);
            parentEntity.SetDataBlob(newdb);
        }
        else
        {
            var db = parentEntity.GetDataBlob<VolumeStorageDB>();
            if (db.TypeStores.ContainsKey(StoreTypeID))
            {
                db.TypeStores[StoreTypeID].MaxVolume += MaxVolume;
                db.TypeStores[StoreTypeID].FreeVolume += MaxVolume;
            }
            else
            {
                db.TypeStores.Add(StoreTypeID, new TypeStore(MaxVolume));
            }
        }
    }

    public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
    {

    }

    public string AtbName()
    {
        return "Cargo Volume";
    }

    public string AtbDescription()
    {
        return "Adds " + MaxVolume + " m^3 Volume to parent cargo storage";
    }
}
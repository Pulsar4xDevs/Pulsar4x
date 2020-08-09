using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    
    public class VolumeStorageDB : BaseDataBlob
    {
        public Dictionary<Guid, TypeStore> TypeStores = new Dictionary<Guid, TypeStore>();
        public double TotalStoredMass { get; internal set; } = 0;

        public int TransferRateInKgHr { get; set; } = 500;

        public double TransferRangeDv_mps { get; set; } = 100;

        [JsonConstructor]
        internal VolumeStorageDB()
        {
        }

        public VolumeStorageDB(Guid type, double maxVolume)
        {
            TypeStores.Add(type, new TypeStore(maxVolume));
        }
        

        public VolumeStorageDB(VolumeStorageDB db)
        {
            TypeStores = new Dictionary<Guid, TypeStore>();
            foreach (var kvp in db.TypeStores)
            {
                TypeStores.Add(kvp.Key, kvp.Value.Clone());
            }
            TotalStoredMass = db.TotalStoredMass;
            TransferRangeDv_mps = db.TransferRangeDv_mps;
            TransferRateInKgHr = db.TransferRateInKgHr;
        }

        public override object Clone()
        {
            return new VolumeStorageDB(this);
        }
    }

    public class TypeStore
    {
        public double MaxVolume;
        public double FreeVolume;
        public Dictionary<Guid, long> CurrentStoreInUnits = new Dictionary<Guid, long>();
        public Dictionary<Guid, ICargoable> Cargoables =  new Dictionary<Guid, ICargoable>();
        public TypeStore(double maxVolume)
        {
            MaxVolume = maxVolume;
            FreeVolume = maxVolume;
        }



        public TypeStore Clone()
        {
            TypeStore clone = new TypeStore(MaxVolume);
            clone.FreeVolume = FreeVolume;
            clone.CurrentStoreInUnits = new Dictionary<Guid, long>(CurrentStoreInUnits);
            clone.Cargoables = new Dictionary<Guid, ICargoable>(Cargoables);
            return clone;
        }

    }
    
    public class VolumeStorageAtb : IComponentDesignAttribute
    {
        public Guid StoreTypeID;
        public double MaxVolume;

        public VolumeStorageAtb(Guid storeTypeID, double maxVolume)
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
    }


}

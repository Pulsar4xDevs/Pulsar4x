using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        /// <summary>
        /// Storage Capacity of this module.
        /// </summary>
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        /// <summary>
        /// Type of cargo this stores
        /// </summary>
        [JsonProperty]
        public Guid CargoTypeGuid { get; internal set; }

        //todo maybe move these to thier own attribute. 
        /// <summary>
        /// Gets or sets the transfer rate.
        /// </summary>
        /// <value>The transfer rate in Kg/h</value>
        public int TransferRate { get; internal set; }
        /// <summary>
        /// Gets or sets the transfer range.
        /// </summary>
        /// <value>DeltaV in km/s, Low Earth Orbit is about 10km/s</value>
        public double TransferRange { get; internal set; }

        /// <summary>
        /// JSON constructor
        /// </summary>
        public CargoStorageAtbDB() { }

        /// <summary>
        /// Parser Constructor
        /// </summary>
        /// <param name="storageCapacity">will get cast to an int</param>
        /// <param name="cargoType">cargo type ID as defined in StaticData CargoTypeSD</param>
        public CargoStorageAtbDB(double storageCapacity, Guid cargoType, double transferRate, double TransferRange) : this((int)storageCapacity, cargoType, (int)transferRate, TransferRange ) { }

        public CargoStorageAtbDB(int storageCapacity, Guid cargoType, int transferRate, double transferRange)
        {
            StorageCapacity = storageCapacity;
            CargoTypeGuid = cargoType;
            TransferRate = transferRate;
            TransferRange = transferRange;
        }

        public CargoStorageAtbDB(CargoStorageAtbDB db)
        {
            StorageCapacity = db.StorageCapacity;
            CargoTypeGuid = db.CargoTypeGuid;
        }

        public override object Clone()
        {
            return new CargoStorageAtbDB(this);
        }
        
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<CargoStorageDB>())
            {
                var db = new CargoStorageDB();
                parentEntity.SetDataBlob(db);
                StorageSpaceProcessor.ReCalcCapacity(parentEntity);
            }
        }
    }

    public class CargoTransferAtbDB : IComponentDesignAttribute
    {
        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            throw new NotImplementedException();
        }
    }

    public class VolumeStorageAtbDB : IComponentDesignAttribute
    {
        public Guid StoreTypeID;
        public double MaxVolume;
        
        
        
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
                }
                else
                {
                    db.TypeStores.Add(StoreTypeID, new TypeStore(MaxVolume));
                }
            }
            
        }
    }



    public class VolumeStorageDB : BaseDataBlob
    {
        public Dictionary<Guid, TypeStore> TypeStores = new Dictionary<Guid, TypeStore>();
        public double TotalStoredMass { get; private set; } = 0;

        public int TransferRateInKgHr { get; set; } = 500;

        public double TransferRangeDv_mps { get; set; } = 100;

        [JsonConstructor]
        VolumeStorageDB()
        {
        }

        public VolumeStorageDB(Guid type, double maxStor)
        {
            TypeStores.Add(type, new TypeStore(maxStor));
        }



        /// <summary>
        /// Add or remove cargo by volume.
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="volume">negitive to remove cargo</param>
        /// <returns>amount of volume successfuly added or removed</returns>
        internal double AddRemoveCargoByVolume(ICargoable cargoItem, double volume)
        {
            //check we're actualy capable of 
            var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            
            TypeStore store = TypeStores[cargoItem.CargoTypeID];
            double volStored = 0;
            if (store.CurrentStore.ContainsKey(cargoItem.ID))
            {
                volStored = store.CurrentStore[cargoItem.ID];
            }
            
            double newStorAmount = Math.Clamp(volume + volStored, 0, store.MaxVolume);
            double volChanged = volStored - newStorAmount;
            
            if (newStorAmount == 0)
                store.CurrentStore.Remove(cargoItem.ID);
            else
                store.CurrentStore[cargoItem.ID] = newStorAmount;

            var massChanged = cargoItem.Density * volChanged;
            store.FreeVolume += volChanged;
            TotalStoredMass += massChanged;
            return volChanged;
        }

        /// <summary>
        /// Add or removes cargo from storage
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="mass">negitive to remove</param>
        /// <returns>amount succesfully added or removed</returns>
        internal double AddRemoveCargoByMass(ICargoable cargoItem, double mass)
        {
            double volume = mass / cargoItem.Density;
            double volChanged = AddRemoveCargoByVolume(cargoItem, volume);
            return cargoItem.Density * volChanged;
        }

        
        
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class TypeStore
    {
        public double MaxVolume;
        public double FreeVolume;
        public Dictionary<Guid,double> CurrentStore = new Dictionary<Guid, double>();

        public TypeStore(double maxVolume)
        {
            MaxVolume = maxVolume;
            FreeVolume = maxVolume;
        }

    }


}

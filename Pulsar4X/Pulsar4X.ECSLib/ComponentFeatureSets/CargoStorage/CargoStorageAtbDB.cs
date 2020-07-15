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

    public class StorageTransferRateAtbDB : IComponentDesignAttribute
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

        public StorageTransferRateAtbDB(int rate_kgh, double rangeDV_ms)
        {
            TransferRate_kgh = rate_kgh;
            TransferRange_ms = rangeDV_ms;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<VolumeStorageDB>())
            {
                var newdb = new VolumeStorageDB();
                parentEntity.SetDataBlob(newdb);
            }
            StorageSpaceProcessor.RecalcVolumeCapacityAndRates(parentEntity);
        }
    }



    public class VolumeStorageDB : BaseDataBlob
    {
        public Dictionary<Guid, TypeStore> TypeStores = new Dictionary<Guid, TypeStore>();
        public double TotalStoredMass { get; private set; } = 0;

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



        /// <summary>
        /// Add or remove cargo by volume. ignores transfer rate.
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
            double volChanged = volStored + newStorAmount;
            
            if (newStorAmount == 0)
                store.CurrentStore.Remove(cargoItem.ID);
            else
                store.CurrentStore[cargoItem.ID] = newStorAmount;

            var massChanged = cargoItem.Density * volChanged;
            store.FreeVolume -= volChanged;
            TotalStoredMass += massChanged;
            return volChanged;
        }

        /// <summary>
        /// Add or removes cargo from storage, ignores transfer rate.
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

        /// <summary>
        /// adds cargo by unit count. ie the minimum MassUnit. 
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount succesfully added</returns>
        internal int AddCargoByUnit(ICargoable cargoItem, int count)
        {
            //check we're actualy capable of 
            var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            
            double volumePerUnit = cargoItem.Mass / cargoItem.Density;
            double totalVolume = volumePerUnit * count;
            TypeStore store = TypeStores[cargoItem.CargoTypeID];

            int amountToAdd = (int)(Math.Min(totalVolume, store.FreeVolume) / cargoItem.Mass);
            
            if(!store.CurrentStore.ContainsKey(cargoItem.ID))
                store.CurrentStore.Add(cargoItem.ID, amountToAdd * volumePerUnit);
            else
                store.CurrentStore[cargoItem.ID] += amountToAdd * volumePerUnit;

            store.FreeVolume -= amountToAdd * volumePerUnit;
            TotalStoredMass += amountToAdd * cargoItem.Mass;

            return amountToAdd;
        }

        /// <summary>
        /// removes cargo by unit count, ie the minimum MassUnit;
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount successfuly removed</returns>
        internal int RemoveCargoByUnit(ICargoable cargoItem, int count)
        {
            //check we're actualy capable of 
            var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            
            
            double volumePerUnit = cargoItem.Mass / cargoItem.Density;
            double totalVolume = volumePerUnit * count;
            TypeStore store = TypeStores[cargoItem.CargoTypeID];
            if (!store.CurrentStore.ContainsKey(cargoItem.ID))
                return 0;
            
            int amountInStore = (int)(store.CurrentStore[cargoItem.ID] / cargoItem.Mass);
            int amountToRemove = Math.Min(count, amountInStore);
            
            
            store.CurrentStore[cargoItem.ID] -= amountToRemove * volumePerUnit;

            store.FreeVolume += amountToRemove * volumePerUnit;
            TotalStoredMass -= amountToRemove * cargoItem.Mass;

            return amountToRemove;
        }

        public double GetVolumeAmount(ICargoable cargoItem)
        {
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0;
            if (!TypeStores[cargoItem.CargoTypeID].CurrentStore.ContainsKey(cargoItem.ID))
                return 0;
            return TypeStores[cargoItem.CargoTypeID].CurrentStore[cargoItem.ID];
        }

        public double GetMassAmount(ICargoable caroItem)
        {
            double volAmount = GetVolumeAmount(caroItem);
            return caroItem.Density * volAmount;
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
        public Dictionary<Guid,double> CurrentStore = new Dictionary<Guid, double>();

        public TypeStore(double maxVolume)
        {
            MaxVolume = maxVolume;
            FreeVolume = maxVolume;
        }

        /// <summary>
        /// Returns the amount of free mass for a given cargoItem
        /// (mass = density * volume)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public double GetFreeMass(ICargoable cargoItem)
        {
            return FreeVolume * cargoItem.Density;
        }

        public TypeStore Clone()
        {
            TypeStore clone = new TypeStore(MaxVolume);
            clone.FreeVolume = FreeVolume;
            clone.CurrentStore = new Dictionary<Guid, double>(CurrentStore);
            return clone;
        }

    }


}

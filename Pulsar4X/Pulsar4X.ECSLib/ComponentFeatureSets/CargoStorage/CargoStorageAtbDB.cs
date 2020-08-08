using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    /*
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
    */

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
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            TypeStore store = TypeStores[cargoItem.CargoTypeID];
            
            double unitsToTryStore = volume / cargoItem.VolumePerUnit;
            double unitsStorable = store.FreeVolume / cargoItem.VolumePerUnit;

            long unitsStoring = (long)Math.Min(unitsToTryStore, unitsStorable);
            double volumeStoring = unitsStoring * cargoItem.VolumePerUnit;
            double massStoring = unitsStoring * cargoItem.MassPerUnit;

            if (!store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                store.CurrentStoreInUnits.Add(cargoItem.ID, unitsStoring);
                store.Cargoables.Add(cargoItem.ID, cargoItem);
            }
            else
            {
                store.CurrentStoreInUnits[cargoItem.ID] += unitsStoring;
            }
            
            store.FreeVolume -= volumeStoring;
            TotalStoredMass += massStoring;
            
            return volumeStoring;
        }

        /// <summary>
        /// Add or removes cargo from storage, ignores transfer rate.
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="mass">negitive to remove</param>
        /// <returns>amount succesfully added or removed</returns>
        internal double AddRemoveCargoByMass(ICargoable cargoItem, double mass)
        {
            //check we're actualy capable of 
            
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            TypeStore store = TypeStores[cargoItem.CargoTypeID];

            
            double unitsToTryStore = cargoItem.MassPerUnit * mass;
            double unitsStorable = store.FreeVolume / cargoItem.VolumePerUnit;

            long unitsStoring = Convert.ToInt64(Math.Min(unitsToTryStore, unitsStorable));
            double volumeStoring = unitsStoring * cargoItem.VolumePerUnit;
            double massStoring = unitsStoring * cargoItem.MassPerUnit;

            if (!store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                store.CurrentStoreInUnits.Add(cargoItem.ID, unitsStoring);
                store.Cargoables.Add(cargoItem.ID, cargoItem);
            }
            else
            {
                store.CurrentStoreInUnits[cargoItem.ID] += unitsStoring;
            }
            
            store.FreeVolume -= volumeStoring;
            TotalStoredMass += massStoring;
            
            return massStoring;
        }

        /// <summary>
        /// adds cargo by unit count. ie the minimum MassUnit. 
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount succesfully added</returns>
        internal long AddCargoByUnit(ICargoable cargoItem, long count)
        {
            //check we're actualy capable of 
            
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            
            double volumePerUnit = cargoItem.VolumePerUnit;
            if (volumePerUnit == 0.0)
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because it does not have a volumetric value.";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }

            double totalVolume = volumePerUnit * count;
            TypeStore store = TypeStores[cargoItem.CargoTypeID];

            long amountToAdd = (long)(Math.Min(totalVolume, store.FreeVolume) / cargoItem.VolumePerUnit);

            if (!store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                store.CurrentStoreInUnits.Add(cargoItem.ID, amountToAdd);
                store.Cargoables.Add(cargoItem.ID, cargoItem);
            }
            else
            {
                store.CurrentStoreInUnits[cargoItem.ID] += amountToAdd;
            }

            store.FreeVolume -= amountToAdd * volumePerUnit;
            TotalStoredMass += amountToAdd * cargoItem.MassPerUnit;

            return amountToAdd;
        }

        /// <summary>
        /// removes cargo by unit count, ie the minimum MassUnit;
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount successfuly removed</returns>
        internal long RemoveCargoByUnit(ICargoable cargoItem, long count)
        {
            //check we're actualy capable of 
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(OwningEntity, errString);
                return 0;
            }
            
            double volumePerUnit = cargoItem.VolumePerUnit;
            double totalVolume = volumePerUnit * count;
            TypeStore store = TypeStores[cargoItem.CargoTypeID];
            if (!store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                return 0;
            }
            
            long amountInStore = store.CurrentStoreInUnits[cargoItem.ID];
            long amountToRemove = Math.Min(count, amountInStore);
            
            store.CurrentStoreInUnits[cargoItem.ID] -= amountToRemove;
            store.FreeVolume += amountToRemove * volumePerUnit;
            TotalStoredMass -= amountToRemove * cargoItem.MassPerUnit;

            if (store.CurrentStoreInUnits[cargoItem.ID] == 0)
            {
                store.CurrentStoreInUnits.Remove(cargoItem.ID);
                store.Cargoables.Remove(cargoItem.ID);
            }
            
            return amountToRemove;
        }

        public double GetVolumeStored(ICargoable cargoItem)
        {
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units * cargoItem.VolumePerUnit;
        }

        public double GetMassStored(ICargoable cargoItem)
        {
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units * cargoItem.MassPerUnit;
        }

        public long GetUnitsStored(ICargoable cargoItem)
        {
            if (!TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0;
            if (!TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0;
            long units = Math.Max(0, TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units;
        }

        /// <summary>
        /// Will randomly dump cargo if volume to remove is more than the free volume.
        /// TODO: should be psudorandom.
        /// TODO: should create an entity in space depending on type of cargo. 
        /// </summary>
        /// <param name="typeID">cargo typeID</param>
        /// <param name="volumeChange">positive to add volume, negitive to remove volume</param>
        public void ChangeMaxVolume(Guid typeID, double volumeChange)
        {
            var type = TypeStores[typeID];
            type.MaxVolume += volumeChange;
            type.FreeVolume += volumeChange;
            
            if(type.FreeVolume < 0)
            {
                Random prng = new Random(); //todo: grab seed from parent entity (or entity manager?) system for this is not yet implemented
                var indexlist = type.CurrentStoreInUnits.Keys.ToList();
                while (type.FreeVolume < 0)
                {
                    var prngIndex = prng.Next(0, type.CurrentStoreInUnits.Count - 1);
                    var cargoID = indexlist[prngIndex];
                    ICargoable cargoItem = StaticRefLib.StaticData.GetICargoable(cargoID);
                    var volPerUnit = cargoItem.VolumePerUnit;
                    long unitsStored = type.CurrentStoreInUnits[cargoID];
                    var volumeRemoved = AddRemoveCargoByVolume(cargoItem, volumeChange);
                    type.FreeVolume += volumeRemoved;
                    indexlist.Remove(cargoID);
                }
            }
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

        /// <summary>
        /// Returns the amount of free mass for a given cargoItem
        /// (mass = density * volume)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public double GetFreeMass(ICargoable cargoItem)
        {
            return (FreeVolume / cargoItem.VolumePerUnit) * cargoItem.MassPerUnit;
        }

        public int GetFreeUnitSpace(ICargoable cargoItem)
        {
            return (int)(FreeVolume / cargoItem.VolumePerUnit);
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


}

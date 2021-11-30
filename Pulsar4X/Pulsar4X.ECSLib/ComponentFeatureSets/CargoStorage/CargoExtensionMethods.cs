using System;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// NOTE!!! none of these update an entites total mass!
    /// </summary>
    public static class CargoExtensionMethods
    {
        /// <summary>
        /// Add or remove cargo by volume. 
        /// Ignores transfer rate. Does  not update MassVolumeDB
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="volume">negitive to remove cargo</param>
        /// <returns>amount of volume successfuly added or removed</returns>
        internal static double AddRemoveCargoByVolume(this VolumeStorageDB db, ICargoable cargoItem, double volume)
        {
            //check we're actualy capable of 
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
                return 0;
            }
            TypeStore store = db.TypeStores[cargoItem.CargoTypeID];
            
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
            db.TotalStoredMass += massStoring;
            
            return volumeStoring;
        }

        /// <summary>
        /// Add or removes cargo from storage, 
        /// Ignores transfer rate. Does  not update MassVolumeDB
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="mass">negitive to remove</param>
        /// <returns>amount succesfully added or removed</returns>
        internal static double AddRemoveCargoByMass(this VolumeStorageDB db, ICargoable cargoItem, double mass)
        {
            //check we're actualy capable of 
            
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity,EventType.Storage, errString);
                return 0;
            }
            TypeStore store = db.TypeStores[cargoItem.CargoTypeID];

            
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
            db.TotalStoredMass += massStoring;
            
            return massStoring;
        }


        /// <summary>
        /// adds cargo by unit count. ie the minimum MassUnit. 
        /// Ignores transfer rate. Does  not update MassVolumeDB
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount succesfully added</returns>
        internal static long AddCargoByUnit(this VolumeStorageDB db, ICargoable cargoItem, long count)
        {
            //check we're actualy capable of 
            
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity,EventType.Storage, errString);
                return 0;
            }
            
            double volumePerUnit = cargoItem.VolumePerUnit;
            if (volumePerUnit == 0.0)
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because it does not have a volumetric value.";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
                return 0;
            }

            double totalVolume = volumePerUnit * count;
            TypeStore store = db.TypeStores[cargoItem.CargoTypeID];

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
            db.TotalStoredMass += amountToAdd * cargoItem.MassPerUnit;

            return amountToAdd;
        }

        /// <summary>
        /// removes cargo by unit count, ie the minimum MassUnit;
        /// Ignores transfer rate. Does  not update MassVolumeDB
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="count"></param>
        /// <returns>amount successfuly removed</returns>
        internal static long RemoveCargoByUnit(this VolumeStorageDB db, ICargoable cargoItem, long count)
        {
            //check we're actualy capable of 
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
                return 0;
            }
    
            double volumePerUnit = cargoItem.VolumePerUnit;
            double totalVolume = volumePerUnit * count;
            TypeStore store = db.TypeStores[cargoItem.CargoTypeID];
            if (!store.CurrentStoreInUnits.ContainsKey(cargoItem.ID))
            {
                return 0;
            }
    
            long amountInStore = store.CurrentStoreInUnits[cargoItem.ID];
            long amountToRemove = Math.Min(count, amountInStore);
    
            store.CurrentStoreInUnits[cargoItem.ID] -= amountToRemove;
            store.FreeVolume += amountToRemove * volumePerUnit;
            db.TotalStoredMass -= amountToRemove * cargoItem.MassPerUnit;

            if (store.CurrentStoreInUnits[cargoItem.ID] == 0)
            {
                store.CurrentStoreInUnits.Remove(cargoItem.ID);
                store.Cargoables.Remove(cargoItem.ID);
            }
    
            return amountToRemove;
        }


        /// <summary>
        /// Gives the amount of volume taken up by a given cargoItem
        /// </summary>
        /// <param name="db"></param>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetVolumeStored(this VolumeStorageDB db, ICargoable cargoItem)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units * cargoItem.VolumePerUnit;
        }

        /// <summary>
        /// Gives the amount of mass stored for a given item
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetMassStored(this VolumeStorageDB db,ICargoable cargoItem)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units * cargoItem.MassPerUnit;
        }

        /// <summary>
        /// Gives the amount of units that are stored of a given item
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static long GetUnitsStored(this VolumeStorageDB db,ICargoable cargoItem)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            return units;
        }

        /// <summary>
        /// Returns the amount of free mass for a given cargoItem
        /// (mass = density * volume)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetFreeMass(this VolumeStorageDB db, ICargoable cargoItem)
        {
            var type = cargoItem.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                return 0;
            return db.TypeStores[type].FreeVolume / cargoItem.VolumePerUnit * cargoItem.MassPerUnit;
        }

        /// <summary>
        /// Returns the amount of free volume for a given cargoItem
        /// (volume = mass / density)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetFreeVolume(this VolumeStorageDB db, ICargoable cargoItem)
        {
            var type = cargoItem.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                return 0;
            return db.TypeStores[type].FreeVolume;
        }

        /// <summary>
        /// Returns the amount of free mass for a given cargoType
        /// (volume = mass / density)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetFreeVolume(this VolumeStorageDB db, Guid cargoType)
        {
            if (!db.TypeStores.ContainsKey(cargoType))
                return 0;
            return db.TypeStores[cargoType].FreeVolume;
        }
        
        /// <summary>
        /// Returns the amount of free space in units for a given cargoItem
        /// (space = freeVolume / VolumePerUnit)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static int GetFreeUnitSpace(this VolumeStorageDB db, ICargoable cargoItem)
        {
            var type = cargoItem.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                return 0;
            return (int)(db.TypeStores[type].FreeVolume / cargoItem.VolumePerUnit);
        }

        /// <summary>
        /// Will randomly dump cargo if volume to remove is more than the free volume.
        /// TODO: should be psudorandom.
        /// TODO: should create an entity in space depending on type of cargo. 
        /// </summary>
        /// <param name="typeID">cargo typeID</param>
        /// <param name="volumeChange">positive to add volume, negitive to remove volume</param>
        public static void ChangeMaxVolume(this VolumeStorageDB db, Guid typeID, double volumeChange)
        {
            var type = db.TypeStores[typeID];
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
                    var volumeRemoved = db.AddRemoveCargoByVolume(cargoItem, volumeChange);
                    type.FreeVolume += volumeRemoved;
                    indexlist.Remove(cargoID);
                }
            }
        }
        
        internal static bool HasSpecificEntity(this VolumeStorageDB storeDB, CargoAbleTypeDB item)
        {
            if (storeDB.TypeStores[item.CargoTypeID].Cargoables.ContainsKey(item.ID))
                return true;

            return false;
        }
        
    }
}
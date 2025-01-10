using System;
using System.Linq;

namespace Pulsar4X.Storage
{
    public static class CargoMath
    {
        
        /// <summary>
        /// Add or remove cargo by volume.
        /// Ignores transfer rate. Does  not update MassVolumeDB
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <param name="volume">negitive to remove cargo</param>
        /// <returns>amount of volume successfuly added or removed</returns>
        internal static double AddRemoveCargoByVolume(this CargoStorageDB db, ICargoable cargoItem, double volume)
        {
            //check we're actualy capable of
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                // FIXME:
                // string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot store this type of cargo";
                // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
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
        internal static double AddRemoveCargoByMass(this CargoStorageDB db, ICargoable cargoItem, double mass)
        {
            //check we're actualy capable of

            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                // var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                // string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity,EventType.Storage, errString);
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
        internal static long AddCargoByUnit(this CargoStorageDB db, ICargoable cargoItem, long count)
        {
            //check we're actualy capable of

            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                // var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                // string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity,EventType.Storage, errString);
                return 0;
            }

            double volumePerUnit = cargoItem.VolumePerUnit;
            if (volumePerUnit == 0.0)
            {
                // var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                // string errString = "Can't add or remove " + cargoItem.Name + " because it does not have a volumetric value.";
                // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
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
        internal static long RemoveCargoByUnit(this CargoStorageDB db, ICargoable cargoItem, long count)
        {
            //check we're actualy capable of
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
            {
                // var type = StaticRefLib.StaticData.CargoTypes[cargoItem.CargoTypeID];
                // string errString = "Can't add or remove " + cargoItem.Name + " because this entity cannot even store " + type.Name + " types of cargo";
                // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(db.OwningEntity, EventType.Storage, errString);
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
        public static double GetVolumeStored(this CargoStorageDB db, ICargoable cargoItem, bool includeEscro)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);
            if (includeEscro)
            {
                units += GetUnitCountInEscro(db, cargoItem);
            }
            return units * cargoItem.VolumePerUnit;
        }

        /// <summary>
        /// Gives the amount of mass stored for a given item
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetMassStored(this CargoStorageDB db,ICargoable cargoItem, bool includeEscro)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);

            if (includeEscro)
            {
                units += GetUnitCountInEscro(db, cargoItem);
            }
            
            return units * cargoItem.MassPerUnit;
        }

        public static long GetUnitCountInEscro(CargoStorageDB db, ICargoable cargoItem)
        {
            long unitCount = 0;
            foreach (var transferData in db.EscroItems)
            {
                if(db.OwningEntity == transferData.PrimaryStorageDB.OwningEntity || db.OwningEntity == transferData.SecondaryStorageDB.OwningEntity)//I think this is wrong
                {
                    foreach (var tup in transferData.EscroHeldInPrimary)
                    {
                        if (tup.item.ID == cargoItem.ID)
                        {
                            unitCount += tup.count;
                            break;
                        }
                    }
                    foreach (var tup in transferData.EscroHeldInSecondary)
                    {
                        if (tup.item.ID == cargoItem.ID)
                        {
                            unitCount += tup.count;
                            break;
                        }
                    }
                }   
            }
            return unitCount;
        }

        /// <summary>
        /// Gives the max amount of mass storeable for a given item
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        internal static double GetMassMax(this CargoStorageDB db,ICargoable cargoItem)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0.0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0.0;
            var volume = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].MaxVolume);

            return volume / cargoItem.VolumePerUnit;
        }

        /// <summary>
        /// Gives the amount of units that are stored of a given item
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static long GetUnitsStored(this CargoStorageDB db,ICargoable cargoItem, bool includeEscro)
        {
            if (!db.TypeStores.ContainsKey(cargoItem.CargoTypeID))
                return 0;
            if (!db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits.ContainsKey(cargoItem.ID))
                return 0;
            long units = Math.Max(0, db.TypeStores[cargoItem.CargoTypeID].CurrentStoreInUnits[cargoItem.ID]);
            if(includeEscro)
                units += GetUnitCountInEscro(db, cargoItem);
            return units;
        }

        /// <summary>
        /// Returns the amount of free mass for a given cargoItem
        /// (mass = density * volume)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetFreeMass(this CargoStorageDB db, ICargoable cargoItem)
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
        public static double GetFreeVolume(this CargoStorageDB db, ICargoable cargoItem)
        {
            var type = cargoItem.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                return 0;
            return db.TypeStores[type].FreeVolume;
        }

        public static double GetMaxVolume(this CargoStorageDB db, ICargoable cargoItem)
        {
            var type = cargoItem.CargoTypeID;
            if(!db.TypeStores.ContainsKey(type))
                return 0;
            return db.TypeStores[type].MaxVolume;
        }

        /// <summary>
        /// Returns the amount of free mass for a given cargoType
        /// escro items are included in this
        /// (volume = mass / density)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns></returns>
        public static double GetFreeVolume(this CargoStorageDB db, string cargoType)
        {
            if (!db.TypeStores.ContainsKey(cargoType))
                return 0;
            return db.TypeStores[cargoType].FreeVolume;
        }

        /// <summary>
        /// Returns the amount of free space in units for a given cargoItem
        /// escro items are included in this
        /// (space = freeVolume / VolumePerUnit)
        /// </summary>
        /// <param name="cargoItem"></param>
        /// <returns>Number of items we can store</returns>
        public static long GetFreeUnitSpace(this CargoStorageDB db, ICargoable cargoItem, bool includeEscro = true)
        {
            var type = cargoItem.CargoTypeID;
            if (!db.TypeStores.ContainsKey(type))
                return 0;
            long items = (int)(db.TypeStores[type].FreeVolume / cargoItem.VolumePerUnit);
            if(includeEscro)
                items -= GetUnitCountInEscro(db, cargoItem);
            return items;
        }

        /// <summary>
        /// Will randomly dump cargo if volume to remove is more than the free volume.
        /// TODO: should create an entity in space depending on type of cargo.
        /// TODO: cargoLibrary should be a global library not just a faction one, or we'll have problems from captured ships.
        /// </summary>
        /// <param name="typeID">cargo typeID</param>
        /// <param name="volumeChange">positive to add volume, negitive to remove volume</param>
        /// <param name="cargoLibrary">TODO this should be a global library not a faction library I think</param>
        internal static void ChangeMaxVolume(this CargoStorageDB db, string typeID, double volumeChange, CargoDefinitionsLibrary cargoLibrary)
        {
            var type = db.TypeStores[typeID];
            type.MaxVolume += volumeChange;
            type.FreeVolume += volumeChange;

            if(type.FreeVolume < 0)
            {
                var mgr = db.OwningEntity.Manager;
                var indexlist = type.CurrentStoreInUnits.Keys.ToList();
                while (type.FreeVolume < 0)
                {
                    var prngIndex = mgr.RNGNext(0, type.CurrentStoreInUnits.Count - 1);
                    var cargoID = indexlist[prngIndex];
                    ICargoable cargoItem = cargoLibrary.GetAny(cargoID);
                    var volPerUnit = cargoItem.VolumePerUnit;
                    long unitsStored = type.CurrentStoreInUnits[cargoID];
                    var volumeRemoved = db.AddRemoveCargoByVolume(cargoItem, volumeChange);
                    type.FreeVolume += volumeRemoved;
                    indexlist.Remove(cargoID);
                }
            }
        }

        internal static bool HasSpecificEntity(this CargoStorageDB storeDB, CargoAbleTypeDB item)
        {
            if (storeDB.TypeStores[item.CargoTypeID].Cargoables.ContainsKey(item.ID))
                return true;

            return false;
        }
    }
}
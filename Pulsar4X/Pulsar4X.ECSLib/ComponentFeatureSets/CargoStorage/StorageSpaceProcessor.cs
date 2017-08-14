using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;


namespace Pulsar4X.ECSLib
{

    public static class StorageSpaceProcessor
    {
        /// <summary>
        /// returns the amount of items for a given item guid.
        /// </summary>
        /// <param name="fromCargo"></param>
        /// <param name="itemID">a min or mat ID</param>
        /// <returns></returns>
        public static long GetAmountOf(CargoStorageDB fromCargo, Guid itemID)
        {
            Guid cargoTypeID = fromCargo.ItemToTypeMap[itemID];
            ICargoable cargo = fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(itemID);
            long returnValue = 0;
            if (fromCargo.MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
            {
                if (fromCargo.MinsAndMatsByCargoType[cargoTypeID].ContainsKey(cargo))
                {
                    returnValue = fromCargo.MinsAndMatsByCargoType[cargoTypeID][cargo];
                }
            }
            return returnValue;
        }

        /// <summary>
        /// a list of entities stored of a given cargotype
        /// </summary>
        /// <param name="typeID">cargo type guid</param>
        /// <returns>new list of Entites or an empty list</returns>
        public static List<Entity> GetEntitesOfCargoType(CargoStorageDB fromCargo, Guid typeID)
        {
            List<Entity> entityList = new List<Entity>();
            if (fromCargo.StoredEntities.ContainsKey(typeID))
            {
                foreach (var kvp in fromCargo.StoredEntities[typeID])
                {
                    entityList.AddRange(kvp.Value.GetInternalList());
                }
            }
            return entityList;
        }

        public static bool HasEntity(CargoStorageDB cargo, Entity entity)
        {
            var designEntity = entity.GetDataBlob<DesignInfoDB>();
            var cargoableDB = entity.GetDataBlob<CargoAbleTypeDB>();
            if (cargo.StoredEntities.ContainsKey(cargoableDB.CargoTypeID))
                if (cargo.StoredEntities[cargoableDB.CargoTypeID].ContainsKey(designEntity.DesignEntity))
                    if (cargo.StoredEntities[cargoableDB.CargoTypeID][designEntity.DesignEntity].Contains(entity))
                        return true;
            return false;
        }

        //public static Entity GetEntity(CargoStorageDB cargo, Entity entity)
        //{
        //    var designEntity = entity.GetDataBlob<DesignInfoDB>();
        //    var cargoableDB = entity.GetDataBlob<CargoAbleTypeDB>();
        //    if (cargo.StoredEntities.ContainsKey(cargoableDB.CargoTypeID))
        //        if (cargo.StoredEntities[cargoableDB.CargoTypeID].ContainsKey(designEntity.DesignEntity))
        //            if (cargo.StoredEntities[cargoableDB.CargoTypeID][designEntity.DesignEntity].Contains(entity))
        //                return cargo.StoredEntities[cargoableDB.CargoTypeID][designEntity.DesignEntity].Contains(entity);
        //    return false;
        //}

        /// <summary>
        /// a Dictionary of resources stored of a given cargotype
        /// </summary>
        /// <param name="typeID">cargo type guid</param>
        /// <returns>new dictionary of resources or an empty dictionary</returns>
        public static Dictionary<ICargoable, long> GetResourcesOfCargoType(CargoStorageDB fromCargo, Guid typeID)
        {
            if (fromCargo.MinsAndMatsByCargoType.ContainsKey(typeID))
                return new Dictionary<ICargoable, long>(fromCargo.MinsAndMatsByCargoType[typeID].GetInternalDictionary());
            return new Dictionary<ICargoable, long>();
        }

        /// <summary>
        /// Adds a value to the dictionary, if the item does not exsist, it will get added to the dictionary.
        /// </summary>
        /// <param name="item">the guid of the item to add</param>
        /// <param name="value">the amount of the item to add</param>
        private static void AddValue(CargoStorageDB toCargo, ICargoable item, long value)
        {
            Guid cargoTypeID = toCargo.ItemToTypeMap[item.ID];
            if (!toCargo.MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
            {
                toCargo.MinsAndMatsByCargoType.Add(cargoTypeID, new PrIwObsDict<ICargoable, long>());             
            }
            if (!toCargo.MinsAndMatsByCargoType[cargoTypeID].ContainsKey(item))
            {
                toCargo.MinsAndMatsByCargoType[cargoTypeID].Add(item, value);                                                        
            }
            else
                toCargo.MinsAndMatsByCargoType[cargoTypeID][item] += value;
        }

        internal static void AddItemToCargo(CargoStorageDB toCargo, Guid itemID, long amount)
        {
            ICargoable item = (ICargoable)toCargo.OwningEntity.Manager.Game.StaticData.FindDataObjectUsingID(itemID);
            long remainingWeightCapacity = RemainingCapacity(toCargo, item.CargoTypeID);
            long remainingNumCapacity = (long)(remainingWeightCapacity / item.Mass);
            float amountWeight = amount / item.Mass;
            if (remainingNumCapacity >= amount)
                AddValue(toCargo, item, amount);
            else
                AddValue(toCargo, item, remainingNumCapacity);
        }

        /// <summary>
        /// Checks storage capacity and stores either the amount or the amount that toCargo is capable of storing.
        /// </summary>
        /// <param name="toCargo"></param>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        internal static void AddItemToCargo(CargoStorageDB toCargo, ICargoable item, int amount)
        {
            long remainingWeightCapacity = RemainingCapacity(toCargo, item.CargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / item.Mass);
            float amountWeight = amount * item.Mass;
            if (remainingNumCapacity >= amount)
                AddValue(toCargo, item, amount);
            else
                AddValue(toCargo, item, remainingNumCapacity);
        }

        /// <summary>
        /// checks the toCargo and stores the item if there is enough space.
        /// </summary>
        /// <param name="toCargo"></param>
        /// <param name="entity"></param>
        /// <param name="cargoTypeDB"></param>
        /// <param name=""></param>
        internal static void AddItemToCargo(CargoStorageDB toCargo, Entity entity)
        {
            Entity designEntity = entity.GetDataBlob<DesignInfoDB>().DesignEntity;
            ICargoable cargoTypeDB = designEntity.GetDataBlob<CargoAbleTypeDB>();
            float amountWeight = cargoTypeDB.Mass;
            long remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeDB.CargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / amountWeight);

            if (remainingNumCapacity >= 1)
                AddToCargo(toCargo, entity, cargoTypeDB);
        }

        /// <summary>
        /// Will remove the item from the dictionary if subtracting the value causes the dictionary value to be 0.
        /// </summary>
        /// <param name="itemID">the guid of the item to subtract</param>
        /// <param name="value">the amount of the item to subtract</param>
        /// <returns>the amount succesfully taken from the dictionary(will not remove more than what the dictionary contains)</returns>
        internal static long SubtractValue(CargoStorageDB fromCargo, Guid itemID, long value)
        {
            Guid cargoTypeID = fromCargo.ItemToTypeMap[itemID];
            ICargoable cargoItem = fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(itemID);
            long returnValue = 0;
            if (fromCargo.MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
                if (fromCargo.MinsAndMatsByCargoType[cargoTypeID].ContainsKey(cargoItem))
                {
                    if (fromCargo.MinsAndMatsByCargoType[cargoTypeID][cargoItem] >= value)
                    {
                        fromCargo.MinsAndMatsByCargoType[cargoTypeID][cargoItem] -= value;
                        returnValue = value;
                    }
                    else
                    {
                        returnValue = fromCargo.MinsAndMatsByCargoType[cargoTypeID][cargoItem];
                        fromCargo.MinsAndMatsByCargoType[cargoTypeID].Remove(cargoItem);
                    }
                }
            return returnValue;
        }

        /// <summary>
        /// Checks storage capacity and transferes either the amount or the amount that toCargo is capable of storing.
        /// </summary>
        /// <param name="fromCargo"></param>
        /// <param name="toCargo"></param>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        internal static void TransferCargo(CargoStorageDB fromCargo, CargoStorageDB toCargo, ICargoable item, int amount)
        {
            Guid cargoTypeID = item.CargoTypeID;
            float itemWeight = item.Mass;
            Guid itemID = item.ID;

            long remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeID);
            long remainingNumCapacity = (long)(remainingWeightCapacity / itemWeight);
            float amountWeight = amount * itemWeight;
            if (remainingNumCapacity >= amount)
            {
                //AddToCargo(toCargo, item, amount);
                //fromCargo.MinsAndMatsByCargoType[cargoTypeID][itemID] -= amount;
                long amountRemoved = SubtractValue(fromCargo, itemID, amount);
                AddValue(toCargo, item, amountRemoved);


            }
            else
            {
                //AddToCargo(toCargo, item, remainingNumCapacity);
                //fromCargo.MinsAndMatsByCargoType[cargoTypeID][itemID] -= remainingNumCapacity;
                long amountRemoved = SubtractValue(fromCargo, itemID, remainingNumCapacity);
                AddValue(toCargo, item, amountRemoved);
            }
        }

        /// <summary>
        /// must be mins or mats
        /// </summary>
        /// <param name="fromCargo"></param>
        /// <param name="amounts">must be mins or mats</param>
        internal static void RemoveResources(CargoStorageDB fromCargo, Dictionary<Guid, int> amounts)
        {
            foreach (var item in amounts)
            {
                SubtractValue(fromCargo, item.Key, item.Value);
            }
        }

        /// <summary>
        /// checks the toCargo and transferes the item if there is enough space.
        /// </summary>
        /// <param name="fromCargo"></param>
        /// <param name="toCargo"></param>
        /// <param name="entityItem"></param>
        internal static void TransferEntity(CargoStorageDB fromCargo, CargoStorageDB toCargo, Entity entityItem)
        {
            CargoAbleTypeDB cargotypedb = entityItem.GetDataBlob<CargoAbleTypeDB>();
            Guid cargoTypeID = cargotypedb.CargoTypeID;
            float itemWeight = cargotypedb.Mass;
            Guid itemID = cargotypedb.ID;

            long remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeID);
            long remainingNumCapacity = (long)(remainingWeightCapacity / itemWeight);
            if (remainingNumCapacity >= 1)
            {
                if (fromCargo.StoredEntities[cargoTypeID].Remove(entityItem))
                    AddToCargo(toCargo, entityItem, cargotypedb);
            }
        }

        private static void AddToCargo(CargoStorageDB toCargo, Entity entityItem, ICargoable cargotypedb)
        {
            if (!entityItem.HasDataBlob<ComponentInstanceInfoDB>())
                new Exception("entityItem does not contain ComponentInstanceInfoDB, it must be an componentInstance type entity");
            Entity design = entityItem.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;
            if (!toCargo.StoredEntities.ContainsKey(cargotypedb.CargoTypeID))
                toCargo.StoredEntities.Add(cargotypedb.CargoTypeID, new PrIwObsDict<Entity, PrIwObsList<Entity>>());
            if (!toCargo.StoredEntities[cargotypedb.CargoTypeID].ContainsKey(design))
                toCargo.StoredEntities[cargotypedb.CargoTypeID].Add(design, new PrIwObsList<Entity>());
            toCargo.StoredEntities[cargotypedb.CargoTypeID][design].Add(entityItem);
        }




        public static long RemainingCapacity(CargoStorageDB cargo, Guid typeID)
        {
            long capacity = cargo.CargoCapicity[typeID];
            long storedWeight = NetWeight(cargo, typeID);
            return capacity - storedWeight;
        }

        public static long NetWeight(CargoStorageDB cargo, Guid typeID)
        {            
            long net = 0;
            if (cargo.MinsAndMatsByCargoType.ContainsKey(typeID))
                net = StoredWeight(cargo.MinsAndMatsByCargoType, typeID);
            else if (cargo.StoredEntities.ContainsKey(typeID))
                net = StoredWeight(cargo.StoredEntities, typeID);
            return net;
        }

        private static long StoredWeight(PrIwObsDict<Guid, PrIwObsDict<ICargoable, long>> dict, Guid TypeID)
        {
            long storedWeight = 0;
            foreach (var amount in dict[TypeID].Values.ToArray())
            {
                storedWeight += amount;
            }
            return storedWeight;
        }

        private static long StoredWeight(PrIwObsDict<Guid, PrIwObsDict<Entity, PrIwObsList<Entity>>> dict, Guid TypeID)
        {
            double storedWeight = 0;
            foreach (var itemType in dict[TypeID])
            {
                foreach (var designInstanceKVP in itemType.Value)
                {
                    storedWeight += designInstanceKVP.GetDataBlob<MassVolumeDB>().Mass;
                }

            }
            return (int)Math.Round(storedWeight, MidpointRounding.AwayFromZero);
        }

        public static bool HasReqiredItems(CargoStorageDB stockpile, Dictionary<Guid, int> costs)
        {
            if (costs == null)
                return true;
            else
            {
                foreach (var costitem in costs)
                {
                    if (costitem.Value >= GetAmountOf(stockpile, costitem.Key))
                        return false;
                }
            }
            return true;
        }

        internal static void ReCalcCapacity(Entity parentEntity)
        {
            CargoStorageDB storageDB = parentEntity.GetDataBlob<CargoStorageDB>();
            PrIwObsDict<Guid, long> totalSpace = storageDB.CargoCapicity;

            List<KeyValuePair<Entity, PrIwObsList<Entity>>> StorageComponents = parentEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<CargoStorageAtbDB>()).ToList();
            foreach (var kvp in StorageComponents)
            {
                Entity componentDesign = kvp.Key;
                Guid cargoTypeID = componentDesign.GetDataBlob<CargoStorageAtbDB>().CargoTypeGuid;
                long alowableSpace = 0;
                foreach (var specificComponent in kvp.Value)
                {
                    var healthPercent = specificComponent.GetDataBlob<ComponentInstanceInfoDB>().HealthPercent();
                    if (healthPercent > 0.75)
                        alowableSpace = componentDesign.GetDataBlob<CargoStorageAtbDB>().StorageCapacity;
                }
                if (!totalSpace.ContainsKey(cargoTypeID))
                {
                    totalSpace.Add(cargoTypeID, alowableSpace);
                    if (!storageDB.MinsAndMatsByCargoType.ContainsKey(cargoTypeID))
                        storageDB.MinsAndMatsByCargoType.Add(cargoTypeID, new PrIwObsDict<ICargoable, long>());                                              
                }
                else if (totalSpace[cargoTypeID] != alowableSpace)
                {
                    totalSpace[cargoTypeID] = alowableSpace;
                    if (RemainingCapacity(storageDB, cargoTypeID) < 0)
                    { //todo: we've lost cargo capacity, and we're carrying more than we have storage for, drop random cargo
                    }
                }
            }
        }
    }
}

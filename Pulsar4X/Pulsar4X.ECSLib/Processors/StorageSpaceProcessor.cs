using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    public static class StorageSpaceProcessor
    {



        internal static void AddItemToCargo(CargoStorageDB toCargo, Guid itemID, int amount)
        {
            ICargoable item = (ICargoable)toCargo.OwningEntity.Manager.Game.StaticData.FindDataObjectUsingID(itemID);
            int remainingWeightCapacity = RemainingCapacity(toCargo, item.CargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / item.Mass);
            float amountWeight = amount / item.Mass;
            if (remainingNumCapacity >= amount)               
                toCargo.AddValue(itemID, amount);
            else
                toCargo.AddValue(itemID, remainingNumCapacity);
        }

        /// <summary>
        /// Checks storage capacity and stores either the amount or the amount that toCargo is capable of storing.
        /// </summary>
        /// <param name="toCargo"></param>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        internal static void AddItemToCargo(CargoStorageDB toCargo, ICargoable item, int amount)
        {
            int remainingWeightCapacity = RemainingCapacity(toCargo, item.CargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / item.Mass);
            float amountWeight = amount * item.Mass;
            if (remainingNumCapacity >= amount)
                toCargo.AddValue(item.ID, amount);
            else
                toCargo.AddValue(item.ID, remainingNumCapacity);
        }

        /// <summary>
        /// checks the toCargo and stores the item if there is enough space.
        /// </summary>
        /// <param name="toCargo"></param>
        /// <param name="entity"></param>
        /// <param name="cargoTypeDB"></param>
        /// <param name=""></param>
        internal static void AddItemToCargo(CargoStorageDB toCargo, Entity entity, ICargoable cargoTypeDB)
        {
            float amountWeight = cargoTypeDB.Mass;
            int remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeDB.CargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / amountWeight);
            
            if (remainingNumCapacity >= 1)
                AddToCargo(toCargo, entity, cargoTypeDB);
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

            int remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / itemWeight);
            float amountWeight = amount * itemWeight;
            if (remainingNumCapacity >= amount)
            {
                //AddToCargo(toCargo, item, amount);
                //fromCargo.MinsAndMatsByCargoType[cargoTypeID][itemID] -= amount;
                int amountRemoved = fromCargo.SubtractValue(itemID, amount);
                toCargo.AddValue(itemID, amountRemoved);
                
                
            }
            else
            {
                //AddToCargo(toCargo, item, remainingNumCapacity);
                //fromCargo.MinsAndMatsByCargoType[cargoTypeID][itemID] -= remainingNumCapacity;
                int amountRemoved = fromCargo.SubtractValue(itemID, remainingNumCapacity);
                toCargo.AddValue(itemID, amountRemoved);
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
                  fromCargo.SubtractValue(item.Key, item.Value);
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

            int remainingWeightCapacity = RemainingCapacity(toCargo, cargoTypeID);
            int remainingNumCapacity = (int)(remainingWeightCapacity / itemWeight);
            if (remainingNumCapacity >= 1)
            {
                if (fromCargo.StoredEntities[cargoTypeID].Remove(entityItem))
                    AddToCargo(toCargo, entityItem, cargotypedb);         
            }
        }      

        private static void AddToCargo(CargoStorageDB toCargo, Entity entityItem, ICargoable cargotypedb)
        {
            if (!toCargo.StoredEntities.ContainsKey(cargotypedb.CargoTypeID))
                toCargo.StoredEntities.Add(cargotypedb.CargoTypeID, new List<Entity>());
            toCargo.StoredEntities[cargotypedb.CargoTypeID].Add(entityItem);
        }

       


        public static int RemainingCapacity(CargoStorageDB cargo, Guid typeID )
        {           
            int capacity = cargo.CargoCapicity[typeID];
            int storedWeight = NetWeight(cargo, typeID);
            return capacity - storedWeight;
        }

        public static int NetWeight(CargoStorageDB cargo, Guid typeID)
        {
            int net = 0;
            if (cargo.MinsAndMatsByCargoType.ContainsKey(typeID))
                net = StoredWeight(cargo.MinsAndMatsByCargoType, typeID);
            else if (cargo.StoredEntities.ContainsKey(typeID))
                net = StoredWeight(cargo.StoredEntities, typeID);
            return net;
        }

        private static int StoredWeight(Dictionary<Guid, Dictionary<Guid, int>> dict, Guid TypeID)
        {
            int storedWeight = 0;
            foreach (var amount in dict[TypeID].Values)
            {
                storedWeight += amount;
            }
            return storedWeight;
        }

        private static int StoredWeight(Dictionary<Guid, List<Entity>> dict, Guid TypeID)
        {
            double storedWeight = 0;
            foreach (var item in dict[TypeID])
            {                
                storedWeight += item.GetDataBlob<MassVolumeDB>().Mass; 
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
                    if (costitem.Value <= stockpile.GetAmountOf(costitem.Key))
                        return false;
                }
            }
            return true;
        }



        internal static void ReCalcCapacity(Entity parentEntity)
        {

            Dictionary<Guid, int> totalSpace = parentEntity.GetDataBlob<CargoStorageDB>().CargoCapicity;

            List<KeyValuePair<Entity, List<Entity>>> StorageComponents = parentEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.Where(item => item.Key.HasDataBlob<CargoStorageAtbDB>()).ToList();
            foreach (var kvp in StorageComponents)
            {
                Entity componentDesign = kvp.Key;
                Guid cargoTypeID = componentDesign.GetDataBlob<CargoStorageAtbDB>().CargoTypeGuid;
                foreach (var specificComponent in kvp.Value)
                {
                    var healthPercent = specificComponent.GetDataBlob<ComponentInstanceInfoDB>().HealthPercent();
                    if (healthPercent > 0.75)
                        totalSpace.SafeValueReplace(cargoTypeID, componentDesign.GetDataBlob<CargoStorageAtbDB>().StorageCapacity);
                }
            }
        }
    }
}

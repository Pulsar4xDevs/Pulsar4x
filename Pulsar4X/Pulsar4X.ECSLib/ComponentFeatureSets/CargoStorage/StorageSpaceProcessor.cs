using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;


namespace Pulsar4X.ECSLib
{

    public static class StorageSpaceProcessor
    {

        /// <summary>
        /// checks if the storage contains all the items and amounts in a given dictionary. 
        /// </summary>
        /// <param name="stockpile"></param>
        /// <param name="costs"></param>
        /// <returns></returns>
        public static bool HasReqiredItems(CargoStorageDB stockpile, Dictionary<ICargoable, int> costs)
        {            
            if (costs == null)
                return true;
            else
            {
                foreach (var costitem in costs)
                {
                    if (costitem.Value >= stockpile.StoredCargoTypes[costitem.Key.CargoTypeID].ItemsAndAmounts[costitem.Key.ID])
                        return false;
                }
            }
            return true;
        }

        public static long GetAmount(CargoStorageDB storeDB, Guid storeTypeGuid, Guid itemGuid)
        {
            return storeDB.StoredCargoTypes[storeTypeGuid].ItemsAndAmounts[itemGuid];
        }
        public static long GetAmount(CargoStorageDB storeDB, ICargoable item)
        {
            return storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID];
        }
        
        /// <summary>
        /// must be mins or mats DOES NOT CHECK Availiblity
        /// will throw normal dictionary exceptions.
        /// </summary>
        /// <param name="fromCargo"></param>
        /// <param name="amounts">must be mins or mats</param>
        internal static void RemoveResources(CargoStorageDB fromCargo, Dictionary<ICargoable, int> amounts)
        {
            
            foreach (var kvp in amounts)
            {
                RemoveCargo(fromCargo, kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// Does not check if cargo or cargotype exsists. will throw normal dictionary exptions if you try.
        /// just removes the amount from store and updates the free capacity
        /// </summary>
        /// <param name="storeDB"></param>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        internal static void RemoveCargo(CargoStorageDB storeDB, ICargoable item, long amount)
        {
            if (item is CargoAbleTypeDB)
            {
                CargoAbleTypeDB cargoItem = (CargoAbleTypeDB)item;
                if (cargoItem.MustBeSpecificCargo)
                    storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites[cargoItem.ID].Remove(cargoItem.OwningEntity);
            }
            storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID] -= amount;
            //FreeCapacity is *MASS*
            storeDB.StoredCargoTypes[item.CargoTypeID].FreeCapacity += item.Mass * amount; 
        }


        internal static void AddCargo(CargoStorageDB storeDB, ICargoable item, long amount)
        {
            if (item is CargoAbleTypeDB)
            {
                CargoAbleTypeDB cargoItem = (CargoAbleTypeDB)item;
                if (cargoItem.MustBeSpecificCargo)
                {
                    if(!storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites.ContainsKey(cargoItem.ID))
                        storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites.Add(cargoItem.ID, new List<Entity>());
                    storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites[cargoItem.ID].Add(cargoItem.OwningEntity);
                }
            }
            if(!storeDB.StoredCargoTypes.ContainsKey(item.CargoTypeID))
                storeDB.StoredCargoTypes.Add(item.CargoTypeID, new CargoTypeStore());
            else if(!storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts.ContainsKey(item.ID))
                storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts.Add(item.ID, 0);
            
            storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID] += amount;
            //FreeCapacity is *MASS*
            storeDB.StoredCargoTypes[item.CargoTypeID].FreeCapacity -= item.Mass * amount; 
        }

        internal static bool HasEntity(CargoStorageDB storeDB, CargoAbleTypeDB item)        
        {
            if(storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites.ContainsKey(item.ID))
                if (storeDB.StoredCargoTypes[item.CargoTypeID].SpecificEntites[item.ID].Contains(item.OwningEntity))
                    return true;
            return false;
        }

        /// <summary>
        /// psudo randomly drops cargo. this could be made a bit better maybe... but should do for now. 
        /// TODO: actualy this is compleatly broken. it's removing amount instead of weight.
        /// </summary>
        /// <param name="typeStore"></param>
        /// <param name="weightToLoose"></param>
        private static void DropRandomCargo(CargoTypeStore typeStore, long weightToLoose)
        {
            int n = typeStore.ItemsAndAmounts.Count();
            int seed = n;
            var prng = new Random(seed);
            List<Guid> indexes = typeStore.ItemsAndAmounts.Keys.ToList();          
            
            while (n > 1) {  
                n--;  
                int k = prng.Next(n + 1);  
                Guid value = indexes[k];  
                indexes[k] = indexes[n];  
                indexes[n] = value;  
            }

            int i = 0;
            while (weightToLoose > 0)
            {
                long amountStored = typeStore.ItemsAndAmounts[indexes[i]];
                long removeAmount = Math.Min(amountStored, weightToLoose);
                //TODO: create a new entity for the dropped cargo so it can be collected.
                typeStore.ItemsAndAmounts[indexes[i]] -= removeAmount;
                weightToLoose -= removeAmount;
                i++;
            }
        }


        internal static void ReCalcCapacity(Entity parentEntity)
        {

            Dictionary<Guid, CargoTypeStore> storageDBStoredCargos = parentEntity.GetDataBlob<CargoStorageDB>().StoredCargoTypes;

            Dictionary<Guid, long> calculatedMaxStorage = new Dictionary<Guid, long>();

            var instances = parentEntity.GetDataBlob<ComponentInstancesDB>();
            var designs = instances.GetDesignsByType(typeof(CargoStorageAtbDB));

            foreach (var design in designs)
            {
                foreach (var instanceInfo in instances.GetComponentsBySpecificDesign(design.Guid))
                {
                    var componentDesign = instanceInfo.DesignEntity.GetDataBlob<CargoStorageAtbDB>();
                    long allowableSpace = 0;

                    Guid cargoTypeID = componentDesign.CargoTypeGuid;

                    var healthPercent = instanceInfo.HealthPercent();
                    if (healthPercent > 0.75) //hardcoded health percent at 3/4, cargo is delecate? TODO: streach goal make this modable
                        allowableSpace = componentDesign.StorageCapacity;

                    calculatedMaxStorage.SafeValueAdd(cargoTypeID, allowableSpace);
                }
            }










            //List<KeyValuePair<Entity, PrIwObsList<Entity>>> storageComponents = parentEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<CargoStorageAtbDB>()).ToList();



            /*
            foreach (var kvp in storageComponents) //first loop through the component types
            {                
                Entity componentDesign = kvp.Key;
                Guid cargoTypeID = componentDesign.GetDataBlob<CargoStorageAtbDB>().CargoTypeGuid;
                long alowableSpace = 0;
                foreach (var specificComponent in kvp.Value) //then loop through each specific component
                {//checking the helth...
                    var healthPercent = specificComponent.GetDataBlob<ComponentInstanceInfoDB>().HealthPercent();
                    if (healthPercent > 0.75) //hardcoded health percent at 3/4, cargo is delecate? todo: streach goal make this modable
                        alowableSpace = componentDesign.GetDataBlob<CargoStorageAtbDB>().StorageCapacity;
                }
                //then add the amount to our tempory dictionary
                if (!calculatedMaxStorage.ContainsKey(cargoTypeID))
                    calculatedMaxStorage.Add(cargoTypeID, alowableSpace);
                else
                    calculatedMaxStorage[cargoTypeID] += alowableSpace;    
            }
            */
            //now loop through our tempory dictionary and match it up with the real one. 
            foreach (var kvp in calculatedMaxStorage)
            {
                Guid cargoTypeID = kvp.Key;
                long validMaxCapacity = kvp.Value;
                
                
                if (!storageDBStoredCargos.ContainsKey(cargoTypeID))
                {
                    var newStore = new CargoTypeStore();
                    newStore.MaxCapacity = validMaxCapacity;
                    newStore.FreeCapacity = validMaxCapacity;
                    storageDBStoredCargos.Add(cargoTypeID, newStore);                                        
                }
                
                else if (storageDBStoredCargos[cargoTypeID].MaxCapacity != validMaxCapacity)
                {    
                    long usedSpace = storageDBStoredCargos[cargoTypeID].MaxCapacity - storageDBStoredCargos[cargoTypeID].FreeCapacity;
                    
                    storageDBStoredCargos[cargoTypeID].MaxCapacity = validMaxCapacity;

                    if (!(usedSpace <= validMaxCapacity))
                    {
                        long overweight = usedSpace - validMaxCapacity;
                        DropRandomCargo(storageDBStoredCargos[cargoTypeID], overweight);
                    }
                }
            }
        }
    }
}

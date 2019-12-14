using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;
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
        /// note that this will not return true for unique(damaged) items.  
        /// </summary>
        /// <param name="stockpile"></param>
        /// <param name="costs"></param>
        /// <returns></returns>
        public static bool HasRequiredItems(CargoStorageDB stockpile, Dictionary<ICargoable, int> costs)
        {            
            if (costs == null)
                return true;
            else
            {
                foreach (var costitem in costs)
                {
                    if (costitem.Value > 0)
                    {
                        if (stockpile.StoredCargoTypes.ContainsKey(costitem.Key.CargoTypeID) == false)
                            return false;
                        if (stockpile.StoredCargoTypes[costitem.Key.CargoTypeID].ItemsAndAmounts.ContainsKey(costitem.Key.ID) == false)
                            return false;
                    }

                    if (costitem.Value > stockpile.StoredCargoTypes[costitem.Key.CargoTypeID].ItemsAndAmounts[costitem.Key.ID].amount)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// returns the number of items of a given item guid,
        /// Not this will not count unique(damaged) items.
        /// </summary>
        /// <param name="storeDB"></param>
        /// <param name="storeTypeGuid"></param>
        /// <param name="itemGuid"></param>
        /// <returns></returns>
        public static long GetAmount(CargoStorageDB storeDB, Guid storeTypeGuid, Guid itemGuid)
        {
            if(storeDB.StoredCargoTypes.ContainsKey(storeTypeGuid))
                if(storeDB.StoredCargoTypes[storeTypeGuid].ItemsAndAmounts.ContainsKey(itemGuid))
                    return storeDB.StoredCargoTypes[storeTypeGuid].ItemsAndAmounts[itemGuid].amount;
            return 0;
        }

        /// <summary>
        /// returns the number of items of a given ICargoable item,
        /// Not this will not count unique(damaged) items.
        /// </summary>
        /// <param name="storeDB"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static long GetAmount(CargoStorageDB storeDB, ICargoable item)
        {
            if(storeDB.StoredCargoTypes.ContainsKey(item.CargoTypeID))
                if(storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts.ContainsKey(item.ID))
                    return storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID].amount;
            return 0;
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
            var newTotal = storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID].amount - amount;
            storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID] = (item, newTotal);
            //FreeCapacity is *MASS*
            storeDB.StoredCargoTypes[item.CargoTypeID].FreeCapacityKg += item.Mass * amount; 
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

            var id = item.ID;
            //if the item is a componentInstance, and has no damage we store it by design.
            if(item is ComponentInstance)
            {
                ComponentInstance ci = (ComponentInstance)item;
                if(ci.HTKRemaining == ci.HTKMax)
                    id = ci.Design.ID;
            }
            if(!storeDB.StoredCargoTypes.ContainsKey(item.CargoTypeID))
                storeDB.StoredCargoTypes.Add(item.CargoTypeID, new CargoTypeStore());

            if(!storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts.ContainsKey(item.ID))
                storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts.Add(id, (item, amount));
            else
            {
                long total = storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID].amount + amount;
                storeDB.StoredCargoTypes[item.CargoTypeID].ItemsAndAmounts[item.ID] = (item, total);
            }
            //FreeCapacity is *MASS*
            storeDB.StoredCargoTypes[item.CargoTypeID].FreeCapacityKg -= item.Mass * amount; 
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
        /// TODO: this is probly not very random, figure a better seed. 
        /// </summary>
        /// <param name="typeStore"></param>
        /// <param name="massToLoose"></param>
        private static void DropRandomCargo(CargoTypeStore typeStore, long massToLoose)
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
            while (massToLoose > 0)
            {
                var itemAmounts = typeStore.ItemsAndAmounts[indexes[i]];
                long amountStored = itemAmounts.amount;
                int itemMass = itemAmounts.item.Mass;
                long totalMass = amountStored * itemMass;
                long removeMass = Math.Min(totalMass, massToLoose);
                long removeAmount = removeMass / itemMass;
                //TODO: create a new entity for the dropped cargo so it can be collected.
                typeStore.ItemsAndAmounts[indexes[i]] = (itemAmounts.item, itemAmounts.amount - removeAmount);
                massToLoose -= removeMass;
                i++;
            }
        }


        internal static void ReCalcCapacity(Entity parentEntity)
        {
            CargoStorageDB cargoStorageDB = parentEntity.GetDataBlob<CargoStorageDB>();
            Dictionary<Guid, CargoTypeStore> storageDBStoredCargos = cargoStorageDB.StoredCargoTypes;

            Dictionary<Guid, long> calculatedMaxStorage = new Dictionary<Guid, long>();

            var instancesDB = parentEntity.GetDataBlob<ComponentInstancesDB>();

            double transferRate = 0;
            double transferRange = 0; 
            
            int i = 0;
            
            if( instancesDB.TryGetComponentsByAttribute<CargoStorageAtbDB>(out var componentInstances))
            {
                
                foreach (var instance in componentInstances)
                {
                    var design = instance.Design;
                    var atbdata = design.GetAttribute<CargoStorageAtbDB>();

                    if (instance.HealthPercent() > 0.75)
                    {
                        calculatedMaxStorage[atbdata.CargoTypeGuid] = atbdata.StorageCapacity;
                        transferRate += atbdata.TransferRate;
                        transferRange += atbdata.TransferRange;
                        i++;
                    }
                }
            }
            
            //transfer rate and ranges are averaged. 
            cargoStorageDB.TransferRateInKgHr = (int)(transferRate / i);
            cargoStorageDB.TransferRangeDv = (transferRange / i);

            

            //List<KeyValuePair<Entity, PrIwObsList<Entity>>> storageComponents = parentEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<CargoStorageAtbDB>()).ToList();



            /*
            foreach (var kvp in storageComponents) //first loop through the component types
            {                
                Entity componentDesign = kvp.Key;
                ID cargoTypeID = componentDesign.GetDataBlob<CargoStorageAtbDB>().CargoTypeGuid;
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
                    newStore.MaxCapacityKg = validMaxCapacity;
                    newStore.FreeCapacityKg = validMaxCapacity;
                    storageDBStoredCargos.Add(cargoTypeID, newStore);                                        
                }
                
                else if (storageDBStoredCargos[cargoTypeID].MaxCapacityKg != validMaxCapacity)
                {    
                    long usedSpace = storageDBStoredCargos[cargoTypeID].MaxCapacityKg - storageDBStoredCargos[cargoTypeID].FreeCapacityKg;
                    
                    storageDBStoredCargos[cargoTypeID].MaxCapacityKg = validMaxCapacity;

                    if (!(usedSpace <= validMaxCapacity))
                    {
                        long overweight = usedSpace - validMaxCapacity;
                        DropRandomCargo(storageDBStoredCargos[cargoTypeID], overweight);
                    }
                }
            }
        }

        public static CargoCapacityCheckResult GetAvailableSpace(CargoStorageDB storeDB, Guid itemGuid, ICargoDefinitionsLibrary library)
        {
            var cargoDefinition = library.GetOther(itemGuid);
            if (cargoDefinition.Mass == 0)
                return new CargoCapacityCheckResult(itemGuid, long.MaxValue, long.MaxValue);

            return new CargoCapacityCheckResult(itemGuid, 
                storeDB.StoredCargoTypes[cargoDefinition.CargoTypeID].FreeCapacityKg / cargoDefinition.Mass,
                storeDB.StoredCargoTypes[cargoDefinition.CargoTypeID].FreeCapacityKg);
        }

    }

}



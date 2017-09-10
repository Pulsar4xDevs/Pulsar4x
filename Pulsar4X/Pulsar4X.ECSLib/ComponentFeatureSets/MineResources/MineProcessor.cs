using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal class MineResourcesProcessor : IHotloopProcessor
    {
        private StaticDataStore _staticDataStore;
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            if (MineProcessor.StaticDataStore == null)
                MineProcessor.StaticDataStore = _staticDataStore;
            
            MineProcessor.MineResources(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            foreach(var entity in manager.GetAllEntitiesWithDataBlob<MiningDB>()) 
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }
    }

    internal static class MineProcessor
    {
        internal static StaticDataStore StaticDataStore;
        internal static void MineResources(Entity colonyEntity)
        {
            Dictionary<Guid, int> mineRates = colonyEntity.GetDataBlob<MiningDB>().MineingRate;
            Dictionary<Guid,MineralDepositInfo> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().Minerals;
            
            CargoStorageDB stockpile = colonyEntity.GetDataBlob<CargoStorageDB>();
            float mineBonuses = 1;//colonyEntity.GetDataBlob<ColonyBonusesDB>().GetBonus(AbilityType.Mine);
            foreach (var kvp in mineRates)
            {
                ICargoable mineral = StaticDataStore.GetICargoable(kvp.Key);
                double accessability = planetMinerals[kvp.Key].Accessibility;
                double actualRate = kvp.Value * mineBonuses * accessability;
                int amountMinableThisTick = (int)Math.Min(actualRate, planetMinerals[kvp.Key].Amount);
                
                long freeCapacity = stockpile.StoredCargoTypes[mineral.CargoTypeID].FreeCapacity;

                Guid cargoTypeID = mineral.CargoTypeID;
                int itemMassPerUnit = mineral.Mass;
                long weightMinableThisTick = itemMassPerUnit * amountMinableThisTick;
                weightMinableThisTick = Math.Min(weightMinableThisTick, freeCapacity);  
                
                int actualAmountToMineThisTick = (int)(weightMinableThisTick / itemMassPerUnit);                                         //get the number of items from the mass transferable
                long actualweightMinaedThisTick = actualAmountToMineThisTick * itemMassPerUnit;
                
                
                if(!stockpile.StoredCargoTypes.ContainsKey(cargoTypeID)) 
                    stockpile.StoredCargoTypes.Add(cargoTypeID, new CargoTypeStore());
                else
                    stockpile.StoredCargoTypes[cargoTypeID].ItemsAndAmounts[mineral.ID] += actualAmountToMineThisTick;
                
                stockpile.StoredCargoTypes[cargoTypeID].FreeCapacity -= actualweightMinaedThisTick;
                
      
                MineralDepositInfo mineralDeposit = planetMinerals[kvp.Key];
                int newAmount = mineralDeposit.Amount -= actualAmountToMineThisTick;

                accessability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
                double newAccess = GMath.Clamp(accessability, 0.1, mineralDeposit.Accessibility);

                mineralDeposit.Amount = newAmount;
                mineralDeposit.Accessibility = newAccess;
                
            }
        }

        /// <summary>
        /// Called by the ReCalcProcessor.
        /// </summary>
        /// <param name="colonyEntity"></param>
        internal static void CalcMaxRate(Entity colonyEntity)
        {

            Dictionary<Guid,int> rates = new Dictionary<Guid, int>();
            var instancesDB = colonyEntity.GetDataBlob<ComponentInstancesDB>();
            List<KeyValuePair<Entity, PrIwObsList<Entity>>> mineEntities = instancesDB.SpecificInstances.GetInternalDictionary().Where(item => item.Key.HasDataBlob<MineResourcesAtbDB>()).ToList();
            foreach (var mineComponentDesignList in mineEntities)
            {
                foreach (var mineInstance in mineComponentDesignList.Value)
                {
                    //todo check if it's damaged, check if it's enabled, check if there's enough workers here to.
                    foreach (var item in mineComponentDesignList.Key.GetDataBlob<MineResourcesAtbDB>().ResourcesPerEconTick)
                    {
                        rates.SafeValueAdd(item.Key, item.Value);
                    }                    
                }
            }
            colonyEntity.GetDataBlob<MiningDB>().MineingRate = rates;
        }
    }
}
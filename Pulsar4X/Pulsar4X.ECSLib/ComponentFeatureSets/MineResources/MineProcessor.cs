using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal class MineResourcesProcessor : IHotloopProcessor, IRecalcProcessor
    {
        private Dictionary<Guid, MineralSD> _minerals;

        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(1);

        public Type GetParameterType => typeof(MiningDB);


        public void Init(Game game)
        {
            _minerals = game.StaticData.CargoGoods.GetMinerals();
        }
        
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {           
            MineResources(entity);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            foreach(var entity in manager.GetAllEntitiesWithDataBlob<MiningDB>()) 
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }
    
        private void MineResources(Entity colonyEntity)
        {
            Dictionary<Guid, int> mineRates = colonyEntity.GetDataBlob<MiningDB>().MineingRate;
            Dictionary<Guid,MineralDepositInfo> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().Minerals;
            
            CargoStorageDB stockpile = colonyEntity.GetDataBlob<CargoStorageDB>();
            float mineBonuses = 1;//colonyEntity.GetDataBlob<ColonyBonusesDB>().GetBonus(AbilityType.Mine);
            foreach (var kvp in mineRates)
            {
                ICargoable mineral = _minerals[kvp.Key];
                Guid cargoTypeID = mineral.CargoTypeID;
                double itemMassPerUnit = mineral.Density;
                
                
                double accessability = planetMinerals[kvp.Key].Accessibility;
                double actualRate = kvp.Value * mineBonuses * accessability;
                int amountMinableThisTick = (int)Math.Min(actualRate, planetMinerals[kvp.Key].Amount);
                
                long freeCapacity = stockpile.StoredCargoTypes[mineral.CargoTypeID].FreeCapacityKg;

                long weightMinableThisTick = (long)itemMassPerUnit * amountMinableThisTick;
                weightMinableThisTick = Math.Min(weightMinableThisTick, freeCapacity);  
                
                int actualAmountToMineThisTick = (int)(weightMinableThisTick / itemMassPerUnit);                                         //get the number of items from the mass transferable
                long actualweightMinaedThisTick = (long)(actualAmountToMineThisTick * itemMassPerUnit);

                StorageSpaceProcessor.AddCargo(stockpile, mineral, actualAmountToMineThisTick);
      
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

            if (instancesDB.TryGetComponentsByAttribute<MineResourcesAtbDB>(out var instances))
            {
                foreach (var instance in instances)
                {
                    float healthPercent = instance.HealthPercent();
                    var designInfo = instance.Design.GetAttribute<MineResourcesAtbDB>();
     
                    foreach (var item in designInfo.ResourcesPerEconTick)
                    {
                        rates.SafeValueAdd(item.Key, (int)(item.Value * healthPercent)); 
                    }
                }
            }
            
            colonyEntity.GetDataBlob<MiningDB>().MineingRate = rates;
        }

        public byte ProcessPriority { get; set; } = 100;


        public void RecalcEntity(Entity entity)
        {
            CalcMaxRate(entity);
        }


    }
}
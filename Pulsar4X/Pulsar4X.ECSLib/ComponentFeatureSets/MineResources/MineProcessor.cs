using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Orbital;

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

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var entities = manager.GetAllEntitiesWithDataBlob<MiningDB>();
            foreach(var entity in entities) 
            {
                ProcessEntity(entity, deltaSeconds);
            }
            return entities.Count;
        }
    
        private void MineResources(Entity colonyEntity)
        {
            Dictionary<Guid, long> actualMiningRates = MiningHelper.CalculateActualMiningRates(colonyEntity);
            Dictionary<Guid,MineralDeposit> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;
            VolumeStorageDB stockpile = colonyEntity.GetDataBlob<VolumeStorageDB>();

            foreach (var kvp in actualMiningRates)
            {
                ICargoable mineral = _minerals[kvp.Key];
                Guid cargoTypeID = mineral.CargoTypeID;

                var unitsMinableThisTick = (long)Math.Min(actualMiningRates[kvp.Key], planetMinerals[kvp.Key].Amount);

                if(!stockpile.TypeStores.ContainsKey(cargoTypeID))
                {
                    var type = StaticRefLib.StaticData.CargoTypes[cargoTypeID];
                    string erstr = "We didn't mine a potential " + unitsMinableThisTick + " of " + mineral.Name + " because we have no way to store " + type.Name + " cargo.";
                    StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity, EventType.Storage, erstr);
                       continue; //can't store this mineral
                }
                
                var unitsMinedThisTick = stockpile.AddCargoByUnit(mineral, unitsMinableThisTick);

                if (unitsMinableThisTick > unitsMinedThisTick)
                {
                    long dif = unitsMinableThisTick - unitsMinedThisTick;
                    var type = StaticRefLib.StaticData.CargoTypes[cargoTypeID];
                    string erstr = "We didn't mine a potential " + dif + " of " + mineral.Name + " because we don't have enough space to store it.";
                    StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity,EventType.Storage, erstr);
                }

                MineralDeposit mineralDeposit = planetMinerals[kvp.Key];
                long newAmount = mineralDeposit.Amount -= unitsMinedThisTick;

                var accessability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
                double newAccess = GeneralMath.Clamp(accessability, 0.1, mineralDeposit.Accessibility);

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
            Dictionary<Guid, long> rates = new Dictionary<Guid, long>();
            var instancesDB = colonyEntity.GetDataBlob<ComponentInstancesDB>();

            if (instancesDB.TryGetComponentsByAttribute<MineResourcesAtbDB>(out var instances))
            {
                foreach (var instance in instances)
                {
                    float healthPercent = instance.HealthPercent();
                    var designInfo = instance.Design.GetAttribute<MineResourcesAtbDB>();
     
                    foreach (var item in designInfo.ResourcesPerEconTick)
                    {
                        rates.SafeValueAdd(item.Key, Convert.ToInt64(item.Value * healthPercent)); 
                    }
                }
            }
            
            colonyEntity.GetDataBlob<MiningDB>().MiningRate = rates;
        }

        public byte ProcessPriority { get; set; } = 100;


        public void RecalcEntity(Entity entity)
        {
            CalcMaxRate(entity);
        }


    }
}
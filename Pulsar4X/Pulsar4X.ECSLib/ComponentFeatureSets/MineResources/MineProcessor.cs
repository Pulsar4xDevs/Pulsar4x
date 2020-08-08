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

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            foreach(var entity in manager.GetAllEntitiesWithDataBlob<MiningDB>()) 
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }
    
        private void MineResources(Entity colonyEntity)
        {
            Dictionary<Guid, long> mineRates = colonyEntity.GetDataBlob<MiningDB>().MineingRate;
            Dictionary<Guid,MineralDepositInfo> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().Minerals;
            
            VolumeStorageDB stockpile = colonyEntity.GetDataBlob<VolumeStorageDB>();
            float mineBonuses = 1;//colonyEntity.GetDataBlob<ColonyBonusesDB>().GetBonus(AbilityType.Mine);
            foreach (var kvp in mineRates)
            {
                ICargoable mineral = _minerals[kvp.Key];
                Guid cargoTypeID = mineral.CargoTypeID;
                double itemMassPerUnit = mineral.MassPerUnit;

                double accessability = planetMinerals[kvp.Key].Accessibility;
                double actualRate = kvp.Value * mineBonuses * accessability;
                var unitsMinableThisTick = (long)Math.Min(actualRate, planetMinerals[kvp.Key].Amount);

                if(!stockpile.TypeStores.ContainsKey(mineral.CargoTypeID))
                {
                    var type = StaticRefLib.StaticData.CargoTypes[mineral.CargoTypeID];
                    string erstr = "We didn't mine a potential " + unitsMinableThisTick + " of " + mineral.Name + " because we have no way to store " + type.Name + " cargo.";
                       StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity, erstr);
                       continue; //can't store this mineral
                }
                
                var unitsMinedThisTick = stockpile.AddCargoByUnit(mineral, unitsMinableThisTick);

                if (unitsMinableThisTick > unitsMinedThisTick)
                {
                    long dif = unitsMinableThisTick - unitsMinedThisTick;
                    var type = StaticRefLib.StaticData.CargoTypes[mineral.CargoTypeID];
                    string erstr = "We didn't mine a potential " + dif + " of " + mineral.Name + " because we don't have enough space to store it.";
                    StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity, erstr);
                }

                MineralDepositInfo mineralDeposit = planetMinerals[kvp.Key];
                long newAmount = mineralDeposit.Amount -= unitsMinedThisTick;

                accessability = Math.Pow((float)mineralDeposit.Amount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
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
            
            colonyEntity.GetDataBlob<MiningDB>().MineingRate = rates;
        }

        public byte ProcessPriority { get; set; } = 100;


        public void RecalcEntity(Entity entity)
        {
            CalcMaxRate(entity);
        }


    }
}
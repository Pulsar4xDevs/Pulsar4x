using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Engine;
using Pulsar4X.Colonies;
using Pulsar4X.Events;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry
{
    internal class MineResourcesProcessor : IHotloopProcessor, IRecalcProcessor
    {
        private Dictionary<int, Mineral> _minerals;
        public TimeSpan RunFrequency => TimeSpan.FromDays(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromHours(1);

        public Type GetParameterType => typeof(MiningDB);


        public void Init(Game game)
        {
            _minerals = new ();

            EventManager.Instance.Subscribe(EventType.ColonyAdministratorAssigned, OnAdminAssigned);

            foreach(var (uniqueID, mineral) in game.StartingGameData.Minerals)
            {
                _minerals.Add(mineral.ID, mineral);
            }
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            if(entity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB)
                && colonyInfoDB.PlanetEntity.TryGetDataBlob<MineralsDB>(out var mineralsDB)
                && entity.TryGetDataBlob<MiningDB>(out var miningDB)
                && entity.TryGetDataBlob<CargoStorageDB>(out var stockpile))
                MineResources(entity, colonyInfoDB, mineralsDB, miningDB, stockpile);
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

        private void MineResources(Entity colonyEntity, ColonyInfoDB colonyInfoDB, MineralsDB mineralsDB, MiningDB miningDB, CargoStorageDB stockpile)
        {
            Dictionary<int, long> actualMiningRates = miningDB.ActualMiningRate;
            Dictionary<int, MineralDeposit> planetMinerals = mineralsDB.Minerals;

            // Mines are buildings too: scale their output by the colony's infrastructure capacity.
            double infraEfficiency = InfrastructureProcessor.GetEfficiency(colonyEntity);

            foreach (var kvp in actualMiningRates)
            {
                ICargoable mineral = _minerals[kvp.Key];
                string cargoTypeID = mineral.CargoTypeID;

                var unitsMinableThisTick = (long)Math.Min(actualMiningRates[kvp.Key] * infraEfficiency, planetMinerals[kvp.Key].Amount.Actual);

                if(!stockpile.TypeStores.ContainsKey(cargoTypeID))
                {
                    // var type = StaticRefLib.StaticData.CargoTypes[cargoTypeID];
                    // string erstr = "We didn't mine a potential " + unitsMinableThisTick + " of " + mineral.Name + " because we have no way to store " + type.Name + " cargo.";
                    // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity, EventType.Storage, erstr);
                    continue; //can't store this mineral
                }

                var unitsMinedThisTick = stockpile.AddCargoByUnit(mineral, unitsMinableThisTick);

                if (unitsMinableThisTick > unitsMinedThisTick)
                {
                    // long dif = unitsMinableThisTick - unitsMinedThisTick;
                    // var type = StaticRefLib.StaticData.CargoTypes[cargoTypeID];
                    // string erstr = "We didn't mine a potential " + dif + " of " + mineral.Name + " because we don't have enough space to store it.";
                    // StaticRefLib.EventLog.AddPlayerEntityErrorEvent(colonyEntity,EventType.Storage, erstr);
                }

                MineralDeposit mineralDeposit = planetMinerals[kvp.Key];
                long newAmount = mineralDeposit.Amount.Actual - unitsMinedThisTick;

                var amount = mineralDeposit.Amount;
                amount.Actual = newAmount;
                mineralDeposit.Amount = amount;

                var accessability = Math.Pow((float)newAmount / mineralDeposit.HalfOriginalAmount, 3) * mineralDeposit.Accessibility;
                double newAccess = GeneralMath.Clamp(accessability, 0.1, mineralDeposit.Accessibility);
                mineralDeposit.Accessibility = newAccess;
            }
        }

        /// <summary>
        /// Called by the ReCalcProcessor.
        /// </summary>
        /// <param name="colonyEntity"></param>
        internal static void CalcMaxRate(Entity colonyEntity)
        {
            if (!colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB) ||
                !colonyEntity.GetFactionOwner.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB) ||
                !colonyEntity.TryGetDataBlob<MiningDB>(out var miningDB))
                return;

            var rates = new Dictionary<int, long>();
            var cargoLibrary = factionInfoDB.Data.CargoGoods;

            if (instancesDB.TryGetComponentsByAttribute<MineResourcesAtbDB>(out var instances))
            {
                foreach (var instance in instances)
                {
                    float healthPercent = instance.HealthPercent;
                    var designInfo = instance.Design.GetAttribute<MineResourcesAtbDB>();

                    foreach (var item in designInfo.ResourcesPerEconTick)
                    {
                        // Need to convert the uniqueID (item.Key) to an int ID
                        var cargoable = cargoLibrary[item.Key];
                        rates.SafeValueAdd(cargoable.ID, Convert.ToInt64(item.Value * healthPercent));
                    }
                }
            }

            miningDB.BaseMiningRate = rates;

            // Calculate the actual mining rates if the planet entity has minerals
            if (colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB) && colonyInfoDB.PlanetEntity.HasDataBlob<MineralsDB>())
            {
                miningDB.ActualMiningRate = MiningHelper.CalculateActualMiningRates(colonyEntity);
            }
        }

        public byte ProcessPriority { get; set; } = 100;


        public void RecalcEntity(Entity entity)
        {
            CalcMaxRate(entity);
        }

        private void OnAdminAssigned(Event e)
        {


        }


    }
}
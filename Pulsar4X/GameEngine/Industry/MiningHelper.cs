using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;
using Pulsar4X.Colonies;

namespace Pulsar4X.Industry
{
    public static class MiningHelper
    {
        public static Dictionary<int, long> CalculateActualMiningRates(Entity colonyEntity)
        {
            if (!colonyEntity.TryGetDataBlob<MiningDB>(out var miningDB))
                throw new Exception("Entity does not have MiningDB");
            if (!colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB))
                throw new Exception("Entity does not have ColonyInfoDB");
            if (!colonyInfoDB.PlanetEntity.TryGetDataBlob<MineralsDB>(out var mineralsDB))
                throw new Exception("Planet entity does not have MineralsDB");

            float miningBonuses = 1.0f;
            if (colonyEntity.TryGetDataBlob<ColonyBonusesDB>(out var colonyBonusesDB))
            {
                miningBonuses = colonyBonusesDB.GetBonus(AbilityType.Mine);
            }

            var mineRates = miningDB.BaseMiningRate.ToDictionary(k => k.Key, v => v.Value);
            var planetMinerals = mineralsDB.Minerals;

            foreach (var (key, value) in mineRates)
            {
                long baseRateFromMiningInstallations = mineRates[key];
                double accessibility = planetMinerals.ContainsKey(key) ? planetMinerals[key].Accessibility : 0;
                double actualRate = baseRateFromMiningInstallations * miningBonuses * accessibility;
                mineRates[key] = Convert.ToInt64(actualRate);
            }

            return mineRates;
        }
    }
}

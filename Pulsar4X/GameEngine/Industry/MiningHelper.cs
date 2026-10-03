using System;
using System.Collections.Generic;
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

            var planetMinerals = mineralsDB.Minerals;
            var mineRates = new Dictionary<int, long>();

            // A mine design lists every mineral it can extract. A body only has some of them.
            foreach (var (key, baseRate) in miningDB.BaseMiningRate)
            {
                if (!planetMinerals.TryGetValue(key, out var deposit))
                    continue;
                double actualRate = baseRate * miningBonuses * deposit.Accessibility;
                mineRates[key] = Convert.ToInt64(actualRate);
            }

            return mineRates;
        }
    }
}

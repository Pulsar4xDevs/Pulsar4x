using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public static class MiningHelper
    {
        public static Dictionary<Guid, long> CalculateActualMiningRates(Entity colonyEntity)
        {
            Dictionary<Guid, long> mineRates = colonyEntity.GetDataBlob<MiningDB>().BaseMiningRate.ToDictionary(k => k.Key, v => v.Value);
            Dictionary<Guid, MineralDeposit> planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;
            float miningBonuses = colonyEntity.HasDataBlob<ColonyBonusesDB>() ? colonyEntity.GetDataBlob<ColonyBonusesDB>().GetBonus(AbilityType.Mine) : 1.0f;

            foreach (var key in mineRates.Keys.ToArray())
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

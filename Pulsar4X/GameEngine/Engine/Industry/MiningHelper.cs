using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Industry
{
    public static class MiningHelper
    {
        public static Dictionary<string, long> CalculateActualMiningRates(Entity colonyEntity)
        {
            var mineRates = colonyEntity.GetDataBlob<MiningDB>().BaseMiningRate.ToDictionary(k => k.Key, v => v.Value);
            var planetMinerals = colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals;
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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Industry
{
    public class MineralDeposit
    {
        [JsonProperty]
        public Masked<long> Amount { get; internal set; }
        [JsonProperty]
        public long HalfOriginalAmount { get; internal set; }
        [JsonProperty]
        public double Accessibility { get; internal set; }
    }

    public class MineralsDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public Dictionary<int, MineralDeposit> Minerals { get; internal set; }

        public MineralsDB()
        {
            Minerals = new();
        }

        public MineralsDB(MineralsDB other)
        {
            Minerals = new Dictionary<int, MineralDeposit>(other.Minerals);
        }

        /// <summary>
        /// Grants full access to all mineral deposit data for the specified faction.
        /// Call this when a faction establishes a colony.
        /// </summary>
        /// <param name="factionMask">The faction's bit mask from FactionInfoDB.FactionMask.</param>
        public void GrantFactionAccess(int factionMask)
        {
            foreach (var deposit in Minerals.Values)
            {
                var amount = deposit.Amount;
                amount.GrantFull(factionMask);
                deposit.Amount = amount;
            }
        }

        /// <summary>
        /// Grants partial access to all mineral deposit data for the specified faction.
        /// Call this when a faction completes a geo survey.
        /// </summary>
        /// <param name="factionMask">The faction's bit mask from FactionInfoDB.FactionMask.</param>
        public void GrantFactionPartialAccess(int factionMask)
        {
            foreach (var deposit in Minerals.Values)
            {
                var amount = deposit.Amount;
                amount.GrantPartial(factionMask);
                deposit.Amount = amount;
            }
        }

        public override object Clone()
        {
            return new MineralsDB(this);
        }

        public new static List<Type> GetDependencies() => new List<Type>();
    }
}
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public struct MineralDeposit
    {
        [JsonProperty]
        public long Amount { get; internal set; }
        [JsonProperty]
        public long HalfOriginalAmount { get; internal set; }
        [JsonProperty]
        public double Accessibility { get; internal set; }
    }

    public class MineralsDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public Dictionary<Guid, MineralDeposit> Minerals { get; internal set; }

        public MineralsDB()
        {
            Minerals = new();
        }

        public MineralsDB(MineralsDB other)
        {
            Minerals = new Dictionary<Guid, MineralDeposit>(other.Minerals);
        }

        public override object Clone()
        {
            return new MineralsDB(this);
        }
    }
}
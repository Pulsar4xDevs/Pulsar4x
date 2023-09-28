using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    public class MineralDeposit
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
        public Dictionary<int, MineralDeposit> Minerals { get; internal set; }

        public MineralsDB()
        {
            Minerals = new();
        }

        public MineralsDB(MineralsDB other)
        {
            Minerals = new Dictionary<int, MineralDeposit>(other.Minerals);
        }

        public override object Clone()
        {
            return new MineralsDB(this);
        }
    }
}
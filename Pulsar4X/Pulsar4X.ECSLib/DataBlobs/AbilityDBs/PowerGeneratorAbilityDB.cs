using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class PowerGeneratorAbilityDB : BaseDataBlob
    {
        /// <summary>
        /// PowerOutput per second
        /// </summary>
        [JsonProperty]
        public float PowerOutput { get; internal set; }

        public override object Clone()
        {
            return new PowerGeneratorAbilityDB {PowerOutput = PowerOutput};
        }
    }
}

using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    class ExplosionChanceAbilityDB : BaseDataBlob
    {
        /// <summary>
        /// Chance this component will causs a secondary explosion when hit.
        /// </summary>
        [JsonProperty]
        public float ExplosionChance { get; internal set; }

        [JsonProperty]
        public float ExplosionDamage { get; internal set; }

        public override object Clone()
        {
            return new ExplosionChanceAbilityDB {ExplosionChance = ExplosionChance, OwningEntity = OwningEntity};
        }
    }
}

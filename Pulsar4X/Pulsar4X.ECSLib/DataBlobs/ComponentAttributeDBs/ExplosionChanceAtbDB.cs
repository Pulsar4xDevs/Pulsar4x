using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ExplosionChanceAtbDB : BaseDataBlob
    {
        /// <summary>
        /// Chance this component will cause a secondary explosion when hit.
        /// </summary>
        [JsonProperty]
        public float ExplosionChance { get; internal set; }

        [JsonProperty]
        public float ExplosionDamage { get; internal set; }
        
        public ExplosionChanceAtbDB(double explosionChance, double explosionDamage) : this((float)explosionChance, (float) explosionDamage) { }

        [JsonConstructor]
        public ExplosionChanceAtbDB(float explosionChance = 0, float explosionDamage = 0)
        {
            ExplosionChance = explosionChance;
            ExplosionDamage = explosionDamage;
        }

        public override object Clone()
        {
            return new ExplosionChanceAtbDB(ExplosionChance, ExplosionDamage);
        }
    }
}

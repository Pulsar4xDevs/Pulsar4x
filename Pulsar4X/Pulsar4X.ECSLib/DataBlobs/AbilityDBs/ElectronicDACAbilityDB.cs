using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ElectronicDACAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int ElectronicDamageChance { get; internal set; }

        public ElectronicDACAbilityDB() { }

        public ElectronicDACAbilityDB(double electronicDamageChance) : this((int)electronicDamageChance) { }

        public ElectronicDACAbilityDB(int electronicDamageChance)
        {
            ElectronicDamageChance = electronicDamageChance;
        }

        public override object Clone()
        {
            return new ElectronicDACAbilityDB(ElectronicDamageChance);
        }
    }
}

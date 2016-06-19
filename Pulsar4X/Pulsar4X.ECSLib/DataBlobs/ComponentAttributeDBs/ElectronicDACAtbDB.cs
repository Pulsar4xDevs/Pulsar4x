using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ElectronicDACAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int ElectronicDamageChance { get; internal set; }

        public ElectronicDACAtbDB() { }

        public ElectronicDACAtbDB(double electronicDamageChance) : this((int)electronicDamageChance) { }

        public ElectronicDACAtbDB(int electronicDamageChance)
        {
            ElectronicDamageChance = electronicDamageChance;
        }

        public override object Clone()
        {
            return new ElectronicDACAtbDB(ElectronicDamageChance);
        }
    }
}

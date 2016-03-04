using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class DamageControlAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int RepairChance { get; set; }

        public DamageControlAbilityDB() { }

        public DamageControlAbilityDB(double repairChance) : this((int)repairChance) { }

        public DamageControlAbilityDB(int repairChance)
        {
            RepairChance = repairChance;
        }

        public override object Clone()
        {
            return new DamageControlAbilityDB(RepairChance);
        }
    }
}

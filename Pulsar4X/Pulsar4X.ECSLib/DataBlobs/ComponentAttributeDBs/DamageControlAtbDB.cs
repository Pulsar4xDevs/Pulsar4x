using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class DamageControlAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int RepairChance { get; set; }

        public DamageControlAtbDB() { }

        public DamageControlAtbDB(double repairChance) : this((int)repairChance) { }

        public DamageControlAtbDB(int repairChance)
        {
            RepairChance = repairChance;
        }

        public override object Clone()
        {
            return new DamageControlAtbDB(RepairChance);
        }
    }
}

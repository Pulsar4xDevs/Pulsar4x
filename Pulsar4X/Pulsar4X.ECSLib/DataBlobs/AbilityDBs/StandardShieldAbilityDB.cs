using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class StandardShieldAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int ShieldHP { get; internal set; }

        [JsonProperty]
        public int ShieldRechargeRate { get; internal set; }

        public StandardShieldAbilityDB() { }

        public StandardShieldAbilityDB(double shieldHP, double shieldRechargeRate) : this((int)shieldHP, (int)shieldRechargeRate) { }

        public StandardShieldAbilityDB(int shieldHP, int shieldRechargeRate)
        {
            ShieldHP = shieldHP;
            ShieldRechargeRate = shieldRechargeRate;
        }

        public override object Clone()
        {
            return new StandardShieldAbilityDB(ShieldHP, ShieldRechargeRate);
        }
    }
}

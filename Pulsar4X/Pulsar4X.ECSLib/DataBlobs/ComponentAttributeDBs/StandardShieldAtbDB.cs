using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class StandardShieldAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int ShieldHP { get; internal set; }

        [JsonProperty]
        public int ShieldRechargeRate { get; internal set; }

        public StandardShieldAtbDB() { }

        public StandardShieldAtbDB(double shieldHP, double shieldRechargeRate) : this((int)shieldHP, (int)shieldRechargeRate) { }

        public StandardShieldAtbDB(int shieldHP, int shieldRechargeRate)
        {
            ShieldHP = shieldHP;
            ShieldRechargeRate = shieldRechargeRate;
        }

        public override object Clone()
        {
            return new StandardShieldAtbDB(ShieldHP, ShieldRechargeRate);
        }
    }
}

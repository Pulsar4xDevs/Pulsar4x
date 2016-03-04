using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EnginePowerAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int EnginePower { get; internal set; }

        public EnginePowerAbilityDB()
        {
        }

        public EnginePowerAbilityDB(double power)
        {
            EnginePower = (int)power;
        }

        public EnginePowerAbilityDB(int enginePower)
        {
            EnginePower = enginePower;
        }

        public EnginePowerAbilityDB(EnginePowerAbilityDB abilityDB)
        {
            EnginePower = abilityDB.EnginePower;
        }

        public override object Clone()
        {
            return new EnginePowerAbilityDB(this);
        }
    }
}
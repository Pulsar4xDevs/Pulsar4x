using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EnginePowerAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int EnginePower { get; internal set; }

        public EnginePowerAtbDB()
        {
        }

        public EnginePowerAtbDB(double power)
        {
            EnginePower = (int)power;
        }

        public EnginePowerAtbDB(int enginePower)
        {
            EnginePower = enginePower;
        }

        public EnginePowerAtbDB(EnginePowerAtbDB abilityDB)
        {
            EnginePower = abilityDB.EnginePower;
        }

        public override object Clone()
        {
            return new EnginePowerAtbDB(this);
        }
    }
}
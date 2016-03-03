using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class EnginePowerDB : BaseDataBlob
    {
        [JsonProperty]
        public int EnginePower { get; internal set; }
        
        /// <summary>
        /// EngineThermalSig = EnginePower * ThermalReductionMultiplier
        /// </summary>
        [JsonProperty]
        public float ThermalReductionMultiplier { get; internal set; }

        public EnginePowerDB()
        {
        }

        public EnginePowerDB(double power)
        {
            EnginePower = (int)power;
        }

        public EnginePowerDB(int enginePower)
        {
            EnginePower = enginePower;
        }

        public EnginePowerDB(EnginePowerDB db)
        {
            EnginePower = db.EnginePower;
        }

        public override object Clone()
        {
            return new EnginePowerDB(this);
        }
    }
}
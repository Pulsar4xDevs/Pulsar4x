using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelConsumptionAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public double FuelUsePerHour { get; internal set; }

        [JsonProperty]
        public float CurrentFlowPercent { get; internal set; }

        public void SetData(double data)
        {
            FuelUsePerHour = data;
        }

        public FuelConsumptionAbilityDB()
        {
        }

        public FuelConsumptionAbilityDB(double fuelUsagePerHour)
        {
            FuelUsePerHour = fuelUsagePerHour;
        }

        public FuelConsumptionAbilityDB(FuelConsumptionAbilityDB db)
        {
            FuelUsePerHour = db.FuelUsePerHour;
        }

        public override object Clone()
        {
            return new FuelConsumptionAbilityDB(this);
        }
    }
}
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelConsumptionAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public double FuelUsePerHour { get; internal set; }

        [JsonProperty]
        public float CurrentFlowPercent { get; internal set; }

        public void SetData(double data)
        {
            FuelUsePerHour = data;
        }

        public FuelConsumptionAtbDB()
        {
        }

        public FuelConsumptionAtbDB(double fuelUsagePerHour)
        {
            FuelUsePerHour = fuelUsagePerHour;
        }

        public FuelConsumptionAtbDB(FuelConsumptionAtbDB db)
        {
            FuelUsePerHour = db.FuelUsePerHour;
        }

        public override object Clone()
        {
            return new FuelConsumptionAtbDB(this);
        }
    }
}
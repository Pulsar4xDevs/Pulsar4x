using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelUseDB : BaseDataBlob
    {
        [JsonProperty]
        private double _fuelUsePerHour;

        public double FuelUsePerHour {get { return _fuelUsePerHour; } internal set { _fuelUsePerHour = value; }}

        public FuelUseDB(double fuelUsagePerHour)
        {
            _fuelUsePerHour = fuelUsagePerHour;
        }

        public FuelUseDB(FuelUseDB db)
        {
            _fuelUsePerHour = db.FuelUsePerHour;
        }

        public override object Clone()
        {
            return new FuelUseDB(this);
        }
    }
}
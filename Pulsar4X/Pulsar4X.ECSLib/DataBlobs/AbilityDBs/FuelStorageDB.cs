using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelStorageDB : BaseDataBlob
    {

        [JsonProperty]
        private int _fuelStorage;

        public int FuelStorage { get { return _fuelStorage; } internal set { _fuelStorage = value; } }


        public FuelStorageDB(int fuelStorage)
        {
            _fuelStorage = fuelStorage;
        }

        public FuelStorageDB(FuelStorageDB db)
        {
            _fuelStorage = db.FuelStorage;
        }

        public override object Clone()
        {
            return new FuelStorageDB(this);
        }
    }
}
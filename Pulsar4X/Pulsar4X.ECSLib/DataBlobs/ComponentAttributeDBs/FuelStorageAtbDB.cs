using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelStorageAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public FuelStorageAtbDB()
        {
        }

        public FuelStorageAtbDB(double fuelStorage)
        {
            StorageCapacity = (int)fuelStorage;
        }

        public FuelStorageAtbDB(FuelStorageAtbDB abilityDB)
        {
            StorageCapacity = abilityDB.StorageCapacity;
        }

        public override object Clone()
        {
            return new FuelStorageAtbDB(this);
        }
    }
}
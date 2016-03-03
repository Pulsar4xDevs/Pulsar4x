using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FuelStorageAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public FuelStorageAbilityDB()
        {
        }

        public FuelStorageAbilityDB(double fuelStorage)
        {
            StorageCapacity = (int)fuelStorage;
        }

        public FuelStorageAbilityDB(FuelStorageAbilityDB abilityDB)
        {
            StorageCapacity = abilityDB.StorageCapacity;
        }

        public override object Clone()
        {
            return new FuelStorageAbilityDB(this);
        }
    }
}
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageAbilityDB : BaseDataBlob
    {
        /// <summary>
        /// Storage Capacity of this module.
        /// </summary>
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public CargoStorageAbilityDB(double storageCapacity) : this((int)storageCapacity) { }

        [JsonConstructor]
        public CargoStorageAbilityDB(int storageCapacity = 0)
        {
            StorageCapacity = storageCapacity;
        }

        public override object Clone()
        {
            return new CargoStorageAbilityDB(StorageCapacity);
        }
    }
}

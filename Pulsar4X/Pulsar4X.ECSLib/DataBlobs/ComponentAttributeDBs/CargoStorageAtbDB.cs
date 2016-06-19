using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoStorageAtbDB : BaseDataBlob
    {
        /// <summary>
        /// Storage Capacity of this module.
        /// </summary>
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public CargoStorageAtbDB(double storageCapacity) : this((int)storageCapacity) { }

        [JsonConstructor]
        public CargoStorageAtbDB(int storageCapacity = 0)
        {
            StorageCapacity = storageCapacity;
        }

        public override object Clone()
        {
            return new CargoStorageAtbDB(StorageCapacity);
        }
    }
}

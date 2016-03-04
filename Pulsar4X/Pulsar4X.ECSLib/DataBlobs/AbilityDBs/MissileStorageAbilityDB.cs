using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MissileStorageAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public override object Clone()
        {
            return new MissileStorageAbilityDB {StorageCapacity = StorageCapacity};
        }
    }
}

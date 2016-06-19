using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MissileStorageAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int StorageCapacity { get; internal set; }

        public override object Clone()
        {
            return new MissileStorageAtbDB { StorageCapacity = StorageCapacity};
        }
    }
}

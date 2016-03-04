using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MSPCapacityAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int MSPCapacity { get; internal set; }

        public MSPCapacityAbilityDB(double mspCapacity) : this((int)mspCapacity) { }

        [JsonConstructor]
        public MSPCapacityAbilityDB(int mspCapacity = 0)
        {
            MSPCapacity = mspCapacity;
        }

        public override object Clone()
        {
            return new MSPCapacityAbilityDB(MSPCapacity);
        }
    }
}

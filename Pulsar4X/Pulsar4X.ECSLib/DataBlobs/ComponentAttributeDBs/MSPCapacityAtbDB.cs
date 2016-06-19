using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MSPCapacityAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int MSPCapacity { get; internal set; }

        public MSPCapacityAtbDB(double mspCapacity) : this((int)mspCapacity) { }

        [JsonConstructor]
        public MSPCapacityAtbDB(int mspCapacity = 0)
        {
            MSPCapacity = mspCapacity;
        }

        public override object Clone()
        {
            return new MSPCapacityAtbDB(MSPCapacity);
        }
    }
}

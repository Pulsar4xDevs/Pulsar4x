using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class LifeSupportAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int LifeSupportCapacity { get; internal set; }

        public LifeSupportAtbDB() { }

        public LifeSupportAtbDB(double lifeSupportCapacity) : this((int)lifeSupportCapacity) { }

        public LifeSupportAtbDB(int lifeSupportCapacity)
        {
            LifeSupportCapacity = lifeSupportCapacity;
        }
        
        public override object Clone()
        {
               return new LifeSupportAtbDB(LifeSupportCapacity);
        }
    }
}

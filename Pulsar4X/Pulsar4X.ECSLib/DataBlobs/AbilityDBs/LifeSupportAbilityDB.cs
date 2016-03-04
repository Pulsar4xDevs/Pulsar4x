using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class LifeSupportAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int LifeSupportCapacity { get; internal set; }

        public LifeSupportAbilityDB() { }

        public LifeSupportAbilityDB(double lifeSupportCapacity) : this((int)lifeSupportCapacity) { }

        public LifeSupportAbilityDB(int lifeSupportCapacity)
        {
            LifeSupportCapacity = lifeSupportCapacity;
        }
        
        public override object Clone()
        {
               return new LifeSupportAbilityDB(LifeSupportCapacity);
        }
    }
}

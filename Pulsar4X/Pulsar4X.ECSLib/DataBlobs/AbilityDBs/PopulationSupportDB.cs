using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    //
    //@summary Provides the ability to support a number of colonists in a population.  Dependent on the colony cost.
    //
    public class PopulationSupportAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        // Population capacity at 1.0 colony cost
        // Infrastructure = 10000
        public int PopulationCapacity { get; internal set; }  

        public PopulationSupportAbilityDB() { }

        public PopulationSupportAbilityDB(double popSupportCapacity) : this((int)popSupportCapacity) { }

        public PopulationSupportAbilityDB(int popSupportCapacity)
        {
            PopulationCapacity = popSupportCapacity;
        }

        public override object Clone()
        {
            return new PopulationSupportAbilityDB(PopulationCapacity);
        }
    }
}

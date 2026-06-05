using Newtonsoft.Json;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Galaxy
{
    /// <summary>
    /// Provides the ability to support a number of colonists in a population.
    /// Dependent on the colony cost.
    /// Colony cost is infrastructure cost to house 10,000 population
    /// </summary>
    public class PopulationSupportAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        [JsonProperty]
        // Population capacity at 1.0 colony cost
        // Infrastructure = 10000
        public int PopulationCapacity { get; internal set; }

        public PopulationSupportAtbDB() { }

        public PopulationSupportAtbDB(double popSupportCapacity) : this((int)popSupportCapacity) { }

        public PopulationSupportAtbDB(int popSupportCapacity)
        {
            PopulationCapacity = popSupportCapacity;
        }

        public override object Clone()
        {
            return new PopulationSupportAtbDB(PopulationCapacity);
        }

        public void OnComponentInstallation(Entity ship, ComponentInstance component)
        {
            // Population support is summed on demand by ComponentInstancesDBExtensions
            // .GetPopulationSupportValue (tolerance-gated), not accumulated here.
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

        }

        public string AtbName()
        {
            return "Population Support";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }
}

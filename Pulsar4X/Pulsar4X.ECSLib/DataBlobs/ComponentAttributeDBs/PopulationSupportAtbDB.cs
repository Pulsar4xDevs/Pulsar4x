using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
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

        public void OnComponentInstantiation(Entity component)
        {
            throw new System.NotImplementedException();
        }

        public void OnComponentInstallation(Entity ship, ComponentInstance component)
        {
            throw new System.NotImplementedException();
        }

        public void OnComponentDeInstalation(Entity ship, Entity component)
        {
            throw new System.NotImplementedException();
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

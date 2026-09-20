using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Industry
{
    /// <summary>
    /// Tracks a colony's infrastructure capacity. Infrastructure represents the
    /// underlying support a body provides for everything built on it; it is the
    /// limiting factor on a colony's industrial output.
    ///
    /// <see cref="CapacityProvided"/> is summed from infrastructure installations,
    /// <see cref="CapacityRequired"/> is summed from every other installation. When
    /// demand exceeds supply, <see cref="Efficiency"/> drops below 1.0 and all
    /// building output on the colony is scaled down by that factor.
    /// </summary>
    public class InfrastructureDB : BaseDataBlob
    {
        [JsonProperty]
        public long CapacityProvided { get; internal set; }

        [JsonProperty]
        public long CapacityRequired { get; internal set; }

        /// <summary>Provided minus required; negative means the colony is over capacity.</summary>
        public long CapacityAvailable => CapacityProvided - CapacityRequired;

        /// <summary>
        /// Output multiplier in (0, 1]. 1.0 while infrastructure meets or exceeds
        /// demand; otherwise the ratio of what's provided to what's required.
        /// </summary>
        public double Efficiency
        {
            get
            {
                if (CapacityRequired <= 0 || CapacityProvided >= CapacityRequired)
                    return 1.0;
                return (double)CapacityProvided / CapacityRequired;
            }
        }

        public InfrastructureDB() { }

        public InfrastructureDB(InfrastructureDB db)
        {
            CapacityProvided = db.CapacityProvided;
            CapacityRequired = db.CapacityRequired;
        }

        public override object Clone()
        {
            return new InfrastructureDB(this);
        }
    }
}

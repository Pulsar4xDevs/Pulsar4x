using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{

    [StaticDataAttribute(true, IDPropertyName = "ID")]
    public struct InstallationSD
    {
        public string Name;
        public string Description;
        public Guid ID;

        /// <summary>
        /// the amount of pop required for this installation to operate.
        /// </summary>
        public int PopulationRequired;
        public int CargoSize;
        public Dictionary<AbilityType, int> BaseAbilityAmounts;
        
        /// <summary>
        /// research ID Requirements to build.
        /// </summary>
        public List<Guid> TechRequirements;

        /// <summary>
        /// The resources required to build this facility
        /// </summary>
        public Dictionary<Guid,int> ResourceCosts;
        /// <summary>
        /// pricetag
        /// </summary>
        public int WealthCost;

        /// <summary>
        /// related to how long it will take to build this facility, I think aurora uses mass for this, but hey why not be more flexable.
        /// </summary>
        public int BuildPoints;
    }
}

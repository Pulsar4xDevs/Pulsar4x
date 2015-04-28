using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public enum InstallationAbilityType
    {
        ShipMaintenance,
        InstallationConstruction,
        OrdnanceConstruction,
        FighterConstruction,
        FuelRefinery,
        Mine,
        AtmosphericModification,
        Research,
        Commercial, //ie aurora "Finance Center" 
        MassDriver,

    }

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
        public JDictionary<InstallationAbilityType, int> BaseAbilityAmounts;
        
        /// <summary>
        /// research ID Requirements to build.
        /// </summary>
        public List<Guid> Requirements;

        /// <summary>
        /// The resources requred to build this facility
        /// </summary>
        public JDictionary<Guid,int> ResourceCosts;
        /// <summary>
        /// pricetag
        /// </summary>
        public int WealthCost;

        /// <summary>
        /// related to how long it will take to build this faclity, I think aurora uses mass for this, but hey why not be more flexable.
        /// </summary>
        public int BuildPoints;
    }
}

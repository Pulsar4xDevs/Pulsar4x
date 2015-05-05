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
        Refinery,
        Mine,
        AtmosphericModification,
        Research,
        Commercial, //ie aurora "Finance Center" 
        Industrial, //intend to use this later on for civ economy and creating random tradegoods.
        Agrucultural, //as above.
        MassDriver,
        SpacePort, //loading/unloading speed;
        GeneratesNavalOfficers,
        GeneratesGroundOfficers,
        GeneratesShipCrew,
        GeneratesTroops, //not sure how we're going to do this yet.aurora kind of did toops and crew different.
        GeneratesScientists,
        GeneratesCivilianLeaders,
        DetectionThermal, //radar
        DetectionEM,    //radar
        Teraforming,
        BasicLiving //ie Auroras infrustructure will have this ability. 
    }

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
        public JDictionary<InstallationAbilityType, int> BaseAbilityAmounts;
        
        /// <summary>
        /// research ID Requirements to build.
        /// </summary>
        public List<Guid> TechRequirements;

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

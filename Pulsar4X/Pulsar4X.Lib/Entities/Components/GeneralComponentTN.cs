using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{


    /// <summary>
    /// GeneralComponentDefTN covers the definitions for the basic components that all ships must have.
    /// </summary>
    public class GeneralComponentDefTN : ComponentDefTN
    {
        /// <summary>
        /// Initializes this basic component.
        /// </summary>
        /// <param name="Title">Name which will be displayed to the user, they don't choose the names of these particular components however.</param>
        /// <param name="ComponentSize">Size of the component. this will determine crew capacity, fuel capacity, engineering percentage, and htk in addition to being merely size.</param>
        /// <param name="ComponentCrew">Crew requirement of the component, fuel tanks won't require any, orbital habitats will require a lot.</param>
        /// <param name="ComponentCost">Cost requirement of the component, mineral costs have yet to be done though those are just a percentage of total cost.</param>
        /// <param name="GeneralComponentType">What type of component is this? see the enum in ComponentTN.cs for a list.</param>
        public GeneralComponentDefTN(string Title, float ComponentSize, byte ComponentCrew, decimal ComponentCost, ComponentTypeTN GeneralComponentType)
        {
            Id = Guid.NewGuid();

            Name = Title;
            size = ComponentSize;
            crew = ComponentCrew;
            cost = ComponentCost;
            componentType = GeneralComponentType;

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }

            switch (componentType)
            {
                case ComponentTypeTN.Crew: //25% Duranium 75% Mercassium
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.25m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = cost * 0.75m;
                    break;
                case ComponentTypeTN.Fuel: //50% Duranium 50% Boronide
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.50m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = cost * 0.50m;
                    break;
                case ComponentTypeTN.Engineering: //100% Duranium
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost;
                    break;
                case ComponentTypeTN.Bridge:      //50% Duranium, 50% corbomite
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.50m;
                    minerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = cost * 0.50m;
                    break;
                //case ComponentTypeTN.FlagBridge: 50% corbomite, 50% Uridium
                //case ComponentTypeTN.MaintenanceBay: 50% Duranium, 50% Neutronium
                //case ComponentTypeTN.OrbitalHabitat: 25% Duranium, 25% Boronide, 50% Mercassium
                //case ComponentTypeTN.RecFacility: 20% Duranium, 20% Tritanium, 60% Boronide
                //case ComponentTypeTN.DamageControl: 50% Duranium, 25% Neutronium, 25% Uridium
            }

            isDivisible = false;

            if (componentType <= ComponentTypeTN.MaintenanceBay)
            {
                if (size < 1.0f)
                    htk = 0;
                else
                    htk = 1;
            }
            else if (componentType == ComponentTypeTN.FlagBridge || componentType == ComponentTypeTN.DamageControl)
            {
                htk = 2;
            }
            else if (componentType == ComponentTypeTN.OrbitalHabitat || componentType == ComponentTypeTN.RecFacility)
            {
                htk = 25;
                isDivisible = true;
            }

            isMilitary = false;
            isObsolete = false;
            isSalvaged = false;
            isElectronic = false;
        }
    }

    /// <summary>
    /// GeneralComponentTN covers the basic components that all ships must have.
    /// </summary>
    public class GeneralComponentTN : ComponentTN
    {
        /// <summary>
        /// Definition for this component.
        /// </summary>
        private GeneralComponentDefTN GenCompDef;
        public GeneralComponentDefTN genCompDef
        {
            get { return GenCompDef; }
        }

        /// <summary>
        /// This is the component itself, which tracks whether or not it is destroyed, and in the case of fuel/engineering whether the component has a cargo load.
        /// </summary>
        /// <param name="definition"></param>
        public GeneralComponentTN(GeneralComponentDefTN definition)
        {
            GenCompDef = definition;

            Name = definition.Name;

            /// <summary>
            /// In the case of destroyed fuel/engineering bays I will assume that all supplies are evenly distributed by percentage and merely subtract an average.
            /// </summary>
            isDestroyed = false;
        }
    }
}

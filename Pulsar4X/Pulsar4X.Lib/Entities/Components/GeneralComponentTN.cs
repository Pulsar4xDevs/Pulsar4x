using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    /// <summary>
    /// several basic components don't really vary around all that much.
    /// </summary>
    public enum GeneralType
    {
        Crew,
        Fuel,
        Engineering,
        Bridge,
        MaintenanceBay,
        FlagBridge,
        DamageControl,
        OrbitalHabitat,
        RecFacility,
        TypeCount
    }

    /// <summary>
    /// GeneralComponentDefTN covers the definitions for the basic components that all ships must have.
    /// </summary>
    public class GeneralComponentDefTN : ComponentDefTN
    {
        /// <summary>
        /// What type of component is this, the above enum defines what kind it can be.
        /// </summary>
        private GeneralType ComponentType;
        public GeneralType componentType
        {
            get { return ComponentType; }
        }

        /// <summary>
        /// Initializes this basic component.
        /// </summary>
        /// <param name="Title">Name which will be displayed to the user, they don't choose the names of these particular components however.</param>
        /// <param name="ComponentSize">Size of the component. this will determine crew capacity, fuel capacity, engineering percentage, and htk in addition to being merely size.</param>
        /// <param name="ComponentCrew">Crew requirement of the component, fuel tanks won't require any, orbital habitats will require a lot.</param>
        /// <param name="ComponentCost">Cost requirement of the component, mineral costs have yet to be done though those are just a percentage of total cost.</param>
        /// <param name="GeneralComponentType">What type of component is this? see the enum in GeneralComponentTN.cs for a list.</param>
        public GeneralComponentDefTN(string Title, float ComponentSize, byte ComponentCrew, decimal ComponentCost, GeneralType GeneralComponentType)
        {
            name = Title;
            size = ComponentSize;
            crew = ComponentCrew;
            cost = ComponentCost;
            ComponentType = GeneralComponentType;

            if (ComponentType <= GeneralType.MaintenanceBay)
            {
                if (size < 1.0f)
                    htk = 0;
                else
                    htk = 1;
            }
            else if (ComponentType == GeneralType.FlagBridge || ComponentType == GeneralType.DamageControl)
            {
                htk = 2;
            }
            else if (ComponentType == GeneralType.OrbitalHabitat || ComponentType == GeneralType.RecFacility)
            {
                htk = 25;
            }

            isMilitary = false;
            isObsolete = false;
            isSalvaged = false;
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

            /// <summary>
            /// In the case of destroyed fuel/engineering bays I will assume that all supplies are evenly distributed by percentage and merely subtract an average.
            /// </summary>
            isDestroyed = false;
        }
    }
}

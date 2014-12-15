using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class CargoHandlingDefTN : ComponentDefTN
    {
        /// <summary>
        /// Tractor multiplier addition of this component. this increases loading and unloading speed of cargo/cryo/troop/drop transports.
        /// </summary>
        private int TractorMultiplier { get; set; }
        public int tractorMultiplier
        {
            get { return TractorMultiplier; }
        }

        /// <summary>
        /// Creates the definition for the cargo handling system in question.
        /// </summary>
        /// <param name="Title">Title of the CHS.</param>
        /// <param name="TractorMult">Tractor multiplier that the CHS possesses.</param>
        public CargoHandlingDefTN(string Title, int TractorMult, decimal ComponentCost)
        {
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.CargoHandlingSystem;

            Name = Title;
            TractorMultiplier = TractorMult;

            size = 2.0f;
            cost = ComponentCost;
            crew = 10;

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost * 0.25m;
            minerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = cost * 0.75m;


            htk = 1;

            isSalvaged = false;
            isObsolete = false;
            isMilitary = false;
            isDivisible = false;
            isElectronic = false;
        }
    }

    public class CargoHandlingTN : ComponentTN
    {
        /// <summary>
        /// As always, the pointer to the definition for cargo handling.
        /// </summary>
        private CargoHandlingDefTN CargoHandleDef { get; set; }
        public CargoHandlingDefTN cargoHandleDef
        {
            get { return CargoHandleDef; }
        }


        /// <summary>
        /// Creates the cargo handler component, and points it towards its creator definition.
        /// </summary>
        /// <param name="definition">definition of component.</param>
        public CargoHandlingTN(CargoHandlingDefTN definition)
        {
            CargoHandleDef = definition;

            Name = definition.Name;

            isDestroyed = false;
        }
    }
}

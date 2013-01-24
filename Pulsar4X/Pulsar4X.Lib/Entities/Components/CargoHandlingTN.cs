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
        public CargoHandlingDefTN(string Title, int TractorMult)
        {
            componentType = ComponentTypeTN.CargoHandlingSystem;

            name = Title;
            TractorMultiplier = TractorMult;

            size = 2.0f;
            cost = 10.0m;
            crew = 10;
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
            isDestroyed = false;
        }
    }
}

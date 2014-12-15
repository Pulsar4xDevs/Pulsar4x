using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities.Components
{
    public class CargoListEntryTN
    {
        /// <summary>
        /// Type of installation being carried.
        /// </summary>
        private Installation.InstallationType CargoType;
        public Installation.InstallationType cargoType
        {
            get { return CargoType; }
        }

        /// <summary>
        /// Type of component, chiefly this contains component size.
        /// </summary>
        private ComponentDefTN CargoComponentType;
        public ComponentDefTN cargoComponentType
        {
            get { return CargoComponentType; }
        }

        /// <summary>
        /// Size of Installation being carried.
        /// </summary>
        private int Tons;
        public int tons
        {
            get { return Tons; }
            set { Tons = value; }
        }

        /// <summary>
        /// CargoListEntryTN creates an entry into the taskgroup cargo hold list for installations.
        /// </summary>
        /// <param name="Type">Type of installation.</param>
        /// <param name="SizeInTons">tons per installation.</param>
        public CargoListEntryTN(Installation.InstallationType Type, int SizeInTons)
        {
            CargoType = Type;
            Tons = SizeInTons;
        }

        public CargoListEntryTN(ComponentDefTN Type, int SizeInTons)
        {
            CargoComponentType = Type;
            Tons = SizeInTons;
        }
    }

    /// <summary>
    /// Cargo hold definitions:
    /// </summary>
    public class CargoDefTN : ComponentDefTN
    {
        /// <summary>
        /// Cargo Capacity of this component in tons:
        /// </summary>
        private int CargoCapacity;
        public int cargoCapacity
        {
            get { return CargoCapacity; }
        }


        /// <summary>
        /// Constructor for CargoDefTN
        /// </summary>
        /// <param name="Title">Name of component.</param>
        /// <param name="HS">Size in HullSpaces of component.</param>
        /// <param name="ComponentCost">Cost.</param>
        /// <param name="CrewRequirement">Required crew.</param>
        public CargoDefTN(string Title, float HS, decimal ComponentCost, byte CrewRequirement)
        {
            Id = Guid.NewGuid();

            componentType = ComponentTypeTN.CargoHold;

            Name = Title;
            size = HS;
            cost = ComponentCost;
            crew = CrewRequirement;

            minerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
            {
                minerialsCost[mineralIterator] = 0;
            }
            minerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = cost;

            htk = 1;

            CargoCapacity = (int)(size * Constants.ShipTN.TonsPerHS);

            isSalvaged = false;
            isObsolete = false;
            isMilitary = false;
            isDivisible = false;
            isElectronic = false;

        }
    }

    /// <summary>
    /// Class for the cargo hold component itself:
    /// </summary>
    public class CargoTN : ComponentTN
    {
        private CargoDefTN CargoDef;
        public CargoDefTN cargoDef
        {
            get { return CargoDef; }
        }

        /// <summary>
        /// Constructor for the component itself.
        /// </summary>
        /// <param name="definition">Definition of the component.</param>
        public CargoTN(CargoDefTN definition)
        {
            CargoDef = definition;

            Name = definition.Name;

            isDestroyed = false;
        }
    }
}

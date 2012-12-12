using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;




namespace Pulsar4X.Entities
{
    public class ShipTN
    {
        public Guid Id { get; set; }
        public Faction Faction { get; set; }

        public ShipClassTN ShipClass { get; set; }

        public string Name { get; set; }
        public string ClassNotes { get; set; }
        public string Notes { get; set; }


        public int CrossSection { get; set; }
        public int CurrentFuel { get; set; }
        public int FuelCapacity { get; set; }
        public int CurrentCrew { get; set; }
        public int MaxCrew { get; set; }
        public int MaxLifeSupport { get; set; }
        public int DamageControlRating { get; set; }

        /// <summary>
        /// The ship will have an armor layering.
        /// </summary>
        public ArmorTN ShipArmor { get; set; }

        /// <summary>
        /// Ships can potentially have multiple engines, though they must all be of the same type.
        /// </summary>
        public BindingList<EngineTN> ShipEngine { get; set; }

        /// <summary>
        /// Engine related ship statistics. Maximum values are in ship class.
        /// </summary>
        public int CurrentEnginePower { get; set; }
        public int CurrentThermalSignature { get; set; }
        public int CurrentSpeed { get; set; }


        /// <summary>
        /// List of passive sensors that this craft will have.
        /// every ship has a base sensitivity 1 thermal and EM sensor, those won't be in this list however.
        /// Best ratings store the best currently working sensor detection, these are where that default will be.
        /// </summary>
        public BindingList<PassiveSensorTN> ShipPSensor { get; set; }
        public int BestThermalRating { get; set; }
        public int BestEMRating { get; set; }
    }
}

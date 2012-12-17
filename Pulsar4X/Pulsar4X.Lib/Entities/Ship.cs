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

        public int MaxLifeSupport { get; set; } //here is life support again.


        /// <summary>
        /// The ship will have an armor layering.
        /// </summary>
        public ArmorTN ShipArmor { get; set; }

        /// <summary>
        /// Crew Quarters, Small Crew Quarters, Tiny Crew Quarters.
        /// </summary>
        public BindingList<GeneralComponentTN> CrewQuarters { get; set; }

        /// <summary>
        /// How many crew are on the ship. Crew aren't necessarily in their quarters when those are destroyed, though the crew requirement for quarters will be at risk.
        /// </summary>
        public int CurrentCrew;

        /// <summary>
        /// How many crew/POWs are in cryo stasis.
        /// </summary>
        public int CurrentCryo;

        /// <summary>
        /// How long has the ship been out on patrol.
        /// </summary>
        public int CurrentDeployment;

        /// <summary>
        /// Fuel Tanks, Small Fuel Tanks, Tiny Fuel Tanks, Large Fuel Tanks, Very Large Fuel Tanks, Ultra Large Fuel Tanks.
        /// </summary>
        public BindingList<GeneralComponentTN> FuelTanks { get; set; }

        /// <summary>
        /// How much fuel in total does the ship have. it will be divided equally among all tanks for convenience.
        /// </summary>
        public int CurrentFuel { get; set; }

        /// <summary>
        /// Engineering bay, Small Engineering Bay, Tiny Engineering Bay, Fighter Engineering Bay.
        /// </summary>
        public BindingList<GeneralComponentTN> EngineeringBays { get; set; }

        /// <summary>
        /// How much maintenance supply is available to this ship, as with fuel it is divided equally among all bays.
        /// </summary>
        public int CurrentMSP { get; set; }

        /// <summary>
        /// Bridge, Flag Bridge, Damage Control, Improved Damage Control, Advanced Damage Control, Maintenance bay, Recreational Facility, Orbital Habitat.
        /// </summary>
        public BindingList<GeneralComponentTN> OtherGeneralComponents { get; set; }

        /// <summary>
        /// What is this ship's current damage control rating. Ebays and damage control both contribute to this value.
        /// </summary>
        public int CurrentDamageControlRating { get; set; }

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

        /// <summary>
        /// List of the actual active sensors, which store whether or not they are active, and if they are destroyed.
        /// </summary>
        public BindingList<ActiveSensorTN> ShipASensor { get; set; }
        public int TotalCrossSection { get; set; }
        public int CurrentEMSignature { get; set; }

        /// <summary>
        /// These lists will store timestamps for whenever this ship is detected. Example:
        /// Faction 0 detects this craft via thermal on tick 102, so ThermalDetection[0] = 102.
        /// On tick 103, the craft is still detected, so ThermalDetection[0] is updated to 103.
        /// on 104, the ship is no longer detected so no update is made.
        /// What this all means is that on any given tick it is possible to quickly determine whether or not a ship has been detected by a faction.
        /// I am thinking that ticks will be counted in 5 second intervals, there should not be any issue with this for my code.
        /// </summary>
        public BindingList<int> ThermalDetection { get; set; }
        public BindingList<int> EMDetection { get; set; }
        public BindingList<int> ActiveDetection { get; set; }

        /// <summary>
        /// Each ship will store its placement in the overall taskgroup.
        /// </summary>
        public LinkedListNode<int> ThermalList;
        public LinkedListNode<int> EMList;
        public LinkedListNode<int> ActiveList;



        public ShipTN(ShipClassTN ClassDefinition)
        {

        }
    }
}

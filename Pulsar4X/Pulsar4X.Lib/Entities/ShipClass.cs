using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class ShipClassTN
    {
        public Guid Id { get; set; }
        public Faction Faction { get; set; }

        public string ClassName { get; set; }
        public int HullDescriptionId { get; set; }
        public string Notes { get; set; }
        public decimal BuildPointCost { get; set; }

        /// <summary>
        /// In 50 ton increments
        /// </summary>
        public float SizeHS { get; set; }
        public float SizeTons { get; set; }

        public int RequiredRank { get; set; }

        public int MaxLifeSupport { get; set; } //not sure what life support is supposed to be? deployment time?


        /// <summary>
        /// Armor statistics that matter to the class itself.
        /// </summary>
        public ArmorDefTN ShipArmorDef { get; set; }

        /// <summary>
        /// Crew Quarters, Small Crew Quarters, Tiny Crew Quarters.
        /// Every ship has a required crew amount, though for some small sensor only craft it might be 0.
        /// </summary>
        public BindingList<GeneralComponentDefTN> CrewQuarters { get; set; }
        public BindingList<ushort> CrewQuartersCount { get; set; }
        public int TotalCrewQuarters { get; set; }
        public int TotalRequiredCrew { get; set; }
        public int SpareCrewQuarters { get; set; }
        public int SpareCryoBerths { get; set; }
        public int MaxDeploymentTime { get; set; }
        public float TonsPerMan { get; set; }
        public float CapPerHS { get; set; }
        public float AccomHSRequirement { get; set; }
        public float AccomHSAvailable { get; set; }


        /// <summary>
        /// Fuel Tanks, Small Fuel Tanks, Tiny Fuel Tanks, Large Fuel Tanks, Very Large Fuel Tanks, Ultra Large Fuel Tanks.
        /// Should fuel compression be a tank type or an option?
        /// </summary>
        public BindingList<GeneralComponentDefTN> FuelTanks { get; set; }
        public BindingList<ushort> FuelTanksCount { get; set; }
        public int TotalFuelCapacity { get; set; }

        /// <summary>
        /// Engineering bay, Small Engineering Bay, Tiny Engineering Bay, Fighter Engineering Bay.
        /// Ebays give 3 benefits, they increase MSP, they give a minor boost to damage control and they reduce failure rate for military vessels.
        /// </summary>
        public BindingList<GeneralComponentDefTN> EngineeringBays { get; set; }
        public BindingList<ushort> EngineeringBaysCount { get; set; }
        public int TotalMSPCapacity { get; set; }
        public float MaintenanceLife { get; set; }
        public float AnnualFailureRate { get; set; }
        public float InitialFailureRate { get; set; }
        public float YearOneFailureTotal { get; set; }
        public float YearFiveFailureTotal { get; set; }
        public int MaxDamageControlRating { get; set; }

        /// <summary>
        /// Bridge, Flag Bridge, Damage Control, Improved Damage Control, Advanced Damage Control, Maintenance bay, Recreational Facility, Orbital Habitat.
        /// Each gives a fairly specialist benefit that not all ships will need, excepting the bridge of course.
        /// </summary>
        public BindingList<GeneralComponentDefTN> OtherComponents { get; set; }
        public BindingList<ushort> OtherComponentsCount { get; set; }

        /// <summary>
        /// each ship class can only have one type of engine, though several copies may be present.
        /// </summary>
        public EngineDefTN ShipEngineDef { get; set; }
        public ushort ShipEngineCount;

        /// <summary>
        /// Ship class Engine statistics.
        /// </summary>
        public int MaxEnginePower { get; set; }
        public int MaxThermalSignature { get; set; }
        public int MaxSpeed { get; set; }

        /// <summary>
        /// List of passive sensor types, and how many of each that there are in this ship.
        /// Likewise the best possible sensors are stored.
        /// </summary>
        public BindingList<PassiveSensorDefTN> ShipPSensorDef { get; set; }
        public BindingList<ushort> ShipPSensorCount { get; set; }
        public int BestThermalRating { get; set; }
        public int BestEMRating { get; set; }

        /// <summary>
        /// List of active sensors, as well as the number of each, and the TCS and EM signatures of the craft.
        /// </summary>
        public BindingList<ActiveSensorDefTN> ShipASensorDef { get; set; }
        public BindingList<ushort> ShipASensorCount { get; set; }
        public int TotalCrossSection { get; set; }
        public int MaxEMSignature { get; set; }
    }
}

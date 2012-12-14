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
        public decimal Size { get; set; }
        public int RequiredRank { get; set; }
        public int CrewSize { get; set; }
        public int MaxLifeSupport { get; set; }
        public int DamageControlRating { get; set; }
        public int FuelCapacity { get; set; }

        /// <summary>
        /// Armor statistics that matter to the class itself.
        /// </summary>
        public ArmorDefTN ShipArmorDef { get; set; }

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

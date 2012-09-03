using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class ShipClass
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
        public int ThermalSignature { get; set; }
        public int CrossSection { get; set; }
        public int RequiredRank { get; set; }
        public int CrewSize { get; set; }
        public int MaxLifeSupport { get; set; }
        public int DamageControlRating { get; set; }
        public int FuelCapacity { get; set; }
        public int ArmourThickness { get; set; }
        public int ArmorWidth { get; set; }
    }
}

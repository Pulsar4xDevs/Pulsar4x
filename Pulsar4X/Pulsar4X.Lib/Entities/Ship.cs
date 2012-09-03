using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Ship
    {
        public Guid Id { get; set; }
        public Faction Faction { get; set; }

        public ShipClass ShipClass { get; set; }

        public string Name { get; set; }
        public string ClassNotes { get; set; }
        public string Notes { get; set; }

        public int ThermalSignature { get; set; }
        public int CrossSection { get; set; }
        public int CurrentFuel { get; set; }
        public int FuelCapacity { get; set; }
        public int CurrentSpeed { get; set; }
        public int MaxSpeed { get; set; }
        public int CurrentCrew { get; set; }
        public int MaxCrew { get; set; }
        public int MaxLifeSupport { get; set; }
        public int DamageControlRating { get; set; }
        public int ArmourThickness { get; set; }
        public int ArmorWidth { get; set; }
    }
}

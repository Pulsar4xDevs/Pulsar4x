using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class ShipClass
    {
        public Guid Id { get; set; }
        public Guid FactionId { get; set; }
        private Faction _faction;
        public Faction Faction
        {
            get { return _faction; }
            set
            {
                _faction = value;
                if (_faction != null) FactionId = _faction.Id;
            }
        }
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class Population
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
        public string Name { get; set; }

        public Guid PlanetId { get; set; }

        private Planet _planet;
        /// <summary>
        /// Planet the population is attached to
        /// </summary>
        public Planet Planet
        {
            get { return _planet; }
            set
            {
                _planet = value;
                if (_planet != null) PlanetId = _planet.Id;
            }
        }

        public int CivilianPopulation { get; set; }
        public int FuelStockpile { get; set; }

        // TODO: store minerals as individual properties? or define constants for each mineral type and
        // store the values in a dictionary keyed off constants?
        public decimal Duranium { get; set; }
        public decimal Neutronium { get; set; }
        public decimal Corbomite { get; set; }
        public decimal Tritanium { get; set; }
        public decimal Boronide { get; set; }
        public decimal Mercassium { get; set; }
        public decimal Vendarite { get; set; }
        public decimal Sorium { get; set; }
        public decimal Uridium { get; set; }
        public decimal Corundium { get; set; }
        public decimal Gallicite { get; set; }

    }
}

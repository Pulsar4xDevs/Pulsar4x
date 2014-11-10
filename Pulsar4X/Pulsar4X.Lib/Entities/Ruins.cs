using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class Ruins : GameEntity
    {
        /// <summary>
        /// Which factions have uncovered the extent of these ruins
        /// </summary>
        public BindingList<Faction> RuinDiscovery { get; set; }

        /// <summary>
        /// How many ruins are on this world.
        /// </summary>
        public int RuinCount { get; set; }

        /// <summary>
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        public int RuinTechLevel { get; set; }

        public Ruins(int a_oCount, int a_oTL)
        {
#warning Generate a name/race for the ruins, and also how should difficulty be effected by having 2 ruins of the same race, with one already looted?
            RuinDiscovery = new BindingList<Faction>();

            RuinCount = a_oCount;

            RuinTechLevel = a_oTL;
        }
    }
}

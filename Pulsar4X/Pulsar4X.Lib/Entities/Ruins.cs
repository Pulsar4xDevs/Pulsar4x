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
        public enum RSize
        {
            NoRuins,
            Outpost,
            Settlement,
            Colony,
            City,
            Count
        }

        public enum RQuality
        {
            Destroyed,
            Ruined,
            PartiallyIntact,
            Intact,
            MultipleIntact,
            Count
        }
        /// <summary>
        /// Which factions have uncovered the extent of these ruins
        /// </summary>
        public BindingList<Faction> RuinDiscovery { get; set; }

        /// <summary>
        /// How many ruins are on this world.
        /// </summary>
        public uint RuinCount { get; set; }

        /// <summary>
        /// What kinds of things should be found in this ruin? including sophistication of killbots?
        /// </summary>
        public int RuinTechLevel { get; set; }

        /// <summary>
        /// How big are these ruins?
        /// </summary>
        public RSize RuinSize { get; set; }

        /// <summary>
        /// What shape are these ruins in?
        /// </summary>
        public RQuality RuinQuality { get; set; }

        public Ruins()
            : base()
        {
            ///< @todo Generate a name/race for the ruins, and also how should difficulty be effected by having 2 ruins of the same race, with one already looted?
            /// -- Can you have two ruins of the same race on a single body?? i thought it was one planet, 1 runis - SnopyDogy
            RuinTechLevel = GameState.RNG.Next(5);

            RuinSize = RSize.NoRuins;
            RuinQuality = RQuality.Destroyed;
            RuinCount = 0;

            RuinDiscovery = new BindingList<Faction>();

            Name = "Ruins";
        }
    }
}

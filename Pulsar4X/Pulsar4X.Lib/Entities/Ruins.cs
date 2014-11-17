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
        public int RuinCount { get; set; }

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

        public Ruins(bool GenerateRuins=false)
        {
#warning Generate a name/race for the ruins, and also how should difficulty be effected by having 2 ruins of the same race, with one already looted?
            RuinTechLevel = GameState.RNG.Next(5);

            RuinSize = RSize.NoRuins;
            RuinQuality = RQuality.Destroyed;
            RuinCount = 0;

            if (GenerateRuins == true)
            {
                RuinDiscovery = new BindingList<Faction>();

                int size = GameState.RNG.Next(0, 100);

                if (size < 40)
                {
                    RuinSize = RSize.Outpost;
                }
                else if (size < 70)
                {
                    RuinSize = RSize.Settlement;
                }
                else if (size < 90)
                {
                    RuinSize = RSize.Colony;
                }
                else
                {
                    RuinSize = RSize.City;
                }

                int quality = GameState.RNG.Next(0, 100);

                if (quality < 40)
                {
                    RuinQuality = RQuality.Destroyed;
                }
                else if (quality < 70)
                {
                    RuinQuality = RQuality.Ruined;
                }
                else if (quality < 80)
                {
                    RuinQuality = RQuality.PartiallyIntact;
                }
                else if (quality < 90)
                {
                    RuinQuality = RQuality.Intact;

                    if (RuinSize == RSize.City && quality >= 95)
                    {
                        RuinQuality = RQuality.MultipleIntact;
                    }
                }


                int RC = 0;

                /// <summary>
                /// Ruins size range:
                /// </summary>
                switch (RuinSize)
                {
                    case RSize.Outpost:
                        RC = GameState.RNG.Next(15, 50);
                    break;
                    case RSize.Settlement:
                        RC = GameState.RNG.Next(50, 100);
                    break;
                    case RSize.Colony:
                        RC = GameState.RNG.Next(100, 200);
                    break;
                    case RSize.City:
                        RC = GameState.RNG.Next(500, 1000);
                    break;
                }

                /// <summary>
                /// Ruins quality adjustment.
                /// </summary>
                switch (RuinQuality)
                {
                    case RQuality.Destroyed:
                        RC = (int)Math.Round((float)RC * 1.25f);
                    break;
                    case RQuality.Ruined:
                        RC = (int)Math.Round((float)RC * 1.5f);
                    break;
                    case RQuality.PartiallyIntact:
                        RC = (int)Math.Round((float)RC * 1.75f);
                    break;
                    case RQuality.Intact:
                        RC = (int)Math.Round((float)RC * 2.0f);
                    break;
                    case RQuality.MultipleIntact:
                        RC = (int)Math.Round((float)RC * 3.0f);
                    break;
                }

                RuinCount = RC;
            }
        }
    }
}

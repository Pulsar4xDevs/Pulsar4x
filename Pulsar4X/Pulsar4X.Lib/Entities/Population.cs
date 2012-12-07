using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Population : GameEntity
    {
        #region Properties

        public Faction Faction { get; set; }

        /// <summary>
        /// Planet the population is attached to
        /// </summary>
        public Planet Planet { get; set; }

        public int CivilianPopulation { get; set; }
        public int FuelStockpile { get; set; }

        int[] m_aiMinerials;
        public int[] Minerials
        {
            get
            {
                return m_aiMinerials;
            }
            set
            {
                m_aiMinerials = value;
            }
        }

        #endregion

        public Population(Planet a_oPlanet, Faction a_oFaction)
        {
            // initialise minerials:
            m_aiMinerials = new int[Constants.Minerals.NO_OF_MINERIALS];
            for (int i = 0; i < Constants.Minerals.NO_OF_MINERIALS; ++i)
            {
                m_aiMinerials[i] = 0;
            }

            CivilianPopulation = 0;
            FuelStockpile = 0;

            Name = "Earth";  // just a default Value!

            Faction = a_oFaction;
            Planet = a_oPlanet;
            Planet.Populations.Add(this); // add us to the list of pops on the planet!
        }

    }
}

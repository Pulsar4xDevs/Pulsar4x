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

        public float CivilianPopulation { get; set; }
        public float PopulationGrowthRate { get; set; }
        public int FuelStockpile { get; set; }
        public float MaintenanceSupplies { get; set; }


        public float PopulationWorkingInAgriAndEnviro
        {
            get
            {
                // 5% of civi pop
                return CivilianPopulation * 0.05f;
            }
        }

        public float PopulationWorkingInServiceIndustries
        {
            get
            {
                // 75% of Civi Pop
                return CivilianPopulation * 0.75f;
            }
        }

        public float PopulationWorkingInManufacturing
        {
            get
            {
                // 20% of civi pop
                return CivilianPopulation * 0.20f;
            }
        }

        public int EMSignature
        {
            get
            {
                // Todo: Proper Formula for EM Sig!
                return (int)((CivilianPopulation * CivilianPopulation) / 10);
            }
        }


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

        Installation[] m_aoInstallations;
        public Installation[] Installations
        {
            get
            {
                return m_aoInstallations;
            }
            set
            {
                m_aoInstallations = value;
            }
        }



        public float ModifierEconomicProduction { get; set; }
        public float ModifierManfacturing { get; set; }
        public float ModifierProduction { get; set; }
        public float ModifierWealthAndTrade { get; set; }
        public float ModifierPoliticalStability { get; set; }

        #endregion

        public Population(Planet a_oPlanet, Faction a_oFaction)
        {
            // initialise minerials:
            m_aiMinerials = new int[Constants.Minerals.NO_OF_MINERIALS];
            for (int i = 0; i < Constants.Minerals.NO_OF_MINERIALS; ++i)
            {
                m_aiMinerials[i] = 0;
            }

            m_aoInstallations = new Installation[Installation.NO_OF_INSTALLATIONS];
            for (int i = 0; i < Installation.NO_OF_INSTALLATIONS; ++i)
            {
                m_aoInstallations[i] = new Installation((Installation.InstallationType)i);
            }

            CivilianPopulation = 0;
            PopulationGrowthRate = 0.1f;
            FuelStockpile = 0;
            MaintenanceSupplies = 0;
            ModifierEconomicProduction = 1.0f;
            ModifierManfacturing = 1.0f;
            ModifierPoliticalStability = 1.0f;
            ModifierProduction = 1.0f;
            ModifierWealthAndTrade = 1.0f;

            Name = "Earth";  // just a default Value!

            Faction = a_oFaction;
            Planet = a_oPlanet;
            Planet.Populations.Add(this); // add us to the list of pops on the planet!
        }

    }
}

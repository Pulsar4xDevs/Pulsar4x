using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class Population : GameEntity
    {

        /// <summary>
        /// What is the political situation on this colony? how productive is it and how much of a military presence is needed to hold control of it.
        /// </summary>
        public enum PoliticalStatus
        {
            Conquered,
            Subjugated,
            Occupied,
            Candidate,
            Imperial,
            Count
        }
        #region Properties

        /// <summary>
        /// Which faction owns this population?
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// Which species is on this planet.
        /// </summary>
        public Species Species { get; set; }

        /// <summary>
        /// Planet the population is attached to
        /// </summary>
        public Planet Planet { get; set; }

        /// <summary>
        /// Does this pop have an assigned governor?
        /// </summary>
        public bool GovernorPresent { get; set; }

        /// <summary>
        /// If so who is he?
        /// </summary>
        public Commander PopulationGovernor { get; set; }

        /// <summary>
        /// How skilled in administration should the Planetary Governor be?
        /// </summary>
        public int AdminRating { get; set; }

        /// <summary>
        /// The contact that this population is associated with.
        /// </summary>
        public SystemContact Contact { get; set; }

        /// <summary>
        /// Which factions have detected a thermal sig from this population?
        /// </summary>
        public BindingList<int> ThermalDetection { get; set; }

        /// <summary>
        /// Which factions have detected an EM signature?
        /// </summary>
        public BindingList<int> EMDetection { get; set; }

        /// <summary>
        /// Any active sensor in range detects a planet.
        /// </summary>
        public BindingList<int> ActiveDetection { get; set; }

        /// <summary>
        /// Populations with structures tend to emit a thermal signature. 5 per installation I believe.
        /// </summary>
        public int ThermalSignature { get; set; }

        /// <summary>
        /// How many orbital terraforming modules are in orbit around this planet?
        /// </summary>
        public float OrbitalTerraformModules { get; set; }


        public float CivilianPopulation { get; set; }
        public float PopulationGrowthRate { get; set; }
        public float FuelStockpile { get; set; }
        public int MaintenanceSupplies { get; set; }

        /// <summary>
        /// What is the situation of this colony.
        /// </summary>
        public PoliticalStatus PoliticalPopStatus { get; set; }


        public float PopulationWorkingInAgriAndEnviro
        {
            get
            {
                // 5% of civi pop

                //5 + 5 * ColonyCost
                float Agriculture = 0.05f + (0.05f * (float)Species.ColonyCost(Planet));
                return CivilianPopulation * Agriculture;
            }
        }

        public float PopulationWorkingInServiceIndustries
        {
            get
            {
                // 75% of Civi Pop
                //ServicePercent = Sqr(Sqr(TotalPop * 100000)) / 100
                float ServicePercent = (float)(Math.Sqrt(Math.Sqrt((double)CivilianPopulation * 100000.0)) / 100.0);
                if (ServicePercent > 0.75f)
                    ServicePercent = 0.75f;

                float pop = CivilianPopulation - PopulationWorkingInAgriAndEnviro;
                return ServicePercent * pop;
            }
        }

        public float PopulationWorkingInManufacturing
        {
            get
            {
                // 20% of civi pop
                return CivilianPopulation - (PopulationWorkingInAgriAndEnviro + PopulationWorkingInServiceIndustries);
            }
        }

        /// <summary>
        /// EM Signature is related to population.
        /// </summary>
        public int EMSignature { get; set; }


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


        /// <summary>
        /// This population's stored TN components. 
        /// </summary>
        public BindingList<ComponentDefTN> ComponentStockpile { get; set; }

        /// <summary>
        /// The number of each component.
        /// </summary>
        public BindingList<float> ComponentStockpileCount { get; set; }

        /// <summary>
        /// Where in the stockpile any particular component is. guid = the guid of the componentdef and int is the array location.
        /// </summary>
        public Dictionary<Guid, int> ComponentStockpileLookup { get; set; }

        /// <summary>
        /// Missiles at this colony
        /// </summary>
        public Dictionary<OrdnanceDefTN, int> MissileStockpile { get; set; }

        #endregion

        public Population(Planet a_oPlanet, Faction a_oFaction, String a_oName = "Earth", Species a_oSpecies = null)
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

            Name = a_oName;  // just a default Value!

            Faction = a_oFaction;
            Planet = a_oPlanet;
            if (a_oSpecies == null)
            {
                Species = Faction.Species;
            }
            else
            {
                Species = a_oSpecies;
            }
            Planet.Populations.Add(this); // add us to the list of pops on the planet!

            Contact = new SystemContact(Faction,this);

            GovernorPresent = false;
            AdminRating = 0;

            ComponentStockpile = new BindingList<ComponentDefTN>();
            ComponentStockpileCount = new BindingList<float>();
            ComponentStockpileLookup = new Dictionary<Guid, int>();
            MissileStockpile = new Dictionary<OrdnanceDefTN, int>();

            OrbitalTerraformModules = 0.0f;

            PoliticalPopStatus = PoliticalStatus.Imperial;

            for (int InstallationIterator = 0; InstallationIterator < (int)Installation.InstallationType.InstallationCount; InstallationIterator++)
            {
                Installations[InstallationIterator].Number = 0.0f;
            }

            FuelStockpile = 0.0f;
            MaintenanceSupplies = 0;
            EMSignature = 0;
            ThermalSignature = 0;
            ModifierEconomicProduction = 1.0f;
            ModifierManfacturing = 1.0f;
            ModifierProduction = 1.0f;
            ModifierWealthAndTrade = 1.0f;
            ModifierPoliticalStability = 1.0f;

            ConventionalStart();
            
        }

        public void ConventionalStart()
        {
            Installations[(int)Installation.InstallationType.ConventionalIndustry].Number = 1000.0f;
            Installations[(int)Installation.InstallationType.DeepSpaceTrackingStation].Number = 1.0f;
            Installations[(int)Installation.InstallationType.MilitaryAcademy].Number = 1.0f;
            Installations[(int)Installation.InstallationType.NavalShipyardComplex].Number = 1.0f;
            Installations[(int)Installation.InstallationType.MaintenanceFacility].Number = 5.0f;
            Installations[(int)Installation.InstallationType.ResearchLab].Number = 5.0f;

            FuelStockpile = 0.0f;
            MaintenanceSupplies = 2000;

            CivilianPopulation = 500.0f;
        }

        public void TNStart()
        {
            CivilianPopulation = 500.0f;
        }

        /// <summary>
        /// I am not sure if this will be necessary but since the population has detection statistics it should have a contact with an accessible
        /// location to the SystemContactList.
        /// </summary>
        public void UpdateLocation()
        {
            Contact.UpdateLocationInSystem(Planet.XSystem, Planet.YSystem);
        }

        /// <summary>
        /// How long does it take to load or unload from this population?
        /// </summary>
        /// <param name="TaskGroupTime">Time that the taskgroup will take barring any planetary modifiers. this is calculated beforehand.</param>
        /// <returns>Time in seconds.</returns>
        public int CalculateLoadTime(int TaskGroupTime)
        {
            float NumStarports = m_aoInstallations[(int)Installation.InstallationType.Spaceport].Number;

            int TotalTime = TaskGroupTime;

            if (GovernorPresent)
                TotalTime = (int)((float)TaskGroupTime / ((NumStarports + 1.0f) * PopulationGovernor.LogisticsBonus));
            else
                TotalTime = (int)((float)TaskGroupTime / (NumStarports + 1.0f));

            return TotalTime;
        }


        /// <summary>
        /// Add Components to stockpile places increment number of componentDefs into the planetary stockpile.
        /// </summary>
        /// <param name="ComponentDef">Component to be added. This is the class all components inherit from, not any particular type of component.</param>
        /// <param name="increment">Number to add to the stockpile.</param>
        public void AddComponentsToStockpile(ComponentDefTN ComponentDef, float increment)
        {
            if (ComponentStockpileLookup.ContainsKey(ComponentDef.Id) == true)
            {
                ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] = ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] + increment;
            }
            else
            {
                ComponentStockpile.Add(ComponentDef);
                ComponentStockpileCount.Add(increment);
                ComponentStockpileLookup.Add(ComponentDef.Id, ComponentStockpile.IndexOf(ComponentDef));
            }
        }

        /// <summary>
        /// TakeComponents from Stockpile takes the specified number of components out of the stockpile, and returns how many were subtracted.
        /// </summary>
        /// <param name="ComponentDef">Component def to be removed.</param>
        /// <param name="decrement">number to remove</param>
        /// <returns>number that were removed.</returns>
        public float TakeComponentsFromStockpile(ComponentDefTN ComponentDef, float decrement)
        {
            float Components = 0.0f;
            if (ComponentStockpileLookup.ContainsKey(ComponentDef.Id) == true)
            {
                Components = ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]];

                if (Components - decrement <= 0.0f)
                {
                    ComponentStockpile.RemoveAt(ComponentStockpileLookup[ComponentDef.Id]);
                    ComponentStockpileCount.RemoveAt(ComponentStockpileLookup[ComponentDef.Id]);
                    ComponentStockpileLookup.Remove(ComponentDef.Id);

                    return Components;
                }
                else
                {
                    Components = Components - decrement;
                    ComponentStockpileCount[ComponentStockpileLookup[ComponentDef.Id]] = Components;
                }
            }
            else
            {
                /// <summary>
                /// Invalid remove request sent from somewhere. Error reporting? logs?
                /// </summary>
                return -1.0f;
            }

            return decrement;
        }

        /// <summary>
        /// Loads or unloads missiles from a population.
        /// </summary>
        /// <param name="Missile">Ordnance type to be loaded or unloaded.</param>
        /// <param name="inc">Amount to load or unload.</param>
        /// <returns>Missiles placed into stockpile or taken out of it.</returns>
        public int LoadMissileToStockpile(OrdnanceDefTN Missile, int inc)
        {
            if (inc > 0)
            {
                if (MissileStockpile.ContainsKey(Missile))
                {
                    MissileStockpile[Missile] = MissileStockpile[Missile] + inc;
                }
                else
                {
                    MissileStockpile.Add(Missile, inc);
                }
                return inc;
            }
            else
            {
                if (MissileStockpile.ContainsKey(Missile) == false)
                {
                    return 0;
                }
                else
                {
                    /// <summary>
                    /// Inc is negative here.
                    /// </summary>
                    int retVal = MissileStockpile[Missile];
                    MissileStockpile[Missile] = MissileStockpile[Missile] + inc;

                    if (MissileStockpile[Missile] <= 0)
                    {
                        MissileStockpile.Remove(Missile);

                        return retVal;
                    }

                    return inc;
                }
            }
        }

        /// <summary>
        /// Calculate the thermal signature of this colony
        /// </summary>
        /// <returns>Thermal Signature</returns>
        public int CalcThermalSignature()
        {
            int signature = (int)Math.Round(CivilianPopulation * Constants.Colony.CivilianThermalSignature);
            foreach (Installation Inst in m_aoInstallations)
            {
                if (Inst.Type == Installation.InstallationType.CommercialShipyard)
                {
                    int ThermalBase = (int)Inst.ThermalSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.Tonnage[SYIterator] * Inst.Slipways[SYIterator];
                        signature = signature + ThermalBase + (int)Math.Round((float)totalTons / Constants.Colony.CommercialShipyardTonnageDivisor);
                    }
                }
                else if (Inst.Type == Installation.InstallationType.NavalShipyardComplex)
                {
                    int ThermalBase = (int)Inst.ThermalSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.Tonnage[SYIterator] * Inst.Slipways[SYIterator];
                        signature = signature + ThermalBase + (int)Math.Round((float)totalTons / Constants.Colony.NavalShipyardTonnageDivisor);
                    }
                }
                else
                {
                    signature = signature + (int)Math.Round(Inst.ThermalSignature * Math.Floor(Inst.Number));
                }
            }
            ThermalSignature = signature;
            return signature;
        }

        /// <summary>
        /// Calculate the EM signature of this colony
        /// </summary>
        /// <returns>EM Signature</returns>
        public int CalcEMSignature()
        {
            int signature = (int)Math.Round(CivilianPopulation * Constants.Colony.CivilianEMSignature);
            foreach (Installation Inst in m_aoInstallations)
            {
                if (Inst.Type == Installation.InstallationType.CommercialShipyard)
                {
                    int EMBase = (int)Inst.EMSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.Tonnage[SYIterator] * Inst.Slipways[SYIterator];
                        signature = signature + EMBase + (int)Math.Round((float)totalTons / Constants.Colony.CommercialShipyardTonnageDivisor);
                    }
                }
                else if (Inst.Type == Installation.InstallationType.NavalShipyardComplex)
                {
                    int EMBase = (int)Inst.EMSignature;
                    int SYCount = (int)Math.Floor(Inst.Number);
                    for (int SYIterator = 0; SYIterator < SYCount; SYIterator++)
                    {
                        int totalTons = Inst.Tonnage[SYIterator] * Inst.Slipways[SYIterator];
                        signature = signature + EMBase + (int)Math.Round((float)totalTons / Constants.Colony.NavalShipyardTonnageDivisor);
                    }
                }
                else
                {
                    signature = signature + (int)Math.Round(Inst.EMSignature * Math.Floor(Inst.Number));
                }
            }
            EMSignature = signature;
            return signature;
        }
    }
}

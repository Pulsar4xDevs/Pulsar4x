using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities;

namespace Pulsar4X.Entities
{
    public class Installation : GameEntity
    {
        public enum InstallationType
        {
            AutomatedMine,
            CivilianMiningComplex,
            CommercialShipyard,
            ConstructionFactory,
            ConventionalIndustry,
            ConvertCIToConstructionFactory,
            ConvertCIToFighterFactory,
            ConvertCIToFuelRefinery,
            ConvertCIToMine,
            ConvertCIToOrdnanceFactory,
            ConvertMineToAutomated,
            DeepSpaceTrackingStation,
            FighterFactory,
            FinancialCentre,
            FuelRefinery,
            GeneticModificationCentre,
            GroundForceTrainingFacility,
            Infrastructure,
            MaintenanceFacility,
            MassDriver,
            MilitaryAcademy,
            Mine,
            NavalShipyardComplex,
            OrdnanceFactory,
            ResearchLab,
            SectorCommand,
            Spaceport,
            TerraformingInstallation,
            InstallationCount
        }

        public const int NO_OF_INSTALLATIONS = (int)InstallationType.InstallationCount;
 
        /// <summary>
        /// Which installation is this?
        /// </summary>
        public InstallationType Type { get; set; }

        /// <summary>
        /// amount of wealth and resource units to build.
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// Number of this installation that a colony has
        /// </summary>
        public float Number { get; set; }

        /// <summary>
        /// Size of this installation for cargo transfering.
        /// </summary>
        public int Mass { get; set; }

        /// <summary>
        /// What thermal signature does this installation have.
        /// </summary>
        public float ThermalSignature { get; set; }

        /// <summary>
        /// What EM Signature does this installation have.
        /// </summary>
        public float EMSignature { get; set; }

        /// <summary>
        /// Shipyard tonnage.
        /// </summary>
        public BindingList<int> Tonnage { get; set; }

        /// <summary>
        /// Shipyard slipway count.
        /// </summary>
        public BindingList<int> Slipways { get; set; }

        /// <summary>
        /// If this installation requires another to be built: CI Conversions and mine conversions. A value of InstallationCount means no required installation
        /// I don't want to cast these as -1s since I'm not really sure how that would work.
        /// </summary>
        public InstallationType RequiredPrerequisitInstallation { get; set; }

        /// <summary>
        /// If this installation represents a conversion of an existing installation, this is the output installation.
        /// </summary>
        public InstallationType OutputInstallation { get; set; }

        /// <summary>
        /// If this installation requires a specific technology to be constructed this will be it. This will be set to Count if not the case.
        /// </summary>
        public Faction.FactionTechnology RequiredTechnology { get; set; }

        /// <summary>
        /// If a technology is required this will be the tech level to check against.
        /// </summary>
        public int RequiredTechLevel { get; set; }


        /// <summary>
        /// Some installations cannot be constructed.
        /// </summary>
        public bool CanBeBuilt { get; set; }

        int[] m_aiMinerialsCost;
        public int[] MinerialsCost
        {
            get
            {
                return m_aiMinerialsCost;
            }
            set
            {
                m_aiMinerialsCost = value;
            }
        }

        public Installation()
        {
            /// <summary>
            /// Id must be present or anything needing it will lose its cookies.
            /// </summary>
            Id = Guid.NewGuid();

            Number = 0;
            Mass = 25000;
            Type = InstallationType.InstallationCount;
            ThermalSignature = 0;
            EMSignature = 0;
            RequiredPrerequisitInstallation = InstallationType.InstallationCount;
            OutputInstallation = InstallationType.InstallationCount;
            RequiredTechnology = Faction.FactionTechnology.Count;
            RequiredTechLevel = -1;
            CanBeBuilt = false;
        }

        public Installation(InstallationType a_eType)
        {
            /// <summary>
            /// Id must be present or anything needing it will lose its cookies.
            /// </summary>
            Id = Guid.NewGuid();

            Number = 0;
            Mass = 25000;
            Tonnage = new BindingList<int>();
            Slipways = new BindingList<int>();
            Type = a_eType;
            m_aiMinerialsCost = new int[Constants.Minerals.NO_OF_MINERIALS];
            ThermalSignature = 0;
            EMSignature = 0;
            RequiredPrerequisitInstallation = InstallationType.InstallationCount;
            OutputInstallation = InstallationType.InstallationCount;
            RequiredTechnology = Faction.FactionTechnology.Count;
            RequiredTechLevel = -1;
            CanBeBuilt = true;

            switch (a_eType)
            {
                case InstallationType.AutomatedMine:
                    {
                        Name = "Automated Mine";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.CivilianMiningComplex:
                    {
                        Name = "Civilian Mining Complex";
                        ThermalSignature = 50;
                        EMSignature = 0;
                        CanBeBuilt = false;
                        break;
                    }
                case InstallationType.CommercialShipyard:
                    {
                        Name = "Commercial Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        Tonnage.Add(10000);
                        Slipways.Add(1);

                        /// <summary>
                        /// For base
                        /// </summary>
                        ThermalSignature = 220;
                        EMSignature = 110;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ConstructionFactory:
                    {
                        Name = "Construction Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 30;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ConventionalIndustry:
                    {
                        Name = "Conventional Industry";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        CanBeBuilt = false;
                        break;
                    }
                    case InstallationType.ConvertCIToConstructionFactory:
                    {
                        Name = "Convert CI to Construction Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 5;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 5;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.ConstructionFactory;
                        break;
                    }
                    case InstallationType.ConvertCIToFighterFactory:
                    {
                        Name = "Convert CI to Fighter Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 5;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 15;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.FighterFactory;
                        break;
                    }
            case InstallationType.ConvertCIToFuelRefinery:
                    {
                        Name = "Convert CI to Fuel Refinery";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 20;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.FuelRefinery;
                        break;
                    }
            case InstallationType.ConvertCIToMine:
                    {
                        Name = "Convert CI to Mine";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 10;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.Mine;
                        break;
                    }
            case InstallationType.ConvertCIToOrdnanceFactory:
                    {
                        Name = "Convert CI to Ordnance Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 10;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.OrdnanceFactory;
                        break;
                    }
            case InstallationType.ConvertMineToAutomated:
                    {
                        Name = "Convert mine to Automated";
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 75;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 75;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.Mine;
                        OutputInstallation = InstallationType.AutomatedMine;
                        break;
                    }

                case InstallationType.DeepSpaceTrackingStation:
                    {
                        Name = "DeepSpace Tracking Station";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 150;
                        ThermalSignature = 5;
                        EMSignature = 0;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FighterFactory:
                    {
                        Name = "Fighter Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 90;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FinancialCentre:
                    {
                        Name = "Financial Centre";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 50;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FuelRefinery:
                    {
                        Name = "Fuel Refinery";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.GeneticModificationCentre:
                    {
                        Name = "Genetic Modification Centre";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 300;
                        Mass = 50000;
                        ThermalSignature = 10;
                        EMSignature = 50;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.GroundForceTrainingFacility:
                    {
                        Name = "Ground Force Training Facility";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        ThermalSignature = 10;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Infrastructure:
                    {
                        Name = "Infrastructure";
                        Cost = 2;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 2;
                        Mass = 2500;
                        ThermalSignature = 0.5f;
                        EMSignature = 0.5f;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MaintenanceFacility:
                    {
                        Name = "Maintenance Facility";
                        Cost = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 75;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 75;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MassDriver:
                    {
                        Name = "Mass Driver";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 100;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MilitaryAcademy:
                    {
                        Name = "Military Academy";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 300;
                        Mass = 100000;
                        ThermalSignature = 10;
                        EMSignature = 100;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Mine:
                    {
                        Name = "Mine";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 60;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.NavalShipyardComplex:
                    {
                        Name = "Naval Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        Tonnage.Add(1000);
                        Slipways.Add(1);
                        /// <summary>
                        /// base signatures.
                        /// </summary>
                        ThermalSignature = 220;
                        EMSignature = 110;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.OrdnanceFactory:
                    {
                        Name = "Ordnance Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 90;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ResearchLab:
                    {
                        Name = "Research Lab";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 1200;
                        Mass = 100000;
                        ThermalSignature = 50;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.SectorCommand:
                    {
                        Name = "Sector Command";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 600;
                        Mass = 100000;
                        ThermalSignature = 20;
                        EMSignature = 150;
                        RequiredTechnology = Faction.FactionTechnology.ImprovedCommandAndControl;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Spaceport:
                    {
                        Name = "Spaceport";
                        Cost = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 300;
                        Mass = 50000;
                        ThermalSignature = 50;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.TerraformingInstallation:
                    {
                        Name = "Terraforming Installation";
                        Cost = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 300;
                        Mass = 50000;
                        ThermalSignature = 100;
                        EMSignature = 25;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
            }
        }


        /// <summary>
        /// Is this installation buildable?
        /// </summary>
        /// <param name="Fact">Faction owner of this installation type.</param>
        /// <param name="Pop">Population on which this installation is to be built.</param>
        /// <returns>Whether or not the installation can be built by Faction Fact on Population Pop</returns>
        public bool IsBuildable(Faction Fact, Population Pop)
        {
            /// <summary>
            /// CI and CMCs are not buildable.
            /// </summary>
            if (CanBeBuilt == false)
            {
                return false;
            }

            /// <summary>
            /// Technology Check
            /// </summary>
            if (RequiredTechnology != Faction.FactionTechnology.Count)
            {
                if (Fact.FactionTechLevel[(int)RequiredTechnology] < RequiredTechLevel)
                {
                    return false;
                }
            }

            /// <summary>
            /// Required installation check.
            /// </summary>
            if (RequiredPrerequisitInstallation != InstallationType.InstallationCount)
            {
                if (Pop.Installations[(int)RequiredPrerequisitInstallation].Number < 1.0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

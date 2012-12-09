using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;

namespace Pulsar4X.Entities
{
    public class Installation : GameEntity
    {
        public enum InstallationType
        {
            AutomatedMine,
            CommercialShipyard,
            ConstructionFactory,
            DeepSpaceTrackingStation,
            FighterFactory,
            FinancialCentre,
            FuelRefinery,
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
            TerraformingInstallation
        }

        public const int NO_OF_INSTALLATIONS = 19;
 
        public InstallationType Type { get; set; }
        public int Cost { get; set; }
        public float Number { get; set; }
        public int Mass { get; set; }

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
            Number = 0;
            Mass = 25000;
        }

        public Installation(InstallationType a_eType)
        {
            Number = 0;
            Mass = 25000;
            Type = a_eType;
            m_aiMinerialsCost = new int[Constants.Minerals.NO_OF_MINERIALS];

            switch (a_eType)
            {
                case InstallationType.AutomatedMine:
                    {
                        Name = "Automated Mine";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 120;
                        break;
                    }
                case InstallationType.CommercialShipyard:
                    {
                        Name = "Commercial Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        break;
                    }
                case InstallationType.ConstructionFactory:
                    {
                        Name = "Construction Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 30;
                        break;
                    }
                case InstallationType.DeepSpaceTrackingStation:
                    {
                        Name = "DeepSpace Tracking Station";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 150;
                        break;
                    }
                case InstallationType.FighterFactory:
                    {
                        Name = "Fighter Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 90;
                        break;
                    }
                case InstallationType.FinancialCentre:
                    {
                        Name = "Financial Centre";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 120;
                        break;
                    }
                case InstallationType.FuelRefinery:
                    {
                        Name = "Fuel Refinery";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        break;
                    }
                case InstallationType.GroundForceTrainingFacility:
                    {
                        Name = "Ground Force Training Facility";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        break;
                    }
                case InstallationType.Infrastructure:
                    {
                        Name = "Infrastructure";
                        Cost = 2;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 2;
                        break;
                    }
                case InstallationType.MaintenanceFacility:
                    {
                        Name = "Maintenance Facility";
                        Cost = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 75;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 75;
                        break;
                    }
                case InstallationType.MassDriver:
                    {
                        Name = "Mass Driver";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 100;

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
                        break;
                    }
                case InstallationType.Mine:
                    {
                        Name = "Mine";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 60;
                        break;
                    }
                case InstallationType.NavalShipyardComplex:
                    {
                        Name = "Naval Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        break;
                    }
                case InstallationType.OrdnanceFactory:
                    {
                        Name = "Ordnance Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 90;
                        break;
                    }
                case InstallationType.ResearchLab:
                    {
                        Name = "Research Lab";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 1200;
                        Mass = 100000;
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
                        break;
                    }
                case InstallationType.TerraformingInstallation:
                    {
                        Name = "Terraforming Installation";
                        Cost = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 300;
                        Mass = 50000;
                        break;
                    }
            }
        }
    }
}

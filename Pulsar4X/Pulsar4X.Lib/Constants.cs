using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using System.Drawing;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X
{
    /// <summary>
    /// Container class for all the constants used elsewhere in the game
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Scientific Constants
        /// </summary>
        public static class Science
        {
			// Gravitation Constant
			public const double GravitationalConstant = 6.67384E-11;
        }

        /// <summary>
        /// Constants dealing with units and measurements
        /// </summary>
        public static class Units
        {
            public const double SolarMassInKG =  1.98855E30;

            public const double EarthMassInKG = 5.97219E24;

            public const double SolMassInEarthMasses = 332946;

            public const double KmPerLightYear = 9460730472580.8;

            public const double AuPerLightYear = KmPerLightYear / KmPerAu;

            public const double KmPerAu = 149597871;

            public const double MetersPerAu = KmPerAu * 1000;

            /// <summary>
            /// Plus or Minus 65Km
            /// </summary>
            public const double SolarRadiusInKm = 696342.0;

            public const double SolarRadiusInAu = SolarRadiusInKm / KmPerAu;

            /// <summary>
            /// Earth's gravity in m/s^2. Aka 1g.
            /// </summary>
            public const double EarthGravity = 9.81;

            /// <summary>
            /// Note that this is = to 1 ATM.
            /// </summary>
            public const double EarthAtmosphereInKpa = 101.325;

            /// <summary>
            /// Add to Kelvin to get degrees c.
            /// </summary>
            public const double DegreesCToKelvin = 273.15;

            /// <summary>
            /// Add to degrees c to get kelvin.
            /// </summary>
            public const double KelvinToDegreesC = -273.15;

            /// <summary>
            /// Number of radians in 360 degrees
            /// </summary>
            public const double RadiansPerRotation = 2.0 * Math.PI;

            /// <summary>
            /// RADIAN is the value of each degree in radians. PI/180.
            /// </summary>
            public const double Radian = Math.PI / 180.0;


            /// <summary>
            /// For anyone worried, these are only to check to see if the large distance model needs to be used or not, we aren't constrained to 32 bit limitations with regards to distances.
            /// </summary>

            /// <summary>
            /// 32 bit limitation number for distances in KM. //14.35504154
            /// </summary>
            public const double MAX_KM_IN_AU = 2147483648.0 / KmPerAu;

            /// <summary>
            /// 5 second speed of light limitation for beam weapons.
            /// </summary>
            public const double BEAM_AU_MAX = 1500000.0 / KmPerAu; //~0.01002 AU

            /// <summary>
            /// Speed of light limitation, this time in KM.
            /// </summary>
            public const double BEAM_KM_MAX = Constants.Units.BEAM_AU_MAX * Constants.Units.KmPerAu;

            /// <summary>
            /// 32 bit limitation for the 10KM unit system used by auroraTN.
            /// </summary>
            public const double TEN_KM_MAX = 214748.0;

            /// <summary>
            /// 32 bit limitation for orbit period days.
            /// </summary>
            public const double MAX_DAYS_IN_SECONDS = 2147483648.0 / Constants.TimeInSeconds.Day;
        }
        
        /// <summary>
        /// This Class is used to lookup RGBA Colors for different star classes.
        /// </summary>
        /// <remarks>
        /// @note The following colors are source fron:
        /// http://www.vendian.org/mncharity/dir3/starcolor/UnstableURLs/starcolors.html
        /// where this source did not provide a color an aproiximation was done using 
        /// the next class above and below the missing one. These are comented as such.
        /// </remarks>
        public class StarColor
        {
            private Dictionary<string, Color> m_dicStarColors;

            private static StarColor m_oStarColor;

            private StarColor()
            {
                m_dicStarColors = new Dictionary<string, Color>();
                
                m_dicStarColors["O5"] = Color.FromArgb(255, 155, 176, 255);
                m_dicStarColors["O6"] = Color.FromArgb(255, 162, 184, 255);
                m_dicStarColors["O7"] = Color.FromArgb(255, 157, 177, 255);
                m_dicStarColors["O8"] = Color.FromArgb(255, 157, 177, 255);
                m_dicStarColors["O9"] = Color.FromArgb(255, 154, 178, 255);
                m_dicStarColors["O9.5"] = Color.FromArgb(255, 164, 186, 255);

                m_dicStarColors["B0"] = Color.FromArgb(255, 156, 178, 255);
                m_dicStarColors["B0.5"] = Color.FromArgb(255, 167, 188, 255);
                m_dicStarColors["B1"] = Color.FromArgb(255, 160, 182, 255);
                m_dicStarColors["B2"] = Color.FromArgb(255, 160, 180, 255);
                m_dicStarColors["B3"] = Color.FromArgb(255, 165, 185, 255);
                m_dicStarColors["B4"] = Color.FromArgb(255, 164, 184, 255);
                m_dicStarColors["B5"] = Color.FromArgb(255, 170, 191, 255);
                m_dicStarColors["B6"] = Color.FromArgb(255, 172, 189, 255);
                m_dicStarColors["B7"] = Color.FromArgb(255, 173, 191, 255);
                m_dicStarColors["B8"] = Color.FromArgb(255, 177, 195, 255);
                m_dicStarColors["B9"] = Color.FromArgb(255, 181, 198, 255);

                m_dicStarColors["A0"] = Color.FromArgb(255, 185, 201, 255);
                m_dicStarColors["A1"] = Color.FromArgb(255, 181, 199, 255);
                m_dicStarColors["A2"] = Color.FromArgb(255, 187, 203, 255);
                m_dicStarColors["A3"] = Color.FromArgb(255, 191, 207, 255);
                m_dicStarColors["A4"] = Color.FromArgb(255, 195, 210, 255);
                m_dicStarColors["A5"] = Color.FromArgb(255, 202, 215, 255);
                m_dicStarColors["A6"] = Color.FromArgb(255, 199, 212, 255);
                m_dicStarColors["A7"] = Color.FromArgb(255, 200, 213, 255);
                m_dicStarColors["A8"] = Color.FromArgb(255, 213, 222, 255);
                m_dicStarColors["A9"] = Color.FromArgb(255, 219, 224, 255);

                m_dicStarColors["F0"] = Color.FromArgb(255, 224, 229, 255);
                m_dicStarColors["F1"] = Color.FromArgb(255, 230, 234, 255);   // This value is estimated using F0 and F2 values.
                m_dicStarColors["F2"] = Color.FromArgb(255, 236, 239, 255);
                m_dicStarColors["F3"] = Color.FromArgb(255, 227, 230, 255);
                m_dicStarColors["F4"] = Color.FromArgb(255, 224, 226, 255);
                m_dicStarColors["F5"] = Color.FromArgb(255, 248, 247, 255);
                m_dicStarColors["F6"] = Color.FromArgb(255, 244, 241, 255);
                m_dicStarColors["F7"] = Color.FromArgb(255, 246, 243, 255);
                m_dicStarColors["F8"] = Color.FromArgb(255, 255, 247, 252);
                m_dicStarColors["F9"] = Color.FromArgb(255, 255, 247, 252);

                m_dicStarColors["G0"] = Color.FromArgb(255, 255, 248, 252);
                m_dicStarColors["G1"] = Color.FromArgb(255, 255, 247, 248);
                m_dicStarColors["G2"] = Color.FromArgb(255, 255, 245, 242);
                m_dicStarColors["G3"] = Color.FromArgb(255, 255, 243, 233);
                m_dicStarColors["G4"] = Color.FromArgb(255, 255, 241, 229);
                m_dicStarColors["G5"] = Color.FromArgb(255, 255, 244, 234);
                m_dicStarColors["G6"] = Color.FromArgb(255, 255, 244, 235);
                m_dicStarColors["G7"] = Color.FromArgb(255, 255, 244, 235);
                m_dicStarColors["G8"] = Color.FromArgb(255, 255, 237, 222);
                m_dicStarColors["G9"] = Color.FromArgb(255, 255, 239, 221);

                m_dicStarColors["K0"] = Color.FromArgb(255, 255, 238, 221);
                m_dicStarColors["K1"] = Color.FromArgb(255, 255, 224, 188);
                m_dicStarColors["K2"] = Color.FromArgb(255, 255, 227, 196);
                m_dicStarColors["K3"] = Color.FromArgb(255, 255, 222, 195);
                m_dicStarColors["K4"] = Color.FromArgb(255, 255, 216, 181);
                m_dicStarColors["K5"] = Color.FromArgb(255, 255, 210, 161);
                m_dicStarColors["K6"] = Color.FromArgb(255, 255, 204, 151);  // This value is estimated using K5 and K7 values.
                m_dicStarColors["K7"] = Color.FromArgb(255, 255, 199, 142);
                m_dicStarColors["K8"] = Color.FromArgb(255, 255, 209, 174);
                m_dicStarColors["K9"] = Color.FromArgb(255, 255, 200, 161);  // This value is estimated using K8 and M0 values.

                m_dicStarColors["M0"] = Color.FromArgb(255, 255, 195, 139);
                m_dicStarColors["M1"] = Color.FromArgb(255, 255, 204, 142);
                m_dicStarColors["M2"] = Color.FromArgb(255, 255, 196, 131);
                m_dicStarColors["M3"] = Color.FromArgb(255, 255, 206, 129);
                m_dicStarColors["M4"] = Color.FromArgb(255, 255, 201, 127);
                m_dicStarColors["M5"] = Color.FromArgb(255, 255, 204, 111);
                m_dicStarColors["M6"] = Color.FromArgb(255, 255, 195, 112);
                m_dicStarColors["M7"] = Color.FromArgb(255, 255, 197, 110);   // This value is estimated using M6 and M8 values.
                m_dicStarColors["M8"] = Color.FromArgb(255, 255, 198, 109);
                m_dicStarColors["M9"] = Color.FromArgb(255, 255, 233, 154);

                m_dicStarColors["O"] = Color.FromArgb(255, 155, 176, 255);
                m_dicStarColors["B"] = Color.FromArgb(255, 170, 191, 255);
                m_dicStarColors["A"] = Color.FromArgb(255, 202, 215, 255);
                m_dicStarColors["F"] = Color.FromArgb(255, 248, 247, 255);
                m_dicStarColors["G"] = Color.FromArgb(255, 255, 244, 234);
                m_dicStarColors["K"] = Color.FromArgb(255, 255, 210, 161);
                m_dicStarColors["M"] = Color.FromArgb(255, 255, 204, 111);
                m_dicStarColors["N"] = Color.FromArgb(255, 255, 157, 000);
            }

            /// <summary>
            /// Returns the color of the provided star.
            /// </summary>
            public static Color LookupColor(Star star)
            {
                if (m_oStarColor == null)
                {
                    m_oStarColor = new StarColor();
                }

                if (star != null)
                {
                    string sClass = star.SpectralType.ToString() + star.SpectralSubDivision.ToString();
                    if (m_oStarColor.m_dicStarColors.ContainsKey(sClass))
                    {
                        return m_oStarColor.m_dicStarColors[sClass];
                    }
                    else
                    {
                        return Color.White; 
                    }
                }

                return Color.White;
            }
        }

        /// <summary>
        /// @todo this (Minerals) should be mnoved.
        /// </summary>
        public static class Minerals
        {
            public enum MinerialNames
            {
                Duranium,
                Neutronium,
                Corbomite,
                Tritanium,
                Boronide,
                Mercassium,
                Vendarite,
                Sorium,
                Uridium,
                Corundium,
                Gallicite,
                MinerialCount
            }

            public const int NO_OF_MINERIALS = (int)MinerialNames.MinerialCount;
        }

        /// <summary>
        /// ShipTN related constants here right now.
        /// </summary>
        public static class ShipTN
        {
            /// <summary>
            /// No sensor may have a resolution greater than 500 HS or about 25,000 tons.
            /// </summary>
            public const int ResolutionMax = 500;

            /// <summary>
            /// there are 50 tons per any single hull space.
            /// </summary>
            public const float TonsPerHS = 50.0f;

            /// <summary>
            /// In seconds per ton.
            /// </summary>
            public const int BaseCargoLoadTimePerTon = 36;

            /// <summary>
            /// In seconds per person. Each cryopod seems to be 1/2 of a ton.
            /// </summary>
            public const int BaseCryoLoadTimePerPerson = 18;

            /// <summary>
            /// In Aurora, load time is a constant 10 days for troop transports.
            /// </summary>
            public const int BaseTroopLoadTime = 864000;

            /// <summary>
            /// I moved the following three enums here to constants to prevent taskgroup from ballooning in size beyond all recognition, I may move them back though once done.
            /// </summary>
            public enum OrderType
            {
                /// <summary>
                /// General Ship Orders:
                /// </summary>
                MoveTo,
                ExtendedOrbit,
                Picket,
                LoadCrewFromColony,
                RefuelFromColony,
                ResupplyFromColony,
                SendMessage,
                EqualizeFuel,
                EqualizeMSP,
                ActivateTransponder,
                DeactivateTransponder,

                /// <summary>
                /// TaskGroups with active sensors:
                /// </summary>
                ActivateSensors,
                DeactivateSensors,

                /// <summary>
                /// Taskgroups with shield equipped ships:
                /// </summary>
                ActivateShields,
                DeactivateShields,

                /// <summary>
                /// Any Taskgroup of more than one vessel.
                /// </summary>
                DivideFleetToSingleShips,

                /// <summary>
                /// Any taskgroup that has sub task groups created from it, such as by a divide order.
                /// </summary>
                IncorporateSubfleet,

                /// <summary>
                /// Military Ship Specific orders:
                /// </summary>
                BeginOverhaul,


                /// <summary>
                /// Targeted on taskforce specific orders:
                /// </summary>
                Follow,
                Join,
                Absorb,

                /// <summary>
                /// JumpPoint Capable orders only:
                /// </summary>
                StandardTransit,
                SquadronTransit,
                TransitAndDivide,

                /// <summary>
                /// Cargo Hold specific orders when targeted on population/planet:
                /// </summary>
                LoadInstallation,
                LoadShipComponent,
                UnloadInstallation,
                UnloadShipComponent,
                UnloadAll,
                LoadAllMinerals,
                UnloadAllMinerals,
                LoadMineral,
                LoadMineralWhenX,
                UnloadMineral,
                LoadOrUnloadMineralsToReserve,

                /// <summary>
                /// Colony ship specific orders:
                /// </summary>
                LoadColonists,
                UnloadColonists,

                /// <summary>
                /// GeoSurvey specific orders:
                /// </summary>
                GeoSurvey,
                DetachNonGeoSurvey,

                /// <summary>
                /// Grav survey specific orders:
                /// </summary>
                GravSurvey,
                DetachNonGravSurvey,

                /// <summary>
                /// Jump Gate Construction Module specific orders:
                /// </summary>
                BuildJumpGate,

                /// <summary>
                /// Tanker Specific:
                /// </summary>
                RefuelTargetFleet,
                RefuelFromOwnTankers,
                UnloadFuelToPlanet,
                DetachTankers,

                /// <summary>
                /// Supply Ship specific:
                /// </summary>
                ResupplyTargetFleet,
                ResupplyFromOwnSupplyShips,
                UnloadSuppliesToPlanet,
                DetachSupplyShips,

                /// <summary>
                /// Collier Specific:
                /// </summary>
                ReloadTargetFleet,
                ReloadFromOwnColliers,
                DetachColliers,

                /// <summary>
                /// Any ship with a magazine:
                /// </summary>
                LoadOrdnanceFromColony,
                UnloadOrdnanceToColony,

                /// <summary>
                /// Any taskgroup, but the target must be a TG with the appropriate ship to fulfill this order.
                /// </summary>
                RefuelFromTargetFleet,
                ResupplyFromTargetFleet,
                ReloadFromTargetFleet,

                /// <summary>
                /// Any taskgroup, but target must have hangar bays, perhaps check to see if capacity is available.
                /// </summary>
                LandOnAssignedMothership,
                LandOnMotherShipNoAssign,
                LandOnMothershipAssign,

                /// <summary>
                /// Tractor Equipped Ships:
                /// </summary>
                TractorSpecifiedShip,
                TractorSpecifiedShipyard,
                ReleaseAt,

                /// <summary>
                /// Number of orders available.
                /// </summary>
                TypeCount
            }

            /// <summary>
            /// What state is the taskgroup in regarding accepting additional orders?
            /// </summary>
            public enum OrderState
            {
                AcceptOrders,
                DisallowOrdersPDC,
                DisallowOrdersSB,
                DisallowOrdersUnknownJump,
                DisallowOrdersFollowingTarget,
                UnableToComply,
                CurrentlyOverhauling,
                CurrentlyLoading,
                CurrentlyUnloading,
                TypeCount
            }

            public enum LoadType
            {
                Cargo,
                Cryo,
                Troop,
                TypeCount
            }
        }

        /// <summary>
        /// Faction related goodness.
        /// </summary>
        public static class Faction
        {
            /// <summary>
            /// FactionMax has to be relatively hard coded or else the sensor model goes to hell.
            /// </summary>
            public const int FactionMax = 64;

            /// <summary>
            /// What should the starting wealth be?
            /// </summary>
            public const decimal StartingWealth = 100000.0m;
        }

        /// <summary>
        /// Colony related constants
        /// </summary>
        public static class Colony
        {
            /// <summary>
            /// Thermal signature per million pop.
            /// </summary>
            public static float CivilianThermalSignature = 5.0f;

            /// <summary>
            /// EM signature per million pop.
            /// </summary>
            public static float CivilianEMSignature = 50.0f;


            /// <summary>
            /// For Thermal and EM signature calculations.
            /// </summary>
            public static float NavalShipyardTonnageDivisor = 50.0f;

            /// <summary>
            /// For Thermal and EM signature calculations.
            /// </summary>
            public static float CommercialShipyardTonnageDivisor = 500.0f;
            /// <summary>
            /// What sensor strength will a single DSTS add? This is about equal to a full sized thermal sensor array at each tech level.
            /// </summary>
            public static int[] ThermalDeepSpaceStrength = { 250, 300, 400, 550, 700, 900, 1200, 1600, 2000, 2500, 3000, 3750 };

            /// <summary>
            /// What sensor strength will a single DSTS add? This is about equal to a full sized thermal sensor array at each tech level.
            /// </summary>
            public static int[]EMDeepSpaceStrength = { 250, 300, 400, 550, 700, 900, 1200, 1600, 2000, 2500, 3000, 3750 };

            /// <summary>
            /// Maximum index to DeepSpaceStrength
            /// </summary>
            public const int DeepSpaceMax = 11;

            /// <summary>
            /// Maintenance supply part cost.
            /// </summary>
            public static decimal MaintenanceSupplyCost = 0.25m;
            public static decimal[] MaintenanceMineralCost = { 0.125m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0625m, 0.0m, 0.0625m };

            /// <summary>
            /// How often should build work be run?
            /// </summary>
            public const uint ConstructionCycle = Constants.TimeInSeconds.FiveDays;

            /// <summary>
            /// What fraction of a year does the construction cycle make up?
            /// </summary>
            public const float ConstructionCycleFraction = (float)Constants.Colony.ConstructionCycle / (float)Constants.TimeInSeconds.Year;

            /// <summary>
            /// How much fuel will one refined unit of Sorium yield?
            /// </summary>
            public const float SoriumToFuel = 2000.0f;

            /// <summary>
            /// YearsOfProduction here being greater than 5K means that this project will essentially never finish under current conditions. so don't bother printing an estimated completion date.
            /// </summary>
            public const int TimerYearMax = 5000;

            /// <summary>
            /// About 1 point per 5 days. experimentation shows it varies between 1 and 2.
            /// </summary>
            public const int RadiationDecayPerYear = 72;

            /// <summary>
            /// About 3 points per 5 days. experimentation shows it varies between 3 and 4.
            /// </summary>
            public const int AtmosphericDustDecayPerYear = 216;
        }

        public static class ShipyardInfo
        {
            /// <summary>
            /// What is this shipyard complex doing.
            /// </summary>
            public enum ShipyardActivity
            {
                NoActivity,
                AddSlipway,
                Add500Tons,
                Add1000Tons,
                Add2000Tons,
                Add5000Tons,
                Add10000Tons,
                Retool,
                CapExpansion,
                CapExpansionUntilX,
                Count
            }

            /// <summary>
            /// Strings used by shipyard activity
            /// </summary>
            public static String[] ShipyardTasks = { "No Activity", "Add Extra Slipway", "Add 500 Ton Capacity per Slipway", "Add 1000 Ton Capacity per Slipway", "Add 2000 Ton Capacity per Slipway",
                                                     "Add 5000 Ton Capacity per Slipway", "Add 10000 Ton Capacity per Slipway", "Retool for Selected Class", "Continuous Capacity Expansion", 
                                                     "Capacity Expansion Until X Tons"};

            /// <summary>
            /// What tasks are available at a Shipyard?
            /// </summary>
            public enum Task
            {
                Construction,
                Repair,
                Refit,
                Scrap,
                Count
            }

            /// <summary>
            /// Strings used by Shipyard Task
            /// </summary>
            public static String[] ShipyardTaskType = { "Construction", "Repair", "Refit To", "Scrap" };

            /// <summary>
            /// Type of Shipyard
            /// </summary>
            public enum SYType
            {
                Naval,
                Commercial,
                Count
            }

            /// <summary>
            /// Base cost unaffected by technology. The formula for cost is:
            /// Value * (AmtTonnage / 500) * slipways. if naval * 10.
            /// New slipway Cost is:
            /// Value * (TotalTonnage/500) * slipways * 10 if Naval
            /// All Shipyard activities(as opposed to tasks) will cost Duranium and Neutronium. if you want to change that then be sure to change the relevant code in construction cycle.
            /// </summary>
            public static decimal[] MineralCostOfExpansion = { 6.0m, 6.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m, 0.0m };

            /// <summary>
            /// cost in wealth to expand a shipyard. if naval this will be x10.
            /// </summary>
            public const decimal BaseTotalCostOfExpansion = 12.0m;

            /// <summary>
            /// Base unit of mineral cost expansion is cost per 500 tons.
            /// </summary>
            public const int TonnageDenominator = 500;

            /// <summary>
            /// Naval shipyard complexes cost 10x what Commercial shipyard complexes cost and also how much bigger commercial shipyards are than naval yards.
            /// </summary>
            public const int NavalToCommercialRatio = 10;


            /// <summary>
            /// The formula for shipyard modification is: AnnualSYProd = (ModRate / 200) * 834
            /// </summary>
            public const int BaseModRateDenominator = 200;

            /// <summary>
            /// How many BP does a shipyard produce to modify itself for every 200 modRate?
            /// </summary>
            public const int BaseModProd = 834;

            /// <summary>
            /// How many BPs a shipyard produces annually(unmodified by leaders) that it can use to build ships.
            /// </summary>
            public static int[] ShipProductionRate = { 400, 560, 750, 1000, 1300, 1600, 2100, 2750, 3500, 4600, 6000, 8000};

            /// <summary>
            /// Current Techlevel maximum for ship production rate.
            /// </summary>
            public const int MaxShipProductionRate = 11;

            /// <summary>
            /// This is used in mod rate formula, also navalyards should start with this, while commercial yards should get 10x this.
            /// </summary>
            public const int BaseShipyardTonnage = 1000;

            /// <summary>
            /// This is used in the mod rate formula.
            /// </summary>
            public const float ModRateTonnageMultiplier = 40.0f;

            /// <summary>
            /// Base ABR for 5KT ships unmodified by technology.
            /// </summary>
            public const decimal BaseShipBuildingRate = 400.0m;

            /// <summary>
            /// Basic SY modification rate unmodified itself by technology. Shipyard tech alters this by the same percentage that it does the building rate. 400-560 = 240-336
            /// </summary>
            public const int BaseModRate = 240;
        }

        /// <summary>
        /// Tick times (in seconds) to complete said interval.
        /// </summary>
        public static class TimeInSeconds
        {
            public const uint FiveSeconds = 5;
            public const uint ThirtySeconds = 30;
            public const uint Minute = 60;
            public const uint TwoMinutes = 120;
            public const uint FiveMinutes = 300;
            public const uint TwentyMinutes = 1200;
            public const uint Hour = 3600;
            public const uint ThreeHours = 10800;
            public const uint EightHours = 28800;
            public const uint Day = 86400;
            public const uint FiveDays = 432000;
            public const uint Week = 604800;
            public const uint Month = 2592000;
            public const uint Year = 31104000;
            public const uint RealYear = 31556736;
            public const uint Century = 3110400000;
        }

        /// <summary>
        /// Sensor TN describes tech levels for active and passive sensors.
        /// </summary>
        public static class SensorTN
        {
            public static byte[] ActiveStrength = { 10, 12, 16, 21, 28, 36, 48, 60, 80, 100, 135, 180 };
            public static byte[] PassiveStrength = { 5, 6, 8, 11, 14, 18, 24, 32, 40, 50, 60, 75 };

            /// <summary>
            /// What value are sensors calibrated around searching for?
            /// </summary>
            public const uint DefaultPassiveSignature = 1000;
        }

        /// <summary>
        /// Beam fire control tech levels.  multiply these values by 1,000 for Range.
        /// </summary>
        public static class BFCTN
        {
            /// <summary>
            /// Range modifier for the BFC, in 10k increments.
            /// </summary>
            public static byte[] BeamFireControlRange = { 10, 16, 24, 32, 40, 48, 60, 75, 100, 125, 150, 175 };

            /// <summary>
            /// Tracking modifier for the BFC, has to be in km.
            /// </summary>
            public static ushort[] BeamFireControlTracking = { 1250, 2000, 3000, 4000, 5000, 6250, 8000, 10000, 12500, 15000, 20000, 25000 };
        };

        /// <summary>
        /// Power plant tech levels. Power generation per HS.
        /// </summary>
        public static class ReactorTN
        {
            /// <summary>
            /// Reactor Base Power Generation.
            /// </summary>
            public static float[] Power = { 2.0f, 3.0f, 4.5f, 6.0f, 8.0f, 10.0f, 12.0f, 16.0f, 20.0f, 24.0f, 32.0f, 40.0f };
        };

        /// <summary>
        /// Engine related tech levels for Power and Fuel consumption.
        /// </summary>
        public static class EngineTN
        {
            /// <summary>
            /// Engine base Power per 1 HS.
            /// </summary>
            public static float[] EngineBase = { 0.2f, 5.0f, 8.0f, 12.0f, 16.0f, 20.0f, 25.0f, 32.0f, 40.0f, 50.0f, 60.0f, 80.0f, 100.0f };

            /// <summary>
            /// Fuel consumption reduction per engine power hour(or else per hour for standard shields).
            /// </summary>
            public static float[] FuelConsumption = { 1.0f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.25f, 0.2f, 0.16f, 0.125f, 0.1f };

            /// <summary>
            /// Thermal reduction for engines.
            /// </summary>
            public static float[] ThermalReduction = { 1.0f, 0.75f, 0.5f, 0.35f, 0.25f, 0.16f, 0.12f, 0.08f, 0.06f, 0.04f, 0.03f, 0.02f, 0.01f };

            /// <summary>
            /// Hyperdrive size modifier for engines.
            /// </summary>
            public static float[] HyperDriveSize = { 2.0f, 1.8f, 1.6f, 1.5f, 1.4f, 1.3f, 1.2f, 1.15f, 1.1f, 1.05f, 1.0f };
        };

        /// <summary>
        /// Beam Weapon constants are related to tech values.
        /// </summary>
        public static class BeamWeaponTN
        {
            /// <summary>
            /// Size in cm of weapons.
            /// </summary>
            public static byte[] SizeClass = { 10, 12, 15, 20, 25, 30, 35, 40, 50, 60, 70, 80 };

            /// <summary>
            /// Capacitor power per tech level
            /// </summary>
            public static byte[] Capacitor = { 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 25 };

            /// <summary>
            /// Point blank damage value for each calibre of railgun.
            /// </summary>
            public static byte[] RailGunDamage = { 1, 2, 3, 4, 5, 7, 9, 12, 16, 20 };

            /// <summary>
            /// Size value for each calibre of railgun from 10cm to 50cm.
            /// </summary>
            public static byte[] RailGunSize = { 3, 5, 6, 7, 8, 9, 10, 11, 13, 15 };

            /// <summary>
            /// Point blank damage values for lasers and plasma for each tech level from 10cm to 80cm, plasmas lack 10 and 12cm guns however.
            /// Also the power consumption values for mesons and HPMs.
            /// </summary>
            public static byte[] LaserDamage = { 3, 4, 6, 10, 16, 24, 32, 40, 64, 96, 128, 168 };

            /// <summary>
            /// Shared damage values for advanced lasers and plasma.
            /// </summary>
            public static byte[] AdvancedLaserDamage = { 4, 5, 8, 12, 20, 30, 40, 50, 80, 120, 160, 210 };

            /// <summary>
            /// Size of each calibre gun for lasers,plasma,microwave, and mesons from 10cm to 80cm.
            /// </summary>
            public static byte[] LaserSize = { 3, 4, 4, 6, 8, 9, 11, 12, 16, 19, 22, 25 };

            /// <summary>
            /// Damage for each Particle beam.
            /// </summary>
            public static byte[] ParticleDamage = { 2, 3, 4, 6, 9, 12, 16, 20, 25, 36, 50 };

            /// <summary>
            /// Damage for advanced particle beams;
            /// </summary>
            public static byte[] AdvancedParticleDamage = { 3, 4, 5, 8, 11, 15, 20, 25, 32, 45, 64 };

            /// <summary>
            /// Power requirement for each Particle beam.
            /// </summary>
            public static byte[] ParticlePower = { 5, 7, 10, 15, 22, 30, 40, 48, 64, 90, 125 };

            /// <summary>
            /// Size of each Particle beam. Not the advanced particle beams however.
            /// </summary>
            public static byte[] ParticleSize = { 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 22 };

            /// <summary>
            /// Range modifier for particle beam technology, in 10k units.
            /// </summary>
            public static ushort[] ParticleRange = { 6, 10, 15, 20, 24, 32, 40, 50, 64, 80, 100, 120 };

            /// <summary>
            /// Size reduction and accuracy modifiers for Gauss weapons.
            /// </summary>
            public static float[] GaussSize = { 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.5f, 1.0f, 0.75f, 0.6f, 0.5f };
            public static float[] GaussAccuracy = { 1.0f, 0.85f, 0.67f, 0.5f, 0.33f, 0.25f, 0.17f, 0.125f, 0.1f, 0.08f };

            /// <summary>
            /// How many shots this weapon takes every time it fires.
            /// </summary>
            public static byte[] GaussShots = { 1, 2, 3, 4, 5, 6, 8 };

            /// <summary>
            /// Gear size multipliers for multi-barreled turrets.
            /// </summary>
            public static float[] TurretGearFactor = { 0.1f, 0.095f, 0.0925f, 0.09f };
        }

        /// <summary>
        /// Shield related constants placed here for now.
        /// </summary>
        public static class ShieldTN
        {
            /// <summary>
            /// Cost of each shield component tech level. 4 + CostBase[Str] + CostBase[Regen] = cost.
            /// </summary>
            public static byte[] CostBase = { 0, 1, 2, 3, 4, 6, 8, 10, 14, 18, 22, 28 };

            /// <summary>
            /// Strength and regen values for normal shields.
            /// for absorption shields Strength is 3x this, and radiate rate is 1/2x this.
            /// </summary>
            public static float[] ShieldBase = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f, 6.0f, 8.0f, 10.0f, 12.0f, 15.0f };
        }

        public static class MagazineTN
        {
            /// <summary>
            /// Internal armor factor for magazines, and for everywhere else that uses armor. Good programming practices.
            /// </summary>
            public static int[] MagArmor = { 2, 5, 6, 8, 10, 12, 15, 18, 21, 25, 30, 36, 45 };

            /// <summary>
            /// Chance of not having catastrophic destruction occur on mag destruction.
            /// </summary>
            public static float[] Ejection = { 0.7f, 0.8f, 0.85f, 0.9f, 0.93f, 0.95f, 0.97f, 0.98f, 0.99f };

            /// <summary>
            /// Internal space not devoted to the feed mechanism.
            /// </summary>
            public static float[] FeedMechanism = { 0.75f, 0.8f, 0.85f, 0.9f, 0.92f, 0.94f, 0.96f, 0.98f, 0.99f };
        }

        public static class LauncherTN
        {
            /// <summary>
            /// Launcher size adjustment
            /// </summary>
            public static float[] Reduction = { 1.0f, 0.75f, 0.5f, 0.33f, 0.25f, 0.15f };

            /// <summary>
            /// Launcher penalty from reduction.
            /// </summary>
            public static float[] Penalty = { 1.0f, 2.0f, 5.0f, 20.0f, 100.0f, 15.0f };

            /// <summary>
            /// Which index is the boxlauncher?
            /// </summary>
            public static int BoxLauncher = 5;
        }


        public static class OrdnanceTN
        {
            public static int[] warheadTech = { 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24, 30 };
            public static int[] agilityTech = { 20, 32, 48, 64, 80, 100, 128, 160, 200, 240, 320, 400 };
            public static float[] passiveTech = { 0.25f, 0.3f, 0.4f, 0.55f, 0.7f, 0.9f, 1.2f, 1.6f, 2.0f, 2.5f, 3.0f, 3.75f };
            public static float[] activeTech = { 0.5f, 0.6f, 0.8f, 1.05f, 1.4f, 1.6f, 2.4f, 3.0f, 4.0f, 5.0f, 6.75f, 9.0f };
            public static float[] geoTech = { 0.01f, 0.02f, 0.03f, 0.05f };
            public static float[] reactorTech = { 0.1f, 0.15f, 0.225f, 0.3f, 0.4f, 0.5f, 0.6f, 0.8f, 1.0f, 1.2f, 1.6f, 2.0f };
            public static int[] radTech = { 2, 3, 4, 5 };
            public static int[] laserTech = { 2, 4, 6, 10 };

            /// <summary>
            /// Launchers are capped at this value so no missile greater than 100 makes any sense to be designed.
            /// </summary>
            public const double MaxSize = 100.0;

            /// <summary>
            /// Maximum missile speed which they may not exceed in km
            /// </summary>
            public const int MaximumSpeed = 299000;

            /// <summary>
            /// No missile will be smaller than size 6 for sensor purposes. This works out to 0.33 HS, not 0.3 HS however. This is subtracted from 6(6-6=0) for the activeSensor LookUpMT array.
            /// </summary>
            public const int MissileResolutionMinimum = 0;

            /// <summary>
            /// 1 HS, or 20 MSP is the maximum size for missile resolution. This is subtracted from 6(20-6=14) for the activeSensor LookUpMT array.
            /// Missiles can be larger than this, but they will just use LookUpST.
            /// </summary>
            public const int MissileResolutionMaximum = 14;
        }

        public static class JumpEngineTN
        {
            /// <summary>
            /// How many HS of ship does 1 HS of JumpEngine support.
            /// </summary>
            public static int[] JumpEfficiency = { 4,5,6,8,10,12,15,18,21,25 };
            public const int JumpEfficiencyMax = 9;

            /// <summary>
            /// How many ships can use this single jump engine.
            /// </summary>
            public static int[] SquadSize = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            public const int SquadSizeMax = 9;

            /// <summary>
            /// How much bigger is this jump engine for allowing increased squadSize? size is JESize * this.
            /// </summary>
            public static float[] SquadSizeModifier = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 2.4f, 2.6f, 3.0f };

            /// <summary>
            /// How far from the jumppoint can a squadron transit go? multiply this by 10k km.
            /// </summary>
            public static int[] JumpRadius = { 5,10,25,50,75,100,150,200,250,300,400 };
            public const int JumpRadiusMax = 10; //can't just do JumpRadius.Count() - 1 here.

            /// <summary>
            /// How much larger will this jumpengine be due to its jump radius tech?
            /// </summary>
            public static float[] JumpRadiusModifier = { 1.0f, 1.05f, 1.1f, 1.15f, 1.2f, 1.25f, 1.3f, 1.4f, 1.5f, 1.6f, 1.8f};

            /// <summary>
            /// What is the minimum size a jump engine can be and still squadron transit with other craft?
            /// </summary>
            public static int[] MinimumSize = { 15,12,10,8,6,5,4,3,2 };
            public const int MinimumSizeMax = 8;

            /// <summary>
            /// How long does it take a jump engine to recharge?
            /// </summary>
            public const int JumpRechargeTime = (int)Constants.TimeInSeconds.FiveMinutes;

            /// <summary>
            /// Standard transit takes a while for jump effects to wear off.
            /// </summary>
            public const int StandardTransitPenalty = (int)Constants.TimeInSeconds.TwoMinutes;

            /// <summary>
            /// Squadrons recover very quickly.
            /// </summary>
            public const int SquadronTransitPenalty = (int)Constants.TimeInSeconds.ThirtySeconds;

        }

        /// <summary>
        /// List of game-specific settings.
        /// Since we don't have save/load yet, I'm just sticking this here.
        ////< @todo Move to correct place in the code.
        /// </summary>
        public static class GameSettings
        {
            // If true, Allows a faction from using a non-friendly faction's JumpGate. (True = default aurora)
            ///< @todo Not currently functional as false. Factions have no relationships with each other yet.
            // Also Jump Construction modules and jump drives need to be implemented.
            // Should gates be phsyical things that can be destroyed or captured with marines? or should they be capturable/destroyable with a jump construction module only?
            public static bool AllowHostileGateJump = true;

            // Starting Build Points used for FastOOB.
            public static decimal FactionStartingShipBP = 8000m;
            public static decimal FactionStartingPDCBP = 4000m;

            // Base tracking speed factions start writh.
            public static int FactionBaseTrackingSpeed = 1250;

            // How connected universe is. Multiplier for number of JP's generated.
            public static decimal JumpPointConnectivity = 1;

            // Percent chance of a "Hub JP System" to be created.
            public static int SystemJumpPointHubChance = 10;

            // How connected a "Hub System" is. Multiplier for number of JP's generated.
            public static decimal JumpPointHubConnectivity = 2;

            // Base chance for each planet to generate a JP.
            public static int JumpPointGenerationChance = 10;

            // Base chance for a JP connection to loop to an already-generated system in it's local group.
            public static int JumpPointLocalGroupConnectionChance = 15;

            // Size of a LocalGroup.
            public static int JumpPointLocalGroupSize = 10;

            // Determine if Gates are added to JP's on generation.
            public static bool JumpGatesOnEveryJumpPoint = true;

            // Chance for a null-owned jumpgate to be present on a newly generated JP.
            public static int JumpPointGatedChance = 10;

            /// <summary>
            /// Jumppoints will not appear on secondary stars if true.
            /// </summary>
            public static bool PrimaryOnlyJumpPoints = false;
        }
    }
}

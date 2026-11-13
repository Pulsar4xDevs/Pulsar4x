using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Events;

namespace Pulsar4X.Engine
{
    [Serializable]
    public class GameSettings
    {
        public const int MinimumTimestep = 5;

        #region General Game Settings

        public int MaxSystems { get; set; } = 1000;

        public WeightedList<int> StarChances { get; set; } = new WeightedList<int>();

        public DateTime StartDateTime { get; set; } = DateTime.Parse("2050-01-01T00:00:00");

        public int MasterSeed = 12345678;

        public Dictionary<EventType, bool> DefaultHaltOnEvents { get; set; } = new Dictionary<EventType, bool>()
        {
            { EventType.ResearchCompleted, true },
            { EventType.GeoSurveyCompleted, true },
            { EventType.JumpPointSurveyCompleted, true },
            { EventType.FuelExhausted, true },
            { EventType.PlantOutOfFuel, true },
            { EventType.PowerShortage, false },
            { EventType.WarpExited, true },
            { EventType.OrdersCompleted, false },
            { EventType.LifeSupportFailing, true },
        };

        public string CurrentTheme { get; set; } = "default-theme";

        #endregion

        #region Game Processing Settings

        public TimeSpan OrbitCycleTime { get; set; } = TimeSpan.FromHours(1); //this is currently not used here, need to re-implement

        public TimeSpan EconomyCycleTime { get; set; } = TimeSpan.FromDays(1); //this is currently not used here, need to re-implement

        public bool EnableMultiThreading { get; set; } = false;
        public bool EnforceSingleThread { get; set; } = false; //if above is false and this is true, everything will be done on the main thread, and the UI will wait for processes to finish before updating.

        #endregion

        #region warp settings

        /// <summary>
        /// when true, end of warp will put the entity on the same Newtonion trajectory that it had when it started warp.
        /// when false the ship will just get dropped into a circular orbit.
        /// </summary>
        public bool StrictNewtonion { get; set; } = true;
        /// <summary>
        /// when true ships saved Newtonion trajectory will be ralative to their immediate SOI parent,
        /// when false it will be ralitive to the root star (ie the absolute velocity).
        /// </summary>
        public bool UseRelativeVelocity { get; set; } = true;

        #endregion

        #region Economy (energy economy phase 5)

        /// <summary>
        /// Credits per kWh of embodied energy: the price anchor (Jon: 1 credit is roughly 1 kWh).
        /// Seed and ask prices are E x this x (1 + <see cref="PriceMarkup"/>).
        /// </summary>
        public double CreditsPerKWh { get; set; } = 1.0;

        /// <summary>
        /// Markup on embodied energy in seed and ask prices, standing in for labour, capital and
        /// scarcity (deferred). 4 makes energy a fifth of the price.
        /// </summary>
        public double PriceMarkup { get; set; } = 4.0;

        /// <summary>A seeded bid as a share of its ask (the depot's spread).</summary>
        public double SeedBidShare { get; set; } = 0.5;

        /// <summary>
        /// Delivery term of a depot's seed prices: the reference hauler's warp leg from the nearest
        /// plausible source, AU. 0.2 AU is the nearest belt neighbours of Ceres at the start (Sidonia
        /// 0.15, Princetonia 0.17, Miriam and Adeona 0.18 AU), where asteroid miners fetch ore.
        /// 0 prices goods produced at the depot itself (no delivery).
        /// </summary>
        public double DepotDeliveryDistance_AU { get; set; } = 0.2;

        /// <summary>The Sun's distance the delivery leg is flown at, AU, for the well's pull (Ceres' semi-major axis).</summary>
        public double DepotDeliverySunDistance_AU { get; set; } = 2.77;

        /// <summary>
        /// The delivery leg's heading against the outward line (cosine). 0: along the belt, as between
        /// neighbouring belt bodies at nearly the same distance from the Sun.
        /// </summary>
        public double DepotDeliveryRadial { get; set; } = 0.0;

        /// <summary>The hauler flies out empty as well as back full, so delivery is priced as a round trip.</summary>
        public bool DepotDeliveryRoundTrip { get; set; } = true;

        #endregion

        #region Crew life support and pay (crew plan phases 4-5)

        public const double DefaultShipLifeSupport_kWPerCrew = 1.0;
        public const double DefaultColonyGardenLifeSupport_kWPerPerson = 0.01;
        public const double DefaultColonySealedLifeSupport_kWPerPerson = 0.05;
        public const double DefaultColonyHostileLifeSupport_kWPerPersonPerCost = 0.6;
        public const decimal DefaultCrewWagePerDay = 100m;

        /// <summary>
        /// Ship life support per crew aboard at life-support tech 0, kW. A must-run load: it comes before
        /// warp and charging. The tech-life-support-efficiency factor multiplies it.
        /// </summary>
        public double ShipLifeSupport_kWPerCrew { get; set; } = DefaultShipLifeSupport_kWPerCrew;

        /// <summary>Life support per person on a garden world (colony cost 0), kW: the survival share of household power.</summary>
        public double ColonyGardenLifeSupport_kWPerPerson { get; set; } = DefaultColonyGardenLifeSupport_kWPerPerson;

        /// <summary>Base life support per person on a sealed world (colony cost above 0), kW, before the hostility term.</summary>
        public double ColonySealedLifeSupport_kWPerPerson { get; set; } = DefaultColonySealedLifeSupport_kWPerPerson;

        /// <summary>
        /// Extra life support per person per point of colony cost (capped at 3) on a sealed world, kW. The
        /// tech-life-support-efficiency factor multiplies it.
        /// </summary>
        public double ColonyHostileLifeSupport_kWPerPersonPerCost { get; set; } = DefaultColonyHostileLifeSupport_kWPerPersonPerCost;

        /// <summary>
        /// Crew pay, cr per crew per day: crew aboard the player's ships and trained crew in its pools.
        /// Also the wage shown on civilian candidates.
        /// </summary>
        public decimal CrewWagePerDay { get; set; } = DefaultCrewWagePerDay;

        #endregion

        #region Network Settings

        public int portNumber { get; set; }

        #endregion


        /// <summary>
        /// Enables orbital motion for Planets and Moons
        /// </summary>
        public bool? OrbitalMotionForPlanetsMoons { get; set; } = true;

        /// <summary>
        /// Enables orbital motion for asteroids.
        /// </summary>
        public bool? OrbitalMotionForAsteroids { get; set; } = true;

        /// <summary>
        /// Determines if all newly discovered JumpPoints will be stabilized.
        /// </summary>
        public bool? AllJumpPointsStabilized { get; set; } = false;



        #region Not Implemented in ECSLib

        /// <summary>
        /// Enables maintenance failures and overhaul mechanics.
        /// </summary>
        public bool? OverhaulsAndMaintenance { get; set; } = true;

        /// <summary>
        /// Enables political reliablity bonuses for commanders
        /// </summary>
        public bool? CommanderPoliticalReliablity { get; set; } = true;

        /// <summary>
        /// Enables inexperienced fleets having delayed orders
        /// </summary>
        public bool? TaskgroupTraining { get; set; } = true;

        /// <summary>
        /// "Difficulty Modifier" from Aurora.
        /// </summary>
        public float NPREconomyBonus { get; set; } = 1.0f;

        #endregion

        public GameSettings()
        {
            StarChances.Add(0.75, 1);
            StarChances.Add(0.125, 2);
            StarChances.Add(0.0625, 3);
            StarChances.Add(0.03125, 4);
            StarChances.Add(0.015625, 5);
        }
    }

    [Serializable]
    public class NewGameSettings : GameSettings
    {
        public const int DEFAULT_MAX_SYSTEMS = 100;
        public const int DEFAULT_NUM_SYSTEMS = 10;

        public string SMPassword { get; set; } = "";

        #region Player Generation

        /// <summary>
        /// False is equivilent to the "Create Spacemaster Empire" option.
        /// Sol will not be generated, and only the SM Player/Faction will be created.
        /// <para></para>
        /// Player will have to manually create a regular player faction.
        /// <para></para>
        /// If this is false, none of the other options in this region will work.
        /// </summary>
        public bool? CreatePlayerFaction { get; set; } = true;

        public string DefaultPlayerName { get; set; } = "Player 1";

        public string DefaultPlayerPassword { get; set; } = "";

        public string DefaultFactionName { get; set; } = "Terran Federation";

        /// <summary>
        /// if player start has a future End of Life Event asteroid impact.
        /// </summary>
        public bool EleStart { get; set; } = true;

        /// <summary>
        /// Defines if the default Sol Start will be used.
        /// Player faction can still be generated without generating Sol.
        /// </summary>
        public bool? DefaultSolStart { get; set; } = true;

        #endregion

        #region Not Implemented in ECSLib

        public int NumberOfStartingNPR { get; set; } = 1;

        public bool? StartingNPRAreConventional { get; set; } = false;

        #endregion
    }
}

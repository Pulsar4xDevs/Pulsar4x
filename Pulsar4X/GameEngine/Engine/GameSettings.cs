using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Events;

namespace Pulsar4X.Engine
{
    [Serializable]
    public class GameSettings
    {
        public const int MinimumTimestep = 5;

        #region General Game Settings

        public string GameName { get; set; } = "New Game";

        public int MaxSystems { get; set; } = 1000;

        public WeightedList<int> StarChances { get; set; } = new WeightedList<int>();

        public DateTime StartDateTime { get; set; } = DateTime.Parse("2050-01-01T00:00:00");

        public int MasterSeed = 12345678;

        public IEnumerable<string> DataSets { get; set; } = new List<string>();

        public Dictionary<EventType, bool> DefaultHaltOnEvents { get; set; } = new Dictionary<EventType, bool>()
        {
            { EventType.OrdersCompleted, true },
            { EventType.FuelExhausted, true }
        };

        public string CurrentTheme { get; set; } = "default-theme";

        #endregion

        #region Game Processing Settings

        public TimeSpan OrbitCycleTime { get; set; } = TimeSpan.FromHours(1); //this is currently not used here, need to re-implement

        public TimeSpan EconomyCycleTime { get; set; } = TimeSpan.FromDays(1); //this is currently not used here, need to re-implement

        public bool EnableMultiThreading { get; set; } = false;
        public bool EnforceSingleThread { get; set; } = false; //if above is false and this is true, everything will be done on the main thread, and the UI will wait for processes to finish before updating.

        public bool StrictNewtonion { get; set; } = false;


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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public static class GameConstants
    {
        public const int MinimumTimestep = 5;

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
            public const double SolarMassInKG = 1.98855E30;

            public const double EarthMassInKG = 5.97219E24;

            public const double SolMassInEarthMasses = 332946;

            public const double KmPerLightYear = 9460730472580.8;

            public const double AuPerLightYear = KmPerLightYear / KmPerAu;

            public const double KmPerAu = MetersPerAu / 1000;

            public const double MetersPerAu = 149597870700;  // this is exact, see: http://en.wikipedia.org/wiki/Astronomical_unit

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
        }
    }
    [Serializable]
    public class GameSettings
    {
        #region General Game Settings

        public string GameName { get; set; } = "New Game";

        public int MaxSystems { get; set; } = 1000;

        public DateTime StartDateTime { get; set; } = DateTime.Parse("2050-01-01T00:00:00");

        public VersionInfo Version => VersionInfo.PulsarVersionInfo;

        public int MasterSeed = 12345678;

        public IEnumerable<string> DataSets { get; set; } = new List<string>();

        public Dictionary<EventType, bool> DefaultHaltOnEvents { get; set; } = new Dictionary<EventType, bool>()
        {
            { EventType.OrdersCompleted, true },
            { EventType.FuelExhausted, true }
        };

        #endregion

        #region Game Processing Settings

        public TimeSpan OrbitCycleTime { get; set; } = TimeSpan.FromHours(1); //this is currently not used here, need to re-implement

        public TimeSpan EconomyCycleTime { get; set; } = TimeSpan.FromDays(1); //this is currently not used here, need to re-implement

        public bool EnableMultiThreading { get; set; } = false;



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
    }

    [Serializable]
    public class NewGameSettings : GameSettings
    {
        [JsonIgnore]
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
        [JsonIgnore]
        public bool? CreatePlayerFaction { get; set; } = true;

        [JsonIgnore]
        public string DefaultPlayerName { get; set; } = "Player 1";

        [JsonIgnore]
        public string DefaultPlayerPassword { get; set; } = "";

        [JsonIgnore]
        public string DefaultFactionName { get; set; } = "Terran Federation";

         
        /// <summary>
        /// Defines if the default Sol Start will be used.
        /// Player faction can still be generated without generating Sol.
        /// </summary>
        [JsonIgnore]
        public bool? DefaultSolStart { get; set; } = true;

        #endregion

        #region Not Implemented in ECSLib

        [JsonIgnore]
        public int NumberOfStartingNPR { get; set; } = 1;

        [JsonIgnore]
        public bool? StartingNPRAreConventional { get; set; } = false;

        #endregion
    }
}

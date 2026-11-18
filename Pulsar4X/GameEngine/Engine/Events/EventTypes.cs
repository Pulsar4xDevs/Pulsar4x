using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pulsar4X.Events
{
    /// <summary>
    /// Game-log event kinds. A plain enum: publish and subscribe match on equality, not a bitmask.
    /// Serialized by name so saved log entries stay readable if values change.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventType
    {
        #region Time
        GlobalDateChange,
        #endregion

        #region Research
        ResearchCompleted,
        TechnologyQueued,
        TechnologyRemovedFromQueue,
        TechnologyMovedInQueue,
        TechnologyFundingChanged,
        TechnologyLabScientistAssigned,
        TechnologyLabScientistUnassigned,
        ScientistAssignedToLab,
        ScientistUnassignedFromLab,
        #endregion

        #region Surveys and exploration
        GeoSurveyCompleted,
        JumpPointSurveyCompleted,
        JumpPointFound,
        NewSystemDiscovered,
        MineralsLocated,
        #endregion

        #region Orders and movement
        OrdersHalt,
        OrdersCompleted,
        WarpExited,
        #endregion

        #region Economy and colonies
        ProductionCompleted,
        ShipRefitBegan,
        ShipRefitCompleted,
        FuelExhausted,
        PopulationChanged,
        ColonyCreated,
        PowerShortage,
        PlantOutOfFuel,
        #endregion

        #region Combat
        TargetHit,
        TargetMissed,
        TargetDestroyed,
        ShipDestroyed,
        CrewLosses,
        #endregion

        #region People
        OfficerPromoted,
        ColonyAdministratorAssigned,
        ColonyAdministratorUnassigned,
        AdministratorAssignedToColony,
        AdministratorUnassignedFromColony,
        #endregion

        #region Movement and orbit
        SOIChanged,
        #endregion

        #region Engine
        EngineError,
        #endregion

        #region Crew (appended so saved ordinals stay put)
        CrewGraduated,
        ShipUnderCrewed,
        /// <summary>A ship's batteries ran empty while its plant cannot carry its life support (crew plan phase 4).</summary>
        LifeSupportFailing,
        #endregion

        #region Market (appended so saved ordinals stay put)
        /// <summary>A same-body market trade settled instantly (multi-faction phase 4 auto-clearing included).</summary>
        MarketTradeSettled,
        #endregion

        #region Colony needs (appended so saved ordinals stay put)
        /// <summary>Food or water stock is below 30 days, or a shortage stage was reached (multi-faction phase 7a).</summary>
        SupplyWarning,
        /// <summary>A colony's food or water ran short today (day 0 of the shortage clock).</summary>
        SupplyShortage,
        /// <summary>People died of a long food or water shortage.</summary>
        PopulationStarved,
        /// <summary>People left a colony in shortage (the −1 %/month decline).</summary>
        PopulationLeft,
        #endregion

        #region Budget (appended so saved ordinals stay put)
        /// <summary>A month's wages, research and staff billing was settled (monthly billing, "budget day").</summary>
        BudgetDay,
        #endregion

        #region Crew market (appended so saved ordinals stay put)
        /// <summary>Trained crew bought on a crew market arrived (multi-faction phase 8).</summary>
        CrewTransferred,
        #endregion

        #region Runtime NPC companies (appended so saved ordinals stay put)
        /// <summary>A bloc chartered a runtime NPC company (multi-faction phase 9).</summary>
        NpcCompanySpawned,
        /// <summary>A runtime NPC company folded back into its sponsor (multi-faction phase 9).</summary>
        NpcCompanyFolded,
        #endregion

        #region Refit (appended so saved ordinals stay put)
        /// <summary>A refit unloaded the overflow of a hold it is removing to the yard colony.</summary>
        ShipRefitUnloaded,
        #endregion
    }
}

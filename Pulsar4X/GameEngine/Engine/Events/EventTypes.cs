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
    }
}

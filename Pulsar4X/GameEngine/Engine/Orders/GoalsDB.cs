using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace GameEngine.Engine.Orders;

/// <summary>
/// Holds goals for an entity (ship, commander, fleet, etc.)
/// </summary>
public class GoalsDB : BaseDataBlob
{
    // Immutable base weights
    public static readonly IReadOnlyDictionary<GoalType, float> BaseWeights = new Dictionary<GoalType, float>
    {
        [GoalType.StayAlive]        = 1.0f,
        [GoalType.DontRunOutOfFuel] = 0.9f,
        [GoalType.MakeProfit]       = 0.7f,
        [GoalType.ExploreJP]        = 0.5f,
        [GoalType.SurveySystem]     = 0.5f,
        [GoalType.ScanBodies]       = 0.5f,
        [GoalType.ScanAnomalies]    = 0.5f,
        [GoalType.Mine]             = 0.5f,
        [GoalType.Colonise]         = 0.5f,
        [GoalType.ListeningPost]    = 0.5f,
        [GoalType.Freighter]        = 0.5f,
        [GoalType.Trade]            = 0.5f,
        [GoalType.Scout]            = 0.5f,
        [GoalType.Patrol]           = 0.5f,
        [GoalType.Attack]           = 0.5f,
        [GoalType.Defend]           = 0.5f,
        [GoalType.Intercept]        = 0.5f,
        [GoalType.Blockade]         = 0.5f,
    };

    public Goal? ParentGoal { get; set; }
    public Goal? GivenGoal { get; set; }        // Order received from superior
    public Goal? ActiveGoal { get; set; }       // Currently pursuing

    // Modifier layers (multiplied together)
    public Dictionary<GoalType, float> PersonalityModifiers { get; } = new();
    public Dictionary<GoalType, float> CapabilityModifiers { get; } = new();   // -1 = impossible
    public Dictionary<GoalType, float> OrdersModifiers { get; } = new();       // from superior

    // Computed effective goals
    public Dictionary<GoalType, Goal> EffectiveGoals { get; } = new();

    public override object Clone()
    {
        // Shallow copy of the concrete goals is enough for now; the modifier
        // dictionaries are rebuilt each pass by the AgentProcessor.
        return new GoalsDB
        {
            ParentGoal = ParentGoal,
            GivenGoal  = GivenGoal,
            ActiveGoal = ActiveGoal,
        };
    }
}

public enum GoalType
{
    StayAlive,
    DontRunOutOfFuel,
    MakeProfit,

    // Explorative
    ExploreJP,
    SurveySystem,
    ScanBodies,
    ScanAnomalies,

    // Exploitative
    Mine,
    Colonise,
    ListeningPost,
    Freighter,
    Trade,

    // Military
    Scout,
    Patrol,
    Attack,
    Defend,
    Intercept,
    Blockade,

    // Generic
    MoveTo,
    RefuelAt,
    RearmAt,
    RepairAt,
}

public class Goal
{
    /// <summary>Stable id so spawned actions / child goals can reference this one.</summary>
    public string Id = Guid.NewGuid().ToString();
    /// <summary>Id of the goal (one level up) that produced this one ("" if top-level).</summary>
    public string ParentGoalId = "";
    public GoalType Type;
    public int TargetEntityID = -1;
    public float Weight = 0.5f;
    public GoalStatus Status = GoalStatus.Pending;
    public string Message = "";
}

public enum GoalStatus
{
    Pending,
    Active,
    Completed,
    Holding,
    Failing,
    Failed,
}
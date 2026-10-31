using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Api;
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
        [GoalType.HelpOwn]          = 0.8f,
        [GoalType.HelpAllied]       = 0.75f,
        [GoalType.HelpFrendly]      = 0.74f,
        [GoalType.HelpNeutral]      = 0.73f,
        [GoalType.ExploreJP]        = 0.5f,
        [GoalType.SurveySystem]     = 0.5f,
        [GoalType.ServeyBodies]     = 0.5f,
        [GoalType.ScanAnomalies]    = 0.5f,
        [GoalType.Mine]             = 0.5f,
        [GoalType.Colonise]         = 0.5f,
        [GoalType.ListeningPost]    = 0.5f,
        [GoalType.Freighter]        = 0.5f,
        [GoalType.Trade]            = 0.5f,
        [GoalType.RunMarket]        = 0.5f,
        [GoalType.Scout]            = 0.5f,
        [GoalType.Patrol]           = 0.5f,
        [GoalType.Attack]           = 0.5f,
        [GoalType.Defend]           = 0.5f,
        [GoalType.Intercept]        = 0.5f,
        [GoalType.Blockade]         = 0.5f,
        [GoalType.Stealth]           = 0.0f,
    };

    // The order itself. The modifier dictionaries below are rebuilt each agent pass and are not saved.
    public Goal? ActiveGoal { get; set; }
    public Goal? GivenGoal { get; set; }

    [JsonIgnore]
    public Dictionary<GoalType, float> PersonalityModifiers { get; } = new();
    [JsonIgnore]
    public Dictionary<GoalType, float> CapabilityModifiers { get; } = new();   // -1 = impossible
    [JsonIgnore]
    public Dictionary<GoalType, float> OrdersModifiers { get; } = new();       // from superior

    [JsonIgnore]
    public Dictionary<GoalType, Goal> EffectiveGoals { get; } = new();

    public override object Clone()
    {
        // Shallow copy of the concrete goals is enough for now; the modifier
        // dictionaries are rebuilt each pass by the AgentProcessor.
        return new GoalsDB
        {
            ActiveGoal  = ActiveGoal,
        };
    }
}

public enum GoalType
{
    StayAlive,
    DontRunOutOfFuel,
    MakeProfit,
    HelpOwn,
    HelpAllied,
    HelpFrendly,
    HelpNeutral,

    // Explorative
    ExploreJP,
    SurveySystem,
    ServeyBodies,
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
    Stealth,

    // Generic
    MoveTo,
    RefuelAt,
    RearmAt,
    RepairAt,

    // Colony logistics office. Appended so existing saved enum values stay put.
    RunMarket,

    // Fleet parcel of the one-ship trade and freight goals. Appended so saved values stay put.
    FleetTrade,
    FleetFreighter,

    // One colony handing RunMarket to offices its bridge can see. Appended so saved values stay put.
    SupplyLocal,

    // Standing colony job: list warehouse stock. Appended so saved values stay put.
    OfferStock,

    // One refining batch, called by Offer stock. Appended so saved values stay put.
    RunIndustry,

    // Ship mines surveyed asteroids and hauls the ore home. Appended so saved values stay put.
    MineAsteroids,

    // Fleet jobs that stay active and hand the next child. Appended so saved values stay put.
    SurveyStanding,
    MineStanding,

    // Ship buys methalox at a listed ask. Appended so saved values stay put.
    BuyFuel,

    // One trip of this faction's own warehouse goods. Appended so saved values stay put.
    MoveGoods,
    FleetMoveGoods,
}

public enum MoveGoodsLeg
{
    Load,
    Unload,
}

public class MoveGoodsLine
{
    public int ItemId;
    public long Requested;
    public long Loaded;
    /// <summary>Units handed to the destination. The rest of <see cref="Loaded"/> stays aboard.</summary>
    public long Delivered;
}

public class Goal
{
    /// <summary>Stable id so spawned actions / child goals can reference this one.</summary>
    public string Id = Guid.NewGuid().ToString();
    /// <summary>Id of the goal (one level up) that produced this one ("" if top-level).</summary>
    public string ParentGoalId = "";
    public GoalType Type;
    public int TargetEntityID = -1;
    /// <summary>Trade and freight cargo id. Empty until a planner commits a route.</summary>
    public string CargoId = "";
    /// <summary>Move goods. Empty for every other goal.</summary>
    public List<MoveGoodsLine> MoveGoodsLines = new();
    /// <summary>Move goods. Load, then Unload. Other goals leave this at Load.</summary>
    public MoveGoodsLeg MoveGoodsLeg = MoveGoodsLeg.Load;
    /// <summary>The unload transfer was handed off. The next wake completes the trip.</summary>
    public bool MoveGoodsUnloaded;
    /// <summary>Where to buy, or the freight source. -1 until a route is committed.</summary>
    public int SourceEntityId = -1;
    /// <summary>Where to sell, or the freight destination. -1 until a route is committed.</summary>
    public int DestEntityId = -1;
    /// <summary>Fleet load cap for one child trip. 0 means the ship decides the size.</summary>
    public long UnitShare = 0;
    /// <summary>Colony supply. Run hands out markets. Balance and Stockpile also edit policy.</summary>
    public SupplyMode SupplyMode = SupplyMode.Run;
    public float Weight = 0.5f;
    public string Name = "";
    public GoalStatus Status = GoalStatus.Planning;
    public string Message = "";
    /// <summary>The player cancelled this order. The agent does not plan it again.</summary>
    public bool PlayerCancelled;
    /// <summary>The entity that handed this goal down. -1 when the player gave it.</summary>
    public int IssuerEntityId = -1;
    /// <summary>Warp-brake bubbles for this goal. Set once when the sequence starts. An old save stays 0.</summary>
    public int BrakeSteps;
    
    public Goal(GoalType type)
    {
        Type = type;
        Name = Enum.GetName(typeof(GoalType), type);
    }
    
}

public enum GoalStatus
{
    Planning,
    Active,
    Completed,
    Holding,
    Failing,
    Failed,
}
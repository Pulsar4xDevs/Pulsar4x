using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.JumpPoints;
using Pulsar4X.Messaging;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.Sensors;
using Pulsar4X.Ships;

namespace GameEngine.Engine.Orders;

interface IGoalPlanner
{
    GoalType Type { get; } 
    
    PlanResult Plan (Entity managedEntity, Goal goal);
    
}

public readonly struct PlanResult
{
    public GoalStatus Status { get; init; }  // Active = “go ahead”, Completed/Failed = terminal
    public string Message { get; init; }
    public IReadOnlyList<EntityAction> Actions { get; init; }
    public IReadOnlyList<(Entity Sub, Goal Goal)> SubGoals { get; init; }

    public static PlanResult Continue(params EntityAction[] actions) => new()
    {
        Status = GoalStatus.Active,
        Actions = actions,
        SubGoals = Array.Empty<(Entity, Goal)>(),
        Message = "",
    };
    public static PlanResult Continue(List<EntityAction> actions) => new()
    {
        Status = GoalStatus.Active,
        Actions = actions,
        SubGoals = Array.Empty<(Entity, Goal)>(),
        Message = "",
    };
    
    public static PlanResult Continue(List<(Entity subordinate, Goal goal)> subgoals) => new()
    {
        Status = GoalStatus.Active,
        Actions = Array.Empty<EntityAction>(),
        SubGoals = subgoals,
        Message = "",
    };

    public static PlanResult Done(string message = "") => new()
    {
        Status = GoalStatus.Completed,
        Message = message,
        Actions = Array.Empty<EntityAction>(),
        SubGoals = Array.Empty<(Entity, Goal)>(),
    };

    public static PlanResult Fail(string message) => new()
    {
        Status = GoalStatus.Failed,
        Message = message,
        Actions = Array.Empty<EntityAction>(),
        SubGoals = Array.Empty<(Entity, Goal)>(),
    };
}



public class AgentProcessor : IInstanceProcessor
{
    // How often an agent with an active goal wakes to re-check progress / re-plan.
    private static readonly TimeSpan RecheckInterval = TimeSpan.FromMinutes(30);

    // Time to hand a goal down one echelon. Placeholder: should scale with administrator skill,
    // staff size (AdminSpaceAtb.ConsoleSpace) and subordinate count.
    private static readonly TimeSpan RelayDelay = TimeSpan.FromSeconds(1);

    // One planner per goal type: adding a goal type == adding an IGoalToActionsPlanner, no switch.
    private static readonly Dictionary<GoalType, IGoalPlanner> _planners;

    static AgentProcessor()
    {
        _planners = FindPlanners();
    }


    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        ProcessEntityStatic(entity, atDateTime);
    }

    public static void ProcessEntityStatic(Entity entity, DateTime atDateTime)
    {
        //check if the entity given is a commander, or if it's an entity a commander is managing. 
        Entity managedEntity = entity;
        Entity agentHost = entity;
        if (entity.TryGetDataBlob<CommanderDB>(out var commanderDB) && commanderDB.AssignedTo != -1)
        {
            if (!entity.Manager.TryGetGlobalEntityById(commanderDB.AssignedTo, out managedEntity))
                return;
        }

        if (!managedEntity.TryGetDataBlob<GoalsDB>(out var goalsDB))
            return; // nothing tasked

        // Orient: AgentDB is optional (personality defaults inside GoalWeighting).
        agentHost.TryGetDataBlob<AgentDB>(out var agentDB);
        GoalWeighting.Recalculate(goalsDB, managedEntity, agentDB);

        var goal = goalsDB.ActiveGoal;
        if (goal == null) return; // autonomous pick not wired yet
        if (goal.Status is GoalStatus.Completed or GoalStatus.Failed) return;

        bool isFleet = managedEntity.HasDataBlob<FleetDB>();
        bool isShip = managedEntity.HasDataBlob<ShipInfoDB>();

        switch (goal.Status)
        {
            case GoalStatus.Planning:
            {
                if (!_planners.TryGetValue(goal.Type, out var planner))
                {
                    Fail(goal, $"no planner for {goal.Type}");
                    break;
                }

                // Planners return data only; agent commits status and side effects.
                // PlanResult.Continue → Status.Active; Done → Completed; Fail → Failed.
                var plan = planner.Plan(managedEntity, goal);
                goal.Message = plan.Message ?? "";

                if (plan.Status is GoalStatus.Failed or GoalStatus.Completed)
                {
                    goal.Status = plan.Status;
                    break;
                }

                // Continue: enqueue work, then mark Active.
                foreach (var (subordinate, subGoal) in plan.SubGoals)
                {
                    if (string.IsNullOrEmpty(subGoal.ParentGoalId))
                        subGoal.ParentGoalId = goal.Id;
                    AssignGoal(subordinate, subGoal, atDateTime + RelayDelay);
                }

                foreach (var action in plan.Actions)
                {
                    action.ParentGoalId = goal.Id;
                    managedEntity.Manager.Game.OrderHandler.HandleOrder(action);
                }

                goal.Status = GoalStatus.Active;
                ScheduleAgent(agentHost, atDateTime + RecheckInterval);
                break;
            }

            case GoalStatus.Active:
            {
                if (isFleet)
                {
                    // Optional top-up: re-plan so free ships get remaining POIs (survey, etc.).
                    if (_planners.TryGetValue(goal.Type, out var planner))
                    {
                        var plan = planner.Plan(managedEntity, goal);
                        goal.Message = plan.Message ?? goal.Message;
                        if (plan.Status is GoalStatus.Failed or GoalStatus.Completed)
                        {
                            goal.Status = plan.Status;
                            break;
                        }
                        foreach (var (subordinate, subGoal) in plan.SubGoals)
                        {
                            if (string.IsNullOrEmpty(subGoal.ParentGoalId))
                                subGoal.ParentGoalId = goal.Id;
                            AssignGoal(subordinate, subGoal, atDateTime + RelayDelay);
                        }
                    }

                    // TODO: cancel-propagation to siblings on failure.
                    var mine = SubGoalsOf(managedEntity, goal);
                    if (mine.Count > 0 && mine.All(g => g.Status == GoalStatus.Completed))
                        goal.Status = GoalStatus.Completed;
                    else if (mine.Any(g => g.Status == GoalStatus.Failed))
                        Fail(goal, "a subordinate's goal failed");
                    else
                        ScheduleAgent(agentHost, atDateTime + RecheckInterval);
                    break;
                }

                if (isShip)
                {
                    if (!managedEntity.TryGetDataBlob<ActionQueueDB>(out var queue))
                        break;

                    var actions = queue.ActionsFor(goal);

                    if (actions.Any(a => a.Status == ActionStatus.Failed))
                    {
                        Fail(goal, "an action failed");
                        queue.ClearFor(goal);
                    }
                    else
                    {
                        queue.ActionList.RemoveAll(a =>
                            a.ParentGoalId == goal.Id && a.Status == ActionStatus.Succeeded);

                        if (!queue.ActionsFor(goal).Any())
                            goal.Status = GoalStatus.Completed;
                        else
                            ScheduleAgent(agentHost, atDateTime + RecheckInterval);
                    }
                    break;
                }

                Fail(goal, "managed entity is not a ship or a fleet");
                break;
            }
        }
        MessagePublisher.Instance.Publish(
            Message.Create(
                MessageTypes.OrdersChanged,
                entity.Id,
                entity.Manager.ManagerID,
                entity.FactionOwnerID));
    }
    
    // -----------------------------------------------------------------
    // helpers
    // -----------------------------------------------------------------

    
    /// <summary>
    /// finds planners via reflection so we don't have to manualy add them.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    static Dictionary<GoalType, IGoalPlanner> FindPlanners()
    {
        var planners = new Dictionary<GoalType, IGoalPlanner>();

        var plannerTypes = AppDomain.CurrentDomain.GetAssemblies()
                                    .SelectMany(a =>
                                    {
                                        try { return a.GetTypes(); }
                                        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
                                    })
                                    .Where(t => t is { IsClass: true, IsAbstract: false })
                                    .Where(t => typeof(IGoalPlanner).IsAssignableFrom(t))
                                    .Where(t => t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in plannerTypes)
        {
            var planner = (IGoalPlanner)Activator.CreateInstance(type)!;

            if (planners.TryGetValue(planner.Type, out var existing))
            {
                throw new InvalidOperationException(
                    $"Duplicate IGoalPlanner for {planner.Type}: {existing.GetType().Name} and {type.Name}. " +
                    "One planner per GoalType (fleet+ship in the same class), or split GoalTypes.");
            }

            planners[planner.Type] = planner;
        }

        return planners;
    }
    

    /// <summary>
    /// The one place a goal is given to a unit: records it and wakes that unit's agent. Planners
    /// yield sub-goals and let this do the handing over, so the assignment rules live here rather
    /// than being re-implemented per planner.
    /// </summary>
    /// <param name="when">
    /// When the unit should start planning. Null means "now, synchronously" — only the player/AI
    /// command seam should do that, so a new order is acknowledged immediately. Goals handed down an
    /// echelon pass a time and pay <see cref="RelayDelay"/>.
    /// </param>
    internal static void AssignGoal(Entity unit, Goal goal, DateTime? when = null)
    {
        if (!unit.TryGetDataBlob<GoalsDB>(out var goals))
        {
            goals = new GoalsDB();
            unit.SetDataBlob(goals);
        }

        // Ordered intent + current work. Interrupt later can change Active only.
        goals.GivenGoal = goal;
        goals.ActiveGoal = goal;
        if (goal.Status != GoalStatus.Planning
            && goal.Status is not (GoalStatus.Completed or GoalStatus.Failed))
            goal.Status = GoalStatus.Planning;

        if (when == null)
            RunAgentNow(unit);
        else
            ScheduleAgent(unit, when.Value);
    }

    /// <summary>
    /// This agent's work items: the sub-goals its subordinates are currently holding on behalf of
    /// <paramref name="goal"/>. Counterpart to <see cref="ActionQueueDB.ActionsFor(Goal)"/>.
    ///
    /// A subordinate's GivenGoal is the same Goal instance we handed it, so its status changes are
    /// visible here with no further plumbing.
    /// </summary>
    static List<Goal> SubGoalsOf(Entity fleet, Goal goal)
    {
        var subGoals = new List<Goal>();
        if (!fleet.TryGetDataBlob<FleetDB>(out var fleetDB)) return subGoals;

        foreach (var child in fleetDB.Children)
        {
            if (child.TryGetDataBlob<GoalsDB>(out var childGoals)
                && childGoals.ActiveGoal != null
                && childGoals.ActiveGoal.ParentGoalId == goal.Id)
            {
                subGoals.Add(childGoals.ActiveGoal);
            }
        }

        return subGoals;
    }

    static void Fail(Goal goal, string message)
    {
        goal.Status = GoalStatus.Failed;
        goal.Message = message;
    }

    internal static void ScheduleAgent(Entity unit, DateTime when)
    {
        unit.Manager.ManagerSubpulses.AddEntityInterupt(when, nameof(AgentProcessor), unit);
    }
    internal static void RunAgentNow(Entity unit)
    {
        var timenow = unit.StarSysDateTime;
        ProcessEntityStatic( unit, timenow);
    }

    // ===================================================================
    // Weighting helpers (kept from the utility-AI sketch; not yet used by the
    // concrete MoveTo path — they drive autonomous goal *selection*, which is
    // the next layer to wire in).
    // ===================================================================

    public static void PruneImpossibleGoals(GoalsDB goalsDB, Entity entity)
    {
        // Populate CapabilityModifiers with -1 where impossible
        goalsDB.CapabilityModifiers.Clear();

        foreach (var type in GoalsDB.BaseWeights.Keys)
        {
            bool canDo = type switch
            {
                GoalType.StayAlive or GoalType.DontRunOutOfFuel => true,
                GoalType.ExploreJP => entity.HasOrChildHasAbility<SensorAbilityDB>(),
                GoalType.SurveySystem or GoalType.ServeyBodies => entity.HasOrChildHasAbility<GeoSurveyAbilityDB>(),
                GoalType.ScanAnomalies => entity.HasOrChildHasAbility<JPSurveyAbilityDB>(),
                GoalType.Mine => entity.HasOrChildHasAbility<MiningDB>(),
                GoalType.ListeningPost => entity.HasOrChildHasAbility<SensorAbilityDB>(),
                GoalType.Scout => entity.HasOrChildHasAbility<SensorAbilityDB>(),
                GoalType.MakeProfit or GoalType.Freighter or GoalType.Trade => false, // TODO
                GoalType.Colonise => false,
                _ => false
            };

            if (!canDo)
                goalsDB.CapabilityModifiers[type] = -1f;
        }
    }

    public static void RecalculateEffectiveGoals(GoalsDB goalsDB, AgentDB? agentDB)
    {
        goalsDB.EffectiveGoals.Clear();

        foreach (var kvp in GoalsDB.BaseWeights)
        {
            GoalType type = kvp.Key;
            float baseWeight = kvp.Value;

            float multiplier = 1.0f;

            // Capability layer (pruning)
            multiplier *= GetModifier(goalsDB.CapabilityModifiers, type);

            // Personality layer
            if (agentDB != null)
                multiplier *= GetPersonalityMultiplier(agentDB, type);

            // Orders from superior
            multiplier *= GetModifier(goalsDB.OrdersModifiers, type);

            float finalWeight = baseWeight * multiplier;

            goalsDB.EffectiveGoals[type] = new Goal(type)
            {
                Weight = finalWeight
            };
        }
    }

    private static float GetModifier(Dictionary<GoalType, float> modifiers, GoalType type)
    {
        return modifiers.TryGetValue(type, out var value) ? value : 1.0f;
    }

    private static float GetPersonalityMultiplier(AgentDB agent, GoalType type)
    {
        return type switch
        {
            GoalType.StayAlive or GoalType.DontRunOutOfFuel => 1.0f * agent.Caution,
            GoalType.ExploreJP or GoalType.SurveySystem or GoalType.ScanAnomalies => 1.0f * agent.Curiosity,
            GoalType.MakeProfit or GoalType.Trade or GoalType.Freighter => 1.0f * agent.Greed,
            GoalType.Attack or GoalType.Patrol or GoalType.Scout => 1.0f * agent.Aggression,
            _ => 1.0f
        };
    }

    // Example commander assignment
    public static void AssignGoalFromSuperior(GoalsDB superiorGoals, GoalsDB subordinateGoals, GoalType goalType, float strength = 2.0f)
    {
        subordinateGoals.OrdersModifiers[goalType] = strength;
        // Optionally propagate alignment to related goals...
    }
    
    
}

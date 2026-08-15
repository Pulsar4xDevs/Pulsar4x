using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace GameEngine.Engine.Orders;

/// <summary>
/// Orient step: build effective goal weights from base × capability × personality × orders × context.
/// Drives (e.g. DontRunOutOfFuel) do not become GivenGoals; they push task weights (RefuelAt).
/// </summary>
public static class GoalWeighting
{
    /// <summary>
    /// Context multipliers for this unit right now. Keyed by the *task* they boost.
    /// </summary>
    public static Dictionary<GoalType, float> BuildContextModifiers(Entity unit, AgentDB? agent)
    {
        var ctx = new Dictionary<GoalType, float>();

        float caution = agent?.Caution ?? 1f;
        var fuel = FuelSituation.Observe(unit);
        float refuelMul = FuelSituation.RefuelContextMultiplier(fuel, caution);
        if (refuelMul > 0)
            ctx[GoalType.RefuelAt] = refuelMul;

        // Drive DontRunOutOfFuel is represented by the RefuelAt context score, not a separate plan.

        return ctx;
    }

    /// <summary>
    /// Full Orient pass into <see cref="GoalsDB.EffectiveGoals"/>.
    /// </summary>
    public static void Recalculate(GoalsDB goalsDB, Entity unit, AgentDB? agentDB)
    {
        AgentProcessor.PruneImpossibleGoals(goalsDB, unit);

        // Refuel is always "possible" if we have a fuel system and storage; mark capability.
        if (FuelSituation.TryGetFuelMass(unit, out _, out _, out _))
            goalsDB.CapabilityModifiers.Remove(GoalType.RefuelAt);
        else
            goalsDB.CapabilityModifiers[GoalType.RefuelAt] = -1f;

        var context = BuildContextModifiers(unit, agentDB);

        goalsDB.EffectiveGoals.Clear();

        // Tasks that participate in autonomous selection: BaseWeights + RefuelAt (task, low base).
        foreach (var kvp in GoalsDB.BaseWeights)
            WriteEffective(goalsDB, kvp.Key, kvp.Value, agentDB, context);

        // RefuelAt is a task not in BaseWeights (player/fleet issued + context-driven).
        if (!goalsDB.EffectiveGoals.ContainsKey(GoalType.RefuelAt))
            WriteEffective(goalsDB, GoalType.RefuelAt, baseWeight: 0.2f, agentDB, context);
    }

    static void WriteEffective(
        GoalsDB goalsDB,
        GoalType type,
        float baseWeight,
        AgentDB? agentDB,
        Dictionary<GoalType, float> context)
    {
        float multiplier = 1f;

        if (goalsDB.CapabilityModifiers.TryGetValue(type, out var cap))
            multiplier *= cap;
        if (multiplier < 0)
        {
            goalsDB.EffectiveGoals[type] = new Goal(type) { Weight = -1f };
            return;
        }

        if (agentDB != null)
            multiplier *= PersonalityMultiplier(agentDB, type);

        if (goalsDB.OrdersModifiers.TryGetValue(type, out var orders))
            multiplier *= orders;

        if (context.TryGetValue(type, out var ctx))
            multiplier *= ctx;

        goalsDB.EffectiveGoals[type] = new Goal(type)
        {
            Weight = baseWeight * multiplier,
        };
    }

    static float PersonalityMultiplier(AgentDB agent, GoalType type) => type switch
    {
        GoalType.StayAlive or GoalType.DontRunOutOfFuel or GoalType.RefuelAt
            => 1f * agent.Caution,
        GoalType.ExploreJP or GoalType.SurveySystem or GoalType.ServeyBodies or GoalType.ScanAnomalies
            => 1f * agent.Curiosity,
        GoalType.MakeProfit or GoalType.Trade or GoalType.Freighter
            => 1f * agent.Greed,
        GoalType.Attack or GoalType.Patrol or GoalType.Scout
            => 1f * agent.Aggression,
        _ => 1f,
    };

    /// <summary>
    /// Autonomous Decide: highest-weight *task* with weight &gt; 0, or null if nothing worth doing.
    /// Does not override an existing non-terminal GivenGoal — caller policy decides interrupts.
    /// </summary>
    public static Goal? PickAutonomousTask(GoalsDB goalsDB)
    {
        Goal? best = null;
        foreach (var g in goalsDB.EffectiveGoals.Values)
        {
            if (!GoalRoles.IsTask(g.Type))
                continue;
            if (g.Weight <= 0)
                continue;
            if (best == null || g.Weight > best.Weight)
                best = g;
        }
        return best;
    }

    /// <summary>
    /// True when RefuelAt context urgency should interrupt a non-refuel GivenGoal.
    /// Threshold is soft; Caution lowers the bar (multiplied into context already).
    /// </summary>
    public static bool ShouldInterruptForRefuel(GoalsDB goalsDB, Goal? current)
    {
        if (current == null)
            return false;
        if (current.Type == GoalType.RefuelAt)
            return false;
        if (!goalsDB.EffectiveGoals.TryGetValue(GoalType.RefuelAt, out var refuel))
            return false;
        // Interrupt when refuel outranks the current task's effective weight and is clearly urgent.
        float currentW = goalsDB.EffectiveGoals.TryGetValue(current.Type, out var cg) ? cg.Weight : 0.5f;
        return refuel.Weight >= 2.0f && refuel.Weight > currentW;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.Sensors;
using Pulsar4X.Ships;

namespace GameEngine.Engine.Orders;

public class AgentProcessor : IInstanceProcessor
{
    // How often an agent with an active goal wakes to re-check progress / re-plan.
    private static readonly TimeSpan RecheckInterval = TimeSpan.FromMinutes(30);

    // One planner per goal type: adding a goal type == adding an IGoalPlanner, no switch.
    private static readonly Dictionary<GoalType, IGoalPlanner> _planners;

    static AgentProcessor()
    {
        _planners = new Dictionary<GoalType, IGoalPlanner>();
        foreach (var planner in new IGoalPlanner[]
                 {
                     new MoveToPlan(),
                     new ScanBodiesPlan(),
                 })
        {
            _planners[planner.Type] = planner;
        }
    }


    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        ProcessEntityStatic(entity, atDateTime);
    }

    public static void ProcessEntityStatic(Entity entity, DateTime atDateTime)
    {
        //check if the entity given is a commander, or if it's an entity a commander is... commanding. 
        Entity managedEntity = entity;
        if (entity.TryGetDataBlob<CommanderDB>(out var commanderDB) && commanderDB.AssignedTo != -1)
        {
            if (!entity.Manager.TryGetGlobalEntityById(commanderDB.AssignedTo, out managedEntity))
                return;
        }

        if (!managedEntity.TryGetDataBlob<GoalsDB>(out var goalsDB))
            return; // nothing tasked
        
        if (managedEntity.HasDataBlob<FleetDB>())
            GoalsProcessor(entity, goalsDB, managedEntity, atDateTime);
        else if (managedEntity.HasDataBlob<ShipInfoDB>())
            ActionsProcessor(entity, goalsDB, managedEntity, atDateTime);
    }

    // -----------------------------------------------------------------
    // BRANCH: a fleet's concrete goal is handed down as a sub-goal to each
    // capable child; each child is itself an agent that plans on its own wake.
    //
    // We decompose the *concrete* GivenGoal (it carries a target). The abstract
    // weighted EffectiveGoals are NOT used here: they carry no target and are
    // regenerated with fresh ids every pass, so they can't anchor queued work.
    // Autonomous goal *selection* layers on top of this later.
    // -----------------------------------------------------------------
    static void GoalsProcessor(Entity agentHost, GoalsDB goalsDB, Entity fleet, DateTime atDateTime)
    {
        var goal = goalsDB.GivenGoal;
        if (goal == null || goal.Type != GoalType.MoveTo) return;
        if (goal.Status is GoalStatus.Completed or GoalStatus.Failed) return;

        goal.Status = GoalStatus.Active;

        if (!fleet.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out var target))
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "MoveTo target not found";
            return;
        }

        var fleetDB = fleet.GetDataBlob<FleetDB>();
        bool anyAssigned = false;
        bool allDone = true;

        foreach (var child in fleetDB.Children.ToArray())
        {
            // A ship must be able to move itself somehow; sub-fleets decompose themselves.
            // Which *kind* of move is MovePlanner's call, not ours.
            if (child.HasDataBlob<ShipInfoDB>() && !MovePlanner.CanMove(child, out _))
                continue;

            var childGoals = GetOrCreateGoals(child);
            var childGoal = childGoals.GivenGoal;

            // Assign once, then leave it alone (idempotent across re-checks).
            if (childGoal == null || childGoal.ParentGoalId != goal.Id)
            {
                childGoal = new Goal
                {
                    Type = GoalType.MoveTo,
                    TargetEntityID = target.Id,
                    ParentGoalId = goal.Id,
                    Status = GoalStatus.Pending,
                };
                childGoals.GivenGoal = childGoal;
                ScheduleAgent(child, atDateTime + TimeSpan.FromSeconds(1));
            }

            anyAssigned = true;
            if (childGoal.Status != GoalStatus.Completed)
                allDone = false;
        }

        if (anyAssigned && allDone)
            goal.Status = GoalStatus.Completed;
        else
            ScheduleAgent(agentHost, atDateTime + RecheckInterval);
    }

    // -----------------------------------------------------------------
    // LEAF: a ship's concrete goal is turned into actions by the planner for
    // that goal type, submitted to the ship's ActionQueue, then monitored.
    // -----------------------------------------------------------------
    static void ActionsProcessor(Entity agentHost, GoalsDB goalsDB, Entity ship, DateTime atDateTime)
    {
        var goal = goalsDB.GivenGoal;
        if (goal == null) return; // no concrete tasking (autonomous mode not wired yet)
        if (goal.Status is GoalStatus.Completed or GoalStatus.Failed) return;
        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue)) return;

        goalsDB.ActiveGoal = goal;

        switch (goal.Status)
        {
            case GoalStatus.Pending:
            {
                if (!_planners.TryGetValue(goal.Type, out var planner))
                {
                    goal.Status = GoalStatus.Failed;
                    goal.Message = $"no planner for {goal.Type}";
                    return;
                }

                try
                {
                    foreach (var action in planner.Plan(goal, ship))
                    {
                        action.ParentGoalId = goal.Id;
                        // HandleOrder validates (resolves acting/target entities),
                        // enqueues on the ActionQueue, and schedules the executor.
                        ship.Manager.Game.OrderHandler.HandleOrder(action);
                    }
                }
                catch (Exception e)
                {
                    // Movement plotting can throw on states it can't predict; fail the
                    // goal rather than crash the pulse.
                    goal.Status = GoalStatus.Failed;
                    goal.Message = e.Message;
                    return;
                }

                // A planner may have resolved the goal itself: Failed (target not found, no drive)
                // or Completed (already at the target, so it emitted no actions). Only advance a
                // goal that is still Pending — otherwise a zero-action plan gets set back to
                // Active and, having no actions to roll up, reschedules forever.
                if (goal.Status == GoalStatus.Pending)
                    goal.Status = GoalStatus.Active;
                else
                    break;

                ScheduleAgent(agentHost, atDateTime + RecheckInterval);
                break;
            }
            case GoalStatus.Active:
            {
                var mine = queue.ActionsFor(goal);
                if (mine.Count > 0 && mine.All(a => a.Status == ActionStatus.Succeeded))
                {
                    goal.Status = GoalStatus.Completed;
                    queue.ClearFor(goal);
                }
                else if (mine.Any(a => a.Status == ActionStatus.Failed))
                {
                    goal.Status = GoalStatus.Failed;
                    goal.Message = "an action failed";
                    queue.ClearFor(goal);
                }
                else
                {
                    ScheduleAgent(agentHost, atDateTime + RecheckInterval); // still running
                }
                break;
            }
        }
    }

    // -----------------------------------------------------------------
    // helpers
    // -----------------------------------------------------------------
    internal static GoalsDB GetOrCreateGoals(Entity entity)
    {
        if (!entity.TryGetDataBlob<GoalsDB>(out var goals))
        {
            goals = new GoalsDB();
            entity.SetDataBlob(goals);
        }
        return goals;
    }

    static void ScheduleAgent(Entity unit, DateTime when)
    {
        unit.Manager.ManagerSubpulses.AddEntityInterupt(when, nameof(AgentProcessor), unit);
    }
    static internal void RunAgentNow(Entity unit)
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
                GoalType.SurveySystem or GoalType.ScanBodies => entity.HasOrChildHasAbility<GeoSurveyAbilityDB>(),
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

            goalsDB.EffectiveGoals[type] = new Goal
            {
                Type = type,
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

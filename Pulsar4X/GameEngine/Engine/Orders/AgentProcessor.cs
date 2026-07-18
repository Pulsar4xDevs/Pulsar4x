using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.JumpPoints;
using Pulsar4X.People;
using Pulsar4X.Sensors;
using Pulsar4X.Ships;

namespace GameEngine.Engine.Orders;

public class AgentProcessor : IInstanceProcessor
{
    internal override void ProcessEntity(Entity entity, DateTime atDateTime)
    {
        var hasAgent = entity.TryGetDataBlob<AgentDB>(out AgentDB? oAgentDB);
        var isCommander = entity.TryGetDataBlob(out CommanderDB? oCommanderDB);
        int assignedToID = oCommanderDB.AssignedTo;
        Entity assignedTo = entity.Manager.GetGlobalEntityById(assignedToID);
        var hasGoals = assignedTo.TryGetDataBlob<GoalsDB>(out GoalsDB? oGoalsDB);
        
        if (assignedTo.TryGetDataBlob<FleetDB>(out FleetDB? oFleetDB))
        {
            var children = oFleetDB.Children.ToArray();
            GoalsProcessor(oAgentDB, oGoalsDB, oFleetDB.OwningEntity, children);
        }
        if(assignedTo.TryGetDataBlob<ShipInfoDB>(out ShipInfoDB? oShipInfoDB))
        {
            var entityCommanding = oShipInfoDB.OwningEntity;
            ActionsProcessor(oAgentDB, oGoalsDB, entityCommanding);
        }
    }
    
    static void GoalsProcessor(AgentDB agentDB, GoalsDB myGoalsDB, Entity assigned, Entity[] children)
    {

        //todo, probabily only need to pruneImpossibleGoals rarely, eg on new assignment, taken damage, etc.
        PruneImpossibleGoals(myGoalsDB, assigned);
        RecalculateEffectiveGoals(myGoalsDB, agentDB);
        
        
        foreach (var subordinateEntity in children)
        {
            var hasGoals = subordinateEntity.TryGetDataBlob<GoalsDB>(out GoalsDB? oGoalsDB);
            
            if (!hasGoals)
            {
                oGoalsDB = new GoalsDB();
                subordinateEntity.SetDataBlob(oGoalsDB);
            }


        }
        
    }
    
    static void ActionsProcessor(AgentDB agentDB, GoalsDB myGoalsDB, Entity entityCommanding)
    {
        if(entityCommanding.TryGetDataBlob<ActionQueueDB>(out ActionQueueDB? oActionQueueDB))
        {
            var primaryGoal = myGoalsDB.GivenGoal;
            
            

            
            var currentActionList = oActionQueueDB.ActionList;
            foreach(var action in currentActionList)
            {
            
            }
        }
    }
    
    
    // ===================================================================
    // Static helper methods
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
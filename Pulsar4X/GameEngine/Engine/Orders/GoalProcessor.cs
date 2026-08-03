using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Movement;

namespace GameEngine.Engine.Orders;

interface IGoalPlanner
{
    GoalType Type { get; } 
    IEnumerable<EntityAction> Plan(Goal goal, Entity ship);
}

public class GoalProcessor
{
    
}

public class ScanBodiesPlan : IGoalPlanner
{
    public GoalType Type => GoalType.ScanBodies;

    public IEnumerable<EntityAction> Plan(Goal goal, Entity ship)
    {
        // Scan == get there, then survey. Reuse the MoveTo plan for the "get there" part.
        var plan = new List<EntityAction>();
        plan.AddRange(new MoveToPlan().Plan(goal, ship));
        // TODO: append a GeoSurveyAction once that leaf action is ported.
        return plan;
    }
}


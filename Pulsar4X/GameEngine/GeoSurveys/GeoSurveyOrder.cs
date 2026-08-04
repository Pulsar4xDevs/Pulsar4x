using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;

namespace Pulsar4X.GeoSurveys;

public class ScanBodyPlan : IGoalToActionsPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    public IEnumerable<EntityAction> Plan(Goal goal, Entity ship)
    {
        // Scan == get there, then survey. Reuse the MoveTo plan for the "get there" part.
        var plan = new List<EntityAction>();
        plan.AddRange(new MoveToPlan().Plan(goal, ship));
        
        plan.Add(new GeoSurveyOrder(ship, goal.TargetEntityID));
        return plan;
    }
}

public class ScanSystemBodiesPlan : IGoalToGoalsPlanner
{
    public GoalType Type => GoalType.ServeyBodies;

    /// <summary>
    /// TODO: should scan everything within the target's SOI — target the sun and we survey the whole
    /// system, target earth and we survey earth and luna — by walking the target's PositionDB
    /// children for anything with a GeoSurveyableDB the faction hasn't finished, then giving each
    /// subunit a *different* body (nearest first) and topping up as ships come free.
    ///
    /// Two things are missing before that can work: this only gets one pass (AgentProcessor plans on
    /// Pending and monitors thereafter), so there is nowhere to hand out the next body from; 
    /// </summary>
    public IEnumerable<(Entity subordinate, Goal goal)> Plan(Goal goal, Entity fleet)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out FleetDB? db))
        {
            goal.Status = GoalStatus.Failed;
            goal.Message = "We have no subordinates to manage";
            yield break;
        }

        foreach (var subunit in db.Children)
        {
            yield return (subunit, new Goal
            {
                Type = GoalType.ServeyBodies,
                TargetEntityID = goal.TargetEntityID,
            });
        }
    }
}

public class GeoSurveyOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => $"Geo Survey {Target.GetOwnersName()} ({GetProgressPercent()}%)";

    public override string Details => "";

    public Entity Target { get; private set; }
    public GeoSurveyableDB? TargetGeoSurveyDB { get; private set; } = null;
    public DateTime? PreviousUpdate { get; private set; } = null;
    public GeoSurveyProcessor? Processor { get; private set; } = null;

    private Entity _entityCommanding;
    internal override Entity EntityCommanding
    {
        get { return _entityCommanding; }
    }

    public GeoSurveyOrder() { }
    
    public GeoSurveyOrder(Entity commandingEntity, int target) : 
        this(commandingEntity, commandingEntity.Manager.GetGlobalEntityById(target))
    {
    }
    
    public GeoSurveyOrder(Entity commandingEntity, Entity target)
    {
        _entityCommanding = commandingEntity;
        Target = target;
        if(Target.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB))
        {
            TargetGeoSurveyDB = geoSurveyableDB;
        }
    }

    public override EntityAction Clone()
    {
        var command = new GeoSurveyOrder(EntityCommanding, Target)
        {
            UseActionLanes = this.UseActionLanes,
            RequestingFactionGuid = this.RequestingFactionGuid,
            EntityCommandingGuid = this.EntityCommandingGuid,
            CreatedDate = this.CreatedDate,
            ActionOnDate = this.ActionOnDate,
            ActionedOnDate = this.ActionedOnDate,
            IsRunning = this.IsRunning
        };

        return command;
    }

    internal override bool IsFinished()
    {
        return _isFinished = TargetGeoSurveyDB == null ? true : TargetGeoSurveyDB.IsSurveyComplete(EntityCommanding.FactionOwnerID);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!IsRunning)
        {
            IsRunning = true;
            PreviousUpdate = atDateTime;
            Processor = new GeoSurveyProcessor(EntityCommanding, Target);
        }
        else
        {
            if(PreviousUpdate != null && atDateTime - PreviousUpdate >= TimeSpan.FromDays(1))
            {
                Processor?.ProcessEntity(EntityCommanding, atDateTime);
                PreviousUpdate = atDateTime;
            }
        }
    }

    internal override bool IsValidCommand(Game game)
    {
        return TargetGeoSurveyDB != null;
    }

    public static GeoSurveyOrder CreateCommand(int requestingFactionId, Entity fleet, Entity target)
    {
        var command = new GeoSurveyOrder(fleet, target)
        {
            RequestingFactionGuid = requestingFactionId
        };

        return command;
    }

    private float GetProgressPercent()
    {
        if(TargetGeoSurveyDB == null) return 0f;
        if(!TargetGeoSurveyDB.HasSurveyStarted(RequestingFactionGuid)) return 0f;

        uint pointsRequired = TargetGeoSurveyDB.PointsRequired;
        uint currentValue = TargetGeoSurveyDB.GeoSurveyStatus[RequestingFactionGuid];

        return (1f - ((float)currentValue / (float)pointsRequired)) * 100f;
    }
}

using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders;

public class JPSurveyOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => $"Jump Point Survey {Target.GetOwnersName()} ({GetProgressPercent()}%)";

    public override string Details => "";

    public Entity Target { get; private set; }
    public JPSurveyableDB? TargetSurveyDB { get; private set; } = null;
    public DateTime? PreviousUpdate { get; private set; } = null;
    public JPSurveyProcessor? Processor { get; private set; } = null;

    private Entity _entityCommanding;
    internal override Entity EntityCommanding
    {
        get { return _entityCommanding; }
    }

    public JPSurveyOrder() { }
    public JPSurveyOrder(Entity commandingEntity, Entity target)
    {
        _entityCommanding = commandingEntity;
        Target = target;
        if(Target.TryGetDatablob<JPSurveyableDB>(out var jpSurveyableDB))
        {
            TargetSurveyDB = jpSurveyableDB;
        }
    }

    public override EntityCommand Clone()
    {
        var command = new JPSurveyOrder(EntityCommanding, Target)
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

    public override bool IsFinished()
    {
        return TargetSurveyDB == null ? true : TargetSurveyDB.IsSurveyComplete(EntityCommanding.FactionOwnerID);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!IsRunning)
        {
            IsRunning = true;
            PreviousUpdate = atDateTime;
            Processor = new JPSurveyProcessor(EntityCommanding, Target);
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
        return TargetSurveyDB != null;
    }

    public static JPSurveyOrder CreateCommand(int requestingFactionId, Entity fleet, Entity target)
    {
        var command = new JPSurveyOrder(fleet, target)
        {
            RequestingFactionGuid = requestingFactionId
        };

        return command;
    }

    private float GetProgressPercent()
    {
        if(TargetSurveyDB == null) return 0f;
        if(!TargetSurveyDB.HasSurveyStarted(RequestingFactionGuid)) return 0f;

        uint pointsRequired = TargetSurveyDB.PointsRequired;
        uint currentValue = TargetSurveyDB.SurveyPointsRemaining[RequestingFactionGuid];

        return (1f - ((float)currentValue / (float)pointsRequired)) * 100f;
    }
}
using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders;

public class GeoSurveyOrder : EntityCommand
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
    public GeoSurveyOrder(Entity commandingEntity, Entity target)
    {
        _entityCommanding = commandingEntity;
        Target = target;
        if(Target.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB))
        {
            TargetGeoSurveyDB = geoSurveyableDB;
        }
    }

    public override EntityCommand Clone()
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

    public override bool IsFinished()
    {
        return TargetGeoSurveyDB == null ? true : TargetGeoSurveyDB.IsSurveyComplete(EntityCommanding.FactionOwnerID);
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
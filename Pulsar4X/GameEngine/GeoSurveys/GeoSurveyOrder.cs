using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Messaging;
using Pulsar4X.Movement;

namespace Pulsar4X.GeoSurveys;


public class GeoSurveyOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name
    {
        get
        {
            if (Target == null)
                return "Geo Survey";
            return $"Geo Survey {Target.GetOwnersName()} ({GetProgressPercent():0}%)";
        }
    }

    public override string Details => $"{GetProgressPercent():0}%";

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
        RequestingFactionGuid = commandingEntity.FactionOwnerID;
        EntityCommandingGuid = commandingEntity.Id;
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
            PublishSurveyChanged();
        }
        else if (PreviousUpdate != null && atDateTime - PreviousUpdate >= TimeSpan.FromDays(1))
        {
            Processor?.ProcessEntity(EntityCommanding, atDateTime);
            PreviousUpdate = atDateTime;
            PublishSurveyChanged();
        }
    }

    void PublishSurveyChanged()
    {
        // EntityWindow re-reads the open entity's snapshot each frame. The ship snapshot
        // carries the order line; the body snapshot carries GeoSurveyView and the mineral
        // reveal. Push both, addressed to the surveying faction, or the body window stays stale.
        int? factionId = EntityCommanding?.FactionOwnerID;
        PublishChanged(EntityCommanding, factionId);
        if (Target != null && !ReferenceEquals(Target, EntityCommanding))
            PublishChanged(Target, factionId);
    }

    static void PublishChanged(Entity? entity, int? factionId)
    {
        if (entity?.Manager == null)
            return;
        MessagePublisher.Instance.Publish(Message.Create(
            MessageTypes.EntityChanged,
            entityId: entity.Id,
            systemId: entity.Manager.ManagerID,
            factionId: factionId));
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
        if (TargetGeoSurveyDB == null) return 0f;

        // Planner constructs this without going through CreateCommand; faction is the ship owner.
        int factionId = EntityCommanding != null
            ? EntityCommanding.FactionOwnerID
            : RequestingFactionGuid;
        if (!TargetGeoSurveyDB.HasSurveyStarted(factionId)) return 0f;

        uint pointsRequired = TargetGeoSurveyDB.PointsRequired;
        if (pointsRequired == 0) return 100f;
        uint currentValue = TargetGeoSurveyDB.GeoSurveyStatus[factionId];

        return (1f - ((float)currentValue / (float)pointsRequired)) * 100f;
    }
}

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
            PublishShipChanged();
        }
        else if (PreviousUpdate != null && atDateTime - PreviousUpdate >= TimeSpan.FromDays(1))
        {
            Processor?.ProcessEntity(EntityCommanding, atDateTime);
            PreviousUpdate = atDateTime;
            PublishShipChanged();
        }
    }

    void PublishShipChanged()
    {
        // Progress lives on the target body; EntityWindow reads a baked OrderSnapshot
        // on the ship. Without this, the percent never updates while the window is open.
        var ship = EntityCommanding;
        if (ship?.Manager == null)
            return;
        MessagePublisher.Instance.Publish(Message.Create(
            MessageTypes.EntityChanged,
            entityId: ship.Id,
            systemId: ship.Manager.ManagerID,
            factionId: ship.FactionOwnerID));
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

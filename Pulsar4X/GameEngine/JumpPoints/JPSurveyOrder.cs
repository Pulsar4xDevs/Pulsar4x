using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;

namespace Pulsar4X.JumpPoints;

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

    internal override bool IsFinished()
    {
        return _isFinished = TargetSurveyDB.IsSurveyComplete(EntityCommanding.FactionOwnerID);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(!IsRunning)
        {
            IsRunning = true;
            PreviousUpdate = atDateTime;

            // Get any ships in the fleet that can survey and add the JPSurveyDB to them
            if (_entityCommanding.TryGetDatablob<FleetDB>(out var fleetDB))
            {
                foreach (var child in fleetDB.Children)
                {
                    if (child.HasJPSurveyAbililty())
                    {
                        var order = JPSurveyOrder.CreateCommand(RequestingFactionGuid, child, Target);
                        child.Manager.Game.OrderHandler.HandleOrder(order);
                    }
                }
            }
            else if (_entityCommanding.TryGetDatablob<ShipInfoDB>(out var shipInfoDB))
            {
                _entityCommanding.SetDataBlob(new JPSurveyDB() { TargetId = Target.Id });
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
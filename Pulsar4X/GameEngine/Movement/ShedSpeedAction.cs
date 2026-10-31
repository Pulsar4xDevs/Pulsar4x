using System;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Coast inside the drop-in well until the cap. No fuel and no bubble.
/// OrbitProcessor propagates the hyperbola. The wake is the cap.
/// </summary>
public class ShedSpeedAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
    public override bool IsBlocking => true;
    public override string Name => "Shedding speed";

    [JsonProperty]
    string _details = "Shedding speed";

    public override string Details => _details;

    [JsonProperty]
    public int ParentId { get; set; }

    [JsonProperty]
    public double CapRadius { get; set; }

    [JsonProperty]
    public double SpeedAtExecute { get; set; }

    [JsonProperty]
    public DateTime WakeTime { get; set; }

    Entity _factionEntity;

    internal override Entity EntityCommanding => _entityCommanding;

    public static ShedSpeedAction Create(Entity ship, int parentId, double capRadius, string details, DateTime now)
    {
        return new ShedSpeedAction
        {
            _entityCommanding = ship,
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            CreatedDate = now,
            ActionOnDate = now,
            ParentId = parentId,
            CapRadius = capRadius,
            _details = details,
        };
    }

    public override void Rehydrate(Game game)
    {
        base.Rehydrate(game);
        if (Status != ActionStatus.Running || _entityCommanding?.Manager == null)
            return;
        if (WakeTime <= _entityCommanding.StarSysDateTime)
            return;
        _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(
            WakeTime, nameof(ActionQueueProcessor), _entityCommanding);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (atDateTime < ActionOnDate || IsRunning)
            return;
        if (_entityCommanding == null)
        {
            Fail("left the gravity well");
            return;
        }

        var parent = _entityCommanding.GetSOIParentEntity();
        if (parent == null || parent.Id != ParentId)
        {
            Fail("left the gravity well");
            return;
        }
        if (!_entityCommanding.TryGetDataBlob<OrbitDB>(out var orbit))
        {
            Fail("speed did not fall");
            return;
        }

        SpeedAtExecute = MoveMath.GetRelativeState(_entityCommanding).Velocity.Length();
        DateTime wake;
        try
        {
            wake = OrbitMath.TimeToRadius(orbit, CapRadius);
        }
        catch
        {
            Fail("speed did not fall");
            return;
        }
        if (wake <= atDateTime)
        {
            Fail("speed did not fall");
            return;
        }

        WakeTime = wake;
        IsRunning = true;
        Status = ActionStatus.Running;
        _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(
            WakeTime, nameof(ActionQueueProcessor), _entityCommanding);
    }

    internal override bool IsFinished()
    {
        if (Status == ActionStatus.Failed)
            return true;
        if (!IsRunning || _entityCommanding == null)
            return false;

        var parent = _entityCommanding.GetSOIParentEntity();
        if (parent == null || parent.Id != ParentId)
        {
            Fail("left the gravity well");
            return true;
        }

        if (_entityCommanding.StarSysDateTime < WakeTime)
            return false;

        double speed = MoveMath.GetRelativeState(_entityCommanding).Velocity.Length();
        double radius = MoveMath.GetRelativeState(_entityCommanding).pos.Length();
        if (speed <= SpeedAtExecute - 1 && radius + 1 >= CapRadius * 0.99)
            return _isFinished = true;

        Fail("speed did not fall");
        return true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return CommandHelpers.IsCommandValid(
            game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid,
            out _factionEntity, out _entityCommanding);
    }

    public override EntityAction Clone()
    {
        return new ShedSpeedAction
        {
            UseActionLanes = UseActionLanes,
            ParentGoalId = ParentGoalId,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            _entityCommanding = _entityCommanding,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentId = ParentId,
            CapRadius = CapRadius,
            _details = _details,
        };
    }
}

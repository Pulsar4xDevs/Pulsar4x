using System;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Engine;

namespace Pulsar4X.Ships;

public class LaunchShipCommand : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf;
    public override bool IsBlocking => true;
    public override string Name { get; } = "Launch Ship";
    public override string Details { get; } = "";

    Entity _factionEntity;
    Entity _entityCommanding;
    internal override Entity EntityCommanding => _entityCommanding;

    private string _padId;
    private bool _hasLaunched = false;

    public static void CreateCommand(int factionId, Entity colonyEntity, string padId)
    {
        var cmd = new LaunchShipCommand()
        {
            RequestingFactionGuid = factionId,
            EntityCommandingGuid = colonyEntity.Id,
            CreatedDate = colonyEntity.Manager.ManagerSubpulses.StarSysDateTime,
            _padId = padId
        };

        colonyEntity.Manager.Game.OrderHandler.HandleOrder(cmd);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (!IsRunning)
        {
            IsRunning = true;
        }

        if (LaunchComplexProcessor.TryLaunchShip(_entityCommanding, _padId))
        {
            _hasLaunched = true;
        }
    }

    internal override bool IsFinished()
    {
        _isFinished = _hasLaunched;
        return _isFinished;
    }

    internal override bool IsValidCommand(Game game)
    {
        return CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding);
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}

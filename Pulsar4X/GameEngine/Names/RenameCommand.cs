using System;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Names
{
    public class RenameCommand : EntityCommand
    {
        public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder;

        public override bool IsBlocking => false;

        public override string Name { get; } = "Rename";

        public override string Details { get; } = "Renames This Entity";

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }
        string NewName;

        public static void CreateRenameCommand(Game game, Entity faction, Entity orderEntity, string newName)
        {
            var cmd = new RenameCommand()
            {
                RequestingFactionGuid = faction.Id,
                EntityCommandingGuid = orderEntity.Id,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                NewName = newName,
                UseActionLanes = false
            };

            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void Execute(DateTime atDateTime)
        {
            var namedb = _entityCommanding.GetDataBlob<NameDB>();
            namedb.SetName(_factionEntity.Id, NewName);
            _isFinished = true;
        }

        internal override bool IsFinished()
        {
            return _isFinished;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                return true;
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}

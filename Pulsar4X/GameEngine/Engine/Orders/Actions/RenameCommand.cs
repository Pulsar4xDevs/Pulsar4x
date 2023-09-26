using System;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders
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
        bool _isFinished = false;
        string NewName;

        public static void CreateRenameCommand(Game game, Entity faction, Entity orderEntity, string newName)
        {
            var cmd = new RenameCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                NewName = newName,
                UseActionLanes = false
            };

            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void Execute(DateTime atDateTime)
        {
            var namedb = _entityCommanding.GetDataBlob<NameDB>();
            namedb.SetName(_factionEntity.Guid, NewName);
            _isFinished = true;
        }

        public override bool IsFinished()
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

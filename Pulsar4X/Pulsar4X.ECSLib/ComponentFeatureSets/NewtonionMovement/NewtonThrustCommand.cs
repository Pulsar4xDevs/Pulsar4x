using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    public class NewtonThrustCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        NewtonMoveDB _db;

        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Vector3 expendDeltaV_m)
        {
            var cmd = new NewtonThrustCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,

            };

            var parent = Entity.GetSOIParentEntity(orderEntity);
            cmd._db = new NewtonMoveDB(parent, Vector3.Zero);
            cmd._db.ActionOnDateTime = actionDateTime;
            cmd._db.DeltaVForManuver_m = expendDeltaV_m;
            
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                if(_entityCommanding.HasDataBlob<OrbitDB>())
                    _entityCommanding.RemoveDataBlob<OrbitDB>();
                _entityCommanding.SetDataBlob(_db);
                IsRunning = true;

            }
        }

        internal override bool IsFinished()
        {
            if (IsRunning && _db.DeltaVForManuver_m.Length() <= 0)
                return true;
            else
                return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
                return true;
            else
                return false;
        }
    }


}
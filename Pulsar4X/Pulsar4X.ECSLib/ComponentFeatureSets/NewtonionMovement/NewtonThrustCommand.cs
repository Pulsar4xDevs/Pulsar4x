using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    public class NewtonThrustCommand : EntityCommand
    {
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Vector3 _orbitRalitiveDeltaV;
        NewtonMoveDB _db;

        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Vector3 expendDeltaV_m)
        {
            var cmd = new NewtonThrustCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                _orbitRalitiveDeltaV = expendDeltaV_m,
                ActionOnDate = actionDateTime

            };

            
            
            
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand()
        {
            if (!IsRunning)
            {
                 var parent = Entity.GetSOIParentEntity(_entityCommanding);
                 var currentVel = Entity.GetVelocity_m(_entityCommanding, ActionOnDate);               
                if(_entityCommanding.HasDataBlob<OrbitDB>())
                    _entityCommanding.RemoveDataBlob<OrbitDB>();
                _db = new NewtonMoveDB(parent, currentVel);
                _db.ActionOnDateTime = ActionOnDate;
                _db.DeltaVForManuver_FoRO_m = _orbitRalitiveDeltaV;
                _entityCommanding.SetDataBlob(_db);
                IsRunning = true;
            }
        }

        public override bool IsFinished()
        {
            if (IsRunning && _db.DeltaVForManuver_FoRO_m.Length() <= 0)
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
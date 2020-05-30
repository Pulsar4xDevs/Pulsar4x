using System;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;
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

        internal override void ActionCommand(DateTime atDateTime)
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

    public class ThrustToTargetCmd : EntityCommand
    {
        public override int ActionLanes => 1;
        public override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        private OrdnanceDesign _missileDesign;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        private Entity _targetEntity;
        NewtonMoveDB _db;

        public static void CreateCommand(Guid faction, Entity orderEntity, DateTime actionDateTime, Entity targetEntity)
        {
            var cmd = new ThrustToTargetCmd()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.StarSysDateTime,
                _targetEntity = targetEntity,
                ActionOnDate = actionDateTime

            };
            StaticRefLib.Game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(DateTime atDateTime)
        {
            if (!IsRunning && atDateTime >= ActionOnDate)
            {
                IsRunning = true;
                
              var soiParentEntity = Entity.GetSOIParentEntity(_entityCommanding);
              var currentVel = Entity.GetVelocity_m(_entityCommanding, ActionOnDate);               
              if(_entityCommanding.HasDataBlob<OrbitDB>())
              _entityCommanding.RemoveDataBlob<OrbitDB>();
              if(_entityCommanding.HasDataBlob<OrbitUpdateOftenDB>())
              _entityCommanding.RemoveDataBlob<OrbitUpdateOftenDB>();
  
              _db = new NewtonMoveDB(soiParentEntity, currentVel);
              _db.ActionOnDateTime = ActionOnDate;
  
              var curPos = Entity.GetPosition_m(_entityCommanding, atDateTime);
              var soiParentPosition = Entity.GetPosition_m(soiParentEntity, atDateTime);
              OrbitDB tgtEntityOrbit = _targetEntity.GetDataBlob<OrbitUpdateOftenDB>();
              if (tgtEntityOrbit == null)
              tgtEntityOrbit = _targetEntity.GetDataBlob<OrbitDB>();
  
              //var tgtintercept = OrbitMath.GetInterceptPosition_m(soiParentPosition, currentVel.Length(), tgtEntityOrbit, atDateTime);
              //var tgtEstPos = tgtintercept.position + _targetEntity.GetDataBlob<PositionDB>().RelativePosition_m;
              var tgtCurPos = Entity.GetPosition_m(_targetEntity, atDateTime);
              
              var vectorToTgt = Vector3.Normalise(tgtCurPos - curPos);
              
  
              var newtonAbility = _entityCommanding.GetDataBlob<NewtonThrustAbilityDB>();
              var dvTotal = newtonAbility.DeltaV;
  
              var manuverDV = vectorToTgt * dvTotal;
  
              _db.DeltaVForManuver_FoRO_m = manuverDV;
              _entityCommanding.SetDataBlob(_db);              
              _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(1), nameof(OrderableProcessor), _entityCommanding);
                
            }
            else
            {
                var soiParentEntity = Entity.GetSOIParentEntity(_entityCommanding);
                var currentVel = Entity.GetVelocity_m(_entityCommanding, atDateTime);
                var curPos = Entity.GetPosition_m(_entityCommanding, atDateTime);
                var soiParentPosition = Entity.GetPosition_m(soiParentEntity, atDateTime);
                OrbitDB tgtEntityOrbit = _targetEntity.GetDataBlob<OrbitUpdateOftenDB>();
                if (tgtEntityOrbit == null)
                    tgtEntityOrbit = _targetEntity.GetDataBlob<OrbitDB>();
  
                var tgtintercept = OrbitMath.GetInterceptPosition_m(soiParentPosition, currentVel.Length(), tgtEntityOrbit, ActionOnDate);
                var tgtEstPos = tgtintercept.position + _targetEntity.GetDataBlob<PositionDB>().RelativePosition_m;
                var vectorToTgt = Vector3.Normalise(tgtEstPos - curPos);

                var newtonAbility = _entityCommanding.GetDataBlob<NewtonThrustAbilityDB>();
                var dvTotal = newtonAbility.DeltaV;
  
                var manuverDV = vectorToTgt * dvTotal;
                
                _db.DeltaVForManuver_FoRO_m = manuverDV;
                _entityCommanding.Manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(1), nameof(OrderableProcessor), _entityCommanding);
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
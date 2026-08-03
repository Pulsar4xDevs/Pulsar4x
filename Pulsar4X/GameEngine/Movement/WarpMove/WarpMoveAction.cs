using System;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Galaxy;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Engine;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Movement
{
    public class WarpMoveAction : EntityAction
    {

        public override string Name
        {
            get
            {
                if(_targetEntity == null || _entityCommanding == null)
                    return "Warp Move";

                return "Warp Move to " + _targetEntity.GetName(_entityCommanding.FactionOwnerID);
            }
        }

        public override string Details
        {
            get
            {
                string targetName = _targetEntity.GetDataBlob<NameDB>().GetName(_factionEntity);
                return "Warp to + " + Stringify.Distance(EndpointRelitivePosition.Length()) + " from " + targetName;
            }
        }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
        public override bool IsBlocking => true;

        [JsonProperty]
        public int TargetEntityGuid { get; set; }

        private Entity _targetEntity;


        [JsonIgnore]
        Entity _factionEntity;
        WarpMovingDB _warpingDB;


        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public DateTime TransitStartDateTime;
        public Vector3 EndpointRelitivePosition { get; set; }
        public Vector3 EndpointTargetExpendDeltaV;
        /// <summary>
        /// the orbit we want to be in at the target.
        /// </summary>
        public KeplerElements EndpointTargetOrbit;

        public static bool CreateCommand(
            Entity orderEntity,
            Entity targetEntity,
            DateTime transitStartDatetime,
            Vector3 endpointRelativePos = new Vector3())
        {
            var datetimeArrive = WarpMath.GetInterceptPosition(orderEntity, targetEntity, transitStartDatetime, endpointRelativePos);

            var cmd = new WarpMoveAction()
            {
                RequestingFactionGuid = orderEntity.FactionOwnerID,
                EntityCommandingGuid = orderEntity.Id,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Id,
                EndpointRelitivePosition = endpointRelativePos,
                TransitStartDateTime = transitStartDatetime,
            };
            if (targetEntity.GetDataBlob<PositionDB>().MoveType != PositionDB.MoveTypes.None)
            {
                var sgp = GeneralMath.StandardGravitationalParameter(targetEntity.GetDataBlob<MassVolumeDB>().MassTotal + orderEntity.GetDataBlob<MassVolumeDB>().MassTotal);
                cmd.EndpointTargetOrbit = OrbitMath.KeplerCircularFromPosition(sgp, endpointRelativePos, datetimeArrive.Item2);;
            }
            return orderEntity.Manager.Game.OrderHandler.HandleOrder(cmd);
        }

        
        /// <summary>
        /// Creates a warp order with an attempted simplenewt circular orbit post warp.
        /// DOES NOT QUEUE THE COMMAND. Game.OrderHandler.HandleOrder(cmd) should be called
        ///
        /// This assumes the caller has already decided that warp is the right way to get there.
        /// MovePlanner is where that decision belongs — it resolves the target, checks the ship
        /// actually has a drive, and rejects targets we can't plot an intercept against.
        /// </summary>
        /// <param name="orderEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="transitStartDatetime"></param>
        /// <returns></returns>
        public static WarpMoveAction CreateCommandEZ(
            Entity orderEntity,
            Entity targetEntity,
            DateTime transitStartDatetime)
        {
            // Resolve colonies to their body and warping ships to their destination.
            // Idempotent, so it's harmless for callers that came via MovePlanner.
            if (!MoveTargeting.TryResolve(targetEntity, out targetEntity, out string resolveFailure))
                throw new InvalidOperationException($"Cannot plot a warp to {targetEntity.Id}: {resolveFailure}");

            (Vector3 pos, Vector3 vel) departureState;
            if(orderEntity.Manager.Game.Settings.UseRelativeVelocity)
            {
                departureState = MoveMath.GetRelativeFutureState(orderEntity, transitStartDatetime);
            }
            else
                departureState = MoveMath.GetAbsoluteState(orderEntity, transitStartDatetime);

            var cmd = new WarpMoveAction()
            {
                RequestingFactionGuid = orderEntity.FactionOwnerID,
                EntityCommandingGuid = orderEntity.Id,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Id,
                TransitStartDateTime = transitStartDatetime,

            };

            switch (targetEntity.GetDataBlob<PositionDB>().MoveType) //if the targetEntity's movetype is this:
            {
                case PositionDB.MoveTypes.None: //this means it's a grav anomaly, jump point
                {
                    break;
                }
                case PositionDB.MoveTypes.Orbit:
                {
                    var sgp = OrbitMath.SGP(targetEntity, orderEntity);
                    var lowOrbitRadius = OrbitMath.LowOrbitRadius(targetEntity);
                    var perpVec = Vector3.Normalise(new Vector3(departureState.vel.Y * -1, departureState.vel.X, 0));
                    var lowOrbitPos = perpVec * lowOrbitRadius;
                    (Vector3 pos, DateTime eti) targetIntercept  = WarpMath.GetInterceptPosition(orderEntity, targetEntity, transitStartDatetime, lowOrbitPos);
                    var lowOrbit = OrbitMath.KeplerCircularFromPosition(sgp, lowOrbitPos, targetIntercept.eti);
                    var lowOrbitState = OrbitMath.GetStateVectors(lowOrbit, targetIntercept.eti);
                    var targetEntityOrbitDb = targetEntity.GetDataBlob<OrbitDB>();
                    Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(departureState.vel, targetEntityOrbitDb, targetIntercept.eti);
                    var deltaV = insertionVector - (Vector3)lowOrbitState.velocity;

                    cmd.EndpointRelitivePosition = lowOrbitPos;
                    cmd.EndpointTargetOrbit = lowOrbit;
                    cmd.EndpointTargetExpendDeltaV = deltaV;
                    break;
                }
                case PositionDB.MoveTypes.NewtonSimple:
                case PositionDB.MoveTypes.NewtonComplex:
                    // A target under thrust has no closed-form future position, so WarpMath can't
                    // solve the intercept. MovePlanner rejects these before we get here; if we're
                    // reached anyway it's a bug in the caller, not a case to guess at.
                    throw new NotImplementedException(
                        $"No warp intercept solution against a {targetEntity.GetDataBlob<PositionDB>().MoveType} target.");

                case PositionDB.MoveTypes.Warp:
                    // MoveTargeting.TryResolve chases warping targets to their destination, so a
                    // warping target should be impossible by this point.
                    throw new InvalidOperationException("Warp target was not resolved to its destination.");

                default:
                    throw new NotImplementedException();
            }

            //orderEntity.Manager.Game.OrderHandler.HandleOrder(cmd);


            return cmd;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.TryGetGlobalEntityById(TargetEntityGuid, out _targetEntity))
                {
                    return true;
                }
            }
            return false;
        }

        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                // Should have been caught at plan time by MovePlanner, but an action can outlive
                // the components that made it possible (battle damage, refit).
                if (!_entityCommanding.TryGetDataBlob<WarpAbilityDB>(out var warpDB)
                    || !_entityCommanding.TryGetDataBlob<EnergyGenAbilityDB>(out var powerDB))
                {
                    if (Goal != null)
                    {
                        Goal.Status = GoalStatus.Failed;
                        Goal.Message = "No working warp drive";
                    }
                    Status = ActionStatus.Failed;
                    return;
                }

                string eType = warpDB.EnergyType;
                double estored = powerDB.EnergyStored[eType];
                double creationCost = warpDB.BubbleCreationCost;

                if (creationCost > estored)
                {
                    Goal.Status = GoalStatus.Holding;
                    Goal.Message = "Charging batteries";
                    return;
                }
                
                _warpingDB = new WarpMovingDB(_entityCommanding, _targetEntity, EndpointRelitivePosition, EndpointTargetOrbit);

                //if we're already in a warp moving state,
                //then we should carry over the SavedNewtonionVector.
                //this will happen in the case of serveying grav anomalies.
                if (_entityCommanding.TryGetDataBlob<WarpMovingDB>(out var warpMovingDB))
                {
                    _warpingDB.SavedNewtonionVector = warpMovingDB.SavedNewtonionVector;
                }

                EntityCommanding.SetDataBlob(_warpingDB);

                WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                IsRunning = true;

                //debug code:
                double distance = (_warpingDB.EntryPointAbsolute - _warpingDB.ExitPointAbsolute).Length();
                double time = distance / _entityCommanding.GetDataBlob<WarpAbilityDB>().MaxSpeed;
                //Assert.AreEqual((_warpingDB.PredictedExitTime - _warpingDB.EntryDateTime).TotalSeconds, time, 1.0e-10);

            }
        }

        internal override bool IsFinished()
        {
            if(_warpingDB != null)
                _isFinished = _warpingDB.IsAtTarget;
            else
                _isFinished = false;
            return _isFinished;
        }

        public override EntityAction Clone()
        {
            throw new NotImplementedException();
        }
    }
}

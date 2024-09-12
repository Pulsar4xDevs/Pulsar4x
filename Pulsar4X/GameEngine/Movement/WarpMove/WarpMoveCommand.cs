using System;
using GameEngine.WarpMove;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders
{
    public class WarpMoveCommand : EntityCommand
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
                return "Warp to + " + Stringify.Distance(TargetOffsetPosition_m.Length()) + " from " + targetName;
            }
        }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;
        public override bool IsBlocking => true;

        [JsonProperty]
        public int TargetEntityGuid { get; set; }

        private Entity _targetEntity;


        [JsonIgnore]
        Entity _factionEntity;
        WarpMovingDB _db;


        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public Vector3 TargetOffsetPosition_m { get; set; }
        public DateTime TransitStartDateTime;
        public Vector3 ExpendDeltaV;

        /// <summary>
        /// Creates the transit cmd.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="faction">Faction.</param>
        /// <param name="orderEntity">Order entity.</param>
        /// <param name="targetEntity">Target entity.</param>
        /// <param name="targetOffsetPos_m">Target offset position in au.</param>
        /// <param name="transitStartDatetime">Transit start datetime.</param>
        /// <param name="expendDeltaV">Amount of DV to expend to change the orbit in m/s</param>
        /// /// <param name="mass">mass of ship after warp (needed for DV calc)</param>
        public static (WarpMoveCommand, NewtonThrustCommand?) CreateCommand(CargoDefinitionsLibrary cargoLibrary, int faction, Entity orderEntity, Entity targetEntity, Vector3 targetOffsetPos_m, DateTime transitStartDatetime, Vector3 expendDeltaV, double mass)
        {
            var cmd = new WarpMoveCommand()
            {
                RequestingFactionGuid = faction,
                EntityCommandingGuid = orderEntity.Id,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Id,
                TargetOffsetPosition_m = targetOffsetPos_m,
                TransitStartDateTime = transitStartDatetime,
                ExpendDeltaV = expendDeltaV,
            };

            orderEntity.Manager.Game.OrderHandler.HandleOrder(cmd);
            if (expendDeltaV.Length() != 0)
            {

                (Vector3 position, DateTime atDateTime) targetIntercept = WarpMath.GetInterceptPosition
                (
                    orderEntity,
                    targetEntity.GetDataBlob<OrbitDB>(),
                    orderEntity.StarSysDateTime,
                    targetOffsetPos_m
                );

                var burntime = TimeSpan.FromSeconds(OrbitMath.BurnTime(orderEntity, expendDeltaV.Length(), mass));
                var ntcmd = NewtonThrustCommand.CreateCommand(cargoLibrary, orderEntity, expendDeltaV, targetIntercept.atDateTime + burntime);

                return (cmd, ntcmd);
            }

            return (cmd, null);
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
                var warpDB = _entityCommanding.GetDataBlob<WarpAbilityDB>();
                var powerDB = _entityCommanding.GetDataBlob<EnergyGenAbilityDB>();
                string eType = warpDB.EnergyType;
                double estored = powerDB.EnergyStored[eType];
                double creationCost = warpDB.BubbleCreationCost;

                // FIXME: alert the player?
                if (creationCost > estored)
                    return;

                _db = new WarpMovingDB(_entityCommanding, _targetEntity, TargetOffsetPosition_m);
                _db.ExpendDeltaV = ExpendDeltaV;

                EntityCommanding.SetDataBlob(_db);

                WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                IsRunning = true;

                //debug code:
                double distance = (_db.EntryPointAbsolute - _db.ExitPointAbsolute).Length();
                double time = distance / _entityCommanding.GetDataBlob<WarpAbilityDB>().MaxSpeed;
                //Assert.AreEqual((_db.PredictedExitTime - _db.EntryDateTime).TotalSeconds, time, 1.0e-10);

            }
        }

        public override bool IsFinished()
        {
            if(_db != null)
                return _db.IsAtTarget;
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }
    }
}

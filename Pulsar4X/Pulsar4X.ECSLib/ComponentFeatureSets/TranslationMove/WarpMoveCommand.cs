using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Pulsar4X.ECSLib
{
    public class WarpMoveCommand : EntityCommand
    {

        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        [JsonProperty]
        public Guid TargetEntityGuid { get; set; }

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
        /// <param name="expendDeltaV_AU">Amount of DV to expend to change the orbit in AU/s</param>
        public static void CreateCommand(Game game, Entity faction, Entity orderEntity, Entity targetEntity, Vector3 targetOffsetPos_m, DateTime transitStartDatetime, Vector3 expendDeltaV)
        {
            var cmd = new WarpMoveCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Guid,
                TargetOffsetPosition_m = targetOffsetPos_m,
                TransitStartDateTime = transitStartDatetime,
                ExpendDeltaV = expendDeltaV,
            };
            game.OrderHandler.HandleOrder(cmd);
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out _targetEntity))
                {
                    return true; 
                }
            }
            return false;
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                var warpDB = _entityCommanding.GetDataBlob<WarpAbilityDB>();
                var powerDB = _entityCommanding.GetDataBlob<EnergyGenAbilityDB>();
                Guid eType = warpDB.EnergyType;
                double estored = powerDB.EnergyStored[eType];
                double creationCost = warpDB.BubbleCreationCost;
                if (creationCost <= estored)
                {
                    (Vector3 position, DateTime atDateTime) targetIntercept = OrbitProcessor.GetInterceptPosition_m
                        (
                            _entityCommanding, 
                        _targetEntity.GetDataBlob<OrbitDB>(), 
                            _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime
                        );
                    
                    Vector3 startPosAbsolute_m = Entity.GetPosition_m(_entityCommanding, TransitStartDateTime, false);
                    Vector3 currentVec_m = Entity.GetVelocity_m(_entityCommanding, TransitStartDateTime);
                    
                    _db = new WarpMovingDB(targetIntercept.position);
                    _db.TranslateEntryAbsolutePoint = startPosAbsolute_m;
                    _db.EntryDateTime = _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime;
                    _db.TranslateRelitiveExit = TargetOffsetPosition_m;
                    _db.PredictedExitTime = targetIntercept.atDateTime;
                    _db.SavedNewtonionVector = currentVec_m;
                    _db.TargetEntity = _targetEntity;
                    if (EntityCommanding.HasDataBlob<OrbitDB>())
                        EntityCommanding.RemoveDataBlob<OrbitDB>();
                    if(EntityCommanding.HasDataBlob<NewtonMoveDB>())
                        EntityCommanding.RemoveDataBlob<NewtonMoveDB>();
                    EntityCommanding.SetDataBlob(_db);
                    
                    WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                    IsRunning = true;
                    
                    
                    //debug code:
                    double distance = (_db.TranslateEntryAbsolutePoint - _db.TranslateExitPoint).Length();
                    double time = distance / _entityCommanding.GetDataBlob<WarpAbilityDB>().MaxSpeed;
                    //Assert.AreEqual((_db.PredictedExitTime - _db.EntryDateTime).TotalSeconds, time, 1.0e-10);





                }
            }
        }

        internal override bool IsFinished()
        {
            if(_db != null)
                return _db.IsAtTarget;
            return false;
        }
    }
}

using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveCommand : EntityCommand
    {

        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        [JsonProperty]
        public Guid TargetEntityGuid { get; set; }

        //public Vector4 TargetPosition { get; set; }
        private Entity _targetEntity;

        public double RangeInKM { get; set; }
        [JsonIgnore]
        Entity _factionEntity;
        WarpMovingDB _db;


        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }


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
                    (Vector3 position, DateTime atDateTime) targetIntercept = InterceptCalcs.GetInterceptPosition_m
                        (
                            _entityCommanding, 
                        _targetEntity.GetDataBlob<OrbitDB>(), 
                            _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime
                        );
                    _db = new WarpMovingDB(targetIntercept.position);
                    _db.EntryDateTime = _entityCommanding.Manager.ManagerSubpulses.StarSysDateTime;
                    _db.PredictedExitTime = targetIntercept.atDateTime;
                    _db.TranslateEntryPoint = _entityCommanding.GetDataBlob<PositionDB>().AbsolutePosition_m;
                    _db.TargetEntity = _targetEntity;
                    if (EntityCommanding.HasDataBlob<OrbitDB>())
                        EntityCommanding.RemoveDataBlob<OrbitDB>();
                    EntityCommanding.SetDataBlob(_db);
                    
                    WarpMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                    IsRunning = true;
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

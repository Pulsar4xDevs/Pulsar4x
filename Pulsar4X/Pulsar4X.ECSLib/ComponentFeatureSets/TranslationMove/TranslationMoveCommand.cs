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
        TranslateMoveDB _db;


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
                _db = new TranslateMoveDB(_targetEntity.GetDataBlob<PositionDB>());
                _db.MoveRangeInKM = RangeInKM;
                if (EntityCommanding.HasDataBlob<OrbitDB>())
                    EntityCommanding.RemoveDataBlob<OrbitDB>();
                EntityCommanding.SetDataBlob(_db);
                IsRunning = true;
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

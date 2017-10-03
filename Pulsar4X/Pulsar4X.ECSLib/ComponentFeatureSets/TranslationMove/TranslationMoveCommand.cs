using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveCommand : EntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid EntityCommandingGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public int ActionLanes => 1;

        public bool IsBlocking => true;
        public bool IsRunning { get; private set; } = false;
        
        public Guid TargetEntityGuid { get; set; }
        //public Vector4 TargetPosition { get; set; }
        private Entity _targetEntity;
        private Entity _entityCommanding;
        public Entity EntityCommanding { get { return _entityCommanding; } }
        public double RangeInKM { get; set; }
        [JsonIgnore]
        Entity _factionEntity;
        TranslateMoveDB _db;

        public bool IsValidCommand(Game game)
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

        public void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                _db = new TranslateMoveDB(_targetEntity.GetDataBlob<PositionDB>());
                _db.MoveRangeInKM = RangeInKM;
                if (_entityCommanding.HasDataBlob<OrbitDB>())
                    _entityCommanding.RemoveDataBlob<OrbitDB>();
                _entityCommanding.SetDataBlob(_db);
                IsRunning = true;
            }
        }

        public bool IsFinished()
        {
            if(_db != null)
                return _db.IsAtTarget;
            return false;
        }
    }
}

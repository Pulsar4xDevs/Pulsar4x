using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OrbitTransferCommand : IEntityCommand
    {
        public OrbitTransferCommand()
        {
        }

        public DateTime ActionedOnDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid RequestingFactionGuid { get; set; }

        public Guid TargetEntityGuid { get; set; }

        public Guid MoveToOrbitParentGuid { get; set; }

        [JsonIgnore]
        Entity _targetEntity;
        [JsonIgnore]
        Entity _factionEntity;
        [JsonIgnore]
        Entity _moveToOrbitParent;

        public bool ActionCommand(Game game)
        {
            if(IsMoveOrderValid(game.GlobalManager))
            {
                OrbitTransferDB newTransferDB = new OrbitTransferDB();

                newTransferDB.OrbitParent = _moveToOrbitParent;

                _targetEntity.Manager.SetDataBlob(_targetEntity.ID, newTransferDB);
                return true;
            }
            return false;
        }

        private bool IsMoveOrderValid(EntityManager globalManager)
        {
            if(CommandHelpers.IsCommandValid(globalManager, RequestingFactionGuid, TargetEntityGuid, out _factionEntity, out _targetEntity))
            {
                if(globalManager.TryGetEntityByGuid(MoveToOrbitParentGuid, out _moveToOrbitParent))
                {
                    return true;
                }
            }
            return false;
        }
    }

    class OrbitTransferDB:BaseDataBlob
    {

        internal Entity OrbitParent { get; set; }
        internal double TargetOrbitRange { get; set; }
        //internal double TargetOrbitEcentricity { get; set; }


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    class OrbitTransferProcessor:IHotloopProcessor
    {
        public TimeSpan RunFrequency {
            get {
                return TimeSpan.FromDays(1);
            }
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            OrbitTransferDB translationDB = entity.GetDataBlob<OrbitTransferDB>();
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();

            OrbitDB orbitDB = entity.GetDataBlob<OrbitDB>();
            //OrbitProcessor.

        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            throw new NotImplementedException();
        }



    }
}

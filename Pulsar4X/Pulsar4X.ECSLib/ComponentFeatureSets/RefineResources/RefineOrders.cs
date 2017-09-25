using System;

namespace Pulsar4X.ECSLib
{
    public class RefineOrdersCommand:IEntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid TargetEntityGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public Guid MaterialGuid { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;

        private Entity _targetentity;
        private Entity _factionEntity;
        private StaticDataStore _staticData;
        private RefineingJob _job;



        public RefineOrdersCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid materal, ushort quantity, bool repeatJob )
        {
            RequestingFactionGuid = factionGuid;
            TargetEntityGuid = thisEntity;
            CreatedDate = systemDate;
            MaterialGuid = materal;
            NumberOrderd = quantity;
            RepeatJob = repeatJob;
        }


        public bool ActionCommand(Game game)
        {
            _staticData = game.StaticData;
            if(IsRefineOrderValid(game.GlobalManager))
            {    
                RefiningProcessor.AddJob(_staticData, _targetentity, _job);
                return true;
            }
            return false;
        }

        private bool IsRefineOrderValid(EntityManager globalManager)
        {
            if(CommandHelpers.IsCommandValid(globalManager, RequestingFactionGuid, TargetEntityGuid, out _factionEntity, out _targetentity))
            {
                if(_staticData.ProcessedMaterials.ContainsKey(MaterialGuid))
                {
                    int pointCost = _staticData.ProcessedMaterials[MaterialGuid].RefineryPointCost;
                    _job = new RefineingJob(MaterialGuid, NumberOrderd, pointCost, RepeatJob);
                    return true;
                }
            }
            return false;
        }
    }
}

using System;
namespace Pulsar4X.ECSLib
{
    public class RefineOrdersVM
    {
        public RefineOrdersVM()
        {
        }
    }

    public class RefineOrdersCommand:IEntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid TargetEntityGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public Guid MaterialGuid { get; set; }
        public ushort numberOrderd { get; set; }
        public bool repeatJob { get; set; } = false;

        private Entity _targetentity;
        private Entity _factionEntity;
        private StaticDataStore _staticData;
        private RefineingJob _job;

        public bool ActionCommand(Game game)
        {
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
                    _job = new RefineingJob(MaterialGuid, numberOrderd, pointCost, repeatJob);
                    return true;
                }
            }
            return false;
        }
    }
}

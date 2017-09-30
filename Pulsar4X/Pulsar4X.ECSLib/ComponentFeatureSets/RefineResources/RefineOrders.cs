using System;

namespace Pulsar4X.ECSLib
{
    public class RefineOrdersCommand:EntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid EntityCommandingGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public Guid MaterialGuid { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;

        public int ActionLanes => 1; //blocks movement

        public bool IsRunning { get; private set; } = false;
        public bool IsBlocking => true;


        private Entity _entityCommanding;
        public Entity EntityCommanding{get{return _entityCommanding;}}


        private Entity _factionEntity;
        private StaticDataStore _staticData;
        private RefineingJob _job;



        public RefineOrdersCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid materal, ushort quantity, bool repeatJob )
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            MaterialGuid = materal;
            NumberOrderd = quantity;
            RepeatJob = repeatJob;
        }


        public void ActionCommand(Game game)
        {
            RefiningProcessor.AddJob(_staticData, _entityCommanding, _job);
            IsRunning = true;
        }


        public bool IsValidCommand(Game game)
        {       
            _staticData = game.StaticData;
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (_staticData.ProcessedMaterials.ContainsKey(MaterialGuid))
                {
                    int pointCost = _staticData.ProcessedMaterials[MaterialGuid].RefineryPointCost;
                    _job = new RefineingJob(MaterialGuid, NumberOrderd, pointCost, RepeatJob);
                    return true;
                }
            }
            return false;
        }

        public bool IsFinished()
        {
            if (_job.Auto == false && _job.NumberCompleted == _job.NumberOrdered)
            {
                return true;
            }
            return false;
        }
    }

    public class RePrioritizeCommand : EntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid EntityCommandingGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public Guid JobID { get; set; }
        public short Delta { get; set; }

        public int ActionLanes => 0;

        public bool IsBlocking => false;
        public bool IsRunning { get; private set; } = false;
        private Entity _factionEntity;

        private Entity _entityCommanding;
        public Entity EntityCommanding { get { return _entityCommanding; } }

        public RePrioritizeCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID, short delta)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            Delta = delta;
        }

        public void ActionCommand(Game game)
        {
            RefiningProcessor.ChangeJobPriority(_entityCommanding, JobID, Delta);
            IsRunning = true;
        }

        private bool IsOrderValid(EntityManager globalManager)
        {
            if (CommandHelpers.IsCommandValid(globalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
               return true;
            }
            return false;
        }

        public bool IsValidCommand(Game game)
        {
            if (IsOrderValid(game.GlobalManager))
            {
                return true;
            }
            return false;
        }

        public bool IsFinished()
        {
            return IsRunning;//its run once, therefore it's finished.
        }

    }
}

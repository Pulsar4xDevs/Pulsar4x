using System;

namespace Pulsar4X.ECSLib
{
    public class RefineOrdersCommand:EntityCommand
    {

        public Guid MaterialGuid { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;

        internal override int ActionLanes => 1; //blocks movement
        internal override bool IsBlocking => true;


        private Entity _entityCommanding;
        internal override Entity EntityCommanding{get{return _entityCommanding;}}


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


        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                RefiningProcessor.AddJob(_staticData, _entityCommanding, _job);
                IsRunning = true;
            }
        }


        internal override bool IsValidCommand(Game game)
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

        internal override bool IsFinished()
        {
            if (_job.Auto == false && _job.NumberCompleted == _job.NumberOrdered)
            {
                return true;
            }
            return false;
        }
    
    }
    
    public class CancelJob : EntityCommand
    {
        public Guid JobID { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public CancelJob(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
        }

        internal override void ActionCommand(Game game)
        {
            var jobBatchList = _entityCommanding.GetDataBlob<RefiningDB>().JobBatchList;
            lock (jobBatchList)
            {
                var job = jobBatchList.Find((obj) => obj.JobID == this.JobID);

                if (job != null)//.Contains(job))
                {
                    jobBatchList.Remove(job);
                }
            }

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

        internal override bool IsValidCommand(Game game)
        {
            if (IsOrderValid(game.GlobalManager))
            {
                return true;
            }
            return false;
        }

        internal override bool IsFinished()
        {
            return IsRunning;//its run once, therefore it's finished.
        }

    }

    public class ChangeRepeatJob : EntityCommand
    {
        public Guid JobID { get; set; }
        public bool Repeats { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public ChangeRepeatJob(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID, bool repeats)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            Repeats = repeats;
        }

        internal override void ActionCommand(Game game)
        {
            var jobBatchList = _entityCommanding.GetDataBlob<RefiningDB>().JobBatchList;
            lock (jobBatchList)
            {
                var job = jobBatchList.Find((obj) => obj.JobID == this.JobID);

                if (job != null)//.Contains(job))
                {
                    job.Auto = Repeats;
                }
            }
                    
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

        internal override bool IsValidCommand(Game game)
        {
            if (IsOrderValid(game.GlobalManager))
            {
                return true;
            }
            return false;
        }

        internal override bool IsFinished()
        {
            return IsRunning;//its run once, therefore it's finished.
        }

    }
    public class RePrioritizeCommand : EntityCommand
    {
        public Guid JobID { get; set; }
        public short Delta { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public RePrioritizeCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID, short delta)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            Delta = delta;
        }

        internal override void ActionCommand(Game game)
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

        internal override bool IsValidCommand(Game game)
        {
            if (IsOrderValid(game.GlobalManager))
            {
                return true;
            }
            return false;
        }

        internal override bool IsFinished()
        {
            return IsRunning;//its run once, therefore it's finished.
        }

    }
}

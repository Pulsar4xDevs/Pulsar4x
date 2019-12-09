using System;

namespace Pulsar4X.ECSLib
{
    public class ConstructItemCommand:EntityCommand
    {

        public Guid DesignGuid { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;

        
        internal override int ActionLanes => 1; //blocks movement
        internal override bool IsBlocking => true;


        private Entity _entityCommanding;
        internal override Entity EntityCommanding{get{return _entityCommanding;}}


        private Entity _factionEntity;
        private ComponentDesign _design;
        private StaticDataStore _staticData;
        private ConstructionJob _job;



        public ConstructItemCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid designGuid, ushort quantity, bool repeatJob )
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            DesignGuid = designGuid;
            NumberOrderd = quantity;
            RepeatJob = repeatJob;
            UseActionLanes = false;
        }


        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                ConstructionProcessor.AddJob(_factionEntity.GetDataBlob<FactionInfoDB>(), _entityCommanding, _job);
                IsRunning = true;
            }
        }


        internal override bool IsValidCommand(Game game)
        {       
            _staticData = game.StaticData;
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var factionInfo = _factionEntity.GetDataBlob<FactionInfoDB>();
                if (factionInfo.ComponentDesigns.ContainsKey(DesignGuid))
                {
                    _design = factionInfo.ComponentDesigns[DesignGuid];
                    _job = new ConstructionJob(_design, NumberOrderd, RepeatJob);
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
    
    public class ConstructCancelJob : EntityCommand
    {
        public Guid JobID { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public ConstructCancelJob(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            UseActionLanes = false;
        }

        internal override void ActionCommand(Game game)
        {
            var jobBatchList = _entityCommanding.GetDataBlob<ConstructAbilityDB>().JobBatchList;
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

    public class ConstructChangeRepeatJob : EntityCommand
    {
        public Guid JobID { get; set; }
        public bool Repeats { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public ConstructChangeRepeatJob(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID, bool repeats)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            Repeats = repeats;
            UseActionLanes = false;
        }

        internal override void ActionCommand(Game game)
        {
            var jobBatchList = _entityCommanding.GetDataBlob<ConstructAbilityDB>().JobBatchList;
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
    public class ConstructRePrioritizeCommand : EntityCommand
    {
        public Guid JobID { get; set; }
        public short Delta { get; set; }

        internal override int ActionLanes => 0;

        internal override bool IsBlocking => false;

        private Entity _factionEntity;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public ConstructRePrioritizeCommand(Guid factionGuid, Guid thisEntity, DateTime systemDate, Guid jobID, short delta)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity;
            CreatedDate = systemDate;
            JobID = jobID;
            Delta = delta;
            UseActionLanes = false;
        }

        internal override void ActionCommand(Game game)
        {
            ConstructionProcessor.ChangeJobPriority(_entityCommanding, JobID, Delta);
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
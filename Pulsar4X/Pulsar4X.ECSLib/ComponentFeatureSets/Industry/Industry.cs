using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Industry
{
    [Flags]
    public enum ConstructionType
    {
        None            = 1 << 1,
        Installations   = 1 << 2,
        ShipComponents  = 1 << 3,
        Ships           = 1 << 4,
        Fighters        = 1 << 5,
        Ordnance        = 1 << 6,
    }

    public interface IIndustryDB
    {
        int PointsPerTick { get; }
        
        List<JobBase> JobBatchList { get; }

        List<ICargoable> GetJobItems(FactionInfoDB factionInfoDB);

    }

    public abstract class JobBase
    {
        public virtual string Name { get; internal set; }
        public Guid JobID = Guid.NewGuid();
        public Guid ItemGuid { get; protected set; }
        public ushort NumberOrdered { get; internal set; }
        public ushort NumberCompleted { get; internal set; }
        public int ProductionPointsLeft { get; internal set; }
        public int ProductionPointsCost { get; protected set; }
        public bool Auto { get; internal set; }

        public JobBase()
        {
        }

        public JobBase(Guid guid, ushort numberOrderd, int jobPoints, bool auto)
        {
            ItemGuid = guid;
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            ProductionPointsLeft = jobPoints;
            ProductionPointsCost = jobPoints;
            Auto = auto;
        }

        public abstract void InitialiseJob(FactionInfoDB infoDB, Entity industryEntity, Guid guid, ushort numberOrderd, bool auto);

    }

    public static class IndustryTools
    {
        /// <summary>
        /// Adds a job to an entities industry
        /// </summary>
        /// <param name="industryEntity"></param>
        /// <param name="job"></param>
        [PublicAPI]
        public static void AddJob<T, U>(Entity industryEntity, U job) where T: BaseDataBlob, IIndustryDB where U: JobBase
        {
            var industryDB = industryEntity.GetDataBlob<T>();
            lock (industryDB.JobBatchList) //prevent threaded race conditions
            {
                industryDB.JobBatchList.Add(job);
            }
        }
        
        /// <summary>
        /// Changes priority in the job queue
        /// </summary>
        /// <param name="industryEntity"></param>
        /// <param name="jobID"></param>
        /// <param name="delta">-1 increase priority, +1 decrease</param>
        /// <typeparam name="T"></typeparam>
        public static void ChangeJobPriority<T>(Entity industryEntity, Guid jobID, int delta) where T: BaseDataBlob, IIndustryDB
        {
            var industryDB = industryEntity.GetDataBlob<T>();
            
            lock (industryDB.JobBatchList) //prevent threaded race conditions
            {
                //first check that the job does still exsist in the list.
                var job = industryDB.JobBatchList.Find((obj) => obj.JobID == jobID);
                if (job != null)
                {
                    var currentIndex = industryDB.JobBatchList.IndexOf(job);
                    var newIndex = currentIndex + delta;
                    if (newIndex <= 0)
                    {
                        industryDB.JobBatchList.RemoveAt(currentIndex);
                        industryDB.JobBatchList.Insert(0, job);
                    }
                    else if (newIndex >= industryDB.JobBatchList.Count - 1)
                    {
                        industryDB.JobBatchList.RemoveAt(currentIndex);
                        industryDB.JobBatchList.Add(job);
                    }
                    else
                    {
                        industryDB.JobBatchList.RemoveAt(currentIndex);
                        industryDB.JobBatchList.Insert(newIndex, job);
                    }
                }
            }
        }

        /// <summary>
        /// change the number ordered, whether it repeats etc.
        /// </summary>
        /// <param name="industryEntity"></param>
        /// <param name="jobID"></param>
        /// <param name="RepeatJob"></param>
        /// <param name="NumberOrderd"></param>
        /// <param name="autoInstall"></param>
        /// <typeparam name="T"></typeparam>
        public static void EditExsistingJob<T>(Entity industryEntity, Guid jobID, bool RepeatJob = false, ushort NumberOrderd = 1, bool autoInstall = false) where T: BaseDataBlob, IIndustryDB
        {
            var jobBatchList = industryEntity.GetDataBlob<T>().JobBatchList;
            lock (jobBatchList)
            {
                var job = jobBatchList.Find((obj) => obj.JobID == jobID);
                if (job != null)//.Contains(job))
                {
                    job.Auto = RepeatJob;
                    job.NumberOrdered = NumberOrderd;
                    if (job is ConstructJob)
                    {
                        var cj = (ConstructJob)job;
                        cj.InstallOn = industryEntity;
                    }
                }
            }
        }

        /// <summary>
        /// as per the tin.
        /// </summary>
        /// <param name="industryEntity"></param>
        /// <param name="jobID"></param>
        /// <typeparam name="T"></typeparam>
        public static void CancelExsistingJob<T>(Entity industryEntity, Guid jobID) where T: BaseDataBlob, IIndustryDB
        {
            var jobBatchList = industryEntity.GetDataBlob<T>().JobBatchList;
            lock (jobBatchList)
            {
                var job = jobBatchList.Find((obj) => obj.JobID == jobID);

                if (job != null)//.Contains(job))
                {
                    jobBatchList.Remove(job);
                }
            }
        }
    }


    public class IndustryOrder<T, U>:EntityCommand where T: BaseDataBlob, IIndustryDB where U: JobBase
    {

        public enum OrderTypeEnum
        {
            NewJob,
            CancelJob,
            ChangePriority,
            EditJob
        }
        public OrderTypeEnum OrderType;
        
        public Guid ItemID { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;
        public bool AutoInstall { get; set; } = false;
        
        public short Delta { get; set; }
        
        internal override int ActionLanes => 1; //blocks movement
        internal override bool IsBlocking => true;


        private Entity _entityCommanding;
        internal override Entity EntityCommanding{get{return _entityCommanding;}}


        private Entity _factionEntity;
        private ComponentDesign _design;
        private StaticDataStore _staticData;
        private U _job;



        public static IndustryOrder<T, U> CreateNewJobOrder(
            Guid factionGuid, Entity thisEntity,
            U jobItem
        )
        {
            
            IndustryOrder<T, U> order = new IndustryOrder<T, U>(factionGuid, thisEntity);
            order._job = jobItem;
            order.OrderType = OrderTypeEnum.NewJob;
            order.ItemID = jobItem.ItemGuid;
            return order;
        }

        
        
        public static IndustryOrder<T, U> CreateCancelJobOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID
        )
        {
            IndustryOrder<T, U> order = new IndustryOrder<T, U>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.CancelJob;
            order.ItemID = OrderID;
            return order;
        }
        
        public static IndustryOrder<T, U> CreateChangePriorityOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID, short delta
        )
        {
            IndustryOrder<T, U> order = new IndustryOrder<T, U>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.ChangePriority;
            order.ItemID = OrderID;
            order.Delta = delta;
            return order;
        }
        
        public static IndustryOrder<T, U> CreateEditJobOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID, ushort quantity = 1, bool repeatJob = false, bool autoInstall = false
        )
        {
            IndustryOrder<T, U> order = new IndustryOrder<T, U>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.ChangePriority;
            order.ItemID = OrderID;
            order.NumberOrderd = quantity;
            order.RepeatJob = repeatJob;
            order.AutoInstall = autoInstall;
            return order;
        }
        
        
        private IndustryOrder(Guid factionGuid, Entity thisEntity)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity.Guid;
            CreatedDate = thisEntity.StarSysDateTime;
            UseActionLanes = false;
        }
        

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                switch (OrderType)
                {
                    case OrderTypeEnum.NewJob:
                        IndustryTools.AddJob<T,U>( _entityCommanding, _job);
                        break;
                    case OrderTypeEnum.CancelJob:
                        IndustryTools.CancelExsistingJob<T>(_entityCommanding, ItemID);
                        break;
                    case OrderTypeEnum.EditJob:
                        IndustryTools.EditExsistingJob<T>(_entityCommanding, ItemID, RepeatJob, NumberOrderd, AutoInstall);
                        break;
                    case OrderTypeEnum.ChangePriority:
                        IndustryTools.ChangeJobPriority<T>(_entityCommanding, ItemID, Delta);
                        break;
                }
                

                IsRunning = true;
            }
        }

        internal override bool IsValidCommand(Game game)
        {       
            _staticData = game.StaticData;
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var factionInfo = _factionEntity.GetDataBlob<FactionInfoDB>();
                //_job.InitialiseJob(factionInfo, _entityCommanding, ItemID, NumberOrderd, RepeatJob);
                
                /* TODO: should we check this? do we need to?
                if (factionInfo.ComponentDesigns.ContainsKey(ItemID))
                {
                    _design = factionInfo.ComponentDesigns[ItemID];
                    _job = new ConstructJob(_design, NumberOrderd, RepeatJob);
                    if (_design.ConstructionType.HasFlag(ConstructionType.Installations))
                        _job.InstallOn = _entityCommanding;
                    return true;
                    
                }
                */
                return true;
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
}
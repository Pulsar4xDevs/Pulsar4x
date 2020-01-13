using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.Industry
{
    [Flags]
    public enum ConstructionType
    {
        None            = 1 << 1,
        Installations   = 1 << 2,
        ShipComponents  = 1 << 3,
        Fighters        = 1 << 4,
        Ordnance        = 1 << 5,
    }

    public interface IIndustryDB
    {
        int ConstructionPoints { get; }
        
        List<JobBase> JobBatchList { get; }

        List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB);

    }

    public class IndustryAbilityDB : BaseDataBlob
    {

        public class ProductionLine
        {
            public string FacName;
            public double MaxVolume;
            public Dictionary<Guid, int> IndustryTypeRates = new Dictionary<Guid, int>();
            public List<IndustryJob> Jobs = new List<IndustryJob>();
        }
        
        //public int ConstructionPoints { get; } = 0;
        //public List<JobBase> JobBatchList { get; } = new List<JobBase>();
        
        //public Dictionary<Guid, int> IndustryTypeRates { get; } = new Dictionary<Guid, int>();
        //public Dictionary<Guid, List<JobBase>> JobsBytype = new Dictionary<Guid, List<JobBase>>();


        public Dictionary<Guid, ProductionLine> ProductionLines { get; } = new Dictionary<Guid, ProductionLine>();
        
        public IndustryAbilityDB(Dictionary<Guid, ProductionLine> productionLines)
        {
            ProductionLines = productionLines;
        }
        public IndustryAbilityDB(Guid componentID, ProductionLine productionLine)
        {
            ProductionLines.Add(componentID, productionLine);
        }

        public IndustryAbilityDB(IndustryAbilityDB db)
        {
            //IndustryTypeRates = new Dictionary<Guid, int>(db.IndustryTypeRates);
            ProductionLines = new Dictionary<Guid, ProductionLine>(db.ProductionLines);
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }


        public List<IConstrucableDesign> GetJobItems(FactionInfoDB factionInfoDB)
        {
            return factionInfoDB.IndustryDesigns.Values.ToList();
        }
    }

    public class IndustryAtb : IComponentDesignAttribute
    {
        [JsonProperty] 
        public Dictionary<Guid, int> IndustryPoints { get; private set; } = new Dictionary<Guid, int>();

        [JsonProperty] 
        private double MaxProductionVolume;

        public IndustryAtb(Dictionary<Guid, double> industryRates)
        {
            MaxProductionVolume = double.PositiveInfinity;
            
            int i = 0;
            foreach (var kvp in industryRates)
            {
                IndustryPoints[kvp.Key] = (int)kvp.Value;
                i++;
            }
        }
        
        public IndustryAtb(Dictionary<Guid, double> industryRates, double maxProductionVolume)
        {
            MaxProductionVolume = maxProductionVolume;
            
            int i = 0;
            foreach (var kvp in industryRates)
            {
                IndustryPoints[kvp.Key] = (int)kvp.Value;
                i++;
            }
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var db = parentEntity.GetDataBlob<IndustryAbilityDB>();
            IndustryAbilityDB.ProductionLine newline = new IndustryAbilityDB.ProductionLine();
            newline.MaxVolume = MaxProductionVolume;
            newline.IndustryTypeRates = IndustryPoints;
            newline.FacName = componentInstance.Name;

            if (db == null)
            {
                db = new IndustryAbilityDB(componentInstance.ID, newline);
                parentEntity.SetDataBlob(db);
            }
            else
            {
                db.ProductionLines.Add(componentInstance.ID, newline);
            }
        }
    }

    public interface IConstrucableDesign
    {
        Guid ID { get;  }
        string Name { get;  } //player defined name. ie "5t 2kn Thruster".

        Dictionary<Guid, int> ResourceCosts { get; }

        int IndustryPointCosts { get; }
        Guid IndustryTypeID { get; }

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

        public Dictionary<Guid, int> ResourcesRequired { get; internal set; } = new Dictionary<Guid, int>();
        
        
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

        public abstract void InitialiseJob(ushort numberOrderd, bool auto);

    }

    public class IndustryJob : JobBase
    {
        internal Guid TypeID;

        public IndustryJob(FactionInfoDB factionInfo, Guid itemID)
        {
            ItemGuid = itemID;
            var design = factionInfo.IndustryDesigns[itemID];
            TypeID = design.IndustryTypeID;
            Name = design.Name;
            ResourcesRequired = design.ResourceCosts;
            ProductionPointsLeft = design.IndustryPointCosts;
            ProductionPointsCost = design.IndustryPointCosts;
            NumberOrdered = 1;
        }

        public override void InitialiseJob(ushort numberOrderd, bool auto)
        {
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            Auto = auto;
        }
    }

    public static class IndustryTools
    {
        /// <summary>
        /// Adds a job to an entities industry
        /// </summary>
        /// <param name="industryEntity"></param>
        /// <param name="job"></param>
        [PublicAPI]
        public static void AddJob<T>(Entity industryEntity, JobBase job) where T: BaseDataBlob, IIndustryDB
        {
            var industryDB = industryEntity.GetDataBlob<T>();
            lock (industryDB.JobBatchList) //prevent threaded race conditions
            {
                industryDB.JobBatchList.Add(job);
            }
        }
        
        public static void AddJob(Entity industryEntity, Guid plineID, IndustryJob job) 
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            AddJob(industryDB, plineID, job);
        }
        
        public static void AddJob(IndustryAbilityDB industryDB, Guid plineID, IndustryJob job)
        {
            lock(industryDB.ProductionLines[plineID])
            {
                var pline = industryDB.ProductionLines[plineID];
                pline.Jobs.Add(job);
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


        public static void ChangeJobPriority(Entity industryEntity, Guid prodLine, Guid jobID, int delta) 
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                var currentIndex = jobList.IndexOf(job);
                var newIndex = currentIndex + delta;
                if (newIndex <= 0)
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Insert(0, job);
                }
                else if (newIndex >= jobList.Count - 1)
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Add(job);
                }
                else
                {
                    jobList.RemoveAt(currentIndex);
                    jobList.Insert(newIndex, job);
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

        public static void EditExsistingJob(Entity industryEntity, Guid prodLine, Guid jobID, bool RepeatJob = false, ushort NumberOrderd = 1, bool autoInstall = false)
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                job.Auto = RepeatJob;
                job.NumberOrdered = NumberOrderd;
                /*if (job is ConstructJob)
                {
                    var cj = (ConstructJob)job;
                    cj.InstallOn = industryEntity;
                }*/
                
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
       
        public static void CancelExsistingJob(Entity industryEntity, Guid prodLine, Guid jobID) 
        {
            var industryDB = industryEntity.GetDataBlob<IndustryAbilityDB>();
            var jobList = industryDB.ProductionLines[prodLine].Jobs;
            //first check that the job does still exsist in the list.
            var job = jobList.Find((obj) => obj.JobID == jobID);
            if (job != null)
            {
                jobList.Remove(job);
            }
        }

        

        
        internal static void ConstructStuff(Entity colony)
        {
            CargoStorageDB stockpile = colony.GetDataBlob<CargoStorageDB>();
            Entity faction;
            colony.Manager.FindEntityByGuid(colony.FactionOwner, out faction);
            var factionInfo = faction.GetDataBlob<FactionInfoDB>();
            var industryDB = colony.GetDataBlob<IndustryAbilityDB>();

            //var pointRates = industryDB.IndustryTypeRates;
            //int maxPoints = industryDB.ConstructionPoints; 

            
            //List<JobBase> constructionJobs = new List<JobBase>(industryDB.JobBatchList);
            foreach (var kvp in industryDB.ProductionLines)
            {
                Guid prodLineID = kvp.Key;
                var prodLine = kvp.Value;
                var industryPointsRemaining = new Dictionary<Guid, int>( prodLine.IndustryTypeRates);
                List<IndustryJob> Joblist = prodLine.Jobs;
                float productionPercentage = 1;
                
                

                for (int i = 0; i < Joblist.Count; i++)
                {
                    JobBase batchJob = Joblist[i];
                    IConstrucableDesign designInfo = factionInfo.IndustryDesigns[batchJob.ItemGuid];
                    float pointsToUse = industryPointsRemaining[designInfo.IndustryTypeID] * productionPercentage;
                    //total number of resources requred for a single job in this batch
                    var resourceSum = designInfo.ResourceCosts.Sum(item => item.Value);
                    //how many construction points each resourcepoint is worth.
                    float pointPerResource = (float)designInfo.IndustryPointCosts / resourceSum;

                    while (
                        productionPercentage > 0 && 
                        batchJob.NumberCompleted < batchJob.NumberOrdered &&
                        pointsToUse > 0)
                    {
                        //gather availible resorces for this job.
                        //right now we take all the resources we can, for an individual item in the batch. 
                        //even if we're taking more than we can use in this turn, we're using/storing it. 
                        IDictionary<Guid, int> resourceCosts = batchJob.ResourcesRequired;
                        //Note: this is editing batchjob.ResourcesRequired variable. 
                        ConsumeResources(stockpile, ref resourceCosts);
                        //we calculate the difference between the design resources and the amount of resources we've squirreled away. 
                    
                        
                        // this is the total of the resources that we don't have access to for this item. 
                        int unusableResourceSum = resourceCosts.Sum(item => item.Value);
                        // this is the total resources that can be used on this item. 
                        int useableResourcePoints = resourceSum - unusableResourceSum;
                        
                        pointsToUse = Math.Min(industryPointsRemaining[designInfo.IndustryTypeID], batchJob.ProductionPointsLeft);
                        pointsToUse = Math.Min(pointsToUse, useableResourcePoints * pointPerResource);

                        var remainingPoints = industryPointsRemaining[designInfo.IndustryTypeID] - pointsToUse;
                        
                        
                        if(pointsToUse < 0)
                            throw new Exception("Can't have negative production");
                        
                        //construct only enough for the amount of resources we have. 
                        batchJob.ProductionPointsLeft -= (int)Math.Floor(pointsToUse);
                        
                        productionPercentage -= remainingPoints * productionPercentage;    
                        
                        if (batchJob.ProductionPointsLeft == 0)
                        {
                            BatchJobItemComplete(colony, stockpile, batchJob, designInfo);
                        }
                    }
                }
            }
        }
        
        internal static void ConsumeResources(CargoStorageDB fromCargo, ref IDictionary<Guid, int> toUse)
        {   
            foreach (KeyValuePair<Guid, int> kvp in toUse.ToArray())
            {             
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(kvp.Key);//fromCargo.OwningEntity.Manager.Game.StaticData.GetICargoable(kvp.Key);
                
                Guid cargoTypeID = cargoItem.CargoTypeID;
                int amountUsedThisTick = 0;
                if (fromCargo.StoredCargoTypes.ContainsKey(cargoTypeID))
                {
                    if (fromCargo.StoredCargoTypes[cargoTypeID].ItemsAndAmounts.ContainsKey(cargoItem.ID))
                    {
                        amountUsedThisTick = Math.Min((int)fromCargo.StoredCargoTypes[cargoTypeID].ItemsAndAmounts[cargoItem.ID].amount, kvp.Value);
                    }
                }

                if (amountUsedThisTick > 0)
                {
                    StorageSpaceProcessor.RemoveCargo(fromCargo, cargoItem, amountUsedThisTick);
                    toUse[kvp.Key] -= amountUsedThisTick;
                }
            }         
        }


        static void BatchJobItemComplete(Entity industryEntity, CargoStorageDB storage, JobBase batchJob, IConstrucableDesign designInfo)
        {
            batchJob.NumberCompleted++; 
            switch (batchJob)
            {
                case RefineingJob r:
                    RefiningProcessor.BatchJobItemComplete(industryEntity, storage, (RefineingJob)batchJob, (ProcessedMaterialSD)designInfo);
                    break;
                case ConstructJob c:
                    ConstructionProcessor.BatchJobItemComplete(industryEntity, storage, (ConstructJob)batchJob, (ComponentDesign)designInfo);
                    break;
                case ShipYardJob s:
                    ShipyardProcessor.BatchJobShipComplete(industryEntity, (ShipYardJob)batchJob, (ShipDesign)designInfo);
                    break;
            }
        }
    }


    public class IndustryOrder<T>:EntityCommand where T: BaseDataBlob, IIndustryDB 
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
        private JobBase _job;



        public static IndustryOrder<T> CreateNewJobOrder(
            Guid factionGuid, Entity thisEntity,
            JobBase jobItem
        )
        {
            
            IndustryOrder<T> order = new IndustryOrder<T>(factionGuid, thisEntity);
            order._job = jobItem;
            order.OrderType = OrderTypeEnum.NewJob;
            order.ItemID = jobItem.ItemGuid;
            return order;
        }

        
        
        public static IndustryOrder<T> CreateCancelJobOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID
        )
        {
            IndustryOrder<T> order = new IndustryOrder<T>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.CancelJob;
            order.ItemID = OrderID;
            return order;
        }
        
        public static IndustryOrder<T> CreateChangePriorityOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID, short delta
        )
        {
            IndustryOrder<T> order = new IndustryOrder<T>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.ChangePriority;
            order.ItemID = OrderID;
            order.Delta = delta;
            return order;
        }
        
        public static IndustryOrder<T> CreateEditJobOrder(
            Guid factionGuid, Entity thisEntity,
            Guid OrderID, ushort quantity = 1, bool repeatJob = false, bool autoInstall = false
        )
        {
            IndustryOrder<T> order = new IndustryOrder<T>(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.EditJob;
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
                        IndustryTools.AddJob<T>( _entityCommanding, _job);
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
                    if (_design.IndustryTypeID.HasFlag(IndustryTypeID.Installations))
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
    
    
    public class IndustryOrder2:EntityCommand 
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

        private Guid productionLineID;
        internal override Entity EntityCommanding{get{return _entityCommanding;}}


        private Entity _factionEntity;
        private ComponentDesign _design;
        private IndustryJob _job;



        public static IndustryOrder2 CreateNewJobOrder(
            Guid factionGuid, Entity thisEntity, Guid productionLineID,
            IndustryJob jobItem
        )
        {
            
            IndustryOrder2 order = new IndustryOrder2(factionGuid, thisEntity);
            order._job = jobItem;
            order.OrderType = OrderTypeEnum.NewJob;
            order.ItemID = jobItem.ItemGuid;
            order.productionLineID = productionLineID;
            return order;
        }

        
        
        public static IndustryOrder2 CreateCancelJobOrder(
            Guid factionGuid, Entity thisEntity, Guid productionLineID,
            Guid OrderID
        )
        {
            IndustryOrder2 order = new IndustryOrder2(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.CancelJob;
            order.ItemID = OrderID;
            order.productionLineID = productionLineID;
            return order;
        }
        
        public static IndustryOrder2 CreateChangePriorityOrder(
            Guid factionGuid, Entity thisEntity,
            Guid productionLineID,
            Guid OrderID, short delta
        )
        {
            IndustryOrder2 order = new IndustryOrder2(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.ChangePriority;
            order.ItemID = OrderID;
            order.Delta = delta;
            order.productionLineID = productionLineID;
            return order;
        }
        
        public static IndustryOrder2 CreateEditJobOrder(
            Guid factionGuid, Entity thisEntity, Guid productionLineID,
            Guid OrderID, ushort quantity = 1, bool repeatJob = false, bool autoInstall = false
        )
        {
            IndustryOrder2 order = new IndustryOrder2(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.EditJob;
            order.ItemID = OrderID;
            order.NumberOrderd = quantity;
            order.RepeatJob = repeatJob;
            order.AutoInstall = autoInstall;
            order.productionLineID = productionLineID;
            return order;
        }
        
        
        private IndustryOrder2(Guid factionGuid, Entity thisEntity)
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
                        IndustryTools.AddJob( _entityCommanding, productionLineID, _job);
                        break;
                    case OrderTypeEnum.CancelJob:
                        IndustryTools.CancelExsistingJob(_entityCommanding, productionLineID, ItemID);
                        break;
                    case OrderTypeEnum.EditJob:
                        IndustryTools.EditExsistingJob(_entityCommanding, productionLineID, ItemID, RepeatJob, NumberOrderd, AutoInstall);
                        break;
                    case OrderTypeEnum.ChangePriority:
                        IndustryTools.ChangeJobPriority(_entityCommanding, productionLineID, ItemID, Delta);
                        break;
                }
                

                IsRunning = true;
            }
        }

        internal override bool IsValidCommand(Game game)
        {       
            
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                var factionInfo = _factionEntity.GetDataBlob<FactionInfoDB>();
                //_job.InitialiseJob(factionInfo, _entityCommanding, ItemID, NumberOrderd, RepeatJob);
                
                /* TODO: should we check this? do we need to?
                if (factionInfo.ComponentDesigns.ContainsKey(ItemID))
                {
                    _design = factionInfo.ComponentDesigns[ItemID];
                    _job = new ConstructJob(_design, NumberOrderd, RepeatJob);
                    if (_design.IndustryTypeID.HasFlag(IndustryTypeID.Installations))
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
using System;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Engine.Orders
{
    public class IndustryOrder2 : EntityCommand
    {

        public override string Name
        {
            get
            {
                return "Industry: " + OrderType.ToString();
            }
        }


        public override string Details { get; } = "Instant";

        public enum OrderTypeEnum
        {
            NewJob,
            CancelJob,
            ChangePriority,
            EditJob
        }
        public OrderTypeEnum OrderType;

        public string ItemID { get; set; }
        public ushort NumberOrderd { get; set; }
        public bool RepeatJob { get; set; } = false;
        public bool AutoInstall { get; set; } = false;

        public bool AutoAddSubJobs { get; set; } = true;
        public short Delta { get; set; }

        public override ActionLaneTypes ActionLanes => ActionLaneTypes.IneteractWithSelf;
        public override bool IsBlocking => true; //?why block?


        private Entity _entityCommanding;

        private string productionLineID;
        internal override Entity EntityCommanding{get{return _entityCommanding;}}


        private Entity _factionEntity;
        // private ComponentDesign _design;
        private IndustryJob _job;



        public static IndustryOrder2 CreateNewJobOrder(
            string factionGuid, Entity thisEntity, string productionLineID,
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
            string factionGuid, Entity thisEntity, string productionLineID,
            string OrderID
        )
        {
            IndustryOrder2 order = new IndustryOrder2(factionGuid, thisEntity);
            order.OrderType = OrderTypeEnum.CancelJob;
            order.ItemID = OrderID;
            order.productionLineID = productionLineID;
            return order;
        }

        public static IndustryOrder2 CreateChangePriorityOrder(
            string factionGuid, Entity thisEntity,
            string productionLineID,
            string OrderID, short delta
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
            string factionGuid, Entity thisEntity, string productionLineID,
            string OrderID, ushort quantity = 1, bool repeatJob = false, bool autoInstall = false
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


        private IndustryOrder2(string factionGuid, Entity thisEntity)
        {
            RequestingFactionGuid = factionGuid;
            EntityCommandingGuid = thisEntity.Guid;
            CreatedDate = thisEntity.StarSysDateTime;
            UseActionLanes = false;
        }


        internal override void Execute(DateTime atDateTime)
        {
            if (!IsRunning)
            {
                switch (OrderType)
                {
                    case OrderTypeEnum.NewJob:
                    {
                        if(AutoAddSubJobs)
                            IndustryTools.AutoAddSubJobs(_entityCommanding, _job);
                        IndustryTools.AddJob(_entityCommanding, productionLineID, _job);
                    }
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

        public override bool IsFinished()
        {
            if (_job.Auto == false && _job.NumberCompleted == _job.NumberOrdered)
            {
                return true;
            }
            return false;
        }

        public override EntityCommand Clone()
        {
            throw new NotImplementedException();
        }

    }
}
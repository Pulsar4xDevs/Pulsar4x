using System;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Engine.Orders
{
    public class ConditionItem
    {
        public string UniqueID { get; init; }
        public ICondition Condition { get; set; }
        public LogicalOperation? LogicalOperation { get; set; }

        public ConditionItem(ICondition condition, LogicalOperation? logicalOperation = null)
        {
            UniqueID = Guid.NewGuid().ToString();
            Condition = condition;
            LogicalOperation = logicalOperation;
        }
    }
}
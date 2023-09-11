using System;

namespace Pulsar4X.ECSLib
{
    public class ConditionItem
    {
        public Guid Guid { get; } = Guid.NewGuid();
        public ICondition Condition { get; set; }
        public LogicalOperation? LogicalOperation { get; set; }

        public ConditionItem(ICondition condition, LogicalOperation? logicalOperation = null)
        {
            Condition = condition;
            LogicalOperation = logicalOperation;
        }
    }
}
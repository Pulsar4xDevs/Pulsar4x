namespace Pulsar4X.ECSLib
{
    public class ConditionalOrder
    {
        public CompoundCondition Condition { get; }
        public IAction Action { get; set; }

        public bool IsValid
        {
            get
            {
                return Condition != null && Action != null;
            }
        }

        public ConditionalOrder()
        {
            Condition = new CompoundCondition();
            Action = null;

        }

        public ConditionalOrder(CompoundCondition condition, IAction action)
        {
            Condition = condition;
            Action = action;
        }
    }
}
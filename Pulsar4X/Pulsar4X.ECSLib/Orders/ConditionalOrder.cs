namespace Pulsar4X.ECSLib
{
    public class ConditionalOrder
    {
        public ICondition Condition { get; }
        public IAction Action { get; }

        public bool IsValid
        {
            get
            {
                return Condition != null && Action != null;
            }
        }

        public ConditionalOrder(ICondition condition, IAction action)
        {
            Condition = condition;
            Action = action;
        }
    }
}
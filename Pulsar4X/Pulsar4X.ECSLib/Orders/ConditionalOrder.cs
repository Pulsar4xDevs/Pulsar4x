namespace Pulsar4X.ECSLib
{
    public class ConditionalOrder
    {
        public ICondition Condition { get; }
        public IAction Action { get; }

        public ConditionalOrder(ICondition condition, IAction action)
        {
            Condition = condition;
            Action = action;
        }
    }
}
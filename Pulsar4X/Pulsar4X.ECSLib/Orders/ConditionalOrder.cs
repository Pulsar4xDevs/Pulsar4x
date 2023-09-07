using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ConditionalOrder
    {
        public string Name { get; set; }
        public CompoundCondition Condition { get; }
        public SafeList<IAction> Actions { get; }

        public bool IsValid
        {
            get
            {
                return Condition != null && Actions != null;
            }
        }

        public ConditionalOrder()
        {
            Condition = new CompoundCondition();
            Actions = new ();
        }

        public ConditionalOrder(CompoundCondition condition, SafeList<IAction> actions)
        {
            Condition = condition;
            Actions = actions;
        }
    }
}
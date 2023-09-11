using System.ComponentModel;

namespace Pulsar4X.ECSLib
{
    public enum ComparisonType
    {
        [Description("<")]
        LessThan,
        [Description("<=")]
        LessThanOrEqual,
        [Description("=")]
        EqualTo,
        [Description(">")]
        GreaterThan,
        [Description(">=")]
        GreaterThanOrEqual
    }

    public enum LogicalOperation
    {
        And,
        Or
    }

    public enum ConditionDisplayType
    {
        Comparison,
        Boolean,

    }

    public interface ICondition
    {
        public ConditionDisplayType DisplayType { get; }
        bool Evaluate(Entity fleet);
    }
}
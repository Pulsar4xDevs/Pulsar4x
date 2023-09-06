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
        [Description(">=")]
        GreaterThanOrEqual,
        [Description(">")]
        GreaterThan
    }
    public interface ICondition
    {
        bool Evaluate(Entity fleet);
    }
}
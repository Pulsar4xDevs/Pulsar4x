namespace Pulsar4X.ECSLib
{
    public abstract class ComparisonCondition : ICondition
    {
        public ComparisonType ComparisionType { get; set; }

        public float Threshold { get; set; }
        public float MaxValue { get; internal set; }
        public float MinValue {get; internal set; }
        public string Description { get; internal set; } = "";
        public ConditionDisplayType DisplayType { get; } = ConditionDisplayType.Comparison;

        public ComparisonCondition(float threshold, ComparisonType comparisonType)
        {
            Threshold = threshold;
            ComparisionType = comparisonType;
        }

        public abstract bool Evaluate(Entity fleet);
    }
}
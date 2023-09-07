using System;

namespace Pulsar4X.ECSLib
{
    public class FuelCondition : ComparisonCondition
    {
        public FuelCondition(float threshold, ComparisonType comparisonType) : base(threshold, comparisonType)
        {
            Description = "percent";
            MaxValue = 100;
            MinValue = 0;
        }

        public override bool Evaluate(Entity fleet)
        {
            switch(ComparisionType)
            {
                case ComparisonType.LessThan:
                    return false;
                case ComparisonType.LessThanOrEqual:
                    return false;
                case ComparisonType.EqualTo:
                    return false;
                case ComparisonType.GreaterThan:
                    return false;
                case ComparisonType.GreaterThanOrEqual:
                    return false;
                default:
                    throw new InvalidOperationException("Unknown comparison type.");
            }
        }
    }
}
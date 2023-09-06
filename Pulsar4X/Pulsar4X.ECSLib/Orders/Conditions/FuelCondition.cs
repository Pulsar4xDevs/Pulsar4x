using System;

namespace Pulsar4X.ECSLib
{
    public class FuelCondition : ComparisonCondition
    {
        private readonly float _threshold;

        public FuelCondition(float threshold, ComparisonType comparisonType) : base(comparisonType)
        {
            _threshold = threshold;
        }

        public override bool Evaluate(Entity fleet)
        {
            switch(_comparisionType)
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
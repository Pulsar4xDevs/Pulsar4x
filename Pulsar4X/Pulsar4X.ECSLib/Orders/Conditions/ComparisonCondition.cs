using System;

namespace Pulsar4X.ECSLib
{
    public abstract class ComparisonCondition : ICondition
    {
        protected readonly ComparisonType _comparisionType;

        public ComparisonCondition(ComparisonType comparisonType)
        {
            _comparisionType = comparisonType;
        }

        public abstract bool Evaluate(Entity fleet);
    }
}
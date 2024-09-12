using System.ComponentModel;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Interfaces
{
    public interface ICondition
    {
        public ConditionDisplayType DisplayType { get; }
        bool Evaluate(Entity fleet);
    }
}
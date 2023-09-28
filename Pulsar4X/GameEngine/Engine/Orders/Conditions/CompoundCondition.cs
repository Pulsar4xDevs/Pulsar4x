using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Engine.Orders
{
    public class CompoundCondition
    {
        public List<ConditionItem> ConditionItems { get; }

        public CompoundCondition(params ConditionItem[] conditionItems)
        {
            ConditionItems = conditionItems.ToList();
        }

        public bool Evaluate(Entity fleet)
        {
            // If there aren't any conditions this is always true
            if(!ConditionItems.Any())
                return true;

            List<bool> orResults = new ();
            bool? andResult = null;

            for(int i = 0; i < ConditionItems.Count; i++)
            {
                bool result = ConditionItems[i].Condition.Evaluate(fleet);

                if(ConditionItems[i].LogicalOperation == LogicalOperation.And || i == ConditionItems.Count - 1)
                {
                    // Group all the and results
                    if(andResult.HasValue)
                    {
                        andResult = andResult.Value && result;
                    }
                    else
                    {
                        andResult = result;
                    }

                    // If we reached the end or the next condition is Or store the and results
                    if(i == ConditionItems.Count - 1 || ConditionItems[i + 1].LogicalOperation == LogicalOperation.Or)
                    {
                        orResults.Add(andResult.Value);
                        andResult = null;
                    }
                }
            }

            return orResults.Any(r => r);
        }
    }
}
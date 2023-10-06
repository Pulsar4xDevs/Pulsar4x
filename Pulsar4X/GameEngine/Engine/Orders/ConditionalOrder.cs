using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Orders
{
    public class ConditionalOrder
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public CompoundCondition Condition { get; set; } = new CompoundCondition();

        [JsonProperty]
        public SafeList<EntityCommand> Actions { get; set; } = new ();

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                return Condition != null && Actions != null;
            }
        }

        public ConditionalOrder() { }

        public ConditionalOrder(CompoundCondition condition, SafeList<EntityCommand> actions)
        {
            Condition = condition;
            Actions = actions;
        }
    }
}
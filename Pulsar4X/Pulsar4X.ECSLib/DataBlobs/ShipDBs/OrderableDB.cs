using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// this datablob allows an entity to be orderable. 
    /// </summary>
    public class OrderableDB : BaseDataBlob
    {        
        [JsonProperty]
        public List<BaseAction> ActionQueue { get; } = new List<BaseAction>();
        
        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            ActionQueue = new List<BaseAction>(db.ActionQueue);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }
}
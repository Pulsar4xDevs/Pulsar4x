using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace GameEngine.Engine.Orders
{
    public class ActionQueueDB : BaseDataBlob
    {
        [JsonProperty]
        public SafeList<EntityAction> ActionList { get; } = new SafeList<EntityAction>();

        public ActionQueueDB()
        {
        }

        public ActionQueueDB(ActionQueueDB db)
        {
            ActionList = new SafeList<EntityAction>(db.ActionList);
        }

        public override object Clone()
        {
            return new ActionQueueDB(this);
        }
    }
}

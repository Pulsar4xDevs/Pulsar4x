using System.Collections.Generic;
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

        /// <summary>All actions currently in the queue that were spawned by the given goal.</summary>
        public List<EntityAction> ActionsFor(string parentGoalId)
        {
            var result = new List<EntityAction>();
            foreach (var action in ActionList)
                if (action.ParentGoalId == parentGoalId)
                    result.Add(action);
            return result;
        }

        public List<EntityAction> ActionsFor(Goal goal) => ActionsFor(goal.Id);

        public void Enqueue(EntityAction action) => ActionList.Add(action);

        public void ClearFor(Goal goal) => ActionList.RemoveAll(a => a.ParentGoalId == goal.Id);

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

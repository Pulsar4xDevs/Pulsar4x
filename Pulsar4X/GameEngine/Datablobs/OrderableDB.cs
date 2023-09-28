using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Datablobs
{
    public class OrderableDB : BaseDataBlob
    {
        public SafeList<EntityCommand> ActionList { get; } = new SafeList<EntityCommand>();

        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            ActionList = new SafeList<EntityCommand>(db.ActionList);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }
}

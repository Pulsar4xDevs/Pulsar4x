using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {

        internal List<EntityCommand> ActionList = new List<EntityCommand>();

        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            ActionList = new List<EntityCommand>(db.ActionList);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }


}

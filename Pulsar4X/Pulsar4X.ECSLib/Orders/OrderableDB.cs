using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {
        private readonly object _lockObj = new object();
        private List<EntityCommand> _actionList = new List<EntityCommand>();

        public List<EntityCommand> ActionList
        {
            get
            {
                return _actionList;
            }
        }

        public object Lock { get { return _lockObj; }}

        internal void AddCommand(EntityCommand command)
        {
            lock (_lockObj)
            {
                _actionList.Add(command);
            }
        }

        internal void RemoveAt(int index)
        {
            lock (_lockObj)
            {
                _actionList.RemoveAt(index);
            }
        }

        public List<EntityCommand> GetActionList()
        {
            //do I need a lock here?
            lock (_lockObj)
            {
                return new List<EntityCommand>(_actionList);
            }
        }

        public OrderableDB()
        {
        }

        public OrderableDB(OrderableDB db)
        {
            _actionList = new List<EntityCommand>(db._actionList);
        }

        public override object Clone()
        {
            return new OrderableDB(this);
        }
    }
}

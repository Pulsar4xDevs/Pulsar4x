using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {
        private readonly object _lockObj = new object();
        //private readonly object _lockListAccess = new object();
        private List<EntityCommand> _actionList = new List<EntityCommand>();
        internal void ProcessOrderList(DateTime atDateTime)
        {
            
            lock (_lockObj)
            {
                //var actionList = new List<EntityCommand>(_actionList);
                int mask = 0;

                int i = 0;
                while (i < _actionList.Count)
                {   var j = _actionList.Count;
                    EntityCommand entityCommand = _actionList[i];

                    if ((mask & ((int)entityCommand.ActionLanes)) == 0) //bitwise and
                    {
                        if (entityCommand.IsBlocking)
                        {
                            mask = mask | ((int)entityCommand.ActionLanes); //bitwise or
                        }
                        if (atDateTime >= entityCommand.ActionOnDate)
                        {
                            entityCommand.ActionCommand(atDateTime);
                        }
                    }

                    if (entityCommand.IsFinished())
                    {
                        if(j != _actionList.Count)
                            throw new Exception ("List Changed");
                        if(_actionList[i] != entityCommand)
                            throw new Exception("How is this possible");
                        _actionList.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
                //_actionList = actionList;
            }
        }
        
        internal void AddCommandToList(EntityCommand command)
        {
            if (command.ActionOnDate > OwningEntity.StarSysDateTime)
            {
                OwningEntity.Manager.ManagerSubpulses.AddEntityInterupt(command.ActionOnDate, nameof(OrderableProcessor), OwningEntity);
            }
            lock (_lockObj)
            {
                _actionList.Add(command);
            }
        }
        
        //public int Count => _actionList.Count;

        private void localAdd(EntityCommand command)
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

using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class OrderableDB : BaseDataBlob
    {

        public List<EntityCommand> ActionList = new List<EntityCommand>();

        
        internal void ProcessOrderList()
        {
            var atDatetime = OwningEntity.StarSysDateTime;
            int mask = 1;

            int i = 0;
            while (i < ActionList.Count)
            {
                EntityCommand entityCommand = ActionList[i];


                if ((mask & entityCommand.ActionLanes) == entityCommand.ActionLanes) //bitwise and
                {
                    if (entityCommand.IsBlocking)
                    {
                        mask |= entityCommand.ActionLanes; //bitwise or
                    }
                    if( atDatetime >= entityCommand.ActionOnDate)
                        entityCommand.ActionCommand();
                }
                if (entityCommand.IsFinished())
                    ActionList.RemoveAt(i);
                else
                    i++;
            }
        }
        
        internal void AddCommandToList(EntityCommand command)
        {
            if (command.ActionOnDate > OwningEntity.StarSysDateTime)
            {
                OwningEntity.Manager.ManagerSubpulses.AddEntityInterupt(command.ActionOnDate, nameof(OrderableProcessor), OwningEntity);
            }
            ActionList.Add(command);
        }
        
        public int Count => ActionList.Count;

        internal void RemoveAt(int index)
        {
            ActionList.RemoveAt(index);
        }

        public List<EntityCommand> GetActionList()
        {
            return new List<EntityCommand>( ActionList );
        }
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

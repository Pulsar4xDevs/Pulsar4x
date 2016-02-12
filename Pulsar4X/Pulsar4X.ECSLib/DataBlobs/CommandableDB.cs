using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class CommandableDB : BaseDataBlob
    {
        public Order CurrentOrder => SequentialOrders.Peek();
        public Queue<Order> SequentialOrders;
        public List<Order> ConditionalOrders; 
        
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}

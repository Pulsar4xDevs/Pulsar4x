using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib.GanttOrders
{
    public class GanttList
    {
        /// <summary>
        /// Nodes with orders that are running now and running consecutivly.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, GanttNode> CurrentNodes { get; } = new Dictionary<string, GanttNode>();
        [JsonProperty]
        private readonly Dictionary<Order, GanttNode> _allNodes = new Dictionary<Order, GanttNode>();

        public GanttList()
        {
        }

        public GanttList(GanttList ganttList)
        {
            _allNodes = new Dictionary<Order, GanttNode>(ganttList._allNodes);
            CurrentNodes = new Dictionary<string, GanttNode>(ganttList.CurrentNodes);
        }

        public void AddOrder(Order order)
        {
            GanttNode newNode = new GanttNode(order);
            if (order.StartAfter != null && _allNodes.ContainsKey(order.StartAfter))
            {
                _allNodes[order.StartAfter].PostCompletionNodes.Add(newNode);
            }
            else
            { //no startAfter defined so start it now.
                if (!CurrentNodes.ContainsKey(newNode.TypeCode))
                {
                    CurrentNodes.Add(newNode.TypeCode, newNode);
                    newNode.OrderObject.Processor.FirstProcess(newNode.OrderObject);
                }
                else
                {
                    //we can't have multiple processors of the same type consecutivly,
                    //ie cant move consecutivly, transfer cargo consecutivly, etc etc.
                    CurrentNodes[newNode.TypeCode].PostCompletionNodes.Add(newNode);
                }
            }
        }

        internal void ProcessCurrentNodes()
        {
            foreach (var kvp in CurrentNodes)
            {
                kvp.Value.OrderObject.Processor.ProcessOrder(kvp.Value.OrderObject);
            }
        }


        internal void OnNodeFinished(Order finishedOrder)
        {
            GanttNode finishedNode = _allNodes[finishedOrder];
            CurrentNodes.Remove(finishedNode.TypeCode);
            _allNodes.Remove(finishedNode.OrderObject);
            foreach (var nodeItem in finishedNode.PostCompletionNodes)
            {
               CurrentNodes.Add(nodeItem.TypeCode, nodeItem);
                nodeItem.OrderObject.Processor.FirstProcess(nodeItem.OrderObject);
            }
        }
    }

    public class GanttNode
    {
        internal List<GanttNode> PostCompletionNodes = new List<GanttNode>();
        internal string TypeCode; //this is the processor name
        public Order OrderObject { get; private set; }


        internal GanttNode( Order order)
        {
            OrderObject = order;
            TypeCode = nameof(order.Processor);
        }
    }
}
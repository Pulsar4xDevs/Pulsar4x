﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// A Node is uniquely identified by its string Key.  A Node also has a Data property of type object
    /// that can be used to store any extra information associated with the Node.
    ///
    /// The Node has a property of type AdjacencyList, which represents the node's neighbors.  To add a neighbor,
    /// the Node class exposes an AddDirected() method, which adds a directed edge with an (optional) weight to
    /// some other Node.  These methods are marked internal, and are called by the Graph class.
    /// </summary>
    public class Node
    {
        public string Key { get; protected set; }
        public object Data { get; protected set; }

        public List<EdgeToNeighbor> Neighbors { get; protected set; }


        protected Node()
        {
            Neighbors = new List<EdgeToNeighbor>();
        }

        public Node(string key) : this()
        {
            Key = key;
        }

        /// <summary>
        /// Adds a directed edge to this node.
        /// </summary>
        protected internal virtual void AddDirected(Node n, double cost)
        {
            AddDirected(new EdgeToNeighbor(n, cost));
        }

        /// <summary>
        /// Adds a directed edge to this node.
        /// </summary>
        protected internal virtual void AddDirected(EdgeToNeighbor e)
        {
            Neighbors.Add(e);
        }

        internal virtual bool IsNeighbor(Node v, out double cost)
        {
            cost = 0;
            foreach (EdgeToNeighbor edge in Neighbors)
            {
                if (edge.Neighbor == v)
                {
                    cost = edge.Cost;
                    return true;
                }
            }
            return false;
        }

        public virtual void ClearNeighbors()
        {
            Neighbors = new List<EdgeToNeighbor>();
        }
    }

    /// <summary>
    /// A pathfinding node.
    /// </summary>
    /// <remarks>
    /// A node consists of two jump points, in effect combining their positions on top of each other.
    /// It contains a list of "neighbor" nodes. Neighbors are any nodes that have jumppoints in the same system as the jumppoints in this node.
    /// </remarks>
    public class JPNode : Node
    {
        public new Tuple<Entity, Entity> Data { get; }

        internal JPNode(Entity jp1, Entity jp2, List<EdgeToNeighbor> neighbors)
        {
            Data = new Tuple<Entity, Entity>(jp1, jp2);
            Neighbors = neighbors;

            if (jp1.Id < jp2.Id)
            {
                Key = $"{jp1.Id} - {jp2.Id}";
            }
            else
            {
                Key = $"{jp2.Id} - {jp1.Id}";
            }
        }

        internal override bool IsNeighbor(Node other, out double cost)
        {
            cost = 0;
            var otherJPNode = other as JPNode;

            if (otherJPNode == null)
            {
                return false;
            }

            Entity thisJP1 = Data.Item1;
            Entity thisJP2 = Data.Item2;
            Entity otherJP1 = otherJPNode.Data.Item1;
            Entity otherJP2 = otherJPNode.Data.Item2;

            var jp1PositionDB = thisJP1.GetDataBlob<PositionDB>();
            var jp2PositionDB = thisJP2.GetDataBlob<PositionDB>();
            var otherJP1PositionDB = otherJP1.GetDataBlob<PositionDB>();
            var otherJP2PositionDB = otherJP2.GetDataBlob<PositionDB>();

            if (jp1PositionDB == null || jp2PositionDB == null || otherJP1PositionDB == null || otherJP2PositionDB == null)
            {
                return false;
            }

            var jp1System = jp1PositionDB.OwningEntity.Manager.ManagerID;
            var jp2System = jp2PositionDB.OwningEntity.Manager.ManagerID;
            var otherJP1System = otherJP1PositionDB.OwningEntity.Manager.ManagerID;
            var otherJP2System = otherJP2PositionDB.OwningEntity.Manager.ManagerID;

            if (jp1System.IsNotNullOrEmpty())
            {
                if (jp1System == otherJP1System)
                {
                    cost = jp1PositionDB.GetDistanceTo_m(otherJP1PositionDB);
                    return true;
                }
                if (jp1System == otherJP2System)
                {
                    cost = jp1PositionDB.GetDistanceTo_m(otherJP2PositionDB);
                    return true;
                }
            }

            if (jp2System.IsNotNullOrEmpty())
            {
                if (jp2System == otherJP1System)
                {
                    cost = jp2PositionDB.GetDistanceTo_m(otherJP1PositionDB);
                    return true;
                }
                if (jp2System == otherJP2System)
                {
                    cost = jp2PositionDB.GetDistanceTo_m(otherJP2PositionDB);
                    return true;
                }
            }
            return false;
        }
    }
}
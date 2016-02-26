using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class PathfindingManager
    {
        private readonly Game _game;
        private readonly Hashtable _dist = new Hashtable();
        private readonly Hashtable _path = new Hashtable();

        private readonly object _syncRoot = new object();

        public PathfindingManager(Game game)
        {
            _game = game;
        }

        /// <summary>
        /// Gets a pathfinding graph for the entire universe.
        /// </summary>
        public Graph GetPathfindingGraph()
        {
            var pathfindingGraph = new Graph();
            foreach (StarSystem starSystem in _game.StarSystems.Values)
            {
                List<Entity> jumpPoints = starSystem.SystemManager.GetAllEntitiesWithDataBlob<TransitableDB>();

                foreach (Entity jumpPoint in jumpPoints)
                {
                    var thisTransitableDB = jumpPoint.GetDataBlob<TransitableDB>();
                    Entity destinationJP = thisTransitableDB.Destination;

                    var node = new JPNode(jumpPoint, destinationJP, new List<EdgeToNeighbor>());
                    pathfindingGraph.AddNode(node);
                }
            }

            return pathfindingGraph;
        }

        /// <summary>
        /// Gets a pathfinding graph for objects/systems known by the provided faction.
        /// </summary>
        public Graph GetPathfindingGraph(Entity faction)
        {
            var factionDB = faction.GetDataBlob<FactionInfoDB>();
            var pathfindingGraph = new Graph();

            foreach (Guid starSystemGuid in factionDB.KnownSystems)
            {
                List<Entity> jumpPoints = factionDB.KnownJumpPoints[starSystemGuid];

                foreach (Entity jumpPoint in jumpPoints)
                {
                    var thisTransitableDB = jumpPoint.GetDataBlob<TransitableDB>();
                    Entity destinationJP = thisTransitableDB.Destination;

                    var node = new JPNode(jumpPoint, destinationJP, new List<EdgeToNeighbor>());
                    pathfindingGraph.AddNode(node);
                }
            }

            return pathfindingGraph;
        }

        /// <summary>
        /// Gets a stack of nodes representing the path from the source to the destination.
        /// </summary>
        public Stack GetPath(Entity source, Entity destination)
        {
            Graph graph;
            if (source.HasDataBlob<OwnedDB>())
            {
                graph = GetPathfindingGraph(source.GetDataBlob<OwnedDB>().Faction);
            }
            else
            {
                graph = GetPathfindingGraph();
            }

            var sourceNode = new JPNode(source, source, new List<EdgeToNeighbor>());
            graph.AddNode(sourceNode);

            var destinationNode = new JPNode(destination, destination, new List<EdgeToNeighbor>());
            graph.AddNode(destinationNode);

            lock (_syncRoot)
            {
                _dist.Clear();
                _path.Clear();

                foreach (JPNode node in graph.Nodes)
                {
                    _dist.Add(node.Key, double.MaxValue);
                    _path.Add(node.Key, null);
                }

                _dist[sourceNode.Key] = 0;

                NodeList nodes = graph.Nodes; // Nodes == Q

                // [Dijkstra]
                while (nodes.Count > 0)
                {
                    Node u = GetMin(nodes); // Get the Minimum Node
                    nodes.Remove(u); // Remove it from set Q.

                    foreach (EdgeToNeighbor edge in u.Neighbors)
                    {
                        Relax(u, edge.Neighbor, edge.Cost);
                    }
                }
                // [/Dijkstra]

                // Determine if a path exists.
                double totalDistance = (double)_dist[destinationNode.Key];
                if (totalDistance == double.MaxValue)
                {
                    // No path to target.
                    return new Stack();
                }

                // Create the stack from the shortest path.
                var pathStack = new Stack();
                Node currentJPNode = destinationNode;
                pathStack.Push(currentJPNode);
                do
                {
                    Node prevJPNode = currentJPNode;
                    currentJPNode = (Node)_path[prevJPNode.Key];

                    pathStack.Push(currentJPNode);
                } while (currentJPNode != sourceNode);
                return pathStack;
            }
        }

        /// <summary>
        /// Retrieves the Node from the passed-in NodeList that has the smallest value in the distance table.
        /// </summary>
        private Node GetMin(NodeList nodes)
        {
            // find the node in nodes with the smallest distance value
            double minDist = double.MaxValue;
            Node minJPNode = null;
            foreach (Node n in nodes)
            {
                if ((double)_dist[n.Key] <= minDist)
                {
                    minDist = (double)_dist[n.Key];
                    minJPNode = n;
                }
            }

            return minJPNode;
        }

        /// <summary>
        /// Relaxes the edge from the Node uNode to vNode.
        /// </summary>
        private void Relax(Node uJPNode, Node vJPNode, double cost)
        {
            double distTouNode = (double)_dist[uJPNode.Key];
            double distTovNode = (double)_dist[vJPNode.Key];

            if (distTovNode > distTouNode + cost)
            {
                // update distance and route
                _dist[vJPNode.Key] = distTouNode + cost;
                _path[vJPNode.Key] = uJPNode;
            }
        }
    }
}

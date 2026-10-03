using System;
using System.Collections;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.JumpPoints;

namespace Pulsar4X.Movement
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
            foreach (StarSystem starSystem in _game.Systems)
            {
                List<Entity> jumpPoints = starSystem.GetAllEntitiesWithDataBlob<JumpPointDB>();

                foreach (Entity jumpPoint in jumpPoints)
                {
                    var thisTransitableDB = jumpPoint.GetDataBlob<JumpPointDB>();
                    Entity destinationJP = starSystem.GetGlobalEntityById(thisTransitableDB.DestinationId);

                    var node = new JPNode(jumpPoint, destinationJP, new List<EdgeToNeighbor>());
                    pathfindingGraph.AddNode(node);
                }
            }

            return pathfindingGraph;
        }

        /// <summary>
        /// Gets a pathfinding graph for objects/systems known by the provided faction.
        /// A known system with no recorded jump points is skipped.
        /// </summary>
        public Graph GetPathfindingGraph(Entity faction)
            => GetPathfindingGraph(faction, null);

        /// <summary>
        /// Same graph, also including jump points recorded for <paramref name="alsoSystemId"/>
        /// when the ship is sitting in a system that is not yet on <see cref="FactionInfoDB.KnownSystems"/>.
        /// </summary>
        public Graph GetPathfindingGraph(Entity faction, string alsoSystemId)
        {
            var factionDB = faction.GetDataBlob<FactionInfoDB>();
            var pathfindingGraph = new Graph();
            var systems = new HashSet<string>(factionDB.KnownSystems);
            if (!string.IsNullOrEmpty(alsoSystemId))
                systems.Add(alsoSystemId);

            foreach (var starSystemGuid in systems)
            {
                if (!factionDB.KnownJumpPoints.TryGetValue(starSystemGuid, out var jumpPoints))
                    continue;

                foreach (Entity jumpPoint in jumpPoints)
                {
                    if (jumpPoint == null || jumpPoint.Manager == null)
                        continue;
                    if (!jumpPoint.TryGetDataBlob<JumpPointDB>(out var thisTransitableDB))
                        continue;
                    if (!jumpPoint.HasDataBlob<PositionDB>())
                        continue;
                    if (!faction.Manager.TryGetGlobalEntityById(thisTransitableDB.DestinationId, out var destinationJP))
                        continue;
                    if (!destinationJP.HasDataBlob<PositionDB>())
                        continue;

                    var node = new JPNode(jumpPoint, destinationJP, new List<EdgeToNeighbor>());
                    pathfindingGraph.AddNode(node);
                }
            }

            return pathfindingGraph;
        }

        public Stack<Node> GetPath(Node sourceNode, Node destinationNode, Graph graph, out double totalCost)
        {
            lock (_syncRoot)
            {
                _dist.Clear();
                _path.Clear();

                foreach (Node node in graph.Nodes)
                {
                    _dist.Add(node.Key, double.MaxValue);
                    _path.Add(node.Key, null);
                }

                _dist[sourceNode.Key] = 0d;

                NodeList nodes = new NodeList(graph.Nodes); // Nodes == Q

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
                if(!_dist.ContainsKey(destinationNode.Key))
                    throw new InvalidOperationException($"Value for key '{destinationNode.Key}' is null or not found.");

                totalCost = (double)_dist[destinationNode.Key];
                if (totalCost == double.MaxValue)
                {
                    // No path to target.
                    return new Stack<Node>();
                }

                // Create the stack from the shortest path.
                var pathStack = new Stack<Node>();
                Node currentNode = destinationNode;
                pathStack.Push(currentNode);
                do
                {
                    Node prevNode = currentNode;
                    currentNode = (Node)_path[prevNode.Key];

                    pathStack.Push(currentNode);
                } while (currentNode != sourceNode);
                return pathStack;
            }
        }

        /// <summary>
        /// Gets a stack of nodes representing the path from the source to the destination.
        /// </summary>
        public Stack<Node> GetPath(Entity source, Entity destination, out double totalCost)
        {
            Graph graph;
            if (source.FactionOwnerID >= 0)
            {
                Entity faction = source.Manager.Game.Factions[source.FactionOwnerID];
                graph = GetPathfindingGraph(faction);
            }
            else
            {
                graph = GetPathfindingGraph();
            }

            var sourceNode = new JPNode(source, source, new List<EdgeToNeighbor>());
            graph.AddNode(sourceNode);

            var destinationNode = new JPNode(destination, destination, new List<EdgeToNeighbor>());
            graph.AddNode(destinationNode);

            return GetPath(sourceNode, destinationNode, graph, out totalCost);
        }

        /// <summary>
        /// Retrieves the Node from the passed-in NodeList that has the smallest value in the distance table.
        /// </summary>
        private Node GetMin(NodeList nodes)
        {
            // find the node in nodes with the smallest distance value
            double minDist = double.MaxValue;
            Node minNode = null;
            foreach (Node n in nodes)
            {
                if ((double)_dist[n.Key] <= minDist)
                {
                    minDist = (double)_dist[n.Key];
                    minNode = n;
                }
            }

            return minNode;
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class PathfindingManager
    {
        private readonly Game _game;

        public PathfindingManager(Game game)
        {
            _game = game;
        }

        public Graph GetPathfindinGraph()
        {
            var pathfindingGraph = new Graph();
            foreach (StarSystem starSystem in _game.StarSystems.Values)
            {
                List<Entity> jumpPoints = starSystem.SystemManager.GetAllEntitiesWithDataBlob<TransitableDB>();

                foreach (Entity jumpPoint in jumpPoints)
                {
                    var thisTransitableDB = jumpPoint.GetDataBlob<TransitableDB>();
                    Entity destinationJP = thisTransitableDB.Destination;

                    var node = new Node(jumpPoint, destinationJP, new AdjacencyList());
                    if (pathfindingGraph.Contains(node))
                    {
                        continue;
                    }

                    pathfindingGraph.AddNode(node);
                    foreach (Node otherNode in pathfindingGraph.Nodes.Cast<Node>().Where(otherNode => node != otherNode))
                    {
                        double weight;
                        if (node.IsNeighbor(otherNode, out weight))
                        {
                            pathfindingGraph.AddUndirectedEdge(node, otherNode, weight);
                        }
                    }
                }
            }

            return pathfindingGraph;
        }

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

                    var node = new Node(jumpPoint, destinationJP, new AdjacencyList());
                    if (pathfindingGraph.Contains(node))
                    {
                        continue;
                    }

                    pathfindingGraph.AddNode(node);
                    foreach (Node otherNode in pathfindingGraph.Nodes.Cast<Node>().Where(otherNode => node != otherNode))
                    {
                        double weight;
                        if (node.IsNeighbor(otherNode, out weight))
                        {
                            pathfindingGraph.AddUndirectedEdge(node, otherNode, weight);
                        }
                    }
                }
            }

            return pathfindingGraph;
        }

        public NodeList GetPath(Entity source, Entity destination)
        {
            var nodeList = new NodeList();

            var graph = GetPathfindinGraph();
            var Q = graph.Nodes.Clone();
            

            return nodeList;
        }
    }
}

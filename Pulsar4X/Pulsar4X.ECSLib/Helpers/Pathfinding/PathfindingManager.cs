using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class PathfindingManager
    {
        private readonly Game _game;
        private Graph _systemGraph;

        public PathfindingManager(Game game)
        {
            _game = game;
        }

        private void InitializeSystemGraph()
        {
            _systemGraph = new Graph();
            foreach (StarSystem starSystem in _game.StarSystems.Values)
            {
                List<Entity> jumpPoints = starSystem.SystemManager.GetAllEntitiesWithDataBlob<TransitableDB>();

                foreach (Entity jumpPoint in jumpPoints)
                {
                    var thisTransitableDB = jumpPoint.GetDataBlob<TransitableDB>();
                    Entity destinationJP = thisTransitableDB.Destination;

                    var node = new Node(jumpPoint, destinationJP, new AdjacencyList());
                    if (_systemGraph.Contains(node))
                    {
                        continue;
                    }

                    _systemGraph.AddNode(node);
                    foreach (Node otherNode in _systemGraph.Nodes.Cast<Node>().Where(otherNode => node != otherNode))
                    {
                        double weight;
                        if (node.IsNeighbor(otherNode, out weight))
                        {
                            _systemGraph.AddUndirectedEdge(node, otherNode, weight);
                        }
                    }
                }
            }
        }
    }
}

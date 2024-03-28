using System;
using System.Collections.Generic;
using NUnit.Framework;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests
{
    [TestFixture]
    class PathfindingTests
    {
        // private Game _game;
        private PathfindingManager _pathfindingManager;
        // private Entity _humanFaction;
        // private Player _testPlayer;
        // private AuthenticationToken _authToken;

        [Test]
        public void GraphTest()
        {
            _pathfindingManager = new PathfindingManager(null);

            var graph = new Graph();

            var node1 = new Node("Node1");
            graph.AddNode(node1);
            var node2 = new Node("Node2");
            graph.AddNode(node2);
            var node3 = new Node("Node3");
            graph.AddNode(node3);
            var node4 = new Node("Node4");
            graph.AddNode(node4);
            var node5 = new Node("Node5");
            graph.AddNode(node5);
            var node6 = new Node("Node6");
            graph.AddNode(node6);

            Assert.AreEqual(6, graph.Count);

            graph.AddUndirectedEdge(node1, node2, 5);
            graph.AddUndirectedEdge(node2, node3, 7);

            double totalCost;
            Stack<Node> pathStack = _pathfindingManager.GetPath(node1, node3, graph, out totalCost);

            Assert.AreEqual(12d, totalCost); // Check shortest path.
            Assert.AreEqual(3, pathStack.Count); // Check for proper node count.
            Assert.AreEqual(node1, pathStack.Pop()); // First node is source node.
            Assert.AreEqual(node2, pathStack.Pop()); // Second node is intermediate node.
            Assert.AreEqual(node3, pathStack.Pop()); // Last node is destination node

            pathStack = _pathfindingManager.GetPath(node1, node4, graph, out totalCost);

            Assert.AreEqual(double.MaxValue, totalCost); // Check no path found
            Assert.AreEqual(0, pathStack.Count); // No nodes found.

            // Now add more complexity to the graph.
            graph.AddUndirectedEdge(node1, node3, 6);   // Shortcut from Node1 to Node3, bypassing Node2.
            graph.AddUndirectedEdge(node3, node4, 9);   // Connection to unrelated node4
            graph.AddUndirectedEdge(node3, node5, 10);
            graph.AddUndirectedEdge(node5, node6, 2);

            // Final path from 1->6 should be 1->3->5->6
            // Total cost should be 6+10+2 (18)
            pathStack = _pathfindingManager.GetPath(node1, node6, graph, out totalCost);

            Assert.AreEqual(18d, totalCost);
            Assert.AreEqual(4, pathStack.Count);
            Assert.AreEqual(node1, pathStack.Pop()); // First node is always source node.
            Assert.AreEqual(node3, pathStack.Pop()); // Ensure the path took the 1->3 shortcut
            Assert.AreEqual(node5, pathStack.Pop()); // Ensure the path ignored node4
            Assert.AreEqual(node6, pathStack.Pop()); // Ensure correct destination.

            // Test bad inputs and ancillery functions.
            Assert.Throws<ArgumentException>(() => graph.AddUndirectedEdge(node1, node2, -1));
            Assert.Throws<ArgumentException>(() => graph.AddDirectedEdge(node1, node2, -1));
            graph.Clear();
            Assert.AreEqual(0, graph.Count);
            Assert.Throws<ArgumentException>(() => graph.AddUndirectedEdge(node1, node2, 1));
            Assert.Throws<ArgumentException>(() => graph.AddDirectedEdge(node1, node2, 1));

            // Generate new nodes to clear the neighbors.
            node1 = new Node("Node1");
            node2 = new Node("Node2");
            node3 = new Node("Node3");

            graph.AddNode(node1);
            graph.AddNode(node2);
            graph.AddNode(node3);
            graph.AddDirectedEdge(node1, node2, 1);
            graph.AddDirectedEdge(node2, node3, 2);

            pathStack = _pathfindingManager.GetPath(node1, node3, graph, out totalCost);
            Assert.AreEqual(3, totalCost);
            Assert.AreEqual(3, pathStack.Count);
            Assert.AreEqual(node1, pathStack.Pop());
            Assert.AreEqual(node2, pathStack.Pop());
            Assert.AreEqual(node3, pathStack.Pop());
        }

        /*
        [Test]
        [Ignore("Incomplete Test")]
        public void PathfindingTest()
        {
            CreateTestUniverse();

            List<StarSystem> systems = _game.GetSystems(_authToken);


        }

        private void CreateTestUniverse()
        {
            _game = new Game(new NewGameSettings { GameName = "Pathfinding Test Game", StartDateTime = DateTime.Now, MaxSystems = 10 });
            _pathfindingManager = new PathfindingManager(_game);

            //_testPlayer = _game.AddPlayer("TestPlayer");
            _authToken = new AuthenticationToken(_testPlayer.ID);

            _humanFaction = DefaultStartFactory.DefaultHumans(_game, "TestHumanFaction");

            List<StarSystem> systems = _game.GetSystems(new AuthenticationToken(_game.SpaceMaster.ID));
            var factionInfoDB = _humanFaction.GetDataBlob<FactionInfoDB>();

            List<StarSystem> testPlayerSystems = _game.GetSystems(_authToken);

            StarSystem starSystem;
            StarSystem sol = testPlayerSystems[0];
            Random RNG = new Random();
            do
            {
                starSystem = systems[RNG.Next(systems.Count - 1)];
            } while (starSystem == sol);

            factionInfoDB.KnownSystems.Add(starSystem.Guid);
        }
        */
    }
}

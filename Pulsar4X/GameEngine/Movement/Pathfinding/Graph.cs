using System;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// The Graph class represents a graph, which is composed of a collection of nodes and edges. This Graph class
    /// maintains its collection of nodes using the NodeList class, which is a collection of Node objects.
    /// It delegates the edge maintenance to the Node class.  The Node class maintains the edge information using
    /// the adjacency list technique.
    /// </summary>
    public class Graph
    {
        public virtual NodeList Nodes => _nodes;
        public virtual int Count => _nodes.Count;
        private readonly NodeList _nodes;

        #region Constructors

        /// <summary>
        /// Default constructor.  Creates a new Graph class instance.
        /// </summary>
        public Graph()
        {
            _nodes = new NodeList();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears out all of the nodes in the graph.
        /// </summary>
        public virtual void Clear()
        {
            _nodes.Clear();
        }

        /// <summary>
        /// Adds a new node to the graph.
        /// This also adds undirected edges to neighbors automatically for Pulsar pathfinding.
        /// </summary>
        /// <param name="u">The node to add.</param>
        public virtual void AddNode(Node u)
        {
            // Make sure this node is unique
            if (!Contains(u))
            {
                _nodes.Add(u);

                // Establish links to neighbors.
                foreach (Node v in _nodes)
                {
                    if (u == v)
                    {
                        continue;
                    }

                    double cost;
                    if (u.IsNeighbor(v, out cost))
                    {
                        AddUndirectedEdge(u, v, cost);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a directed, weighted edge from one node to another.
        /// </summary>
        /// <param name="u">The node from which the directed edge eminates.</param>
        /// <param name="v">The node from which the directed edge leads to.</param>
        /// <param name="cost">The weight of the edge.</param>
        /// <exception cref="ArgumentException">Thrown if the provided nodes are not part of this graph.</exception>
        public virtual void AddDirectedEdge(Node u, Node v, double cost = 0)
        {
            if (cost < 0)
            {
                throw new ArgumentException($"{nameof(cost)} cannot be negative", nameof(cost));
            }

            if (!Contains(u) || !Contains(v))
            {
                throw new ArgumentException("One or both of the nodes supplied were not members of the graph.");
            }

            u.AddDirected(v, cost);
        }

        /// <summary>
        /// Adds an undirected edge from one node to another.
        /// </summary>
        /// <param name="u">The node from which the directed edge eminates.</param>
        /// <param name="v">The node from which the directed edge leads to.</param>
        /// <param name="cost">The weight of the edge.</param>
        /// <exception cref="ArgumentException">Thrown if the provided nodes are not part of this graph.</exception>
        public virtual void AddUndirectedEdge(Node u, Node v, double cost = 0)
        {
            if (cost < 0)
            {
                throw new ArgumentException($"{nameof(cost)} cannot be negative", nameof(cost));
            }

            if (!Contains(u) || !Contains(v))
            {
                throw new ArgumentException("One or both of the nodes supplied were not members of the graph.");
            }

            // Add an edge from u -> v and from v -> u
            _nodes[u.Key].AddDirected(v, cost);
            _nodes[v.Key].AddDirected(u, cost);
        }

        /// <summary>
        /// Determines if a node exists within the graph.
        /// </summary>
        /// <param name="n">The node to check for in the graph.</param>
        /// <returns>True if the node n exists in the graph, False otherwise.</returns>
        public virtual bool Contains(Node n)
        {
            return _nodes.ContainsKey(n.Key);
        }

        #endregion
    }
}

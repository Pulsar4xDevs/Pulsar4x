namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// EdgeToNeighbor represents an edge eminating from one Node to its neighbor.  The EdgeToNeighbor
    /// class, then, contains a reference to the neighbor and the weight of the edge.
    /// </summary>
    public class EdgeToNeighbor
    {       
        /// <summary>
        /// The neighbor the edge is leading to.
        /// </summary>
        public Node Neighbor { get; }

        /// <summary>
        /// The weight of the edge.
        /// </summary>
        /// <remarks>
        /// A value of 0 would indicate that there is no weight, and is the value used when an unweighted
        /// edge is added via the Graph class.
        /// </remarks>
        public double Cost { get; }

        public EdgeToNeighbor(Node neighbor) : this (neighbor, 0) { }

        public EdgeToNeighbor(Node neighbor, double cost)
        {
            Neighbor = neighbor;
            Cost = cost;
        }
    }
}
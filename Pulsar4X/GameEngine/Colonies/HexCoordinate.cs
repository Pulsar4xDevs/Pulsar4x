using System;

namespace Pulsar4X.Colonies
{
    /// <summary>
    /// Represents a hexagonal coordinate using axial coordinate system (q, r)
    /// </summary>
    public struct HexCoordinate : IEquatable<HexCoordinate>
    {
        public int Q { get; }
        public int R { get; }
        public int S => -Q - R; // Third coordinate for validation

        public HexCoordinate(int q, int r)
        {
            Q = q;
            R = r;
        }

        /// <summary>
        /// Calculate distance between two hex coordinates
        /// </summary>
        public int DistanceTo(HexCoordinate other)
        {
            return (Math.Abs(Q - other.Q) + Math.Abs(Q + R - other.Q - other.R) + Math.Abs(R - other.R)) / 2;
        }

        /// <summary>
        /// Get all neighbors of this hex coordinate
        /// </summary>
        public HexCoordinate[] GetNeighbors()
        {
            var directions = new HexCoordinate[]
            {
                new(1, 0), new(1, -1), new(0, -1),
                new(-1, 0), new(-1, 1), new(0, 1)
            };

            var neighbors = new HexCoordinate[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = new HexCoordinate(Q + directions[i].Q, R + directions[i].R);
            }
            return neighbors;
        }

        /// <summary>
        /// Check if coordinate is within a radius from origin
        /// </summary>
        public bool IsWithinRadius(int radius)
        {
            return DistanceTo(new HexCoordinate(0, 0)) <= radius;
        }

        public bool Equals(HexCoordinate other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Q, R);
        }

        public static bool operator ==(HexCoordinate left, HexCoordinate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoordinate left, HexCoordinate right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"({Q}, {R})";
        }
    }
}
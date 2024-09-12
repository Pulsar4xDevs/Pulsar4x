using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Extensions
{
    public static class PositionExtensions
    {
        /// <summary>
        /// Static function to find the distance in m between two positions.
        /// </summary>
        /// <returns>Distance between posA and posB.</returns>
        public static double GetDistanceTo_m(this IPosition posA, IPosition posB)
        {
            return (posA.AbsolutePosition - posB.AbsolutePosition).Length();
        }

        public static double GetDistanceTo_m(this IPosition posA, Vector3 posB)
        {
            return (posA.AbsolutePosition - posB).Length();
        }

        public static double GetDistanceTo_m(this Vector3 posA, PositionDB posB)
        {
            return (posA - posB.AbsolutePosition).Length();
        }

        public static double GetDistanceTo_m(this Vector3 posA, Vector3 posB)
        {
            return (posA - posB).Length();
        }

        /// <summary>
        /// Static Function to find the Distance Squared betweeen two positions.
        /// </summary>
        public static double GetDistanceToSqrd(this PositionDB posA, PositionDB posB)
        {
            return (posA.AbsolutePosition - posB.AbsolutePosition).LengthSquared();
        }
    }
}

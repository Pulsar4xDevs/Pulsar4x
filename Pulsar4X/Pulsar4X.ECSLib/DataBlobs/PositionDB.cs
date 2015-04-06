using System;

namespace Pulsar4X.ECSLib
{
    public class PositionDB : BaseDataBlob
    {
        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        public double X;

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        public double Y;

        /// <summary>
        /// System Z coordinate in AU
        /// </summary>
        public double Z;

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public PositionDB(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns>distance between posA and posB</returns>
        public static double GetDistanceBetween(PositionDB posA, PositionDB posB)
        {
            double distX = (posA.X - posB.X);
            double distY = (posA.Y - posB.Y);
            double distZ = (posA.Z - posB.Z);

            return Math.Sqrt((distX * distX) + (distY * distY) + (distZ * distZ));
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        /// <param name="otherPos"></param>
        /// <returns></returns>
        public double GetDistanceTo(PositionDB otherPos)
        {
            return GetDistanceBetween(this, otherPos);
        }

        /// <summary>
        /// Adds two PositionDBs together.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns></returns>
        public static PositionDB operator +(PositionDB posA, PositionDB posB)
        {
            return new PositionDB(posA.X + posB.X, posA.Y + posB.Y, posA.Z + posB.Z);
        }
    }
}

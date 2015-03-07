using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.DataBlobs
{
    struct PositionDB : IDataBlob
    {
        public bool IsValid { get { return m_isValid; } }
        private readonly bool m_isValid;

        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        public readonly double X;

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        public readonly double Y;

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public PositionDB(double x, double y, bool isValid = true) : this()
        {
            m_isValid = isValid;

            X = x;
            Y = y;
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns>distance between posA and posB</returns>
        public static float GetDistanceBetween(PositionDB posA, PositionDB posB)
        {
            float distX = (float)(posA.X - posB.X);
            float distY = (float)(posA.Y - posB.Y);

            return (float)Math.Sqrt((distX * distX) + (distY * distY));
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        /// <param name="otherPos"></param>
        /// <returns></returns>
        public float GetDistanceTo(PositionDB otherPos)
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
            return new PositionDB(posA.X + posB.X, posA.Y + posB.Y);
        }
    }
}

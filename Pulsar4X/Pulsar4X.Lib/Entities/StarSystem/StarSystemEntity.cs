using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public enum StarSystemEntityType
    {
        Invalid,
        Body,
        Waypoint,
        JumpPoint,
        TaskGroup,
        Population,
        Missile,
        TypeCount
    }

    public struct SystemPosition
    {
        /// <summary>
        /// System currently in.
        /// </summary>
        public StarSystem System { get; set; }

        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        public SystemPosition(StarSystem system, double x, double y) : this()
        {
            System = system;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Static function to find the distance between two positions.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns>distance between posA and posB</returns>
        public static float GetDistanceBetween(SystemPosition posA, SystemPosition posB)
        {
            if (posA.System != posB.System)
            {
                throw new InvalidOperationException("Cannont compare distances between positions in different systems.");
            }
            float distX = (float)(posA.X - posB.X);
            float distY = (float)(posA.Y - posB.Y);

            return (float)Math.Sqrt((distX * distX) + (distY * distY));
        }

        /// <summary>
        /// Instance function for those who don't like static functions.
        /// </summary>
        /// <param name="otherPos"></param>
        /// <returns></returns>
        public float GetDistanceTo(SystemPosition otherPos)
        {
            return GetDistanceBetween(this, otherPos);
        }

        /// <summary>
        /// Adds two SystemPositions together.
        /// </summary>
        /// <param name="posA"></param>
        /// <param name="posB"></param>
        /// <returns></returns>
        public static SystemPosition operator +(SystemPosition posA, SystemPosition posB)
        {
            if (posA.System != posB.System)
            {
                throw new InvalidOperationException("Cannot add positions in different systems.");
            }

            posA.X += posB.X;
            posA.Y += posB.Y;
            return posA;
        }
    }

    public abstract class StarSystemEntity : GameEntity
    {
        /// <summary>
        /// Current System and Position of the entity.
        /// </summary>
        public SystemPosition Position;

        /// <summary>
        /// Type of entity that is represented here.
        /// </summary>
        public StarSystemEntityType SSEntity { get; set; }

        public StarSystemEntity()
            : base()
        {
        }
    }
}

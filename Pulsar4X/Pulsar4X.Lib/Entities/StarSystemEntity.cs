using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public enum StarSystemEntityType
    {
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
        /// System Z coordinante in AU
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Initilized constructor.
        /// </summary>
        /// <param name="system">StarSystem value.</param>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        /// <param name="z">Z value.</param>
        public SystemPosition(StarSystem system, double x, double y, double z) : this()
        {
            System = system;
            X = x;
            Y = y;
            Z = z;
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

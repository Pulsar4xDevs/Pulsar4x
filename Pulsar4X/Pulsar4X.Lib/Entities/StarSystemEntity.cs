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

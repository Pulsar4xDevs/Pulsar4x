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
        TaskGroup,
        Population,
        Missile,
        TypeCount
    }

    public abstract class StarSystemEntity : GameEntity
    {
        /// <summary>
        /// System X coordinante in AU
        /// </summary>
        public double XSystem { get; set; }

        /// <summary>
        /// System Y coordinante in AU
        /// </summary>
        public double YSystem { get; set; }

        /// <summary>
        /// System Z coordinante in AU
        /// </summary>
        public double ZSystem { get; set; }

        /// <summary>
        /// Type of entity that is represented here.
        /// </summary>
        public StarSystemEntityType SSEntity { get; set; }

        /// <summary>
        /// The Mass of this object.
        /// </summary>
        protected double m_dMass;

        /// <summary>
        /// Mass in Solar Masses.
        /// </summary>
        public abstract double Mass { get; set; }

        public StarSystemEntity()
            : base()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public abstract class StarSystemEntity : GameEntity
    {
        public double XSystem { get; set; }
        public double YSystem { get; set; }
        public double ZSystem { get; set; }

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

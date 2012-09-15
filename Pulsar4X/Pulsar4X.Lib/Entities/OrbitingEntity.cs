using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;

namespace Pulsar4X.Entities
{
    public abstract class OrbitingEntity : GameEntity
    {

        public double XSystem { get; set; }
        public double YSystem { get; set; }
        public double ZSystem { get; set; }

        /// <summary>
        /// equitorial radius (in km)
        /// </summary>
        public double Radius { get; set; }

        [Obsolete("OrbitalRadius is Obsolete, use SemiMajorAxis instead")]
        public double OrbitalRadius { get; set; }

        /// <summary>
        /// semi-major axis of solar orbit (in AU)
        /// </summary>
        public double SemiMajorAxis { get; set; }

        /// <summary>
        /// eccentricity of solar orbit
        /// </summary>
        public double Eccentricity { get; set; } 

        /// <summary>
        /// unit of degrees
        /// </summary>
        public double AxialTilt { get; set; }

        /// <summary>
        /// the zone of the planet
        /// </summary>
        public int OrbitZone { get; set; }

        /// <summary>
        /// length of local year (in days)
        /// </summary>
        public double OrbitalPeriod { get; set; }

        /// <summary>
        /// length of local day (hours)
        /// </summary>
        public double LengthOfDay { get; set; }

        /// <summary>
        /// tidally locked
        /// </summary>
        public bool IsInResonantRotation { get; set; }

        /// <summary>
        /// The Entity this object orbits.
        /// </summary>
        public OrbitingEntity Primary { get; set; }

        protected double m_dMass;
        /// <summary>
        /// Mass in Solar Masses.
        /// </summary>
        public abstract double Mass { get; set; }

        /// <summary>
        /// The Age of the body in Years
        /// </summary>
        public abstract double Age { get; set; }


        ///< @todo Move these back into Star class, will require refactor of StarGen Code.
        public double EcoSphereRadius { get; set; }
        public double Luminosity { get; set; }


        public OrbitingEntity() : base()
        {
        }
    }
}

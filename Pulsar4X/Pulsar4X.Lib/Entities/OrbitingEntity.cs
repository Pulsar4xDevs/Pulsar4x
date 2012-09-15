using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;

namespace Pulsar4X.Entities
{
    public abstract class OrbitingEntity : StarSystemEntity
    {

        /// <summary>
        /// equitorial radius (in km)
        /// </summary>
        public double Radius { get; set; }

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
        /// The Star this object orbits.
        /// </summary>
        public Star Primary { get; set; }

        /// <summary>
        /// The Age of the body in Years
        /// </summary>
        public abstract double Age { get; set; }

        /// <summary>
        /// The Parent Orbiting Body, for Planets and stars this is the same as Primary, for moons it will be a planet.
        /// </summary>
        public OrbitingEntity Parent { get; set; }


        public OrbitingEntity() : base()
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public abstract class OrbitingEntity : StarSystemEntity
    {

        /// <summary>
        /// equitorial radius (in km)
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// semi-major axis of orbit (in AU)
        /// </summary>
        public double SemiMajorAxis { get; set; }

        /// <summary>
        /// semi-major axis of solar orbit (in AU)
        /// </summary>
        [JsonIgnore]
        public double SolarSemiMajorAxis { get { return (IsMoon ? Parent.SemiMajorAxis : SemiMajorAxis); } }

        /// <summary>
        /// eccentricity of solar orbit
        /// </summary>
        public double Eccentricity { get; set; } 

        /// <summary>
        /// length of local year (in days)
        /// </summary>
        public double OrbitalPeriod { get; set; }

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

        /// <summary>
        /// Boolean set if the body is a moon
        /// </summary>
        public bool IsMoon { get; set; }

        public long TimeSinceApogee { get; set; }

        /// <summary>
        /// angle counterclockwise from system 'north' to SemiMajorAxis at Apogee
        /// </summary>
        public double LongitudeOfApogee { get; set; }

        public OrbitingEntity() : base()
        {
        }
    }
}

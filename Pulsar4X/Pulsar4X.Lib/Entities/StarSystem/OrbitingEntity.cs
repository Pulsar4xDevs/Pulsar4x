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
        /// equitorial radius (in AU)
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
        /// The Parent Orbiting Body, for Planets and stars this is the same as Primary, for moons it will be a planet.
        /// </summary>
        public OrbitingEntity Parent { get; set; }

        /// <summary>
        /// Boolean set if the body is a moon
        /// </summary>
        public bool IsMoon { get; set; }

        /// <summary>
        /// Days since Apogee
        /// </summary>
        public long TimeSinceApogee { get; set; }

        /// <summary>
        /// Seconds component of time Since Apogee. when this is greater than 86400 TimeSinceApogee should be incremented by 1.
        /// </summary>
        public long TimeSinceApogeeRemainder { get; set; }

        /// <summary>
        /// angle counterclockwise from system 'north' to SemiMajorAxis at Periapsis
        /// </summary>
        public double LongitudeOfPeriapsis { get; set; }

        /// <summary>
        /// precise angle of where this orbiting entity is.
        /// </summary>
        public double TrueAnomaly { get; set; }
        
        /// <summary>
        /// Mass in KG
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// Indicates weither the system body supports populations and can be settled by Plaerys/NPRs..
        /// </summary>
        public bool SupportsPopulations { get; set; }

        public OrbitingEntity()
            : base()
        {
            TrueAnomaly = 0.0;
            TimeSinceApogeeRemainder = 0;
        }
    }
}

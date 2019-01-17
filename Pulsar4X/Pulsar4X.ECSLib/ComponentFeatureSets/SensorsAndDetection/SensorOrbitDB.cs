using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class SensorOrbitDB : BaseDataBlob
    {
        internal DataFrom GetDataFrom = DataFrom.Parent;

        /// <summary>
        /// Semimajor Axis of orbit stored in AU.
        /// Radius of an orbit at the orbit's two most distant points.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double SemiMajorAxis { get; private set; }

        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Eccentricity { get; private set; }

        /// <summary>
        /// Angle between the orbit and the flat reference plane.
        /// Stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Inclination { get; private set; }

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the reference frame stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double LongitudeOfAscendingNode { get; private set; }

        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double ArgumentOfPeriapsis { get; private set; }

        /// <summary>
        /// Definition of the position of the body in the orbit at the reference time
        /// epoch. Mathematically convenient angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MeanAnomaly { get; private set; }

        /// <summary>
        /// reference time. Orbital parameters are stored relative to this reference.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public DateTime Epoch { get; internal set; }

        /// <summary>
        /// 2-Body gravitational parameter of system in km^3/s^2
        /// </summary>
        [PublicAPI]
        public double GravitationalParameter { get; private set; }
        [PublicAPI]
        public double GravitationalParameterAU { get; private set; }

        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        [PublicAPI]
        public TimeSpan OrbitalPeriod { get; private set; }

        /// <summary>
        /// Mean Motion of orbit. Stored as Degrees/Sec.
        /// </summary>
        [PublicAPI]
        public double MeanMotion { get; private set; }

        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Apoapsis { get; private set; }

        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Periapsis { get; private set; }

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public bool IsStationary { get; private set; }

        [JsonProperty]
        private double _parentMass;
        [JsonProperty]
        private double _myMass;

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}

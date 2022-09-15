using System;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// A struct to hold kepler elements without the need to give a 'parent' as OrbitDB does.
    /// </summary>
    public struct KeplerElements
    {
        /// <summary>
        /// In meters kg^3
        /// </summary>
        public double StandardGravParameter;
        
        /// <summary>
        /// SemiMajorAxis in Metres
        /// </summary>
        /// <remarks>a</remarks>
        public double SemiMajorAxis;

        /// <summary>
        /// SemiMinorAxis in Metres
        /// </summary>
        /// <remarks>b</remarks>
        public double SemiMinorAxis;

        /// <summary>
        /// Eccentricity
        /// </summary>
        /// <remarks>e</remarks>
        public double Eccentricity;

        /// <summary>
        /// Linear Eccentricity
        /// </summary>
        /// <remarks>ae</remarks>
        public double LinearEccentricity;

        /// <summary>
        /// Periapsis
        /// </summary>
        /// <remarks>q</remarks>
        public double Periapsis;

        /// <summary>
        /// Apoapsis
        /// </summary>
        /// <remarks>Q</remarks>
        public double Apoapsis;

        /// <summary>
        /// Longitude of the Ascending Node
        /// </summary>
        /// <remarks>Ω (upper case Omega)</remarks>
        public double LoAN;

        /// <summary>
        /// Argument of Periapsis
        /// </summary>
        /// <remarks>ω (lower case omega)</remarks>
        public double AoP;

        /// <summary>
        /// Inclination
        /// </summary>
        /// <remarks>i</remarks>
        public double Inclination;

        /// <summary>
        /// Mean Motion
        /// </summary>
        /// <remarks>n</remarks>
        public double MeanMotion;

        /// <summary>
        /// Orbital Period in Seconds
        /// </summary>
        /// <remarks>P</remarks>
        public double OrbitalPeriod;
        
        /// <summary>
        /// Mean Anomaly At Epoch
        /// </summary>
        /// <remarks>M0</remarks>
        public double MeanAnomalyAtEpoch;

        /// <summary>
        /// True Anomaly At Epoch
        /// </summary>
        /// <remarks>ν or f or  θ</remarks>
        public double TrueAnomalyAtEpoch;
        
        /// <summary>
        /// Eccentric Anomaly
        /// </summary>
        /// <remarks>E</remarks>
        public double EccentricAnomalyAtEpoch;

        /// <summary>
        /// Epoch
        /// </summary>
        public DateTime Epoch;

	}

    /// <summary>
    /// State Vectors are orbital parent ralitive.
    /// </summary>
    public struct StateVectors
    {
        /// <summary>
        /// Position ralitive to SOI parent
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Velocity Vector Ralitive to SOI parent, but X is global East, Y global North Z global up. 
        /// </summary>
        public Vector3 Velocity;
        
        /// <summary>
        /// Velocity as a prograde ie (0, velocity.length, 0) vector
        /// </summary>
        public Vector3 ProgradeVector
        {
            get { return new Vector3(0, Velocity.Y, 0); }
        }
    }
}

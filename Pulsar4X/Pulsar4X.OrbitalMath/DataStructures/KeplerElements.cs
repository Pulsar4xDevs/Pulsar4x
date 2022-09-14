using System;
using System.Diagnostics;

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

        public KeplerElements(double sgp, Vector3 position, Vector3 velocity, DateTime epoch)
        {
			StandardGravParameter = sgp;
			Vector3 eccentVector = OrbitalMath.EccentricityVector(StandardGravParameter, position, velocity);
			Eccentricity = eccentVector.Length();

			double specificOrbitalEnergy = velocity.LengthSquared() * 0.5 - StandardGravParameter / position.Length();
			Vector3 angularVelocity = Vector3.Cross(position, velocity);
			double angularSpeed = angularVelocity.Length();
			Inclination = Math.Acos(angularVelocity.Z / angularSpeed); //should be 0 in 2d. or pi if counter clockwise orbit. 
			if (double.IsNaN(Inclination))
				Inclination = 0;

			double p; //p is where the ellipse or hypobola crosses a line from the focal point 90 degrees from the sma

			// If we run into negative eccentricity we have big problems
			Debug.Assert(Eccentricity >= 0, "Negative eccentricity, this is physically impossible");
			if (Eccentricity > 1) //hypobola
			{
				SemiMajorAxis = -(-StandardGravParameter / (2 * specificOrbitalEnergy)); //in this case the sma is negitive
				p = SemiMajorAxis * (1 - Eccentricity * Eccentricity);
			} else if (Eccentricity < 1) //ellipse
			{
				SemiMajorAxis = -StandardGravParameter / (2 * specificOrbitalEnergy);
				p = SemiMajorAxis * (1 - Eccentricity * Eccentricity);
			} else //parabola
			{
				p = angularSpeed * angularSpeed / StandardGravParameter;
				SemiMajorAxis = double.MaxValue;
			}

			Apoapsis = EllipseMath.Apoapsis(Eccentricity, SemiMajorAxis);
			Periapsis = EllipseMath.Periapsis(Eccentricity, SemiMajorAxis);
			LinearEccentricity = EllipseMath.LinearEccentricity(Apoapsis, SemiMajorAxis);
			SemiMinorAxis = EllipseMath.SemiMinorAxis(SemiMajorAxis, Eccentricity);


			TrueAnomalyAtEpoch = OrbitalMath.TrueAnomaly(eccentVector, position, velocity);
			
            Vector3 nodeVector = Vector3.Cross(Vector3.UnitZ, angularVelocity);
			LoAN = OrbitalMath.CalculateLongitudeOfAscendingNode(nodeVector); ;
			
            AoP = OrbitalMath.GetArgumentOfPeriapsis(position, Inclination, LoAN, TrueAnomalyAtEpoch); ;
			MeanMotion = Math.Sqrt(StandardGravParameter / Math.Pow(SemiMajorAxis, 3)); ;
			
            double eccentricAnomaly = OrbitalMath.GetEccentricAnomalyFromTrueAnomaly(TrueAnomalyAtEpoch, Eccentricity);
			MeanAnomalyAtEpoch = OrbitalMath.GetMeanAnomaly(Eccentricity, eccentricAnomaly);
			
            OrbitalPeriod = 2 * Math.PI / MeanMotion;
			Epoch = epoch; //TimeFromPeriapsis(semiMajorAxis, standardGravParam, meanAnomaly);
                           //Epoch(semiMajorAxis, semiMinorAxis, eccentricAnomoly, OrbitalPeriod(standardGravParam, semiMajorAxis));
            
            // This was not set in this method in OrbitalMath where I got this method
            EccentricAnomalyAtEpoch = default;
		}

		public KeplerElements(Vector3 relativePosition, double sgp, DateTime epoch)
		{

			var ralpos = relativePosition;
			var r = ralpos.Length();
			var i = Math.Atan2(ralpos.Z, r);
			var m0 = Math.Atan2(ralpos.Y, ralpos.X);

			SemiMajorAxis = r;
			SemiMinorAxis = r;
			Apoapsis = r;
			Periapsis = r;
			LinearEccentricity = 0;
			Eccentricity = 0;
			Inclination = i;
			LoAN = 0;
			AoP = 0;
			MeanMotion = Math.Sqrt(sgp / Math.Pow(r, 3));
			MeanAnomalyAtEpoch = m0;
			TrueAnomalyAtEpoch = m0;
			EccentricAnomalyAtEpoch = m0;
            OrbitalPeriod = 2 * Math.PI /MeanMotion;
			Epoch = epoch;
			StandardGravParameter = sgp;
		}
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

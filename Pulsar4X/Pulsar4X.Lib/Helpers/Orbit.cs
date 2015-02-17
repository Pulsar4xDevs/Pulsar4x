using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities;

namespace Pulsar4X.Lib
{
    /// <summary>
    /// Calculates and handles orbits for bodies.
    /// All angles stored in Degrees, but calculated in Radians.
    /// </summary>
	public class Orbit
	{
        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass { get { return m_mass; } }
        private double m_mass;

        /// <summary>
        /// Mass in KG of parent (object this orbit orbits)
        /// </summary>
        public double ParentMass { get { return m_parentMass; } }
        private double m_parentMass;

        /// <summary>
        /// Semimajor Axis of orbit stored in AU.
        /// Average distance of orbit from center.
        /// </summary>
		public double SemiMajorAxis { get; set; } 

        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
		public double Eccentricity { get; set; }

        /// <summary>
        /// Angle between the orbit and the flat referance plane.
        /// Stored in degrees.
        /// </summary>
		public double Inclination { get; set; }

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the referance frame stored in degrees.
        /// </summary>
		public double LongitudeOfAscendingNode { get; set; }

        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
		public double ArgumentOfPeriapsis { get; set; }

        /// <summary>
        /// Definition of the position of the body in the orbit at the referance time
        /// epoch. Mathematically convienant angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
		public double MeanAnomaly { get; set; }

        /// <summary>
        /// Referance time. Orbital parameters are stored relative to this referance.
        /// </summary>
		public DateTime Epoch { get; set; }

        /// <summary>
        /// 2-Body gravitational parameter of system.
        /// </summary>
        public double GravitationalParameter { get { return m_gravitationalParameter; } }
        private double m_gravitationalParameter;

        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        public TimeSpan OrbitalPeriod { get { return m_orbitalPeriod; } }
        private TimeSpan m_orbitalPeriod;

        /// <summary>
        /// Mean Motion of orbit. Stored as Degrees/Sec.
        /// </summary>
        public double MeanMotion { get { return m_meanMotion; } }
        private double m_meanMotion;

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        private bool m_isStationary;

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="longitudeOfPeriapsis">Longitude of periapsis in degrees.</param>
        /// <param name="meanLongitude">Longitude of object at epoch in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static Orbit FromMajorPlanetFormat(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                    double longitudeOfAscendingNode, double longitudeOfPeriapsis, double meanLongitude, DateTime epoch)
        {
            double argumentOfPeriapsis = GetArgumentOfPeriapsisFromLongitudeOfPeriapsis(longitudeOfPeriapsis, longitudeOfAscendingNode);
            double meanAnomaly = GetMeanAnomalyFromMeanLongitude(meanLongitude, longitudeOfAscendingNode, argumentOfPeriapsis);
            return new Orbit(mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis in degrees.</param>
        /// <param name="meanAnomaly">Mean Anomaly in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static Orbit FromAsteroidFormat(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            return new Orbit(mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Creates an orbit that never moves.
        /// Kinda a hack for stationary stars.
        /// </summary>
        public static Orbit FromStationary(double mass)
        {
            return new Orbit(mass);
        }

        /// <summary>
        /// Constructor for stationary orbits.
        /// </summary>
        private Orbit(double mass)
        {
            m_mass = mass;
            m_isStationary = true;
        }

        /// <summary>
        /// Constructor for the orbit.
        /// Calculates commonly-used parameters for future use.
        /// </summary>
        private Orbit(double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                        double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            m_mass = mass;
            m_parentMass = parentMass;
            SemiMajorAxis = semiMajorAxis;
            Eccentricity = Math.Min(eccentricity, 0.8D); // Max eccentricity is 0.8 Orbit code has issues at higher eccentricity. (Note: If restriction lifed, fix code in GetEccentricAnomaly)
            Inclination = inclination;
            LongitudeOfAscendingNode = longitudeOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            MeanAnomaly = meanAnomaly;
            Epoch = epoch;
            m_isStationary = false;

            // Calculate extended parameters.

            // http://en.wikipedia.org/wiki/Standard_gravitational_parameter#Two_bodies_orbiting_each_other
            m_gravitationalParameter = Constants.Science.GRAVITATIONAL_CONSTANT * (ParentMass + Mass) / (1000 * 1000 * 1000); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2

            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            m_orbitalPeriod = TimeSpan.FromSeconds(2 * Math.PI * Math.Sqrt(Math.Pow(SemiMajorAxis * Constants.Units.KM_PER_AU, 3) / (GravitationalParameter)));

            // http://en.wikipedia.org/wiki/Mean_motion
            m_meanMotion = Math.Sqrt(GravitationalParameter / Math.Pow(SemiMajorAxis * Constants.Units.KM_PER_AU, 3)) * 180 / Math.PI;
        }

        /// <summary>
        /// Converts longitude of periapsis to argument of periapsis
        /// </summary>
        private static double GetArgumentOfPeriapsisFromLongitudeOfPeriapsis(double longitudeOfPeriapsis, double longitudeOfAscendingNode)
        {
            // http://en.wikipedia.org/wiki/Longitude_of_the_periapsis
            return longitudeOfPeriapsis - longitudeOfAscendingNode;
        }

        /// <summary>
        /// Converts mean longitude to mean anomaly.
        /// </summary>
        private static double GetMeanAnomalyFromMeanLongitude(double meanLongitude, double longitudeOfAscendingNode, double argumentOfPeriapsis)
        {
            // http://en.wikipedia.org/wiki/Mean_longitude
            return meanLongitude - (longitudeOfAscendingNode + argumentOfPeriapsis);
        }

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        public void GetPosition(DateTime time, out double x, out double y)
		{
            if (m_isStationary)
            {
                x = 0;
                y = 0;
                return;
            }

			TimeSpan timeSinceEpoch = time - Epoch;

            while (timeSinceEpoch > m_orbitalPeriod)
			{
                // Don't attempt to calculate large timeframes.
                // This will cause orbital drift, but not orbital degradation.
                // dift == "Hmm, that planet is 20 seconds ahead of where it should be"
				timeSinceEpoch -= m_orbitalPeriod;
			}

            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = MeanAnomaly * Math.PI / 180; 
            // Add nT
			currentMeanAnomaly += ((MeanMotion * Math.PI / 180) * timeSinceEpoch.TotalSeconds);

			double EccentricAnomaly = GetEccentricAnomaly(currentMeanAnomaly);
			double TrueAnomaly = GetTrueAnomaly(Eccentricity, EccentricAnomaly);

            GetPosition(TrueAnomaly, out x, out y);
		}

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="TrueAnomaly">Angle in Radians.</param>
        public void GetPosition(double TrueAnomaly, out double x, out double y)
        {
            if (m_isStationary)
            {
                x = 0;
                y = 0;
                return;
            }

            double radius = GetRadius(TrueAnomaly);

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            TrueAnomaly += ArgumentOfPeriapsis * Math.PI / 180;

            // Convert KM to AU
            radius /= Constants.Units.KM_PER_AU;

            // Polar to Cartesian conversion.
            x = radius * Math.Cos(TrueAnomaly);
            y = radius * Math.Sin(TrueAnomaly);
        }

        /// <summary>
        /// Calculates the current radius given the input angle.
        /// </summary>
        /// <param name="TrueAnomaly">Angle in Radians.</param>
        /// <returns>Current Radius of an orbit in KM.</returns>
        private double GetRadius(double TrueAnomaly)
        {
            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            return (SemiMajorAxis * Constants.Units.KM_PER_AU * (1 - Eccentricity * Eccentricity) / (1 + Eccentricity * Math.Cos(TrueAnomaly)));
        }

		/// <summary>
		/// Calculates the current TrueAnomaly given certain orbital parameters.
		/// </summary>
		/// <param name="Eccentricity"></param>
		/// <param name="EccentricAnomaly"></param>
		/// <returns>True Anomaly</returns>
		private double GetTrueAnomaly(double Eccentricity, double EccentricAnomaly)
		{
            // http://en.wikipedia.org/wiki/True_anomaly#From_the_eccentric_anomaly
            return Math.Atan2(Math.Sqrt(1 - Eccentricity * Eccentricity) * Math.Sin(EccentricAnomaly), Math.Cos(EccentricAnomaly) - Eccentricity);
		}

		/// <summary>
		/// Calculates the current Eccentric Anomaly given certain orbital parameters.
		/// </summary>
		private double GetEccentricAnomaly(double currentMeanAnomaly)
		{
			//Kepler's Equation
			List<double> E = new List<double>();
			double Epsilon = 1E-12; // Plenty of accuracy.
			/* Eccentricity is currently clamped @ 0.8
            if (Eccentricity > 0.8)
			{
				E.Add(Math.PI);
			} else
            */
			{
				E.Add(currentMeanAnomaly);
			}
			int i = 0;

			do
			{
				// Newton's Method.
                /*					 E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                E.Add(E[i] - ((E[i] - Eccentricity * Math.Sin(E[i]) - currentMeanAnomaly) / (1 - Eccentricity * Math.Cos(E[i]))));
				i++;
			} while (Math.Abs(E[i] - E[i - 1]) > Epsilon && i < 1000);

			if (i > 1000)
			{
				// <? todo: Flag an error about non-convergence of Newton's method.
			}

            double eccentricAnomaly = E[i - 1];

			return E[i - 1];
		}
    }
}


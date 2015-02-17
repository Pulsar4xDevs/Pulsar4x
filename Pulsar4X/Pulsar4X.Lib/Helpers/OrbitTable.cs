using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pulsar4X.Entities;

namespace Pulsar4X.Lib
{
	public class Orbit
	{
        // Kludge to get mass.
		public OrbitingEntity ParentBody { get; set; }
		public OrbitingEntity ThisEntity { get; set; }

		// Orbital Elements
		public double SemiMajorAxis { get; set; } // AU
		public double Eccentricity { get; set; }
		public double Inclination { get; set; }
		public double LongitudeOfAscendingNode { get; set; } // Radians
		public double ArgumentOfPeriapsis { get; set; } //  Radians
		public double MeanAnomaly { get; set; } // Radians
		public DateTime Epoch { get; set; }

        /// <summary>
        /// Calculates the absolute cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void GetPosition(DateTime time, out double x, out double y)
		{
            if (ParentBody == null)
            {
                // No Parent, we are the fixed frame of referance.
                x = 0;
                y = 0;
                return;
            }
			TimeSpan timeSinceEpoch = time - Epoch;

            double GravitationalParameter = Constants.Science.GRAVITATIONAL_CONSTANT * (ParentBody.Mass + ThisEntity.Mass);
            GravitationalParameter /= (1000 * 1000 * 1000); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2

            TimeSpan orbitalPeriod = TimeSpan.FromSeconds(2 * Math.PI * Math.Sqrt(Math.Pow(SemiMajorAxis * Constants.Units.KM_PER_AU, 3) / (GravitationalParameter)));

			while (orbitalPeriod < timeSinceEpoch)
			{
                // Move our epoch and don't try to calculate many orbits ahead.
				timeSinceEpoch -= orbitalPeriod;
			}

            double meanMotion = Math.Sqrt(GravitationalParameter / Math.Pow(SemiMajorAxis * Constants.Units.KM_PER_AU, 3));
			double CurrentMeanAnomaly = MeanAnomaly + (meanMotion * timeSinceEpoch.TotalSeconds);
			double EccentricAnomaly = GetEccentricAnomaly(CurrentMeanAnomaly);
			double TrueAnomaly = GetTrueAnomaly(Eccentricity, EccentricAnomaly);

            GetPosition(TrueAnomaly, out x, out y);
		}


        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="TrueAnomaly">True Anomaly angle in Radians.</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void GetPosition(double TrueAnomaly, out double x, out double y)
        {
            TrueAnomaly += ArgumentOfPeriapsis;
            double radius = GetRadius(TrueAnomaly);

            // Convert KM to AU
            radius /= Constants.Units.KM_PER_AU;

            // Polar to Cartesian
            x = radius * Math.Cos(TrueAnomaly);
            y = radius * Math.Sin(TrueAnomaly);
        }

        /// <summary>
        /// Calculates the current radius given the input angle.
        /// </summary>
        /// <param name="TrueAnomaly">True Anomaly in Radians.</param>
        /// <returns>Current Radius of an orbit in KM.</returns>
        private double GetRadius(double TrueAnomaly)
        {
            return (SemiMajorAxis * Constants.Units.KM_PER_AU * (1 - Eccentricity * Eccentricity) / (1 + Eccentricity * Math.Cos(TrueAnomaly)));
        }

        public static double LongitudeOfPeriapsisToArgumentOfPeriapsis(double longitudeOfPeriapsis, double longitudeOfAscendingNode)
        {
            return longitudeOfPeriapsis - longitudeOfAscendingNode;
        }

        public static double MeanLongitudeToMeanAnomaly(double meanLongitude, double longitudeOfAscendingNode, double argumentOfPeriapsis)
        {
            return meanLongitude - (longitudeOfAscendingNode + argumentOfPeriapsis) * Math.PI / 180;
        }

		/// <summary>
		/// Calculates the current TrueAnomaly given certain orbital parameters.
		/// </summary>
		/// <param name="Eccentricity"></param>
		/// <param name="EccentricAnomaly"></param>
		/// <returns>True Anomaly</returns>
		private double GetTrueAnomaly(double Eccentricity, double EccentricAnomaly)
		{
            return Math.Atan2(Math.Sqrt(1 - Eccentricity * Eccentricity) * Math.Sin(EccentricAnomaly), Math.Cos(EccentricAnomaly) - Eccentricity);
		}

		/// <summary>
		/// Calculates the current Eccentric Anomaly given certain orbital parameters.
		/// </summary>
		/// <param name="SemiMajorAxis"></param>
		/// <param name="Eccentricity"></param>
		/// <param name="MeanAnomaly"></param>
		/// <returns>Eccentric Anomaly</returns>
		private double GetEccentricAnomaly(double meanAnomaly)
		{
			//Kepler's Equation
			List<double> E = new List<double>();
			double Epsilon = 1E-12; // Plenty of accuracy.
			if (Eccentricity > 0.8)
			{
				E.Add(Math.PI);
			} else
			{
				E.Add(meanAnomaly);
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
				*/
				E.Add(E[i] - ((E[i] - Eccentricity * Math.Sin(E[i]) - meanAnomaly) / (1 - Eccentricity * Math.Cos(E[i]))));
				i++;
			} while (Math.Abs(E[i] - E[i - 1]) > Epsilon && i < 1000);

			if (i < 1000)
			{
				// <? todo: Flag an error about non-convergence of Newton's method.
			}

            double eccentricAnomaly = E[i - 1];

			return E[i - 1];
		}
	}
}


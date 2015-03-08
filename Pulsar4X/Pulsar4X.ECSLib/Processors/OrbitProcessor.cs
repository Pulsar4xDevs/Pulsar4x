using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.ECSLib.Helpers.GameMath;

namespace Pulsar4X.ECSLib.Processors
{
    static class OrbitProcessor
    {
        static int m_orbitTypeIndex = -1;
        static int m_positionTypeIndex = -1;

        public static void Process(StarSystem system, int deltaSeconds)
        {
            EntityManager currentManager = system.SystemManager;

            if (m_orbitTypeIndex == -1)
            {
                // Important for maximum performance. Otherwise we would do this lookup several times.
                m_orbitTypeIndex = currentManager.GetDataBlobTypeIndex<OrbitDB>();
                m_positionTypeIndex = currentManager.GetDataBlobTypeIndex<PositionDB>();
            }

            // Find the first orbital entity.
            int firstOrbital = currentManager.GetFirstEntityWithDataBlob(m_orbitTypeIndex);

            if (firstOrbital == -1)
            {
                // No orbitals in this manager.
                return;
            }

            OrbitDB firstOrbit;

            do
            {
                // Get the parentmost orbit.
                firstOrbit = currentManager.GetDataBlob<OrbitDB>(firstOrbital, m_orbitTypeIndex);
            } while (firstOrbit.Parent != -1);

            DateTime currentTime = Game.Instance.CurrentDateTime;

            // Call recursive function to update every orbit in this system.
            UpdateOrbit(currentManager, firstOrbit, new PositionDB(0,0), currentTime);
        }

        private static void UpdateOrbit(EntityManager currentManager, OrbitDB orbit, PositionDB parentPosition, DateTime currentTime)
        {
            // Get our Parent-Relative coordinates.
            PositionDB newPosition = GetPosition(orbit, currentTime);

            // Get our Absolute coordinates.
            newPosition += parentPosition;

            // Set our absolute coordinates.
            currentManager.SetDataBlob(orbit.Entity, newPosition);

            // Update our children.
            foreach (int child in orbit.Children)
            {
                // RECURSION!
                OrbitDB childOrbit = currentManager.GetDataBlob<OrbitDB>(child, m_orbitTypeIndex);
                UpdateOrbit(currentManager, childOrbit, newPosition, currentTime);
            }
        }

        #region Orbit Position Calculations
        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        public static PositionDB GetPosition(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new PositionDB(0, 0);
            }

            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            while (timeSinceEpoch > orbit.OrbitalPeriod)
            {
                // Don't attempt to calculate large timeframes.
                timeSinceEpoch -= orbit.OrbitalPeriod;
                orbit.Epoch += orbit.OrbitalPeriod;
            }

            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = Angle.ToRadians(orbit.MeanAnomaly);
            // Add nT
            currentMeanAnomaly += Angle.ToRadians(orbit.MeanMotion) * timeSinceEpoch.TotalSeconds;

            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);

            // http://en.wikipedia.org/wiki/True_anomaly#From_the_eccentric_anomaly
            double trueAnomaly = Math.Atan2(Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly), Math.Cos(eccentricAnomaly) - orbit.Eccentricity);

            return GetPosition(orbit, trueAnomaly);
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static PositionDB GetPosition(OrbitDB orbit, double trueAnomaly)
        {
            if (orbit.IsStationary)
            {
                return new PositionDB(0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = Distance.ToKm(orbit.SemiMajorAxis) * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            trueAnomaly += Angle.ToRadians(orbit.ArgumentOfPeriapsis);

            // Convert KM to AU
            radius = Distance.ToAU(radius);

            // Polar to Cartesian conversion.
            double x, y;
            x = radius * Math.Cos(trueAnomaly);
            y = radius * Math.Sin(trueAnomaly);

            return new PositionDB(x, y);
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        private static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
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
                E.Add(E[i] - ((E[i] - orbit.Eccentricity * Math.Sin(E[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(E[i]))));
                i++;
            } while (Math.Abs(E[i] - E[i - 1]) > Epsilon && i < 1000);

            if (i > 1000)
            {
                // <? todo: Flag an error about non-convergence of Newton's method.
            }

            double eccentricAnomaly = E[i - 1];

            return E[i - 1];
        }

        #endregion
    }
}

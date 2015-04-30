using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    internal static class OrbitProcessor
    {
        private static int _orbitTypeIndex = -1;
        private static int _starInfoTypeIndex = -1;

        public static void Initialize()
        {
            _orbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
            _starInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();
        }

        public static void Process(List<StarSystem> systems, int deltaSeconds)
        {
            DateTime currentTime = Game.Instance.CurrentDateTime;

            foreach (StarSystem system in systems)
            {
                EntityManager currentManager = system.SystemManager;

                // Find the first orbital entity.
                Entity firstOrbital = currentManager.GetFirstEntityWithDataBlob(_starInfoTypeIndex);

                if (!firstOrbital.IsValid)
                {
                    // No orbitals in this manager.
                    continue;
                }

                OrbitDB rootOrbit = firstOrbital.GetDataBlob<OrbitDB>(_orbitTypeIndex).RootDB as OrbitDB;

                // Call recursive function to update every orbit in this system.
                UpdateOrbit(rootOrbit, new PositionDB(0, 0, 0), currentTime);
            }
        }

        private static void UpdateOrbit(OrbitDB orbit, PositionDB parentPosition, DateTime currentTime)
        {
            // Get our Parent-Relative coordinates.
            PositionDB newPosition = GetPosition(orbit, currentTime);

            // Get our Absolute coordinates.
            newPosition += parentPosition;

            // Set our absolute coordinates.
            orbit.OwningEntity.SetDataBlob(newPosition);

            // Update our children.
            foreach (OrbitDB childOrbit in orbit.ChildrenDBs.Cast<OrbitDB>())
            {
                // RECURSION!
                UpdateOrbit(childOrbit, newPosition, currentTime);
            }
        }

        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static PositionDB GetPosition(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new PositionDB(0, 0, 0);
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
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static PositionDB GetPosition(OrbitDB orbit, double trueAnomaly)
        {
            if (orbit.IsStationary)
            {
                return new PositionDB(0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = Distance.ToKm(orbit.SemiMajorAxis) * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            trueAnomaly += Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double inclination = Angle.ToRadians(orbit.Inclination);

            // Convert KM to AU
            radius = Distance.ToAU(radius);

            // Spherical to Cartesian conversion.
            double x = radius * Math.Sin(inclination) * Math.Cos(trueAnomaly);
            double y = radius * Math.Sin(inclination) * Math.Sin(trueAnomaly);
            double z = radius * Math.Cos(inclination);

            return new PositionDB(x, y, z);
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        private static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            var e = new List<double>();
            const double epsilon = 1E-12; // Plenty of accuracy.
            if (orbit.Eccentricity > 0.8)
            {
                e.Add(Math.PI);
            } else
            {
                e.Add(currentMeanAnomaly);
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
                e.Add(e[i] - ((e[i] - orbit.Eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(e[i]))));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i < 1000);

            if (i > 1000)
            {
                // <? todo: Flag an error about non-convergence of Newton's method.
            }

            return e[i - 1];
        }

        #endregion
    }
}
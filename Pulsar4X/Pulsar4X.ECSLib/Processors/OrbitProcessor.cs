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
        public static void Process(EntityManager currentManager, List<OrbitDB> orbits)
        {
            DateTime currentTime = Game.Instance.CurrentDateTime;
            for (int entityID = 0; entityID < orbits.Count; entityID++)
            {
                OrbitDB currentOrbit = orbits[entityID];
                if (currentOrbit.IsValid)
                {
                    // Make sure our Epoch isn't too long ago.
                    DateTime newEpoch = ClampEpoch(currentOrbit, currentTime);

                    if (newEpoch != currentOrbit.Epoch)
                    {
                        // Update the orbit with newEpoch.
                        currentManager.SetDataBlob<OrbitDB>(entityID,
                            OrbitDB.FromAsteroidFormat(
                            currentOrbit.Parent,
                            currentOrbit.Mass,
                            currentOrbit.ParentMass,
                            currentOrbit.SemiMajorAxis,
                            currentOrbit.Eccentricity,
                            currentOrbit.Inclination,
                            currentOrbit.LongitudeOfAscendingNode,
                            currentOrbit.ArgumentOfPeriapsis,
                            currentOrbit.MeanAnomaly,
                            newEpoch
                            )
                        );
                        currentOrbit = currentManager.GetDataBlob<OrbitDB>(entityID);
                    }

                    PositionDB orbitOffset = GetPosition(currentOrbit, currentTime);
                    PositionDB parentPosition = currentManager.GetDataBlob<PositionDB>(currentOrbit.Parent);
                    currentManager.SetDataBlob<PositionDB>(entityID, orbitOffset + parentPosition);
                }
            }
        }

        #region Orbit Position Calculations
        public static DateTime ClampEpoch(OrbitDB orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;
            DateTime Epoch = orbit.Epoch;

            while (timeSinceEpoch > orbit.OrbitalPeriod)
            {
                // Don't attempt to calculate large timeframes.
                timeSinceEpoch -= orbit.OrbitalPeriod;
                Epoch += orbit.OrbitalPeriod;
            }

            return Epoch;
        }

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

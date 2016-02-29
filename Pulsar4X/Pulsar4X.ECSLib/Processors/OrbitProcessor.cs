using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class OrbitProcessor
    {
        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        private static readonly int StarInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();

        /// <summary>
        /// Function called by Game.RunProcessors to run this processor.
        /// </summary>
        internal static int Process(Game game, List<StarSystem> systems, int deltaSeconds)
        {
            DateTime currentTime = game.CurrentDateTime;

            int orbitsProcessed = 0;

            if (game.Settings.EnableMultiThreading)
            {
                Parallel.ForEach(systems, system => UpdateSystemOrbits(system, currentTime, ref orbitsProcessed));
            }
            else
            {
                foreach (StarSystem system in systems)
                {
                    UpdateSystemOrbits(system, currentTime, ref orbitsProcessed);
                }
            }
            return orbitsProcessed;
        }

        private static void UpdateSystemOrbits(StarSystem system, DateTime currentTime, ref int orbitsProcessed)
        {
            EntityManager currentManager = system.SystemManager;

            // Find the first orbital entity.
            Entity firstOrbital = currentManager.GetFirstEntityWithDataBlob(StarInfoTypeIndex);

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>(OrbitTypeIndex).Root;
            var rootPositionDB = root.GetDataBlob<PositionDB>(PositionTypeIndex);

            // Call recursive function to update every orbit in this system.
            UpdateOrbit(root, rootPositionDB, currentTime, ref orbitsProcessed);
        }

        private static void UpdateOrbit(ProtoEntity entity, PositionDB parentPositionDB, DateTime currentTime, ref int orbitsProcessed)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>(OrbitTypeIndex);
            var entityPosition = entity.GetDataBlob<PositionDB>(PositionTypeIndex);

            // Get our Parent-Relative coordinates.
            try
            {
                Vector4 newPosition = GetPosition(entityOrbitDB, currentTime);

                // Get our Absolute coordinates.
                entityPosition.Position = parentPositionDB.Position + newPosition;

                Interlocked.Increment(ref orbitsProcessed);
            }
            catch (OrbitProcessorException e)
            {
                // TODO: Debug log this exception. Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.
            }

            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                UpdateOrbit(child, entityPosition, currentTime, ref orbitsProcessed);
            }
        }

        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector4 GetPosition(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new Vector4(0, 0, 0, 0);
            }
            return GetPosition(orbit, GetTrueAnomaly(orbit, time));
        }

        public static double GetTrueAnomaly(OrbitDB orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            // Don't attempt to calculate large timeframes.
            while (timeSinceEpoch > orbit.OrbitalPeriod && orbit.OrbitalPeriod.Ticks != 0)
            {
                long years = timeSinceEpoch.Ticks / orbit.OrbitalPeriod.Ticks;
                timeSinceEpoch -= TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
                orbit.Epoch += TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
            }

            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)
            // Convert MeanAnomaly to radians.
            double currentMeanAnomaly = Angle.ToRadians(orbit.MeanAnomaly);
            // Add nT
            currentMeanAnomaly += Angle.ToRadians(orbit.MeanMotion) * timeSinceEpoch.TotalSeconds;
            // Large nT can cause meanAnomaly to go past 2*Pi. Roll it down. It shouldn't, because timeSinceEpoch should be tapered above, but it has.
            currentMeanAnomaly = currentMeanAnomaly % (Math.PI * 2);


            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return Math.Atan2(Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly), Math.Cos(eccentricAnomaly) - orbit.Eccentricity);
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static Vector4 GetPosition(OrbitDB orbit, double trueAnomaly)
        {

            if (orbit.IsStationary)
            {
                return new Vector4(0, 0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = Distance.ToKm(orbit.SemiMajorAxis) * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            // Adjust TrueAnomaly by the Argument of Periapsis (converted to radians)
            trueAnomaly += Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double inclination = Angle.ToRadians(orbit.Inclination);

            // Convert KM to AU
            radius = Distance.ToAU(radius);

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = Angle.ToRadians(orbit.LongitudeOfAscendingNode);
            double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double tA = trueAnomaly;
            double incl = inclination;

            double x = Math.Cos(lofAN) * Math.Cos(aofP + tA) - Math.Sin(lofAN) * Math.Sin(aofP + tA) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(aofP + tA) + Math.Cos(lofAN) * Math.Sin(aofP + tA) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(aofP + tA);

            return new Vector4(x, y, z, 0) * radius;
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        private static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 100;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

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
                e[i + 1] = e[i] - (e[i] - orbit.Eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cos(e[i]));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return e[i - 1];
        }

        private class OrbitProcessorException : Exception
        {
            public override string Message { get; }
            public Entity Entity { get; }

            public OrbitProcessorException(string message, Entity entity)
            {
                Message = message;
                Entity = entity;
            }
        }

        #endregion
    }
}
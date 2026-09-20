using Pulsar4X.Orbital;
using System;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Orbits
{
    public static class OrbitExtensions
    {


        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetPosition_AU(this OrbitDB orbit, DateTime time)
        {
            return Distance.MToAU(orbit.GetPosition(orbit.GetTrueAnomaly(time)));
        }


        public static Vector3 GetPosition(this OrbitDB orbit, DateTime time)
        {
            return OrbitMath.GetPosition(orbit, orbit.GetTrueAnomaly(time));
        }

        public static Vector3 GetPosition(this OrbitDB orbit, double trueAnomaly)
        {
            return OrbitMath.GetPosition(orbit, trueAnomaly);
        }

        public static double GetTrueAnomaly(this OrbitDB orbit, DateTime time)
        {
            return OrbitMath.GetTrueAnomaly(orbit, time);
        }


        /// <summary>
        /// Parent relative velocity vector.
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_m(this OrbitDB orbit, DateTime atDateTime)
        {
            return OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, atDateTime);
        }

        /// <summary>
        /// Parent relative velocity vector.
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        /// <param name="preCalculatedTrueAnomaly">Pre-calculated true anomaly to avoid redundant calculation.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_m(this OrbitDB orbit, DateTime atDateTime, double preCalculatedTrueAnomaly)
        {
            return OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, atDateTime, preCalculatedTrueAnomaly);
        }






        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, relative to the root object (ie sun)</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 AbsoluteOrbitalVector_m(this OrbitDB orbit, DateTime atDateTime)
        {
            Vector3 vector = orbit.InstantaneousOrbitalVelocityVector_m(atDateTime);
            if (orbit.Parent != null)
            {
                vector += orbit.Parent.GetDataBlob<OrbitDB>().AbsoluteOrbitalVector_m(atDateTime);
            }
            return vector;
        }


        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit.
        /// </summary>
        /// <returns>The orbital velocity in metres.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInm(this OrbitDB orbit)
        {
            double a = orbit.SemiMajorAxis;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.OrbitalPeriod.TotalSeconds;
            double peremeter = Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return peremeter / orbitalPerodSeconds;
        }



        public static bool IsTidallyLocked(this OrbitDB orbit, SystemBodyInfoDB systemBodyInfo)
        {
            // Define a tolerance threshold to account for small differences
            // (you can adjust this value as needed)
            const double tolerance = 0.05; // 5% tolerance

            double ratio = systemBodyInfo.LengthOfDay.TotalDays / orbit.OrbitalPeriod.TotalDays;

            // Check if the ratio is approximately 1 (within the tolerance)
            return Math.Abs(ratio - 1.0) < tolerance;
        }

        public static bool IsTidallyLocked(this SystemBodyInfoDB systemBodyInfo, OrbitDB orbit)
        {
            return IsTidallyLocked(orbit, systemBodyInfo);
        }
    }
}

using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using System.Text;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Extensions
{
    public static class OrbitExtensions
    {
        public static OrbitDB FindSOIForOrbit(this OrbitDB orbit, Vector3 AbsolutePosition)
        {
            var soi = orbit.SOI_m;
            var pos = orbit.OwningEntity.GetDataBlob<PositionDB>();
            if (AbsolutePosition.GetDistanceTo_m(pos) < soi)
            {
                foreach (OrbitDB subOrbit in orbit.ChildrenDBs)
                {
                    var suborbitb = subOrbit.FindSOIForOrbit(AbsolutePosition);
                    if (suborbitb != null)
                        return suborbitb;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculates the root relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetAbsolutePosition_AU(this OrbitDB orbit, DateTime time)
        {
            return Distance.MToAU(GetAbsolutePosition_m(orbit, time));
        }

        public static Vector3 GetAbsolutePosition_m(this OrbitDB orbit, DateTime time)
        {
            if (orbit.Parent == null)//if we're the parent sun
                return orbit.GetPosition_m(orbit.GetTrueAnomaly(time));
            //else if we're a child
            Vector3 rootPos = orbit.Parent.GetDataBlob<OrbitDB>().GetAbsolutePosition_m(time);
            if (orbit.IsStationary)
            {
                return rootPos;
            }

            if (orbit.Eccentricity < 1)
                return rootPos + orbit.GetPosition_m(orbit.GetTrueAnomaly(time));
            else
                return rootPos + orbit.GetPosition_m(orbit.GetTrueAnomaly(time));

            //if (orbit.Eccentricity == 1)
            //    return GetAbsolutePositionForParabolicOrbit_AU();
            //else
            //    return GetAbsolutePositionForHyperbolicOrbit_AU(orbit, time);
        }

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetPosition_AU(this OrbitDB orbit, DateTime time)
        {
            return Distance.MToAU(orbit.GetPosition_m(orbit.GetTrueAnomaly(time)));
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static Vector3 GetPosition_AU(this OrbitDB orbit, double trueAnomaly)
        {
            return Distance.MToAU(GetPosition_m(orbit, trueAnomaly));
        }

        public static Vector3 GetPosition(this OrbitDB orbit, DateTime time)
        {
            return orbit.GetPosition_m(orbit.GetTrueAnomaly(time));
        }

        public static Vector3 GetPosition_m(this OrbitDB orbit, double trueAnomaly)
        {
            if (orbit.IsStationary)
            {
                return Vector3.Zero;
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            double incl = orbit.Inclination;

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = orbit.LongitudeOfAscendingNode;
            //double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double angleFromLoAN = trueAnomaly + orbit.ArgumentOfPeriapsis;

            double x = Math.Cos(lofAN) * Math.Cos(angleFromLoAN) - Math.Sin(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angleFromLoAN) + Math.Cos(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angleFromLoAN);

            return new Vector3(x, y, z) * radius;
        }

        public static double GetTrueAnomaly(this OrbitDB orbit, DateTime time)
        {
            TimeSpan timeSinceEpoch = time - orbit.Epoch;

            // Don't attempt to calculate large timeframes.
            while (timeSinceEpoch > orbit.OrbitalPeriod && orbit.OrbitalPeriod.Ticks != 0)
            {
                long years = timeSinceEpoch.Ticks / orbit.OrbitalPeriod.Ticks;
                timeSinceEpoch -= TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
                orbit.Epoch += TimeSpan.FromTicks(years * orbit.OrbitalPeriod.Ticks);
            }

            double m0 = orbit.MeanAnomalyAtEpoch;
            double n = orbit.MeanMotion;
            double currentMeanAnomaly = OrbitMath.GetMeanAnomalyFromTime(m0, n, timeSinceEpoch.TotalSeconds);

            double eccentricAnomaly = orbit.GetEccentricAnomaly(currentMeanAnomaly);
            return OrbitMath.TrueAnomalyFromEccentricAnomaly(orbit.Eccentricity, eccentricAnomaly);
            /*
            var x = Math.Cos(eccentricAnomaly) - orbit.Eccentricity;
            var y = Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly);
            return Math.Atan2(y, x);
            */
        }

        /// <summary>
        /// this allows us to re-use the array, rather than re-creating it each time.
        /// ThreadStatic ensures that we have a different array for each thread which avoids thread problems.
        /// however it only initialises once, so we're checking if it's null.
        /// this whole thing should help unnessisary memory allocation which means less garbage collection.
        /// </summary>
        private const int numIterations = 1000;
        [ThreadStatic]
        private static double[] e = new double[numIterations];
        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        public static double GetEccentricAnomaly(this OrbitDB orbit, double currentMeanAnomaly)
        {
            if (e is null)// threadstatic only inits once so we need to do this...
                e = new double[numIterations];
            //Kepler's Equation
            //const int numIterations = 1000;
            //var e = new double[numIterations];
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
                // Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly.")
                // {
                //     Entity = orbit.OwningEntity,
                //     EventType = EventType.Opps
                // };

                // StaticRefLib.EventLog.AddEvent(gameEvent);
                //throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return e[i - 1];
        }

        /// <summary>
        /// Untested.
        /// Gets the Eccentric Anomaly for a hyperbolic trajectory.
        /// This still requres the Mean Anomaly to be known however.
        /// Which I'm unsure how to calculate from time. so this may not be useful. included for completeness.
        /// </summary>
        /// <param name="orbit"></param>
        /// <param name="currentMeanAnomaly"></param>
        /// <returns></returns>
        public static double GetEccentricAnomalyH(this OrbitDB orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 1000;
            var f = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (orbit.Eccentricity > 0.8)
            {
                f[i] = Math.PI;
            }
            else
            {
                f[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*					 F(n) - e sinh(E(n)) - M(t)
                 * F(n+1) = E(n) - ( ------------------------- )
                 *					      1 - e cosh(F(n)
                 *
                 * F == EccentricAnomalyH, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                f[i + 1] = f[i] - (f[i] - orbit.Eccentricity * Math.Sinh(f[i]) - currentMeanAnomaly) / (1 - orbit.Eccentricity * Math.Cosh(f[i]));
                i++;
            } while (Math.Abs(f[i] - f[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                // Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly.")
                // {
                //     Entity = orbit.OwningEntity,
                //     EventType = EventType.Opps
                // };

                // StaticRefLib.EventLog.AddEvent(gameEvent);
                //throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return f[i - 1];
        }

        /// <summary>
        /// Parent relative velocity vector.
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_AU(this OrbitDB orbit, DateTime atDateTime)
        {
            return Distance.MToAU(orbit.InstantaneousOrbitalVelocityVector_m(atDateTime));
        }

        /// <summary>
        /// Parent relative velocity vector.
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 InstantaneousOrbitalVelocityVector_m(this OrbitDB orbit, DateTime atDateTime)
        {
            var position = orbit.GetPosition(atDateTime);
            var sma = orbit.SemiMajorAxis;
            if (orbit.GravitationalParameter_m3S2 == 0 || sma == 0)
                return new Vector3(); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameter_m3S2;

            double e = orbit.Eccentricity;
            double trueAnomaly = orbit.GetTrueAnomaly(atDateTime);
            double aoP = orbit.ArgumentOfPeriapsis;
            double i = orbit.Inclination;
            double loAN = orbit.LongitudeOfAscendingNode;
            return OrbitMath.ParentLocalVeclocityVector(sgp, position, sma, e, trueAnomaly, aoP, i, loAN);
        }


        // Messed something up in this method...

        /// <summary>
        /// PreciseOrbital Velocy in polar coordinates
        /// </summary>
        /// <returns>item1 is speed, item2 angle</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static (double speed, double heading) InstantaneousOrbitalVelocityPolarCoordinate(this OrbitDB orbit, DateTime atDateTime)
        {
            var position = orbit.GetPosition(atDateTime);
            var sma = orbit.SemiMajorAxis;
            if (orbit.GravitationalParameter_m3S2 == 0 || sma == 0)
                return (0, 0); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameter_m3S2;

            double e = orbit.Eccentricity;
            double trueAnomaly = orbit.GetTrueAnomaly(atDateTime);
            double aoP = orbit.ArgumentOfPeriapsis;

            (double speed, double heading) polar = OrbitMath.ObjectLocalVelocityPolar(sgp, position, sma, e, trueAnomaly, aoP);

            polar = (polar.speed, polar.heading);

            return polar;
        }

        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, relative to the root object (ie sun)</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 AbsoluteOrbitalVector_AU(this OrbitDB orbit, DateTime atDateTime)
        {
            Vector3 vector = orbit.InstantaneousOrbitalVelocityVector_AU(atDateTime);
            if (orbit.Parent != null)
                vector += ((OrbitDB)orbit.ParentDB).AbsoluteOrbitalVector_AU(atDateTime);
            return vector;

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
                if (orbit is OrbitUpdateOftenDB)//this is a horrbile hack. very brittle.
                    vector += orbit.Parent.GetDataBlob<OrbitDB>().AbsoluteOrbitalVector_m(atDateTime);
                else
                    vector += ((OrbitDB)orbit.ParentDB).AbsoluteOrbitalVector_m(atDateTime);
            }
            return vector;
        }

        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit.
        /// </summary>
        /// <returns>The orbital velocity in au.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInAU(this OrbitDB orbit)
        {
            return Distance.MToAU(orbit.MeanOrbitalVelocityInm());
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

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future.
        /// Fairly slow and inefficent.
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        public static double Hackspeed(this OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = orbit.GetPosition(atDatetime);
            var pos2 = orbit.GetPosition(atDatetime + TimeSpan.FromSeconds(1));

            return Distance.DistanceBetween(pos1, pos2);
        }

        public static double HackVelocityHeading(this OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = orbit.GetPosition(atDatetime);
            var pos2 = orbit.GetPosition(atDatetime + TimeSpan.FromSeconds(1));

            Vector3 vector = pos2 - pos1;
            double heading = Math.Atan2(vector.Y, vector.X);
            return heading;
        }

        public static Vector3 HackVelocityVector(this OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = orbit.GetPosition(atDatetime);
            var pos2 = orbit.GetPosition(atDatetime + TimeSpan.FromSeconds(1));
            //double speed = Distance.DistanceBetween(pos1, pos2);
            return pos2 - pos1;
        }

        /// <summary>
        /// Incorrect/Incomplete Unfinished DONOTUSE
        /// </summary>
        /// <returns>The to radius from periapsis.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="radiusAU">Radius au.</param>
        public static double TimeToRadiusFromPeriapsis(this OrbitDB orbit, double radiusAU)
        {
            throw new NotImplementedException();
            //var a = Distance.MToAU(orbit.SemiMajorAxis);
            //var e = orbit.Eccentricity;
            //var p = EllipseMath.SemiLatusRectum(a, e);
            //var angle = OrbitMath.TrueAnomalyAtRadus(radiusAU, p, e);
            ////var meanAnomaly = CurrentMeanAnomaly(orbit.MeanAnomalyAtEpoch, meanMotion, )
            //return OrbitMath.TimeFromPeriapsis(a, orbit.GravitationalParameterAU, orbit.MeanAnomalyAtEpoch_Degrees);
        }
    }
}

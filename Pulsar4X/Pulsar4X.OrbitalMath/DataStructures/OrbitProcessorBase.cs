using Pulsar4X.Orbital.Helpers;
using System;

namespace Pulsar4X.Orbital
{
    /// <summary>
     /// Orbit processor.
     /// How Orbits are calculated:
     /// First we get the time since epoch. (time from when the planet is at its closest to it's parent)
     /// Then we get the Mean Anomaly. (stored) 
     /// Eccentric Anomaly is calculated from the Mean Anomaly, and takes the most work. 
     /// True Anomaly, is calculated using the Eccentric Anomaly this is the angle from the parent (or focal point of the ellipse) to the body. 
     /// With the true anomaly, we can then use trig to calculate the position.  
     /// ***
     /// Top Thanks to dev 22367rh for seperating all this out into it's own project from the ECSLib project.
     /// *** 
     /// </summary>
    public class OrbitProcessorBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pulsar4X.Orbital.OrbitProcessorBase"/> uses relative velocity.
        /// </summary>
        /// <value><c>true</c> if use relative velocity; otherwise, uses absolute <c>false</c>.</value>
        public static bool UseRelativeVelocity { get; set; } = true;

        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">KeplerElements to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetPosition(KeplerElements orbit, DateTime time, bool isStationary = false)
        {
            return (isStationary) ? Vector3.Zero : GetPosition(orbit, GetTrueAnomaly(orbit, time));
        }

		/// <summary>
		/// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
		/// </summary>
		/// <param name="orbit">KeplerElements to calculate position from.</param>
		/// <param name="trueAnomaly">Angle in Radians.</param>
		public static Vector3 GetPosition(KeplerElements orbit, double trueAnomaly, bool isStationary = false)
		{
            if (isStationary) return Vector3.Zero;
			// http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
			double radius = orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

			double incl = orbit.Inclination;

			//https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
			double lofAN = orbit.LoAN;
			//double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
			double angleFromLoAN = trueAnomaly + orbit.AoP;

			double x = Math.Cos(lofAN) * Math.Cos(angleFromLoAN) - Math.Sin(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
			double y = Math.Sin(lofAN) * Math.Cos(angleFromLoAN) + Math.Cos(lofAN) * Math.Sin(angleFromLoAN) * Math.Cos(incl);
			double z = Math.Sin(incl) * Math.Sin(angleFromLoAN);

			return new Vector3(x, y, z) * radius;
		}

		/// <summary>
		/// Calculates the root relative cartesian coordinate of an orbit for a given time.
		/// </summary>
		/// <param name="entity">Base Entity to calculate position from.</param>
		/// <param name="time">Time position desired from.</param>
		public static Vector3 GetAbsolutePosition(EntityBase entity, DateTime time, bool isStationary = false)
        {
            if (entity.Parent == null)//if we're the parent sun
                return GetPosition(entity.Orbit, GetTrueAnomaly(entity.Orbit, time));

            //else if we're a child
            Vector3 rootPos = PositionHelper.GetAbsolutePositionInMetres(entity.Parent);
            if (isStationary)
            {
                return rootPos;
            }

            if (entity.Orbit.Eccentricity < 1)
                return rootPos + GetPosition(entity.Orbit, GetTrueAnomaly(entity.Orbit, time));
            else
                return rootPos + GetPosition(entity.Orbit, GetTrueAnomaly(entity.Orbit, time));
            //if (orbit.Eccentricity == 1)
            //    return GetAbsolutePositionForParabolicOrbit_AU();
            //else
            //    return GetAbsolutePositionForHyperbolicOrbit_AU(orbit, time);

        }

        //public static Vector4 GetAbsolutePositionForParabolicOrbit_AU()
        //{ }

        //public static Vector4 GetAbsolutePositionForHyperbolicOrbit_AU(KeplerElements orbitDB, DateTime time)
        //{

        //}

        public static double GetTrueAnomaly(KeplerElements orbit, DateTime time)
        {
            // Get seconds since last time we passed the epoch point in the orbit
            double timeSinceEpoch = (time - orbit.Epoch).TotalSeconds % orbit.OrbitalPeriod;

            double currentMeanAnomaly = OrbitalMath.GetMeanAnomalyFromTime(
				orbit.MeanAnomalyAtEpoch, orbit.MeanMotion, timeSinceEpoch
            );

            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return OrbitalMath.TrueAnomalyFromEccentricAnomaly(orbit.Eccentricity, eccentricAnomaly);
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        public static double GetEccentricAnomaly(KeplerElements orbit, double currentMeanAnomaly)
        {
            //Kepler's Equation
            const int numIterations = 1000;
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
                //Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly from kepler Elements.");
                //gameEvent.EventType = EventType.Opps;
                //StaticRefLib.EventLog.AddEvent(gameEvent);
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
        public static double GetEccentricAnomalyH(KeplerElements orbit, double currentMeanAnomaly)
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
                throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit);
            }

            return f[i - 1];
        }

        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="entity">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector(EntityBase entity, DateTime atDateTime)
        {
            if (UseRelativeVelocity)
            {
                return InstantaneousOrbitalVelocityVector(entity, atDateTime);
            }
            else
            {
                return AbsoluteOrbitalVector(entity, atDateTime);
            }
        }

        public static Vector3 GetOrbitalInsertionVector(Vector3 departureVelocity, EntityBase targetEntity, DateTime arrivalDateTime)
        {
            if (UseRelativeVelocity)
                return departureVelocity;
            else
            {
                var targetVelocity = AbsoluteOrbitalVector(targetEntity, arrivalDateTime);
                return departureVelocity - targetVelocity;
            }
        }

        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, relative to the root object (ie sun)</returns>
        /// <param name="entity">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 AbsoluteOrbitalVector(EntityBase entity, DateTime atDateTime)
        {
            Vector3 vector = InstantaneousOrbitalVelocityVector(entity, atDateTime);
            if (entity.Parent != null)
            {
                vector += AbsoluteOrbitalVector(entity.Parent, atDateTime);
            }
            return vector;
        }

        /// <summary>
        /// PreciseOrbital Velocy in polar coordinates
        /// 
        /// </summary>
        /// <returns>item1 is speed, item2 angle</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static (double speed, double heading) InstantaneousOrbitalVelocityPolarCoordinate(KeplerElements orbit, DateTime atDateTime, double entityDryMassInKG, double parentEntityDryMassInKG)
        {
            var position = Distance.MToAU(GetPosition(orbit, atDateTime));
            var sma = Distance.MToAU(orbit.SemiMajorAxis);
            var gravitationalParameter_Km3S2 = GeneralMath.GravitationalParameter_Km3s2(parentEntityDryMassInKG + entityDryMassInKG);   // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2
            if (gravitationalParameter_Km3S2 == 0 || sma == 0)
                return (0, 0); //so we're not returning NaN;

            var gravitationalParameterAU = GeneralMath.GravitiationalParameter_Au3s2(parentEntityDryMassInKG + entityDryMassInKG);// (149597870700 * 149597870700 * 149597870700);
            var sgp = gravitationalParameterAU;

            double e = orbit.Eccentricity;
            double trueAnomaly = GetTrueAnomaly(orbit, atDateTime);
            double aoP = orbit.AoP;

            (double speed, double heading) polar = OrbitalMath.ObjectLocalVelocityPolar(sgp, position, sma, e, trueAnomaly, aoP);

            return polar;
        }

        /// <summary>
        /// Parent relative velocity vector. 
        /// </summary>
        /// <returns>The orbital vector relative to the parent</returns>
        /// <param name="entity">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        /// <param name="gravitationalParameter_Km3S2"></param>
        public static Vector3 InstantaneousOrbitalVelocityVector(EntityBase entity, DateTime atDateTime)
        {
            var parentEntityDryMassInKG = entity.Parent != null ? entity.Parent.DryMassInKG : 0;
            var position = GetPosition(entity.Orbit, atDateTime);
            var sma = entity.Orbit.SemiMajorAxis; 
            var gravitationalParameter_Km3S2 = GeneralMath.GravitationalParameter_Km3s2(parentEntityDryMassInKG + entity.DryMassInKG);   // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2
            if (gravitationalParameter_Km3S2 == 0 || sma == 0)
                return Vector3.Zero; //so we're not returning NaN;

            var gravitationalParameter_m3S2 = GeneralMath.StandardGravitationalParameter(parentEntityDryMassInKG + entity.DryMassInKG);
            var sgp = gravitationalParameter_m3S2;

            double e = entity.Orbit.Eccentricity;
            double trueAnomaly = GetTrueAnomaly(entity.Orbit, atDateTime);
            double aoP = entity.Orbit.AoP;
            double i = entity.Orbit.Inclination;
            double loAN = entity.Orbit.LoAN;
            return OrbitalMath.ParentLocalVeclocityVector(sgp, position, sma, e, trueAnomaly, aoP, i, loAN);
        }

        /// <summary>
        /// Get Shpere of Influence radius in Metres
        /// </summary>
        /// <param name="orbit">Current entity's orbital details.</param>
        /// <param name="entityDryMassInKG">Current entity's dry mass in KG.</param>
        /// <param name="parentEntityDryMassInKG">Parent entity's dry mass in KG. Pass in null if entity has no parent</param>
        /// <returns>Sphere of Influence radius in Metres</returns>
        public static double GetSOI(KeplerElements orbit, double entityDryMassInKG, double? parentEntityDryMassInKG)
        {
            if (parentEntityDryMassInKG.HasValue) //if we're not the parent star
            {
                return OrbitalMath.GetSOI(orbit.SemiMajorAxis, entityDryMassInKG, parentEntityDryMassInKG.Value);
            }
            
            return double.PositiveInfinity; //if we're the parent star, then Sphere of Influence is infinite.
        }
        #endregion
    }
}

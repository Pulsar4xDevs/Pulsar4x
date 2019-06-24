using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Orbit processor.
    /// How Orbits are calculated:
    /// First we get the time since epoch. (time from when the planet is at its closest to it's parent)
    /// Then we get the Mean Anomaly. (stored) 
    /// Eccentric Anomaly is calculated from the Mean Anomaly, and takes the most work. 
    /// True Anomaly, is calculated using the Eccentric Anomaly this is the angle from the parent (or focal point of the ellipse) to the body. 
    /// With the true anomaly, we can then use trig to calculate the position.  
    /// </summary>
    public class OrbitProcessor : IHotloopProcessor
    {
        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        private static readonly int StarInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pulsar4X.ECSLib.OrbitProcessor"/> use ralitive velocity.
        /// </summary>
        /// <value><c>true</c> if use ralitive velocity; otherwise, uses absolute <c>false</c>.</value>
        public static bool UseRalitiveVelocity { get; set; } = true;

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime toDate = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(entity, entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>(), toDate);
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateSystemOrbits(manager, toDate);
        }

        internal static void UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
       
            //TimeSpan orbitCycle = manager.Game.Settings.OrbitCycleTime;
            //DateTime toDate = manager.ManagerSubpulses.SystemLocalDateTime + orbitCycle;
            //starSystem.SystemSubpulses.AddSystemInterupt(toDate + orbitCycle, UpdateSystemOrbits);
            //manager.ManagerSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.OrbitProcessor);
            // Find the first orbital entity.
            Entity firstOrbital = manager.GetFirstEntityWithDataBlob(StarInfoTypeIndex);

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>(OrbitTypeIndex).Root;
            var rootPositionDB = root.GetDataBlob<PositionDB>(PositionTypeIndex);

            // Call recursive function to update every orbit in this system.
            UpdateOrbit(root, rootPositionDB, toDate);
        }

        private static void UpdateOrbit(ProtoEntity entity, PositionDB parentPositionDB, DateTime toDate)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>(OrbitTypeIndex);
            var entityPosition = entity.GetDataBlob<PositionDB>(PositionTypeIndex);

            //if(toDate.Minute > entityOrbitDB.OrbitalPeriod.TotalMinutes)

            // Get our Parent-Relative coordinates.
            try
            {
                Vector3 newPosition = GetPosition_AU(entityOrbitDB, toDate);

                // Get our Absolute coordinates.
                entityPosition.AbsolutePosition_AU = parentPositionDB.AbsolutePosition_AU + newPosition;

            }
            catch (OrbitProcessorException e)
            {
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.  
                Event evt = new Event(StaticRefLib.CurrentDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + entity.Guid + " " + e.Message);
                evt.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(evt);
            }

            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                UpdateOrbit(child, entityPosition, toDate);
            }
        }


        #region Orbit Position Calculations

        /// <summary>
        /// Calculates the parent-relative cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetPosition_AU(OrbitDB orbit, DateTime time)
        {
            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }
            return GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
        }

        /// <summary>
        /// Calculates the root ralitive cartesian coordinate of an orbit for a given time.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="time">Time position desired from.</param>
        public static Vector3 GetAbsolutePosition_AU(OrbitDB orbit, DateTime time)
        {
            if (orbit.Parent == null)//if we're the parent sun
                return GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            //else if we're a child
            Vector3 rootPos = orbit.Parent.GetDataBlob<PositionDB>().AbsolutePosition_AU;
            if (orbit.IsStationary)
            {
                return rootPos;
            }

            if(orbit.Eccentricity < 1)
                return rootPos + GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            else
                return rootPos + GetPosition_AU(orbit, GetTrueAnomaly(orbit, time));
            //if (orbit.Eccentricity == 1)
            //    return GetAbsolutePositionForParabolicOrbit_AU();
            //else
            //    return GetAbsolutePositionForHyperbolicOrbit_AU(orbit, time);

        }

        //public static Vector4 GetAbsolutePositionForParabolicOrbit_AU()
        //{ }

        //public static Vector4 GetAbsolutePositionForHyperbolicOrbit_AU(OrbitDB orbitDB, DateTime time)
        //{
            
        //}

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

            double m0 = Angle.ToRadians(orbit.MeanAnomalyAtEpoch);
            double n = Angle.ToRadians(orbit.MeanMotion);
            double currentMeanAnomaly = OrbitMath.GetMeanAnomalyFromTime(m0, n, timeSinceEpoch.TotalSeconds);

            double eccentricAnomaly = GetEccentricAnomaly(orbit, currentMeanAnomaly);
            return OrbitMath.TrueAnomalyFromEccentricAnomaly(orbit.Eccentricity, eccentricAnomaly);
            /*
            var x = Math.Cos(eccentricAnomaly) - orbit.Eccentricity;
            var y = Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity) * Math.Sin(eccentricAnomaly);
            return Math.Atan2(y, x);
            */
        }

        /// <summary>
        /// Calculates the cartesian coordinates (relative to it's parent) of an orbit for a given angle.
        /// </summary>
        /// <param name="orbit">OrbitDB to calculate position from.</param>
        /// <param name="trueAnomaly">Angle in Radians.</param>
        public static Vector3 GetPosition_AU(OrbitDB orbit, double trueAnomaly)
        {

            if (orbit.IsStationary)
            {
                return new Vector3(0, 0, 0);
            }

            // http://en.wikipedia.org/wiki/True_anomaly#Radius_from_true_anomaly
            double radius = orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity) / (1 + orbit.Eccentricity * Math.Cos(trueAnomaly));

            double inclination = Angle.ToRadians(orbit.Inclination);

            //https://downloads.rene-schwarz.com/download/M001-Keplerian_Orbit_Elements_to_Cartesian_State_Vectors.pdf
            double lofAN = Angle.ToRadians(orbit.LongitudeOfAscendingNode);
            //double aofP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double tA = trueAnomaly + Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double incl = inclination;

            double x = Math.Cos(lofAN) * Math.Cos(tA) - Math.Sin(lofAN) * Math.Sin(tA) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(tA) + Math.Cos(lofAN) * Math.Sin(tA) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(tA);

            return new Vector3(x, y, z) * radius;
        }

        /// <summary>
        /// Calculates the current Eccentric Anomaly given certain orbital parameters.
        /// </summary>
        public static double GetEccentricAnomaly(OrbitDB orbit, double currentMeanAnomaly)
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
                Event gameEvent = new Event("Non-convergence of Newton's method while calculating Eccentric Anomaly.");
                gameEvent.Entity = orbit.OwningEntity;
                gameEvent.EventType = EventType.Opps;

                StaticRefLib.EventLog.AddEvent(gameEvent);
                //throw new OrbitProcessorException("Non-convergence of Newton's method while calculating Eccentric Anomaly.", orbit.OwningEntity);
            }

            return e[i - 1];
        }

        /// <summary>
        /// Gets the orbital vector, will be either Absolute or Ralitive depending on static bool UseRalitiveVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector2 GetOrbitalVector(OrbitDB orbit, DateTime atDateTime)
        {


            if (UseRalitiveVelocity)
            {
                Vector2 vector = InstantaneousOrbitalVelocityVector(orbit, atDateTime);
                return InstantaneousOrbitalVelocityVector(orbit, atDateTime);
            }
            else
            {
                return AbsoluteOrbitalVector(orbit, atDateTime);
            }
        }

        public static Vector2 GetOrbitalInsertionVector(Vector2 departureVelocity, OrbitDB targetOrbit, DateTime arrivalDateTime)
        {
            if (UseRalitiveVelocity)
                return departureVelocity;
            else
            {
                var targetVelocity = AbsoluteOrbitalVector(targetOrbit, arrivalDateTime);
                return departureVelocity - targetVelocity;
            }
        }

        /// <summary>
        /// The orbital vector.
        /// </summary>
        /// <returns>The orbital vector, ralitive to the root object (ie sun)</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector2 AbsoluteOrbitalVector(OrbitDB orbit, DateTime atDateTime)       
        {
            Vector2 vector = InstantaneousOrbitalVelocityVector(orbit, atDateTime);
            if(orbit.Parent != null)
                vector += AbsoluteOrbitalVector((OrbitDB)orbit.ParentDB, atDateTime);
            return vector;

        }

        /// <summary>
        /// PreciseOrbital Velocy in polar coordinates
        /// 
        /// </summary>
        /// <returns>item1 is speed, item2 angle</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static (double speed, double heading) InstantaneousOrbitalVelocityPolarCoordinate(OrbitDB orbit, DateTime atDateTime)
        {
            var position = GetPosition_AU(orbit, atDateTime);
            var sma = orbit.SemiMajorAxis;
            if (orbit.GravitationalParameter == 0 || sma == 0)
                return (0,0); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameterAU;
            
            double e = orbit.Eccentricity;

            double trueAnomaly = GetTrueAnomaly(orbit, atDateTime);
            double loAN = Angle.ToRadians(orbit.LongitudeOfAscendingNode);
            double aoP = Angle.ToRadians(orbit.ArgumentOfPeriapsis);
            double inclination = Angle.ToRadians(orbit.Inclination);
            var loP = OrbitMath.GetLongditudeOfPeriapsis(inclination, aoP, loAN);

            (double speed,double heading) polar = OrbitMath.InstantaneousOrbitalVelocityPolarCoordinate(sgp, position, sma, e, trueAnomaly);
            polar.heading += loP;
            return polar;
            
        }

        /// <summary>
        /// 2d vector
        /// </summary>
        /// <returns>The orbital vector ralitive to the parent</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector2 InstantaneousOrbitalVelocityVector(OrbitDB orbit, DateTime atDateTime)
        {
            var position = GetPosition_AU(orbit, atDateTime);
            var sma = orbit.SemiMajorAxis;
            if (orbit.GravitationalParameter == 0 || sma == 0)
                return new Vector2(); //so we're not returning NaN;
            var sgp = orbit.GravitationalParameterAU;
      
            double e = orbit.Eccentricity;
            double trueAnomaly = OrbitProcessor.GetTrueAnomaly(orbit, atDateTime);

            (double speed, double angle) polarvector = InstantaneousOrbitalVelocityPolarCoordinate(orbit, atDateTime);
            var v = new Vector2()
            {
                X = Math.Cos(polarvector.angle) * polarvector.speed,
                Y = Math.Sin(polarvector.angle) * polarvector.speed,
            };
            return v;
            //return OrbitMath.InstantaneousOrbitalVelocityVector(sgp, position, sma, e, trueAnomaly);
        }

        /// <summary>
        /// Gets the SOI radius of a given body.
        /// </summary>
        /// <returns>The SOI radius in AU</returns>
        /// <param name="entity">Entity which has OrbitDB and MassVolumeDB</param>
        public static double GetSOI(Entity entity)
        {
            var orbitDB = entity.GetDataBlob<OrbitDB>();
            if (orbitDB.Parent != null) //if we're not the parent star
            {
                var semiMajAxis = orbitDB.SemiMajorAxis;

                var myMass = entity.GetDataBlob<MassVolumeDB>().Mass;

                var parentMass = orbitDB.Parent.GetDataBlob<MassVolumeDB>().Mass;

                return OrbitMath.GetSOI(semiMajAxis, myMass, parentMass);
            }
            else return double.MaxValue; //if we're the parent star, then soi is infinate. 
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
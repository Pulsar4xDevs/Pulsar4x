using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OrbitDB : TreeHierarchyDB, IGetValuesHash
    {
        /// <summary>
        /// Semimajor Axis of orbit stored in AU.
        /// Radius of an orbit at the orbit's two most distant points.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double SemiMajorAxis { get; private set; }

        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Eccentricity { get; private set; }

        /// <summary>
        /// Angle between the orbit and the flat reference plane.
        /// Stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Inclination { get; private set; }

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the reference frame stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double LongitudeOfAscendingNode { get; private set; }

        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double ArgumentOfPeriapsis { get; private set; }

        /// <summary>
        /// Definition of the position of the body in the orbit at the reference time
        /// epoch. Mathematically convenient angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MeanAnomalyAtEpoch { get; private set; }

        /// <summary>
        /// reference time. Orbital parameters are stored relative to this reference.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public DateTime Epoch { get; internal set; }

        /// <summary>
        /// 2-Body gravitational parameter of system in km^3/s^2
        /// </summary>
        [PublicAPI]
        public double GravitationalParameter { get; private set; }
        [PublicAPI]
        public double GravitationalParameterAU { get; private set; }

        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        [PublicAPI]
        public TimeSpan OrbitalPeriod { get; private set; }

        /// <summary>
        /// Mean Motion of orbit. Stored as Degrees/Sec.
        /// </summary>
        [PublicAPI]
        public double MeanMotion { get; private set; }

        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Apoapsis { get; private set; }

        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Periapsis { get; private set; }

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public bool IsStationary { get; private set; }

        [JsonProperty]
        private double _parentMass;
        [JsonProperty]
        private double _myMass;

        #region Construction Interface

        /// <summary>
        /// Creates on Orbit at current location from a given velocity
        /// </summary>
        /// <returns>The Orbit Does not attach the OrbitDB to the entity!</returns>
        /// <param name="parent">Parent.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="velocityAU">Velocity.</param>
        public static OrbitDB FromVector(Entity parent, Entity entity, Vector4 velocityAU, DateTime atDateTime)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().Mass;
            var myMass = entity.GetDataBlob<MassVolumeDB>().Mass;

            var epoch1 = parent.Manager.ManagerSubpulses.StarSysDateTime; //getting epoch from here is incorrect as the local datetime doesn't change till after the subpulse.

            var parentPos = OrbitProcessor.GetAbsolutePosition_AU(parent.GetDataBlob<OrbitDB>(), atDateTime); //need to use the parent position at the epoch
            var ralitivePos = entity.GetDataBlob<PositionDB>().AbsolutePosition_AU - parentPos;
            if (ralitivePos.Length() > OrbitProcessor.GetSOI(parent))
                throw new Exception("Entity not in target SOI");

            var sgp = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, ralitivePos, velocityAU);

            var epoch = atDateTime;// - TimeSpan.FromSeconds(ke.Epoch); //ke.Epoch is seconds from periapsis.   

            OrbitDB orbit = new OrbitDB(parent, parentMass, myMass,
                        Math.Abs(ke.SemiMajorAxis),
                        ke.Eccentricity,
                        Angle.ToDegrees(ke.Inclination),
                        Angle.ToDegrees(ke.LoAN),
                        Angle.ToDegrees(ke.AoP),
                        Angle.ToDegrees(ke.MeanAnomalyAtEpoch),
                        epoch);

            var pos = OrbitProcessor.GetPosition_AU(orbit, atDateTime);
            var d = Distance.AuToKm(pos - ralitivePos).Length();
            if (d > 1)
            {
                var e = new Event(atDateTime, "Positional difference of " + d + "Km when creating orbit from velocity");
                e.Entity = entity;
                e.SystemGuid = entity.Manager.ManagerGuid;
                //e.Faction =  entity.FactionOwner;
                StaticRefLib.EventLog.AddEvent(e);
            }
            return orbit;
        }

        public static OrbitDB FromVector(Entity parent, double myMass, double parentMass, double sgp, Vector4 position, Vector4 velocity, DateTime atDateTime)
        {
            if (position.Length() > OrbitProcessor.GetSOI(parent))
                throw new Exception("Entity not in target SOI");
            //var sgp  = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity);
            OrbitDB orbit = new OrbitDB(parent, parentMass, myMass,
                        ke.SemiMajorAxis,
                        ke.Eccentricity,
                        Angle.ToDegrees(ke.Inclination),
                        Angle.ToDegrees(ke.LoAN),
                        Angle.ToDegrees(ke.AoP),
                        Angle.ToDegrees(ke.MeanAnomalyAtEpoch),
                        atDateTime);// - TimeSpan.FromSeconds(ke.Epoch));
            var pos = OrbitProcessor.GetAbsolutePosition_AU(orbit, atDateTime);
            return orbit;
        }


        public static OrbitDB FromVectorKM(Entity parent, double myMass, double parentMass, double sgp, Vector4 position, Vector4 velocity, DateTime atDateTime)
        {
            if (Distance.KmToAU(position.Length()) > OrbitProcessor.GetSOI(parent))
                throw new Exception("Entity not in target SOI");
            //var sgp  = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity);
            OrbitDB orbit = new OrbitDB(parent, parentMass, myMass,
                        Distance.KmToAU(ke.SemiMajorAxis),
                        ke.Eccentricity,
                        Angle.ToDegrees(ke.Inclination),
                        Angle.ToDegrees(ke.LoAN),
                        Angle.ToDegrees(ke.AoP),
                        Angle.ToDegrees(ke.MeanAnomalyAtEpoch),
                        atDateTime);// - TimeSpan.FromSeconds(ke.Epoch));
            var pos = OrbitProcessor.GetAbsolutePosition_AU(orbit, atDateTime);
            return orbit;
        }

        public static OrbitDB FromKeplerElements(Entity parent, double parentMass, double myMass, KeplerElements ke, DateTime atDateTime)
        {

            OrbitDB orbit = new OrbitDB(parent, parentMass, myMass,
                       ke.SemiMajorAxis,
                       ke.Eccentricity,
                       Angle.ToDegrees(ke.Inclination),
                       Angle.ToDegrees(ke.LoAN),
                       Angle.ToDegrees(ke.AoP),
                       Angle.ToDegrees(ke.MeanAnomalyAtEpoch),
                       atDateTime);// - TimeSpan.FromSeconds(ke.Epoch));
            //var pos = OrbitProcessor.GetAbsolutePosition_AU(orbit, atDateTime);
            return orbit;
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="longitudeOfPeriapsis">Longitude of periapsis in degrees.</param>
        /// <param name="meanLongitude">Longitude of object at epoch in degrees.</param>
        /// <param name="epoch">reference time for these orbital elements.</param>
        public static OrbitDB FromMajorPlanetFormat([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis, double eccentricity, double inclination,
                                                    double longitudeOfAscendingNode, double longitudeOfPeriapsis, double meanLongitude, DateTime epoch)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            // http://en.wikipedia.org/wiki/Longitude_of_the_periapsis
            double argumentOfPeriapsis = longitudeOfPeriapsis - longitudeOfAscendingNode;
            // http://en.wikipedia.org/wiki/Mean_longitude
            double meanAnomaly = meanLongitude - (longitudeOfAscendingNode + argumentOfPeriapsis);

            return new OrbitDB(parent, parentMass, myMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis in degrees.</param>
        /// <param name="meanAnomaly">Mean Anomaly in degrees.</param>
        /// <param name="epoch">reference time for these orbital elements.</param>
        public static OrbitDB FromAsteroidFormat([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis, double eccentricity, double inclination,
                                                double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            return new OrbitDB(parent, parentMass, myMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        internal OrbitDB(Entity parent, double parentMass, double myMass, double semiMajorAxis, double eccentricity, double inclination,
                        double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
            : base(parent)
        {
            SemiMajorAxis = semiMajorAxis;
            Eccentricity = eccentricity;
            Inclination = inclination;
            LongitudeOfAscendingNode = longitudeOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            MeanAnomalyAtEpoch = meanAnomaly;
            Epoch = epoch;

            _parentMass = parentMass;
            _myMass = myMass;

            CalculateExtendedParameters();
        }

        public OrbitDB()
            : base(null)
        {
            IsStationary = true;
        }

        public OrbitDB(Entity parent)
            : base(parent)
        {
            IsStationary = true;
        }

        public OrbitDB(OrbitDB toCopy)
            : base(toCopy.Parent)
        {
            if (toCopy.IsStationary)
            {
                IsStationary = true;
                return;
            }

            SemiMajorAxis = toCopy.SemiMajorAxis;
            Eccentricity = toCopy.Eccentricity;
            Inclination = toCopy.Inclination;
            LongitudeOfAscendingNode = toCopy.LongitudeOfAscendingNode;
            ArgumentOfPeriapsis = toCopy.ArgumentOfPeriapsis;
            MeanAnomalyAtEpoch = toCopy.MeanAnomalyAtEpoch;
            _parentMass = toCopy._parentMass;
            _myMass = toCopy._myMass;
            Epoch = toCopy.Epoch;
        }
        #endregion

        private void CalculateExtendedParameters()
        {
            if (IsStationary)
            {
                return;
            }
            // Calculate extended parameters.
            // http://en.wikipedia.org/wiki/Standard_gravitational_parameter#Two_bodies_orbiting_each_other
            GravitationalParameter = GameConstants.Science.GravitationalConstant * (_parentMass + _myMass) / (1000 * 1000 * 1000); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2
            GravitationalParameterAU = GameConstants.Science.GravitationalConstant * (_parentMass + _myMass) / 3.347928976e33;// (149597870700 * 149597870700 * 149597870700);
            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(Distance.AuToKm(SemiMajorAxis), 3) / (GravitationalParameter));
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                OrbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                OrbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            MeanMotion = Math.Sqrt(GravitationalParameter / Math.Pow(Distance.AuToKm(SemiMajorAxis), 3)); // Calculated in radians.
            MeanMotion = Angle.ToDegrees(MeanMotion); // Stored in degrees.

            Apoapsis = (1 + Eccentricity) * SemiMajorAxis;
            Periapsis = (1 - Eccentricity) * SemiMajorAxis;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CalculateExtendedParameters();
        }

        public override object Clone()
        {
            return new OrbitDB(this);
        }

        #region ISensorCloneInterface



        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            UpdateFromSensorInfo(sensorInfo.DetectedEntity.GetDataBlob<OrbitDB>(), sensorInfo);
        }

        OrbitDB(OrbitDB toCopy, SensorInfoDB sensorInfo, Entity parentEntity) : base(parentEntity)
        {

            Epoch = toCopy.Epoch; //This should stay the same
            IsStationary = toCopy.IsStationary;
            UpdateFromSensorInfo(toCopy, sensorInfo);

        }

        void UpdateFromSensorInfo(OrbitDB actualDB, SensorInfoDB sensorInfo)
        {
            //var quality = sensorInfo.HighestDetectionQuality.detectedSignalQuality.Percent; //quality shouldn't affect positioning. 
            double signalBestMagnatude = sensorInfo.HighestDetectionQuality.SignalStrength_kW;
            double signalNowMagnatude = sensorInfo.LatestDetectionQuality.SignalStrength_kW;


            SemiMajorAxis = actualDB.SemiMajorAxis;
            Eccentricity = actualDB.Eccentricity;
            Inclination = actualDB.Inclination;
            LongitudeOfAscendingNode = actualDB.LongitudeOfAscendingNode;
            ArgumentOfPeriapsis = actualDB.ArgumentOfPeriapsis;
            MeanAnomalyAtEpoch = actualDB.MeanAnomalyAtEpoch;
            _parentMass = actualDB._parentMass;
            _myMass = actualDB._myMass;
            CalculateExtendedParameters();
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(SemiMajorAxis, hash);
            hash = Misc.ValueHash(Eccentricity, hash);


            return hash;
        }

        #endregion
    }
}

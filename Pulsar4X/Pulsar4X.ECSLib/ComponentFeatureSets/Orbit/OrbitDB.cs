using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class OrbitDB : TreeHierarchyDB, IGetValuesHash
    {
        /// <summary>
        /// Semimajor Axis of orbit in AU.
        /// Radius of an orbit at the orbit's two most distant points.
        /// </summary>
        [PublicAPI]
        public double SemiMajorAxisAU
        {
            get { return Distance.MToAU(SemiMajorAxis);}
            private set { SemiMajorAxis = Distance.AuToMt(value); }
        }
        
        /// <summary>
        /// Stored in Meters
        /// </summary>
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
        /// in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Inclination_Degrees
        {
            get { return Angle.ToDegrees(Inclination);}
            private set { Inclination = Angle.ToRadians(value); }
        }
        
        /// <summary>
        /// i in radians
        /// </summary>
        public double Inclination { get; private set; }
        
        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the reference frame stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double LongitudeOfAscendingNode_Degrees 
        {
            get { return Angle.ToDegrees(LongitudeOfAscendingNode);}
            private set { LongitudeOfAscendingNode = Angle.ToRadians(value); }
        }

        /// <summary>
        /// LoAN in radians
        /// </summary>
        public double LongitudeOfAscendingNode { get; private set; }
        
        
        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double ArgumentOfPeriapsis_Degrees        
        {
            get { return Angle.ToDegrees(ArgumentOfPeriapsis);}
            private set { ArgumentOfPeriapsis = Angle.ToRadians(value); }
        }


        /// <summary>
        /// AoP in radians
        /// </summary>
        public  double ArgumentOfPeriapsis { get; private set; }
        
        
        /// <summary>
        /// Definition of the position of the body in the orbit at the reference time
        /// epoch. Mathematically convenient angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double MeanAnomalyAtEpoch_Degrees        
        {
            get { return Angle.ToDegrees(MeanAnomalyAtEpoch);}
            private set { MeanAnomalyAtEpoch = Angle.ToRadians(value); }
        }

        public double MeanAnomalyAtEpoch { get; private set; }
        
        /// <summary>
        /// reference time. Orbital parameters are stored relative to this reference.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public DateTime Epoch { get; internal set; }

        
        
        public double GravitationalParameter_m3S2 { get; private set; }
        
        /// <summary>
        /// 2-Body gravitational parameter of system in km^3/s^2
        /// </summary>
        [PublicAPI]
        public double GravitationalParameter_Km3S2 { get; private set; }
        [PublicAPI]
        public double GravitationalParameterAU { get; private set; }

        public double SOI_m { get; private set; }
        
        
        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        [PublicAPI]
        public TimeSpan OrbitalPeriod { get; private set; }

        /// <summary>
        /// Mean Motion of orbit. in Degrees/Sec.
        /// </summary>
        [PublicAPI]
        public double MeanMotion_DegreesSec
        {
            get { return Angle.ToDegrees(MeanMotion);}
            private set { MeanMotion = Angle.ToRadians(value); }
        }
        /// <summary>
        /// In Radians/Sec
        /// </summary>
        public double MeanMotion { get; private set; }

        
        
        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Apoapsis_AU
        {
            get { return Distance.MToAU(Apoapsis);}
            private set { Apoapsis = Distance.AuToMt(value); }
        }
        public double Apoapsis { get; private set; }
        
        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in AU.
        /// </summary>
        [PublicAPI]
        public double Periapsis_AU
        {
            get { return Distance.MToAU(Periapsis);}
            private set { Periapsis = Distance.AuToMt(value); }
        }
        
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
        public static OrbitDB FromVector(Entity parent, Entity entity, Vector3 velocityAU, DateTime atDateTime)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().Mass;
            var myMass = entity.GetDataBlob<MassVolumeDB>().Mass;

            //var epoch1 = parent.Manager.ManagerSubpulses.StarSysDateTime; //getting epoch from here is incorrect as the local datetime doesn't change till after the subpulse.

            //var parentPos = OrbitProcessor.GetAbsolutePosition_AU(parent.GetDataBlob<OrbitDB>(), atDateTime); //need to use the parent position at the epoch
            var posdb = entity.GetDataBlob<PositionDB>();
            posdb.SetParent(parent);
            var ralitivePos = posdb.RelativePosition_AU;//entity.GetDataBlob<PositionDB>().AbsolutePosition_AU - parentPos;
            if (ralitivePos.Length() > OrbitProcessor.GetSOI_AU(parent))
                throw new Exception("Entity not in target SOI");

            var sgp = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, ralitivePos, velocityAU, atDateTime);

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
                e.EventType = EventType.Opps;
                //e.Faction =  entity.FactionOwner;
                StaticRefLib.EventLog.AddEvent(e);

                //other info:
                var keta = Angle.ToDegrees( ke.TrueAnomalyAtEpoch);
                var obta = Angle.ToDegrees( OrbitProcessor.GetTrueAnomaly(orbit, atDateTime));
                var tadif = Angle.ToDegrees(Angle.DifferenceBetweenRadians(keta, obta));
                var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDateTime);
                var pos2 = OrbitProcessor.GetPosition_AU(orbit, ke.TrueAnomalyAtEpoch);
                var d2 = Distance.AuToKm(pos1 - pos2).Length();
            }
            return orbit;
        }

        public static OrbitDB FromVector(Entity parent, double myMass, double parentMass, double sgp, Vector3 position, Vector3 velocity, DateTime atDateTime)
        {
            if (position.Length() > OrbitProcessor.GetSOI_AU(parent))
                throw new Exception("Entity not in target SOI");
            //var sgp  = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, atDateTime);
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


        public static OrbitDB FromVectorKM(Entity parent, double myMass, double parentMass, double sgp, Vector3 position, Vector3 velocity, DateTime atDateTime)
        {
            if (Distance.KmToAU(position.Length()) > OrbitProcessor.GetSOI_AU(parent))
                throw new Exception("Entity not in target SOI");
            //var sgp  = GameConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity, atDateTime);
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

        public static OrbitDB FromKeplerElements(Entity parent, double myMass, KeplerElements ke, DateTime atDateTime)
        {

            OrbitDB orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = ke.SemiMajorAxis,
                Eccentricity = ke.Eccentricity,
                Inclination_Degrees = ke.Inclination,
                LongitudeOfAscendingNode = ke.LoAN,
                ArgumentOfPeriapsis = ke.AoP,
                MeanAnomalyAtEpoch = ke.MeanAnomalyAtEpoch,
                Epoch = atDateTime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().Mass,
                _myMass = myMass

            };
            
            orbit.IsStationary = false;
            orbit.CalculateExtendedParameters();
            return orbit;
        }

        /// <summary>
        /// Circular orbit from position. 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="obj"></param>
        /// <param name="atDatetime"></param>
        /// <returns></returns>
        public static OrbitDB FromPosition(Entity parent, Entity obj, DateTime atDatetime)
        {
            
            var parpos = parent.GetDataBlob<PositionDB>();
            var objPos = obj.GetDataBlob<PositionDB>();
            var ralpos = objPos.AbsolutePosition_m - parpos.AbsolutePosition_m;
            var r = ralpos.Length();
            var i = Math.Atan2(r, ralpos.Z);
            var m0 = Math.Atan2(ralpos.Y, ralpos.X);
            var orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = r,
                Eccentricity = 0,
                Inclination_Degrees = i,
                LongitudeOfAscendingNode = 0,
                ArgumentOfPeriapsis = 0,
                MeanAnomalyAtEpoch = m0,
                Epoch = atDatetime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().Mass,
                _myMass = obj.GetDataBlob<MassVolumeDB>().Mass

                
                
            };
            orbit.IsStationary = false;
            orbit.CalculateExtendedParameters();

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

        internal OrbitDB(Entity parent, double parentMass, double myMass, double semiMajorAxisAU, double eccentricity, double inclinationDegrees,
                        double longitudeOfAscendingNodeDegrees, double argumentOfPeriapsisDegrees, double meanAnomaly, DateTime epoch)
            : base(parent)
        {
            SemiMajorAxisAU = semiMajorAxisAU;
            Eccentricity = eccentricity;
            Inclination_Degrees = inclinationDegrees;
            LongitudeOfAscendingNode_Degrees = longitudeOfAscendingNodeDegrees;
            ArgumentOfPeriapsis_Degrees = argumentOfPeriapsisDegrees;
            MeanAnomalyAtEpoch_Degrees = meanAnomaly;
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
            GravitationalParameter_Km3S2 = GMath.GravitationalParameter_Km3s2(_parentMass + _myMass); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2
            GravitationalParameterAU = GMath.GrabitiationalParameter_Au3s2(_parentMass + _myMass);// (149597870700 * 149597870700 * 149597870700);
            GravitationalParameter_m3S2 = GMath.StandardGravitationalParameter(_parentMass + _myMass);

            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(Distance.AuToKm(SemiMajorAxisAU), 3) / (GravitationalParameter_Km3S2));
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                OrbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                OrbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            MeanMotion_DegreesSec = Math.Sqrt(GravitationalParameter_Km3S2 / Math.Pow(Distance.AuToKm(SemiMajorAxisAU), 3)); // Calculated in radians.
            MeanMotion_DegreesSec = Angle.ToDegrees(MeanMotion_DegreesSec); // Stored in degrees.

            Apoapsis_AU = (1 + Eccentricity) * SemiMajorAxisAU;
            Periapsis_AU = (1 - Eccentricity) * SemiMajorAxisAU;

            SOI_m = OrbitMath.GetSOI(SemiMajorAxis, _myMass, _parentMass);

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


            SemiMajorAxisAU = actualDB.SemiMajorAxisAU;
            Eccentricity = actualDB.Eccentricity;
            Inclination_Degrees = actualDB.Inclination_Degrees;
            LongitudeOfAscendingNode_Degrees = actualDB.LongitudeOfAscendingNode_Degrees;
            ArgumentOfPeriapsis_Degrees = actualDB.ArgumentOfPeriapsis_Degrees;
            MeanAnomalyAtEpoch_Degrees = actualDB.MeanAnomalyAtEpoch_Degrees;
            _parentMass = actualDB._parentMass;
            _myMass = actualDB._myMass;
            CalculateExtendedParameters();
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(SemiMajorAxisAU, hash);
            hash = Misc.ValueHash(Eccentricity, hash);


            return hash;
        }

        #endregion
    }
}

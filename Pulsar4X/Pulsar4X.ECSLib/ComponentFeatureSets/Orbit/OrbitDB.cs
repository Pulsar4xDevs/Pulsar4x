using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class OrbitDB : TreeHierarchyDB, IGetValuesHash
    {
        /// <summary>
        /// Semimajor Axis of orbit in AU.
        /// Radius of an orbit at the orbit's two most distant points.
        /// </summary>
        [PublicAPI]
        public double SemiMajorAxis_AU
        {
            get { return Distance.MToAU(SemiMajorAxis);}
            protected set { SemiMajorAxis = Distance.AuToMt(value); }
        }
        
        /// <summary>
        /// Stored in Meters
        /// </summary>
        [JsonProperty]
        public double SemiMajorAxis { get; protected set; }
        
        
        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Eccentricity { get; protected set; }

        /// <summary>
        /// Angle between the orbit and the flat reference plane.
        /// in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Inclination_Degrees
        {
            get { return Angle.ToDegrees(Inclination);}
            protected set { Inclination = Angle.ToRadians(value); }
        }
        
        /// <summary>
        /// i in radians
        /// </summary>
        public double Inclination { get; protected set; }
        
        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the reference frame stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double LongitudeOfAscendingNode_Degrees 
        {
            get { return Angle.ToDegrees(LongitudeOfAscendingNode);}
            protected set { LongitudeOfAscendingNode = Angle.ToRadians(value); }
        }

        /// <summary>
        /// LoAN in radians
        /// </summary>
        public double LongitudeOfAscendingNode { get; protected set; }
        
        
        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double ArgumentOfPeriapsis_Degrees        
        {
            get { return Angle.ToDegrees(ArgumentOfPeriapsis);}
            protected set { ArgumentOfPeriapsis = Angle.ToRadians(value); }
        }


        /// <summary>
        /// AoP in radians
        /// </summary>
        public  double ArgumentOfPeriapsis { get; protected set; }
        
        
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
            protected set { MeanAnomalyAtEpoch = Angle.ToRadians(value); }
        }

        public double MeanAnomalyAtEpoch { get; protected set; }
        
        /// <summary>
        /// reference time. Orbital parameters are stored relative to this reference.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public DateTime Epoch { get; internal set; }

        
        
        public double GravitationalParameter_m3S2 { get; protected set; }
        
        /// <summary>
        /// 2-Body gravitational parameter of system in km^3/s^2
        /// </summary>
        [PublicAPI]
        public double GravitationalParameter_Km3S2 { get; protected set; }
        [PublicAPI]
        public double GravitationalParameterAU { get; protected set; }

        public double SOI_m { get; protected set; } = double.MaxValue;
        
        
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
        public double MeanMotion { get; protected set; }

        
        
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
        public bool IsStationary { get; private set; } = false;

        [JsonProperty]
        internal double _parentMass;
        [JsonProperty]
        internal double _myMass;

        #region Construction Interface

        
        /// <summary>
        /// Creates on Orbit at current location from a given velocity
        /// </summary>
        /// <returns>The Orbit Does not attach the OrbitDB to the entity!</returns>
        /// <param name="parent">Parent. must have massdb</param>
        /// <param name="entity">Entity. must have massdb</param>
        /// <param name="velocity_m">Velocity in meters.</param>
        public static OrbitDB FromVelocity_m(Entity parent, Entity entity, Vector3 velocity_m, DateTime atDateTime)
        {
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = entity.GetDataBlob<MassVolumeDB>().MassDry;

            //var epoch1 = parent.Manager.ManagerSubpulses.StarSysDateTime; //getting epoch from here is incorrect as the local datetime doesn't change till after the subpulse.

            //var parentPos = OrbitProcessor.GetAbsolutePosition_AU(parent.GetDataBlob<OrbitDB>(), atDateTime); //need to use the parent position at the epoch
            var posdb = entity.GetDataBlob<PositionDB>();
            posdb.SetParent(parent);
            var relativePos = posdb.RelativePosition_m;//entity.GetDataBlob<PositionDB>().AbsolutePosition_AU - parentPos;
            if (relativePos.Length() > parent.GetSOI_m())
                throw new Exception("Entity not in target SOI");

            //var sgp = UniversalConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var sgp_m = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
            var ke_m = OrbitMath.KeplerFromPositionAndVelocity(sgp_m, relativePos, velocity_m, atDateTime);

            
            OrbitDB orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = ke_m.SemiMajorAxis,
                Eccentricity = ke_m.Eccentricity,
                Inclination = ke_m.Inclination,
                LongitudeOfAscendingNode = ke_m.LoAN,
                ArgumentOfPeriapsis = ke_m.AoP,
                MeanAnomalyAtEpoch = ke_m.MeanAnomalyAtEpoch,
                Epoch = atDateTime,

                _parentMass = parentMass,
                _myMass = myMass

            };
            orbit.CalculateExtendedParameters();

            var pos = orbit.GetPosition_m(atDateTime);
            var d = (pos - relativePos).Length();
            if (d > 1)
            {
                var e = new Event(atDateTime, "Positional difference of " + Stringify.Distance(d) + " when creating orbit from velocity");
                e.Entity = entity;
                e.SystemGuid = entity.Manager.ManagerGuid;
                e.EventType = EventType.Opps;
                //e.Faction =  entity.FactionOwner;
                StaticRefLib.EventLog.AddEvent(e);

                //other info:
                var keta = Angle.ToDegrees(ke_m.TrueAnomalyAtEpoch);
                var obta = Angle.ToDegrees(orbit.GetTrueAnomaly(atDateTime));
                var tadif = Angle.ToDegrees(Angle.DifferenceBetweenRadians(keta, obta));
                var pos1 = orbit.GetPosition_m(atDateTime);
                var pos2 = orbit.GetPosition_m(ke_m.TrueAnomalyAtEpoch);
                var d2 = (pos1 - pos2).Length();
            }
            
            
            return orbit;
        }
        
                
        /// <summary>
        /// In Meters!
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="myMass"></param>
        /// <param name="parentMass"></param>
        /// <param name="sgp_m"></param>
        /// <param name="position_m"></param>
        /// <param name="velocity_m"></param>
        /// <param name="atDateTime"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static OrbitDB FromVector(Entity parent, double myMass, double parentMass, double sgp_m, Vector3 position_m, Vector3 velocity_m, DateTime atDateTime)
        {
            if (position_m.Length() > parent.GetSOI_AU())
                throw new Exception("Entity not in target SOI");
            //var sgp  = UniversalConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp_m, position_m, velocity_m, atDateTime);
            
            
            OrbitDB orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = ke.SemiMajorAxis,
                Eccentricity = ke.Eccentricity,
                Inclination = ke.Inclination,
                LongitudeOfAscendingNode = ke.LoAN,
                ArgumentOfPeriapsis = ke.AoP,
                MeanAnomalyAtEpoch = ke.MeanAnomalyAtEpoch,
                Epoch = atDateTime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry,
                _myMass = myMass

            };
            orbit.CalculateExtendedParameters();
            
            var pos = orbit.GetAbsolutePosition_m(atDateTime);
            return orbit;
        }



        /// <summary>
        /// In Meters
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="myMass"></param>
        /// <param name="ke">in m</param>
        /// <param name="atDateTime"></param>
        /// <returns></returns>
        public static OrbitDB FromKeplerElements(Entity parent, double myMass, KeplerElements ke, DateTime atDateTime)
        {

            OrbitDB orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = ke.SemiMajorAxis,
                Eccentricity = ke.Eccentricity,
                Inclination = ke.Inclination,
                LongitudeOfAscendingNode = ke.LoAN,
                ArgumentOfPeriapsis = ke.AoP,
                MeanAnomalyAtEpoch = ke.MeanAnomalyAtEpoch,
                Epoch = atDateTime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry,
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
            var i = Math.Atan2(ralpos.Z, r);
            var m0 = Math.Atan2(ralpos.Y, ralpos.X);
            var orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = r,
                Eccentricity = 0,
                Inclination = i,
                LongitudeOfAscendingNode = 0,
                ArgumentOfPeriapsis = 0,
                MeanAnomalyAtEpoch = m0,
                Epoch = atDatetime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry,
                _myMass = obj.GetDataBlob<MassVolumeDB>().MassDry
                
            };
            orbit.IsStationary = false;
            orbit.CalculateExtendedParameters();

            return orbit;
        }
        
        /// <summary>
        /// Circular orbit from position. 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="objPos">ralitive to parent</param>
        /// <param name="objMass"></param>
        /// <param name="atDatetime"></param>
        /// <returns></returns>
        public static OrbitDB FromPosition(Entity parent, Vector3 objPos, double objMass, DateTime atDatetime)
        {
            
            var parpos = parent.GetDataBlob<PositionDB>();
            
            
            var r = objPos.Length();
            var i = Math.Atan2(objPos.Z, r);
            var m0 = Math.Atan2(objPos.Y, objPos.X);
            var orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = r,
                Eccentricity = 0,
                Inclination = i,
                LongitudeOfAscendingNode = 0,
                ArgumentOfPeriapsis = 0,
                MeanAnomalyAtEpoch = m0,
                Epoch = atDatetime,

                _parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry,
                _myMass = objMass
                
            };
            orbit.IsStationary = false;
            orbit.CalculateExtendedParameters();

            return orbit;
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="semiMajorAxis_AU">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="longitudeOfPeriapsis">Longitude of periapsis in degrees.</param>
        /// <param name="meanLongitude">Longitude of object at epoch in degrees.</param>
        /// <param name="epoch">reference time for these orbital elements.</param>
        public static OrbitDB FromMajorPlanetFormat([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis_AU, double eccentricity, double inclination,
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
            double sma_m = Distance.AuToMt(semiMajorAxis_AU);
            return new OrbitDB(parent, parentMass, myMass, sma_m, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="semiMajorAxis_AU">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis in degrees.</param>
        /// <param name="meanAnomaly">Mean Anomaly in degrees.</param>
        /// <param name="epoch">reference time for these orbital elements.</param>
        public static OrbitDB FromAsteroidFormat([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis_AU, double eccentricity, double inclination,
                                                double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            double sma_m = Distance.AuToMt(semiMajorAxis_AU);
            
            OrbitDB orbit = new OrbitDB(parent)
            {
                SemiMajorAxis = sma_m,
                Eccentricity = eccentricity,
                Inclination_Degrees = inclination,
                LongitudeOfAscendingNode_Degrees = longitudeOfAscendingNode,
                ArgumentOfPeriapsis_Degrees = argumentOfPeriapsis,
                MeanAnomalyAtEpoch_Degrees = meanAnomaly,
                Epoch = epoch,

                _parentMass = parentMass,
                _myMass = myMass

            };
            orbit.CalculateExtendedParameters();
            return orbit;
        }

        internal OrbitDB(Entity parent, double parentMass, double myMass, double semiMajorAxis_m, double eccentricity, double inclinationDegrees,
                        double longitudeOfAscendingNodeDegrees, double argumentOfPeriapsisDegrees, double meanAnomaly, DateTime epoch)
            : base(parent)
        {
            SemiMajorAxis = semiMajorAxis_m;
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
            IsStationary = false;
        }
        
        public OrbitDB(OrbitUpdateOftenDB orbit) : base(orbit.Parent)
        {
            if (orbit.IsStationary)
            {
                IsStationary = true;
                return;
            }

            SemiMajorAxis = orbit.SemiMajorAxis;
            Eccentricity = orbit.Eccentricity;
            Inclination = orbit.Inclination;
            LongitudeOfAscendingNode = orbit.LongitudeOfAscendingNode;
            ArgumentOfPeriapsis = orbit.ArgumentOfPeriapsis;
            MeanAnomalyAtEpoch = orbit.MeanAnomalyAtEpoch;
            _parentMass = orbit._parentMass;
            _myMass = orbit._myMass;
            Epoch = orbit.Epoch;
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

        protected void CalculateExtendedParameters()
        {
            if (IsStationary)
            {
                return;
            }
            // Calculate extended parameters.
            // http://en.wikipedia.org/wiki/Standard_gravitational_parameter#Two_bodies_orbiting_each_other
            GravitationalParameter_Km3S2 = GeneralMath.GravitationalParameter_Km3s2(_parentMass + _myMass); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2
            GravitationalParameterAU = GeneralMath.GravitiationalParameter_Au3s2(_parentMass + _myMass);// (149597870700 * 149597870700 * 149597870700);
            GravitationalParameter_m3S2 = GeneralMath.StandardGravitationalParameter(_parentMass + _myMass);

            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(Distance.AuToKm(SemiMajorAxis_AU), 3) / (GravitationalParameter_Km3S2));
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                OrbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                OrbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            MeanMotion_DegreesSec = Math.Sqrt(GravitationalParameter_Km3S2 / Math.Pow(Distance.AuToKm(SemiMajorAxis_AU), 3)); // Calculated in radians.
            MeanMotion_DegreesSec = Angle.ToDegrees(MeanMotion_DegreesSec); // Stored in degrees.

            Apoapsis_AU = (1 + Eccentricity) * SemiMajorAxis_AU;
            Periapsis_AU = (1 - Eccentricity) * SemiMajorAxis_AU;

            SOI_m = OrbitMath.GetSOI(SemiMajorAxis, _myMass, _parentMass);

        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            CalculateExtendedParameters();
        }

        public KeplerElements GetElements()
        {
            KeplerElements ke = new KeplerElements();
            ke.SemiMajorAxis = SemiMajorAxis;                                            //a
            ke.SemiMinorAxis = SemiMajorAxis * Math.Sqrt(1 - Eccentricity * Eccentricity);//b
            ke.Eccentricity = Eccentricity;                                              //e
            ke.Periapsis = Periapsis;                                                    //q
            ke.Apoapsis = Apoapsis;                                                      //Q
            ke.LoAN = LongitudeOfAscendingNode;                                          //Ω (upper case Omega)
            ke.AoP = ArgumentOfPeriapsis;                                                //ω (lower case omega)
            ke.Inclination = Inclination;                                                //i
            ke.MeanMotion = MeanMotion;                                                  //n
            ke.MeanAnomalyAtEpoch = MeanAnomalyAtEpoch;                                  //M0
            ke.Epoch = Epoch;
            ke.LinearEccentricity = Eccentricity * SemiMajorAxis;                        //ae
            ke.Period = OrbitalPeriod.TotalSeconds;
            
            //ke.TrueAnomalyAtEpoch  ;   //ν or f or  θ
            return ke;
        }

        public override object Clone()
        {
            return new OrbitDB(this);
        }


        internal override void OnSetToEntity()
        {
            if (OwningEntity.HasDataBlob<NewtonMoveDB>())
            {
                OwningEntity.RemoveDataBlob<NewtonMoveDB>();
            }
            if (OwningEntity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                OwningEntity.RemoveDataBlob<OrbitUpdateOftenDB>();
            }
            if (OwningEntity.HasDataBlob<WarpMovingDB>())
            {
                OwningEntity.RemoveDataBlob<WarpMovingDB>();
            }
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


            SemiMajorAxis_AU = actualDB.SemiMajorAxis_AU;
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
            hash = Misc.ValueHash(SemiMajorAxis, hash);
            hash = Misc.ValueHash(Eccentricity, hash);


            return hash;
        }

        #endregion
    }

    /// <summary>
    /// This allows us to update an orbit fairly frequently, however it's a bit brittle, especialy where it interacts with treeheirarchy.
    /// </summary>
    public class OrbitUpdateOftenDB : OrbitDB
    {

        [JsonConstructor]
        private OrbitUpdateOftenDB()
        {
        }

        public OrbitUpdateOftenDB(OrbitDB orbit) : base(orbit.Parent)
        {
            SemiMajorAxis = orbit.SemiMajorAxis;
            Eccentricity = orbit.Eccentricity;
            Inclination = orbit.Inclination;
            LongitudeOfAscendingNode = orbit.LongitudeOfAscendingNode;
            ArgumentOfPeriapsis = orbit.ArgumentOfPeriapsis;
            MeanAnomalyAtEpoch = orbit.MeanAnomalyAtEpoch;
            _parentMass = orbit._parentMass;
            _myMass = orbit._myMass;
            Epoch = orbit.Epoch;
            CalculateExtendedParameters();
        }
        
        internal override void OnSetToEntity()
        {
            if (OwningEntity.HasDataBlob<OrbitDB>())
            {
                OwningEntity.RemoveDataBlob<OrbitDB>();
            }
            if (OwningEntity.HasDataBlob<NewtonMoveDB>())
            {
                OwningEntity.RemoveDataBlob<NewtonMoveDB>();
            }
            if (OwningEntity.HasDataBlob<WarpMovingDB>())
            {
                OwningEntity.RemoveDataBlob<WarpMovingDB>();
            }
        }

    }
}

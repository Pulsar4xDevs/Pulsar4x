using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Events;
using Pulsar4X.Sensors;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Orbits
{
    public class OrbitDB : TreeHierarchyDB
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(PositionDB) };

        /// <summary>
        /// Stored in Meters
        /// </summary>
        public double SemiMajorAxis { get; protected set; }


        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
        [PublicAPI]
        public double Eccentricity { get; protected set; }

		/// <summary>
		/// Angle between the orbit and the flat reference plane.
		/// in radians.
		/// </summary>
		[PublicAPI]
        public double Inclination { get; protected set; }

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the reference frame stored in radians.
        /// </summary>
        [PublicAPI]
        public double LongitudeOfAscendingNode { get; protected set; }


        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in radians.
        /// </summary>
        [PublicAPI]
        public  double ArgumentOfPeriapsis { get; protected set; }


        /// <summary>
        /// Definition of the position of the body in the orbit at the reference time
        /// epoch. Mathematically convenient angle does not correspond to a real angle.
        /// Stored in radians.
        /// </summary>
        [PublicAPI]
        public double MeanAnomalyAtEpoch { get; protected set; }

        /// <summary>
        /// True anomaly at <see cref="Epoch"/>. Warp leftover hyperbolas cannot
        /// reconstruct this from mean anomaly; drop-in copies it from the leftover Kepler.
        /// </summary>
        public double TrueAnomalyAtEpoch { get; protected set; } = double.NaN;

        /// <summary>
        /// reference time. Orbital parameters are stored relative to this reference.
        /// </summary>
        [PublicAPI]
        public DateTime Epoch { get; internal set; }

		/// <summary>
		/// 2-Body gravitational parameter of system in m^3/s^2
		/// </summary>
		[PublicAPI]
		public double GravitationalParameter_m3S2 { get; protected set; }

        /// <summary>
        /// This body's sphere, in metres. Same number as <see cref="EntityExtensions.GetSOI_m"/>.
        /// The star, and an orbit that is not on an entity yet, are infinite.
        /// </summary>
        public double SOI_m => OwningEntity != null ? OwningEntity.GetSOI_m() : double.PositiveInfinity;


        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        [PublicAPI]
        public TimeSpan OrbitalPeriod { get; private set; }

		/// <summary>
		/// Mean Motion of orbit. in Radians/Sec.
		/// </summary>
		[PublicAPI]
		public double MeanMotion { get; protected set; }

        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in m.
        /// </summary>
        [PublicAPI]
        public double Apoapsis { get; private set; }

        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in m.
        /// </summary>
        [PublicAPI]
        public double Periapsis { get; private set; }

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        [PublicAPI]
        public bool IsStationary { get; private set; } = false;

        [JsonProperty]
        internal double _parentMass;
        [JsonProperty]
        internal double _myMass;

        internal Vector2 _position;

        // Cached trigonometric values for performance
        private double _cachedCosLoAN;
        private double _cachedSinLoAN;
        private double _cachedCosIncl;
        private double _cachedSinIncl;
        private bool _trigCacheValid = false;

        /// <summary>
        /// The EnterSOI interrupt this orbit scheduled. Default means this orbit scheduled none,
        /// so an interrupt left by a replaced orbit is ignored.
        /// </summary>
        [JsonProperty]
        internal DateTime EntryDueAt;

        /// <summary>The rescan interrupt. Default means this orbit scheduled none.</summary>
        [JsonProperty]
        internal DateTime EntryRescanAt;

        /// <summary>
        /// Position samples taken by the last entry scan. A band reject leaves this at zero.
        /// </summary>
        public static int SoiEntryPositionChecks;

        internal bool EntryIsDue(DateTime at) => EntryDueAt != default && EntryDueAt == at;

        internal bool RescanIsDue(DateTime at) => EntryRescanAt != default && EntryRescanAt == at;

        #region Construction Interface


        /// <summary>
        /// Creates on Orbit at current location from a given velocity
        /// </summary>
        /// <returns>The Orbit Does not attach the OrbitDB to the entity!</returns>
        /// <param name="parent">Parent. must have massdb</param>
        /// <param name="entity">Entity. must have massdb</param>
        /// <param name="velocity_m">Velocity in meters.</param>
        public static OrbitDB FromVelocity(Entity parent, Entity entity, Vector3 velocity_m, DateTime atDateTime)
        {
            var myMass = entity.GetDataBlob<MassVolumeDB>().MassDry;

            //var epoch1 = parent.Manager.ManagerSubpulses.StarSysDateTime; //getting epoch from here is incorrect as the local datetime doesn't change till after the subpulse.

            //var parentPos = OrbitProcessor.GetAbsolutePosition_AU(parent.GetDataBlob<OrbitDB>(), atDateTime); //need to use the parent position at the epoch
            var posdb = entity.GetDataBlob<PositionDB>();
            posdb.SetParent(parent);
            var relativePos = posdb.RelativePosition;//entity.GetDataBlob<PositionDB>().AbsolutePosition_AU - parentPos;
            if (relativePos.Length() > parent.GetSOI_m())
                throw new Exception("Entity not in target SOI");

            //var sgp = UniversalConstants.Science.GravitationalConstant * (myMass + parentMass) / 3.347928976e33;
            var sgp_m = GeneralMath.StandardGravitationalParameter(myMass + parent.GetDataBlob<MassVolumeDB>().MassDry);
            var ke_m = OrbitMath.KeplerFromPositionAndVelocity(sgp_m, relativePos, velocity_m, atDateTime);


            OrbitDB orbit = FromKeplerElements(parent, myMass, ke_m, atDateTime);

            var pos = orbit.GetPosition(atDateTime);
            var d = (pos - relativePos).Length();
            if (d > 1)
            {
                Event e = Event.Create(EventType.Opps, atDateTime, $"Positional difference of {Stringify.Distance(d)} when creating orbit from velocity", entity.FactionOwnerID, entity.Manager.ManagerID, entity.Id);
                EventManager.Instance.Publish(e);

                //other info:
                var keta = Angle.ToDegrees(ke_m.TrueAnomalyAtEpoch);
                var obta = Angle.ToDegrees(orbit.GetTrueAnomaly(atDateTime));
                var tadif = Angle.ToDegrees(Angle.DifferenceBetweenRadians(keta, obta));
                var pos1 = orbit.GetPosition(atDateTime);
                var pos2 = orbit.GetPosition(ke_m.TrueAnomalyAtEpoch);
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
        public static OrbitDB FromVector(Entity parent, double myMass, double parentMass, Vector3 position_m, Vector3 velocity_m, DateTime atDateTime)
        {
            if (position_m.Length() > parent.GetSOI_m())
                throw new Exception("Entity not in target SOI");
            var sgp = GeneralMath.StandardGravitationalParameter(myMass + parentMass);
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, position_m, velocity_m, atDateTime);

            return FromKeplerElements(parent, myMass, ke, atDateTime);
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
                TrueAnomalyAtEpoch = ke.TrueAnomalyAtEpoch,
                Epoch = ke.Epoch != default ? ke.Epoch : atDateTime,

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
            var ralpos = objPos.AbsolutePosition - parpos.AbsolutePosition;
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
                TrueAnomalyAtEpoch = m0,
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
                TrueAnomalyAtEpoch = m0,
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
            return new OrbitDB(parent, parentMass, myMass, sma_m, eccentricity, Angle.ToRadians(inclination), Angle.ToRadians(longitudeOfAscendingNode), Angle.ToRadians(argumentOfPeriapsis), Angle.ToRadians(meanAnomaly), epoch);
        }

        /// <summary>
        /// Arguments in meters and degrees.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="parentMass"></param>
        /// <param name="myMass"></param>
        /// <param name="semiMajorAxis_m"></param>
        /// <param name="eccentricity"></param>
        /// <param name="inclination"></param>
        /// <param name="longitudeOfAscendingNode"></param>
        /// <param name="longitudeOfPeriapsis"></param>
        /// <param name="meanLongitude"></param>
        /// <param name="epoch"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static OrbitDB FromMajorPlanetFormat_r([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis_m, double eccentricity, double inclination,
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

            return new OrbitDB(parent, parentMass, myMass, semiMajorAxis_m, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch);
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
            double o_i = Angle.NormaliseRadiansPositive(Angle.ToRadians(inclination));
            double o_loAN = Angle.NormaliseRadiansPositive(Angle.ToRadians(longitudeOfAscendingNode));
            double o_aoP = Angle.NormaliseRadiansPositive(Angle.ToRadians(argumentOfPeriapsis));
            double o_M = Angle.NormaliseRadiansPositive(Angle.ToRadians(meanAnomaly));
			return new OrbitDB(parent, parentMass, myMass, sma_m, eccentricity, o_i, o_loAN, o_aoP, o_M, epoch);
        }

        /// <summary>
        /// in radians and meters.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="parentMass"></param>
        /// <param name="myMass"></param>
        /// <param name="semiMajorAxis_AU"></param>
        /// <param name="eccentricity"></param>
        /// <param name="inclination"></param>
        /// <param name="longitudeOfAscendingNode"></param>
        /// <param name="argumentOfPeriapsis"></param>
        /// <param name="meanAnomaly"></param>
        /// <param name="epoch"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static OrbitDB FromAsteroidFormat_r([NotNull] Entity parent, double parentMass, double myMass, double semiMajorAxis_m, double eccentricity, double inclination,
                                                 double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            double sma_m = semiMajorAxis_m;
            double o_i = inclination;
            double o_loAN = longitudeOfAscendingNode;
            double o_aoP = argumentOfPeriapsis;
            double o_M = meanAnomaly;
            return new OrbitDB(parent, parentMass, myMass, sma_m, eccentricity, o_i, o_loAN, o_aoP, o_M, epoch);
        }

        internal OrbitDB(Entity? parent, double parentMass, double myMass, double semiMajorAxis_m, double eccentricity, double inclination,
                        double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
            : base(parent)
        {
            SemiMajorAxis = semiMajorAxis_m;
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

        public OrbitDB(Entity? parent)
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
            TrueAnomalyAtEpoch = toCopy.TrueAnomalyAtEpoch;
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
            GravitationalParameter_m3S2 = GeneralMath.StandardGravitationalParameter(_parentMass + _myMass);

            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(SemiMajorAxis, 3) / (GravitationalParameter_m3S2));
            if (orbitalPeriod is double.NaN)
                orbitalPeriod = double.PositiveInfinity;
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                OrbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                OrbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            MeanMotion = OrbitMath.GetMeanMotion(GravitationalParameter_m3S2, SemiMajorAxis);

            Apoapsis = EllipseMath.Apoapsis(Eccentricity, SemiMajorAxis);
            Periapsis = EllipseMath.Periapsis(Eccentricity, SemiMajorAxis);

            // Cache trigonometric values for performance
            UpdateTrigCache();
        }

        /// <summary>
        /// Updates cached trigonometric values
        /// </summary>
        internal void UpdateTrigCache()
        {
            _cachedCosLoAN = Math.Cos(LongitudeOfAscendingNode);
            _cachedSinLoAN = Math.Sin(LongitudeOfAscendingNode);
            _cachedCosIncl = Math.Cos(Inclination);
            _cachedSinIncl = Math.Sin(Inclination);
            _trigCacheValid = true;
        }

        /// <summary>
        /// Gets cached trigonometric values for position calculations
        /// </summary>
        internal (double cosLoAN, double sinLoAN, double cosIncl, double sinIncl) GetCachedTrigValues()
        {
            if (!_trigCacheValid)
                UpdateTrigCache();
            return (_cachedCosLoAN, _cachedSinLoAN, _cachedCosIncl, _cachedSinIncl);
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
            ke.SemiMinorAxis = EllipseMath.SemiMinorAxis(SemiMajorAxis, Eccentricity);//b
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
			ke.StandardGravParameter = GravitationalParameter_m3S2;
            ke.TrueAnomalyAtEpoch = double.IsFinite(TrueAnomalyAtEpoch)
                ? TrueAnomalyAtEpoch
                : OrbitMath.TrueAnomalyFromTime(GravitationalParameter_m3S2, SemiMajorAxis ,Eccentricity, MeanAnomalyAtEpoch, 0);
			return ke;
        }

        public KeplerElements GetElements(double preCalculatedTrueAnomaly)
        {
            KeplerElements ke = new KeplerElements();
            ke.SemiMajorAxis = SemiMajorAxis;                                            //a
            ke.SemiMinorAxis = EllipseMath.SemiMinorAxis(SemiMajorAxis, Eccentricity);//b
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
			ke.StandardGravParameter = GravitationalParameter_m3S2;
            ke.TrueAnomalyAtEpoch = preCalculatedTrueAnomaly;                            //ν or f or  θ (use pre-calculated value)
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
            if (OwningEntity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                OwningEntity.RemoveDataBlob<NewtonSimpleMoveDB>();
            }
            if (OwningEntity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                OwningEntity.RemoveDataBlob<OrbitUpdateOftenDB>();
            }
            if (OwningEntity.HasDataBlob<WarpMovingDB>())
            {
                OwningEntity.RemoveDataBlob<WarpMovingDB>();
            }

            if (OwningEntity.GetDataBlob<PositionDB>().Parent != Parent)
                OwningEntity.GetDataBlob<PositionDB>().SetParent(Parent);

            // A hyperbola always leaves. A bound ellipse leaves only when its apoapsis
            // crosses the parent's sphere. The star's sphere is infinite, so neither schedules.
            if (TryParentSphere(out double soiRadius)
                && (Eccentricity >= 1 || (Eccentricity < 1 && Apoapsis > soiRadius)))
            {
                var soiChangeAt = OrbitMath.TimeToRadius(this, soiRadius);
                var now = OwningEntity.Manager.ManagerSubpulses.StarSysDateTime;
                if (soiChangeAt >= now)
                    OwningEntity.Manager.ManagerSubpulses.AddEntityInterupt(soiChangeAt, nameof(ChangeSOIProcessor), OwningEntity);
            }

            // SOI entry prediction for ships/projectiles (not system bodies)
            if (!OwningEntity.HasDataBlob<SystemBodyInfoDB>())
            {
                ScheduleSOIEntry();
            }
        }

        /// <summary>
        /// The parent's sphere. False at the star, where the sphere is infinite.
        /// </summary>
        bool TryParentSphere(out double soiRadius)
        {
            soiRadius = 0;
            var soiParent = OwningEntity?.GetSOIParentEntity();
            if (soiParent == null)
                return false;
            soiRadius = soiParent.GetSOI_m();
            return double.IsFinite(soiRadius) && soiRadius > 0;
        }

        /// <summary>
        /// Scans the orbit for encounters with sibling bodies and schedules an
        /// <see cref="EnterSOIProcessor"/> interrupt at the crossing. An empty scan
        /// schedules <see cref="SoiEntryRescanProcessor"/> at the horizon instead of
        /// deciding the ship never meets anyone.
        /// </summary>
        private void ScheduleSOIEntry() => ScheduleSOIEntryFrom(Epoch);

        internal void ScheduleSOIEntryFrom(DateTime from)
        {
            SoiEntryPositionChecks = 0;
            EntryDueAt = default;
            EntryRescanAt = default;
            if (Parent == null || !Parent.HasDataBlob<OrbitDB>() || OwningEntity?.Manager == null)
                return;
            if (!TryRadialBand(this, out double shipInner, out double shipOuter))
                return;

            var siblings = Parent.GetDataBlob<OrbitDB>().Children;
            if (siblings.Count == 0)
                return;

            double shipSpeed = MaxOrbitalSpeed(this);
            Entity? hitChild = null;
            DateTime hitOutside = default;
            DateTime hitInside = DateTime.MaxValue;
            DateTime? rescanAt = null;

            foreach (var child in siblings)
            {
                if (child == OwningEntity)
                    continue;
                if (!child.HasDataBlob<OrbitDB>() || !child.HasDataBlob<MassVolumeDB>())
                    continue;
                // A sphere at or inside the body (Phobos) is not a frame change.
                if (!child.HasExteriorSoi())
                    continue;
                double childSoi = child.GetSOI_m();

                var childOrbit = child.GetDataBlob<OrbitDB>();
                if (!TryRadialBand(childOrbit, out double childInner, out double childOuter))
                    continue;
                // Distance from the parent is a necessary condition. Two comparisons,
                // and no position sample, drops a sibling the ship can never reach.
                if (shipInner > childOuter + childSoi || childInner - childSoi > shipOuter)
                    continue;

                DateTime horizon = ChildHorizon(childOrbit, from, out bool frameExit);
                if (hitChild != null && horizon > hitInside)
                    horizon = hitInside;
                if (horizon <= from)
                    continue;

                bool truncated = false;
                bool found = TryAdvanceToCrossing(
                    childOrbit, childSoi, from, horizon, shipSpeed, MaxOrbitalSpeed(childOrbit),
                    out DateTime outsideAt, out DateTime insideAt, ref truncated);
                if (found && insideAt < hitInside)
                {
                    hitInside = insideAt;
                    hitOutside = outsideAt;
                    hitChild = child;
                    rescanAt = null;
                }
                else if (!found && (truncated || !frameExit))
                {
                    DateTime at = truncated && outsideAt > from ? outsideAt : horizon;
                    if (rescanAt == null || at < rescanAt)
                        rescanAt = at;
                }
            }

            var manager = OwningEntity.Manager;
            DateTime now = manager.ManagerSubpulses.StarSysDateTime;
            if (hitChild != null)
            {
                DateTime cross = BisectEntry(hitChild, hitOutside, hitInside);
                if (cross > now)
                {
                    EntryDueAt = cross;
                    manager.ManagerSubpulses.AddEntityInterupt(cross, nameof(EnterSOIProcessor), OwningEntity);
                }
                return;
            }

            if (rescanAt is DateTime again && again > now && again > from)
            {
                EntryRescanAt = again;
                manager.ManagerSubpulses.AddEntityInterupt(again, nameof(SoiEntryRescanProcessor), OwningEntity);
            }
        }

        /// <summary>
        /// Bound orbits sweep periapsis to apoapsis. A hyperbola sweeps from periapsis
        /// out to the radius where this frame ends.
        /// </summary>
        static bool TryRadialBand(OrbitDB orbit, out double inner, out double outer)
        {
            inner = 0;
            outer = 0;
            double peri = orbit.Periapsis;
            if (!(peri >= 0) || !double.IsFinite(peri))
                return false;
            if (orbit.Eccentricity < 1)
            {
                double apo = orbit.Apoapsis;
                if (!double.IsFinite(apo) || apo < peri)
                    return false;
                inner = peri;
                outer = apo;
                return true;
            }

            inner = peri;
            outer = LeaveRadius(orbit);
            return outer > inner;
        }

        static double LeaveRadius(OrbitDB orbit)
        {
            var body = orbit.OwningEntity;
            if (body == null)
                return double.PositiveInfinity;
            var parent = body.GetSOIParentEntity();
            if (parent == null)
                return double.PositiveInfinity;
            double soi = parent.GetSOI_m();
            if (!double.IsFinite(soi) || !(soi > 0))
                return double.PositiveInfinity;
            return soi;
        }

        /// <summary>Speed at periapsis. Nothing in the orbit is faster than this.</summary>
        static double MaxOrbitalSpeed(OrbitDB orbit)
        {
            double mu = orbit.GravitationalParameter_m3S2;
            double rp = orbit.Periapsis;
            double a = orbit.SemiMajorAxis;
            if (!(mu > 0) || !(rp > 1) || !double.IsFinite(a) || a == 0)
                return 0;
            double v2 = mu * (2.0 / rp - 1.0 / a);
            if (!(v2 > 0) || !double.IsFinite(v2))
                return 0;
            return Math.Sqrt(v2);
        }

        /// <summary>
        /// The earlier of the ship's frame change, one synodic period, and ten years.
        /// <paramref name="frameExit"/> is set when that frame change is the one that won.
        /// </summary>
        DateTime ChildHorizon(OrbitDB childOrbit, DateTime from, out bool frameExit)
        {
            frameExit = false;
            DateTime horizon = from.AddYears(10);
            double leave = LeaveRadius(this);
            if (double.IsFinite(leave) && (Eccentricity >= 1 || (double.IsFinite(Apoapsis) && Apoapsis > leave)))
            {
                try
                {
                    DateTime exit = OrbitMath.TimeToRadius(this, leave);
                    if (exit > from && exit < horizon)
                    {
                        horizon = exit;
                        frameExit = true;
                    }
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                }
            }

            double synodic = SynodicSeconds(OrbitalPeriod.TotalSeconds, childOrbit.OrbitalPeriod.TotalSeconds);
            if (double.IsFinite(synodic) && synodic > 60)
            {
                DateTime synEnd;
                try
                {
                    synEnd = from.AddSeconds(synodic);
                }
                catch (ArgumentOutOfRangeException)
                {
                    synEnd = DateTime.MaxValue;
                }
                if (synEnd < horizon)
                {
                    horizon = synEnd;
                    frameExit = false;
                }
            }

            return horizon;
        }

        static double SynodicSeconds(double period1, double period2)
        {
            if (!(period1 > 0) || !(period2 > 0) || !double.IsFinite(period1) || !double.IsFinite(period2))
                return double.PositiveInfinity;
            if (period1 >= TimeSpan.MaxValue.TotalSeconds * 0.5 || period2 >= TimeSpan.MaxValue.TotalSeconds * 0.5)
                return double.PositiveInfinity;
            double difference = Math.Abs(1.0 / period1 - 1.0 / period2);
            if (difference < 1e-30)
                return double.PositiveInfinity;
            return 1.0 / difference;
        }

        /// <summary>
        /// Step by gap / (vShip + vChild). That step cannot jump a crossing.
        /// The 60 s floor ignores a graze shorter than 60 s of relative travel:
        /// at 15 km/s that is 900 km, against a sphere of hundreds of thousands of km.
        /// </summary>
        bool TryAdvanceToCrossing(
            OrbitDB childOrbit, double childSoi, DateTime from, DateTime horizon,
            double shipSpeed, double childSpeed,
            out DateTime outsideAt, out DateTime insideAt, ref bool truncated)
        {
            outsideAt = from;
            insideAt = from;
            const int maxSteps = 20000;
            const double floorSeconds = 60;
            double vSum = shipSpeed + childSpeed;
            DateTime t = from;
            double gap = Gap(childOrbit, childSoi, t);
            bool outside = gap > 0;
            if (outside)
                outsideAt = t;

            for (int step = 0; step < maxSteps && t < horizon; step++)
            {
                double dt;
                if (!(vSum > 1))
                {
                    if (gap > 0)
                        return false;
                    dt = floorSeconds;
                }
                else
                {
                    dt = Math.Abs(gap) / vSum;
                    if (dt < floorSeconds)
                        dt = floorSeconds;
                }

                double remain = (horizon - t).TotalSeconds;
                if (dt > remain)
                    dt = remain;
                if (!(dt >= 1) || !double.IsFinite(dt))
                    return false;

                DateTime next;
                try
                {
                    next = t.AddSeconds(dt);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return false;
                }

                double nextGap = Gap(childOrbit, childSoi, next);
                if (!outside)
                {
                    if (nextGap > 0)
                    {
                        outside = true;
                        outsideAt = next;
                    }
                }
                else if (nextGap <= 0)
                {
                    insideAt = next;
                    return true;
                }
                else
                {
                    outsideAt = next;
                }

                t = next;
                gap = nextGap;
            }

            if (t < horizon)
                truncated = true;
            outsideAt = t;
            return false;
        }

        double Gap(OrbitDB childOrbit, double childSoi, DateTime at)
        {
            SoiEntryPositionChecks += 2;
            return (PositionAt(this, at) - PositionAt(childOrbit, at)).Length() - childSoi;
        }

        static Vector3 PositionAt(OrbitDB orbit, DateTime at)
        {
            try
            {
                return orbit.GetPosition(at);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new Vector3(double.NaN, double.NaN, double.NaN);
            }
        }

        DateTime BisectEntry(Entity child, DateTime outsideAt, DateTime insideAt)
        {
            var childOrbit = child.GetDataBlob<OrbitDB>();
            double childSoi = child.GetSOI_m();
            DateTime lo = outsideAt;
            DateTime hi = insideAt;
            for (int iter = 0; iter < 20; iter++)
            {
                var mid = new DateTime(lo.Ticks / 2 + hi.Ticks / 2);
                SoiEntryPositionChecks += 2;
                double dist = (PositionAt(this, mid) - PositionAt(childOrbit, mid)).Length();
                if (dist < childSoi)
                    hi = mid;
                else
                    lo = mid;
            }

            return hi;
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
            ArgumentOfPeriapsis= actualDB.ArgumentOfPeriapsis;
            MeanAnomalyAtEpoch= actualDB.MeanAnomalyAtEpoch;
            _parentMass = actualDB._parentMass;
            _myMass = actualDB._myMass;
            CalculateExtendedParameters();
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
            if (OwningEntity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                OwningEntity.RemoveDataBlob<NewtonSimpleMoveDB>();
            }
            if (OwningEntity.HasDataBlob<WarpMovingDB>())
            {
                OwningEntity.RemoveDataBlob<WarpMovingDB>();
            }
        }

    }
}

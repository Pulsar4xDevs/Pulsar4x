using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{   
    /// <summary>
    /// Used for holding a percentage stores as a byte so 255 bits precision.
    /// Takes and Returns 0.0 to 1.0 for easy multiplcation math.
    /// </summary>
    public struct PercentValue
    {
        private byte _percent;

        /// <summary>
        /// 0.0f to 1.0f with 255 bits of precision
        /// </summary>
        public float Percent
        {
            /// <summary>
            /// returns a percent value between 0.0f and 1.0f 
            /// </summary>
            /// <returns>The percent.</returns>
            get { return _percent / 255f; }
            /// <summary>
            /// Sets the percent
            /// </summary>
            /// <param name="value">Value. between 0.0f and 1.0f</param>
            set { _percent = (byte)(value * 255); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.PercentValue"/> struct.
        /// </summary>
        /// <param name="percent">Percent. a value between 0 and 1</param>
        public PercentValue(float percent)
        {
            _percent = (byte)(percent * 255);
        }
        

        public static PercentValue SetRawValue(byte rawValue)
        {
            return new PercentValue(){_percent = rawValue};
        }

        public static byte GetRawValue(PercentValue percentValue)
        {
            return percentValue._percent;
        }

        public static implicit operator float(PercentValue percentValue)
        {
            return percentValue.Percent;
        }

        public static implicit operator PercentValue(float percentValue)
        {
            return new PercentValue(percentValue);
        }
    }

    public class WeightedValue<T>
    {
        public double Weight { get; set; }
        public T Value { get; set; }

        protected bool Equals(WeightedValue<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((WeightedValue<T>)obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }
    }

    /// <summary>
    /// Weighted list used for selecting values with a random number generator.
    /// </summary>
    /// <remarks>
    /// This is a weighted list. Input values do not need to add up to 1.
    /// </remarks>
    /// <example>
    /// <code>
    /// WeightedList<string> fruitList = new WeightList<string>();
    /// fruitList.Add(0.2, "Apple");
    /// fruitList.Add(0.5, "Banana");
    /// fruitList.Add(0.3, "Tomatoe");
    /// 
    /// fruitSelection = fruitList.Select(0.1)
    /// print(fruitSelection); // "Apple"
    /// 
    /// fruitSelection = fruitList.Select(0.69)
    /// print(fruitSelection); // "Banana"
    /// 
    /// string fruitSelection = fruitList.Select(0.7)
    /// print(fruitSelection); // "Tomatoe"
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// WeightedList<string> fruitList = new WeightList<string>();
    /// fruitList.Add(4, "Apple");
    /// fruitList.Add(6, "Banana");
    /// fruitList.Add(10, "Tomatoe");
    /// 
    /// fruitSelection = fruitList.Select(0.19)
    /// print(fruitSelection); // "Apple"
    /// 
    /// fruitSelection = fruitList.Select(0.2)
    /// print(fruitSelection); // "Banana"
    /// 
    /// string fruitSelection = fruitList.Select(0.5)
    /// print(fruitSelection); // "Tomatoe"
    /// </code>
    /// </example>
    //[JsonObjectAttribute]
    public class WeightedList<T> : IEnumerable<WeightedValue<T>>, ISerializable
    {
        private List<WeightedValue<T>> _valueList;

        /// <summary>
        /// Total weights of the list.
        /// </summary>
        public double TotalWeight { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WeightedList()
        {
            _valueList = new List<WeightedValue<T>>();
        }

        /// <summary>
        /// Deep copy consturctor
        /// </summary>
        public WeightedList(WeightedList<T> weightedList)
        {
            _valueList = new List<WeightedValue<T>>(weightedList._valueList);
            TotalWeight = weightedList.TotalWeight;
        }

        /// <summary>
        /// Adds a value to the weighted list.
        /// </summary>
        /// <param name="weight">Weight of this value in the list.</param>
        public void Add(double weight, T value)
        {
            var listEntry = new WeightedValue<T> { Weight = weight, Value = value };
            _valueList.Add(listEntry);
            TotalWeight += weight;
        }

        public void Add(WeightedValue<T> value)
        {
            Add(value.Weight, value.Value);
        }
        /// <summary>
        /// Adds the contents of another weighted list to this one.
        /// </summary>
        /// <param name="otherList">The list to add.</param>
        public void AddRange(WeightedList<T> otherList)
        {
            _valueList.AddRange(otherList._valueList);
            TotalWeight += otherList.TotalWeight;
        }

        /// <summary>
        /// Removes the specified value from the list.
        /// </summary>
        public void Remove(T value)
        {
            int removeAtIndex = -1;
            for (int i = 0; i < _valueList.Count; i++)
            {
                if (_valueList[i].Value.Equals(value))
                {
                    removeAtIndex = i;
                    break;
                }
            }

            RemoveAt(removeAtIndex);
        }

        /// <summary>
        /// Remove the value at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            double weight = _valueList[index].Weight;
            _valueList.RemoveAt(index);

            TotalWeight -= weight;
        }

        public bool ContainsValue(T Value)
        {
            return _valueList.Contains(new WeightedValue<T> { Value = Value });
        }

        public int IndexOf(T Value)
        {
            return _valueList.IndexOf(new WeightedValue<T> { Value = Value });
        }

        public IEnumerator<WeightedValue<T>> GetEnumerator()
        {
            return _valueList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Selects a value from the list based on the input.
        /// </summary>
        /// <param name="rngValue">Value 0.0 to 1.0 represending the random value selected by the RNG.</param>
        /// <returns></returns>
        public T Select(double rngValue)
        {
            double cumulativeChance = 0;
            foreach (WeightedValue<T> listEntry in _valueList)
            {
                double realChance = listEntry.Weight / TotalWeight;
                cumulativeChance += realChance;

                if (rngValue < cumulativeChance)
                {
                    return listEntry.Value;
                }
            }
            throw new InvalidOperationException("Failed to choose a random value.");
        }

        /// <summary>
        /// Selects the value at the specified index.
        /// </summary>
        public T SelectAt(int index)
        {
            return _valueList[index].Value;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Values", _valueList);
        }

        public WeightedList(SerializationInfo info, StreamingContext context)
        {
            _valueList = (List<WeightedValue<T>>)info.GetValue("Values", typeof(List<WeightedValue<T>>));

            // rebuild total weight:
            TotalWeight = 0;
            foreach (var w in _valueList)
            {
                TotalWeight += w.Weight;
            }
        }

        public WeightedValue<T> this[int index]
        {
            get { return _valueList[index]; }
            set { RemoveAt(index); Add(value); }
        }
    }

    

    public static class InterceptCalcs
    {
 

        /// <summary>
        /// assumes circular orbit, attempts to calculate transfer window. 
        /// </summary>
        /// <param name="deltaV"></param>
        /// <param name="currentParent"></param>
        /// <param name="targetParent"></param>
        /// <param name="manuverEntity"></param>
        /// <returns>3 manuvers, 0: soi escape, 1:1st hohmman manuver 2: 2nd hohmman manuver</returns>
        public static (Vector3 deltaV, double timeInSeconds)[] InterPlanetaryHohmann(Entity currentParent, Entity targetParent, Entity manuverEntity)
        {
            var meState = manuverEntity.GetRelativeState();
            var meOdb = manuverEntity.GetDataBlob<OrbitDB>();
            var meMass = manuverEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var meSMA = meOdb.SemiMajorAxis;
            var meOprd = meOdb.OrbitalPeriod;
            var meMeanMotion = meOdb.MeanMotion;
            var meAngle = Math.Atan2(meState.pos.Y, meState.pos.X);


            var cpOdb = currentParent.GetDataBlob<OrbitDB>();
            var cpSOI = currentParent.GetSOI_m() + 100; //might as well go another 100m past soi so less likely problems.
            var cpmass = currentParent.GetDataBlob<MassVolumeDB>().MassTotal;            
            var cpsgp = OrbitalMath.CalculateStandardGravityParameterInM3S2(meMass, cpmass);
            var cpSMA = cpOdb.SemiMajorAxis;
            var cpOprd = cpOdb.OrbitalPeriod;
            var cppos = currentParent.GetDataBlob<PositionDB>().RelativePosition_m;
            var cpAngle = Math.Atan2(cppos.Y, cppos.X);

            var tpOdb = targetParent.GetDataBlob<OrbitDB>();
            var tpSOI = targetParent.GetSOI_m();
            var tpMass = targetParent.GetDataBlob<MassVolumeDB>().MassTotal;
            var tpsgp = OrbitMath.CalculateStandardGravityParameterInM3S2(meMass, tpMass);
            var tpSMA = tpOdb.SemiMajorAxis;
            var tpOprd = tpOdb.OrbitalPeriod;
            var tppos = targetParent.GetDataBlob<PositionDB>().RelativePosition_m;
            var tpAngle = Math.Atan2(tppos.Y, tppos.X);

            //grandparent (sol in earth to mars)
            var grandParent = currentParent.GetSOIParentEntity();
            var gpMass = grandParent.GetDataBlob<MassVolumeDB>().MassTotal;
            var gpSGP = OrbitMath.CalculateStandardGravityParameterInM3S2(meMass, gpMass);
            
            var gpHomman = OrbitalMath.Hohmann2(gpSGP, cpSMA, tpSMA);
            var gpHommanAngle = Math.PI*( (1-1/2*Math.Sqrt(2))*Math.Sqrt( Math.Pow((cpSMA / tpSMA +1),3)));


            
            var rads = cpAngle - tpAngle;
            var closinRads = Math.Max(cpOdb.MeanMotion, tpOdb.MeanMotion) - Math.Min(cpOdb.MeanMotion, tpOdb.MeanMotion);
            var ttXferWnidow = rads / closinRads;



            var wca1 = Math.Sqrt(cpsgp / meSMA);
            var wca2 = Math.Sqrt((2 * cpSOI) / (meSMA + cpSOI)) - 1;
            var dva = wca1 * wca2;
            var ttsoi = Math.PI * Math.Sqrt((Math.Pow(meSMA + cpSOI, 3)) / (8 * cpsgp));

            var soiBurnstart = ttXferWnidow - ttsoi;
            var periods = soiBurnstart / meOprd.TotalSeconds;
            var meFutureAngle = meAngle * periods;
            var cpFutureAngle = soiBurnstart / cpOprd.TotalSeconds;
            var dif = cpFutureAngle - meFutureAngle;
            soiBurnstart += dif * meMeanMotion;
            
            if (cpSMA > tpSMA) //larger orbit to smaller. 
            {
                soiBurnstart += meOprd.TotalSeconds * 0.5; //add half an orbit
            }

            var manuvers = new (Vector3 burn, double time)[3];
            manuvers[0] = (new Vector3(0,dva, 0), soiBurnstart);
            manuvers[1] = (gpHomman[0].deltaV, ttsoi);
            manuvers[2] = (gpHomman[1].deltaV, gpHomman[1].timeInSeconds);

            return manuvers;

        }

        /// <summary>
        /// This intercept only works if time to intercept is less than the orbital period. 
        /// </summary>
        /// <returns>The ntercept.</returns>
        /// <param name="mover">Mover.</param>
        /// <param name="targetOrbit">Target orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static (Vector3, TimeSpan) FTLIntercept(Entity mover, OrbitDB targetOrbit, DateTime atDateTime)
        {

            //OrbitDB targetOrbit = target.GetDataBlob<OrbitDB>();
            //PositionDB targetPosition = target.GetDataBlob<PositionDB>();
            //PositionDB moverPosition = mover.GetDataBlob<PositionDB>();

            OrbitDB moverOrbit = mover.GetDataBlob<OrbitDB>();
            Vector3 moverPosInKM = Distance.AuToKm(moverOrbit.GetAbsolutePosition_AU(atDateTime));

            //PropulsionAbilityDB moverPropulsion = mover.GetDataBlob<PropulsionAbilityDB>();

            Vector3 targetPosInKM = Distance.AuToKm((targetOrbit.GetAbsolutePosition_AU(atDateTime)));

            int speed = 25000;//moverPropulsion.MaximumSpeed * 100; //299792458;

            (Vector3, TimeSpan) intercept = (new Vector3(), TimeSpan.Zero);



            TimeSpan eti = new TimeSpan();
            TimeSpan eti_prev = new TimeSpan();
            DateTime edi = atDateTime;
            DateTime edi_prev = atDateTime;

            Vector3 predictedPosKM = Distance.AuToKm(targetOrbit.GetAbsolutePosition_AU(edi_prev));
            double distance = (predictedPosKM - moverPosInKM).Length();
            eti = TimeSpan.FromSeconds((distance * 1000) / speed);

            int steps = 0;
            if (eti < targetOrbit.OrbitalPeriod)
            {

                double timeDifference = double.MaxValue;
                double distanceDifference = timeDifference * speed;
                while (distanceDifference >= 1000)
                {

                    eti_prev = eti;
                    edi_prev = edi;

                    predictedPosKM = Distance.AuToKm(targetOrbit.GetAbsolutePosition_AU(edi_prev));

                    distance = (predictedPosKM - moverPosInKM).Length();
                    eti = TimeSpan.FromSeconds((distance * 1000) / speed);
                    edi = atDateTime + eti;

                    timeDifference = Math.Abs(eti.TotalSeconds - eti_prev.TotalSeconds);
                    distanceDifference = timeDifference * speed;
                    steps++;
                }
            }

            return intercept;
        }

        /// <summary>
        /// used to get manuvers to rendevus with an object in the same orbit, or advance our position in a given orbit.
        /// </summary>
        /// <param name="orbit"></param>
        /// <param name="manuverTime">datetime the manuver should start (idealy at periapsis)</param>
        /// <param name="phaseAngle">angle in radians between our position and the rendevous position</param>
        /// <returns>an array of vector3(normal,prograde,radial) and seconds from first manuver. first seconds in array will be 0 </returns>
        public static (Vector3 deltaV, double timeInSeconds)[] OrbitPhasingManuvers(KeplerElements orbit, double sgp, DateTime manuverTime, double phaseAngle)
        {
            //https://en.wikipedia.org/wiki/Orbit_phasing
            double orbitalPeriod = orbit.OrbitalPeriod;
            double e = orbit.Eccentricity;

            var wc1 = Math.Sqrt((1 - e) / (1 + e));
            var wc2 = Math.Tan(phaseAngle / 2);
            
            double E = 2 * Math.Atan(wc1 * wc2);

            double wc3 = orbitalPeriod / (Math.PI * 2);
            double wc4 = E - e * Math.Sin(E);

            double phaseTime = wc3 * wc4;

            double phaseOrbitPeriod = orbitalPeriod - phaseTime;

            

            //double phaseOrbitSMA0 = Math.Pow(Math.Sqrt(sgp) * phaseOrbitPeriod / (Math.PI * 2), (2.0 / 3.0)); //I think this one will be slightly slower
            
            //using the full Major axis here rather than semiMaj.
            double phaseOrbitMA = 2 * Math.Cbrt((sgp * phaseOrbitPeriod * phaseOrbitPeriod) / (4 * Math.PI * Math.PI));
            
            
            //one of these will be the periapsis, the other the appoapsis, depending on whether we're behind or ahead of the target.
            double phaseOrbitApsis1 = OrbitProcessor.GetPosition(orbit, manuverTime).Length();// 
            double phaseOrbitApsis2 = phaseOrbitMA - phaseOrbitApsis1;


            double wc7 = Math.Sqrt( (phaseOrbitApsis1 * phaseOrbitApsis2) / (phaseOrbitMA));
            double wc8 = Math.Sqrt(2 * sgp);
            double phaseOrbitAngularMomentum = wc8 * wc7;


            double wc9 = Math.Sqrt( (orbit.Apoapsis * orbit.Periapsis) / (orbit.Apoapsis + orbit.Periapsis));
            double wc10 = Math.Sqrt(2 * sgp);
            double orbitAngularMomentum = wc9 * wc10;

            double r = OrbitProcessor.GetPosition(orbit, manuverTime).Length();

            double dv = phaseOrbitAngularMomentum / r - orbitAngularMomentum / r;

            (Vector3, double)[] manuvers = new (Vector3, double)[2];
            manuvers[0] = (new Vector3(0, dv, 0), 0);
            manuvers[1] = (new Vector3(0, -dv, 0), phaseOrbitPeriod);
            
            return manuvers;
        }
        
        public static (Vector3 deltaV, double timeInSeconds)[] OrbitPhasingManuvers(OrbitDB orbit, DateTime manuverTime, double phaseAngle)
        {
            //https://en.wikipedia.org/wiki/Orbit_phasing
            double orbitalPeriod = orbit.OrbitalPeriod.TotalSeconds;
            double e = orbit.Eccentricity;

            var wc1 = Math.Sqrt((1 - e) / (1 + e));
            var wc2 = Math.Tan(phaseAngle / 2);
            
            double E = 2 * Math.Atan(wc1 * wc2);

            double wc3 = orbitalPeriod / (Math.PI * 2);
            double wc4 = E - e * Math.Sin(E);

            double phaseTime = wc3 * wc4;

            double phaseOrbitPeriod = orbitalPeriod - phaseTime;

            double sgp = orbit.GravitationalParameter_m3S2;

            //double phaseOrbitSMA0 = Math.Pow(Math.Sqrt(sgp) * phaseOrbitPeriod / (Math.PI * 2), (2.0 / 3.0)); //I think this one will be slightly slower
            
            //using the full Major axis here rather than semiMaj.
            double phaseOrbitMA = 2 * Math.Cbrt((sgp * phaseOrbitPeriod * phaseOrbitPeriod) / (4 * Math.PI * Math.PI));
            
            
            //one of these will be the periapsis, the other the appoapsis, depending on whether we're behind or ahead of the target.
            double phaseOrbitApsis1 = orbit.GetPosition_m(manuverTime).Length();// 
            double phaseOrbitApsis2 = phaseOrbitMA - phaseOrbitApsis1;


            double wc7 = Math.Sqrt( (phaseOrbitApsis1 * phaseOrbitApsis2) / (phaseOrbitMA));
            double wc8 = Math.Sqrt(2 * sgp);
            double phaseOrbitAngularMomentum = wc8 * wc7;


            double wc9 = Math.Sqrt( (orbit.Apoapsis * orbit.Periapsis) / (orbit.Apoapsis + orbit.Periapsis));
            double wc10 = Math.Sqrt(2 * sgp);
            double orbitAngularMomentum = wc9 * wc10;

            double r = orbit.GetPosition_m(manuverTime).Length();

            double dv = phaseOrbitAngularMomentum / r - orbitAngularMomentum / r;

            (Vector3, double)[] manuvers = new (Vector3, double)[2];
            manuvers[0] = (new Vector3(0, dv, 0), 0);
            manuvers[1] = (new Vector3(0, -dv, 0), phaseOrbitPeriod);
            
            return manuvers;
        }
    }

    /// <summary>
    /// An experimental distance value struct. 
    /// idea here was to simply define what a distance value was and handle very small or very large numbers equaly well.   
    /// </summary>
    public struct DistanceValue
    {
        public enum ValueTypeEnum : sbyte//number of zeros. 
        {
            NanoMeters  = -9,
            MicroMeters = -6,
            MilliMeters = -3,
            CentiMeters = -2,
            DeciMeters  = -1,
            Meters      = 0,
            DecaMeters  = 1,
            HectoMeters = 2,
            KeloMeters  = 3,
            MegaMeters  = 6,
            GigaMeters  = 9,
        }
        public ValueTypeEnum ValueType;
        public double Value;

        public static double Convert(DistanceValue value, ValueTypeEnum convertTo)
        {
            int fval = (int)value.ValueType;    //from
            int tval = (int)convertTo;          //to

            return value.Value  * Math.Pow(10, tval - fval);
        }

    }

}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Small Helper Class for Angle unit Conversions
    /// </summary>
    public static class Angle
    {
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// returns a number between -2 * pi and 2 * pi
        /// </summary>
        /// <returns>The radians.</returns>
        /// <param name="radians">Radians.</param>
        public static double NormaliseRadians(double radians)
        {
            radians = radians % (2 * Math.PI);
            return radians;
        }

        /// <summary>
        /// returns a number between 0 and 2 * pi
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static double NormaliseRadiansPositive(double radians)
        {
            radians = NormaliseRadians(radians);
            if (radians < 0)
                radians += (2 *Math.PI);
            return radians;
        }

        /// <summary>
        /// returns a number between -360 and 360
        /// </summary>
        /// <returns>The degrees.</returns>
        /// <param name="degrees">Degrees.</param>
        public static double NormaliseDegrees(double degrees)
        {
            degrees = degrees % 360;
            return degrees;
        }

        public static double DifferenceBetweenRadians(double a1, double a2)
        {
            return Math.PI - Math.Abs(Math.Abs(a1 - a2) - Math.PI);
        }

        public static double DifferenceBetweenDegrees(double a1, double a2)
        {
            return 180 - Math.Abs(Math.Abs(a1 - a2) - 180);
        }

    }

    /// <summary>
    /// Small helper class for Temperature unit conversions
    /// </summary>
    public static class Temperature
    {
        public static double ToKelvin(double celsius)
        {
            return celsius + GameConstants.Units.DegreesCToKelvin;
        }

        public static float ToKelvin(float celsius)
        {
            return (float)(celsius + GameConstants.Units.DegreesCToKelvin);
        }

        public static double ToCelsius(double kelvin)
        {
            return kelvin + GameConstants.Units.KelvinToDegreesC;
        }

        public static float ToCelsius(float kelvin)
        {
            return (float)(kelvin + GameConstants.Units.KelvinToDegreesC);
        }
    }

    /// <summary>
    /// Small helper class for Distance unit conversions
    /// </summary>
    public static class Distance
    {
        public static Vector3 MToAU(Vector3 meters)
        {
            return meters / GameConstants.Units.MetersPerAu;
        }
        public static double MToAU(double meters)
        {
            return meters / GameConstants.Units.MetersPerAu;
        }
        
        public static double MToKm(double meters)
        {
            return meters / 1000.0;
        }
        public static double KmToM(double kilometers)
        {
            return kilometers * 1000.0;
        }
        
        public static double KmToAU(double km)
        {
            return km / GameConstants.Units.KmPerAu;
        }
        public static Vector3 KmToAU(Vector3 km)
        {
            return km / GameConstants.Units.KmPerAu;
        }
        public static Vector2 KmToAU(Vector2 km)
        {
            return km / GameConstants.Units.KmPerAu;
        }
        public static double AuToKm(double au)
        {
            return au * GameConstants.Units.KmPerAu;
        }
        public static Vector3 AuToKm(Vector3 Au)
        {
            return new Vector3(AuToKm(Au.X), AuToKm(Au.Y), AuToKm(Au.Z));
        }
        public static Vector2 AuToKm(Vector2 Au)
        {
            return new Vector2(AuToKm(Au.X), AuToKm(Au.Y));
        }

        public static Vector3 AuToMt(Vector3 au)
        {
            Vector3 meters = au * GameConstants.Units.MetersPerAu;
            return meters;
        }
        public static Vector2 AuToMt(Vector2 au)
        {
            Vector2 meters = au * GameConstants.Units.MetersPerAu;
            return meters;
        }
        public static double AuToMt(double au)
        {
            return au * GameConstants.Units.MetersPerAu; 
        }

        public static double DistanceBetween(Vector3 p1, Vector3 p2)
        {
            return (p1 - p2).Length();
        }

    }

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

    /// <summary>
    /// Just a container for some general math functions.
    /// </summary>
    public class GMath
    {
        /// <summary>
        /// Clamps a value between the provided man and max.
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        public static double Clamp(double value, MinMaxStruct minMax)
        {
            return Clamp(value, minMax.Min, minMax.Max);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(MinMaxStruct minMax, double selection)
        {
            return minMax.Min + selection * (minMax.Max - minMax.Min);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(double min, double max, double selection)
        {
            return min + selection * (max - min);
        }

        /// <summary>
        /// Calculates where the value falls inside the MinMaxStruct.
        /// </summary>
        /// <returns>Value's percent in the MinMaxStruct (Ranged from 0.0 to 1.0)</returns>
        public static double GetPercentage(double value, MinMaxStruct minMax)
        {
            return GetPercentage(value, minMax.Min, minMax.Max);
        }

        /// <summary>
        /// Calculates where the value falls between the min and max.
        /// </summary>
        /// <returns>Value's percent in the MinMaxStruct (Ranged from 0.0 to 1.0)</returns>
        public static double GetPercentage(double value, double min, double max)
        {
            if (min >= max)
            {
                throw new ArgumentOutOfRangeException("min", "Min value must be less than Max value.");
            }
            double adjustedMax = max - min;
            double adjustedValue = value - min;
            return adjustedValue / adjustedMax;
        }

        /// <summary>
        /// Returns the gravitational attraction between two masses.
        /// </summary>
        /// <param name="mass1">Mass of first body. (KG)</param>
        /// <param name="mass2">Mass of second body. (KG)</param>
        /// <param name="distance">Distance between bodies. (M)</param>
        /// <returns>Force (Newtons)</returns>
        public static double GetGravitationalAttraction(double mass1, double mass2, double distance)
        {
            // http://en.wikipedia.org/wiki/Newton%27s_law_of_universal_gravitation
            return GameConstants.Science.GravitationalConstant * mass1 * mass2 / (distance * distance);
        }

        /// <summary>
        /// Returns the gravitational attraction of a body at a specified distance.
        /// </summary>
        /// <param name="mass">Mass of the body. (KG)</param>
        /// <param name="distance">Distance to the body. (M)</param>
        /// <returns>Force (Newtons)</returns>
        public static double GetStandardGravitationAttraction(double mass, double distance)
        {
            return GetGravitationalAttraction(mass, 1, distance);
        }

        /// <summary>
        /// Standard Gravitational parameter. in m^3 s^-2
        /// </summary>
        /// <returns>The gravitational parameter.</returns>
        /// <param name="mass">Mass.</param>
        public static double StandardGravitationalParameter(double mass)
        {
            return mass * GameConstants.Science.GravitationalConstant;
        }

        public static double GravitationalParameter_Km3s2(double mass)
        {
            return GameConstants.Science.GravitationalConstant * mass / 1000000000; // (1000^3)
        }

        public static double GrabitiationalParameter_Au3s2(double mass)
        {
            return GameConstants.Science.GravitationalConstant * mass / 3.347928976e33; // (149597870700^3)
        }

        /// <summary>
        /// calculates a vector from two positions and a magnatude
        /// </summary>
        /// <returns>The vector.</returns>
        /// <param name="currentPosition">Current position.</param>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="speedMagnitude_AU">Speed magnitude.</param>
        public static Vector3 GetVector(Vector3 currentPosition, Vector3 targetPosition, double speedMagnitude_AU)
        {
            Vector3 speed = new Vector3(0, 0, 0);
            double length;


            Vector3 speedMagInAU = new Vector3(0, 0, 0);

            Vector3 direction = new Vector3(0, 0, 0);
            direction.X = targetPosition.X - currentPosition.X;
            direction.Y = targetPosition.Y - currentPosition.Y;
            direction.Z = targetPosition.Z - currentPosition.Z;

            length = direction.Length(); // Distance between targets in AU
            if (length != 0)
            {
                direction.X = (direction.X / length);
                direction.Y = (direction.Y / length);
                direction.Z = (direction.Z / length);

                speedMagInAU.X = direction.X * speedMagnitude_AU;
                speedMagInAU.Y = direction.Y * speedMagnitude_AU;
                speedMagInAU.Z = direction.Z * speedMagnitude_AU;
            }


            speed.X = (speedMagInAU.X);
            speed.Y = (speedMagInAU.Y);
            speed.Z = (speedMagInAU.Z);

            return speed;
        }



        /// <summary>
        /// A decimal Sqrt. not as fast as normal Math.Sqrt, but better precision. 
        /// </summary>
        /// <returns>The sqrt. of x</returns>
        /// <param name="x">x</param>
        /// <param name="guess">normaly ignored, this is for the recursion</param>
        public static decimal Sqrt(decimal x, decimal? guess = null)
        {
            var ourGuess = guess.GetValueOrDefault(x / 2m);
            var result = x / ourGuess;
            var average = (ourGuess + result) / 2m;

            if (average == ourGuess) // This checks for the maximum precision possible with a decimal.
                return average;
            else
                return Sqrt(x, average);
        }

    }

    /// <summary>
    /// Small helper struct to make all these min/max dicts. nicer.
    /// </summary>
    public struct MinMaxStruct
    {
        public double Min, Max;

        public MinMaxStruct(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    public static class InterceptCalcs
    {
        /// <summary>
        /// Hohmann the specified GravParamOfParent, semiMajAxisCurrentBody and semiMajAxisOfTarget.
        /// </summary>
        /// <returns>Dv burn1 prograde, Dv burn2 retrograde</returns>
        /// <param name="GravParamOfParent">Grav parameter of parent.</param>
        /// <param name="semiMajAxisCurrentBody">Semi maj axis current body.</param>
        /// <param name="semiMajAxisOfTarget">Semi maj axis of target.</param>
        public static (double burn1, double burn2) Hohmann(double GravParamOfParent, double semiMajAxisCurrentBody, double semiMajAxisOfTarget)
        {
            double semMajAxisOfHohman = semiMajAxisCurrentBody + semiMajAxisOfTarget;
            double velCurrentBody = Math.Sqrt(GravParamOfParent / semiMajAxisCurrentBody);
            double velTarg = Math.Sqrt(GravParamOfParent / semiMajAxisOfTarget);

            double velOfHohmannAtPeriapsis = Math.Sqrt(2 * (-GravParamOfParent / semMajAxisOfHohman + GravParamOfParent / semiMajAxisCurrentBody));

            double velOfHohmannAtApoaxis = Math.Sqrt(2 * (-GravParamOfParent / semMajAxisOfHohman + GravParamOfParent / semiMajAxisOfTarget));

            double deltaVBurn1 = velOfHohmannAtPeriapsis - velCurrentBody;
            double deltaVBurn2 = velOfHohmannAtApoaxis - velTarg;

            return (deltaVBurn1, deltaVBurn2);
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
            Vector3 moverPosInKM = Distance.AuToKm(OrbitProcessor.GetAbsolutePosition_AU(moverOrbit, atDateTime));

            //PropulsionAbilityDB moverPropulsion = mover.GetDataBlob<PropulsionAbilityDB>();

            Vector3 targetPosInKM = Distance.AuToKm((OrbitProcessor.GetAbsolutePosition_AU(targetOrbit, atDateTime)));

            int speed = 25000;//moverPropulsion.MaximumSpeed * 100; //299792458;

            (Vector3, TimeSpan) intercept = (new Vector3(), TimeSpan.Zero);



            TimeSpan eti = new TimeSpan();
            TimeSpan eti_prev = new TimeSpan();
            DateTime edi = atDateTime;
            DateTime edi_prev = atDateTime;

            Vector3 predictedPosKM = Distance.AuToKm(OrbitProcessor.GetAbsolutePosition_AU(targetOrbit, edi_prev));
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

                    predictedPosKM = Distance.AuToKm(OrbitProcessor.GetAbsolutePosition_AU(targetOrbit, edi_prev));

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
        public static (Vector3 deltaV, double timeInSeconds)[] OrbitPhasingManuvers(OrbitDB orbit, DateTime manuverTime, double phaseAngle)
        {
            //https://en.wikipedia.org/wiki/Orbit_phasing
            double orbitalPeriod = orbit.OrbitalPeriod.TotalSeconds;
            double e = orbit.Eccentricity;

            var wc1 = Math.Sqrt((1 - e) / (1 + 3));
            var wc2 = Math.Tan(phaseAngle / 2);
            
            double E = 2 * Math.Atan(wc1 * wc2);

            double wc3 = orbitalPeriod / Math.PI * 2;
            double wc4 = e * Math.Sin(E);

            double phaseTime = wc3 * (E - wc4);

            double phaseOrbitPeriod = orbitalPeriod - phaseTime;

            double sgp = orbit.GravitationalParameter_m3S2;

            //double phaseOrbitSMA0 = Math.Pow(Math.Sqrt(sgp) * phaseOrbitPeriod / (Math.PI * 2), (2.0 / 3.0)); //I think this one will be slightly slower
            double phaseOrbitSMA = Math.Cbrt((sgp * phaseOrbitPeriod * phaseOrbitPeriod) / (4 * Math.PI * Math.PI));
            
            double phaseOrbitAppoaspis = OrbitProcessor.GetPosition_m(orbit, manuverTime).Length();// 
            double phaseOrbitPeriapsis = phaseOrbitSMA * 2 - phaseOrbitAppoaspis;


            double wc7 = Math.Sqrt( (phaseOrbitAppoaspis * phaseOrbitPeriapsis) / (phaseOrbitAppoaspis + phaseOrbitPeriapsis));
            double wc8 = Math.Sqrt(2 * sgp);
            double phaseOrbitAngularMomentum = wc8 * wc7;


            double wc9 = Math.Sqrt( (orbit.Apoapsis * orbit.Periapsis) / (orbit.Apoapsis + orbit.Periapsis));
            double wc10 = Math.Sqrt(2 * sgp);
            double orbitAngularMomentum = wc9 * wc10;

            double r = OrbitProcessor.GetPosition_m(orbit, manuverTime).Length();

            double dv = phaseOrbitAngularMomentum / r - orbitAngularMomentum / r;

            (Vector3, double)[] manuvers = new (Vector3, double)[2];
            manuvers[0] = (new Vector3(0, -dv, 0), 0);
            manuvers[1] = (new Vector3(0, dv, 0), phaseOrbitPeriod);
            
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

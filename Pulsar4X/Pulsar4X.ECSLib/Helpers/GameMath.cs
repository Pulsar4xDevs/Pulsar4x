using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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
    }

    /// <summary>
    /// Small helper class for Temperature unit conversions
    /// </summary>
    public static class Temperature
    {
        public static double ToKelvin(double celsius)
        {
            return celsius + GameSettings.Units.DegreesCToKelvin;
        }

        public static float ToKelvin(float celsius)
        {
            return (float)(celsius + GameSettings.Units.DegreesCToKelvin);
        }

        public static double ToCelsius(double kelvin)
        {
            return kelvin + GameSettings.Units.KelvinToDegreesC;
        }

        public static float ToCelsius(float kelvin)
        {
            return (float)(kelvin + GameSettings.Units.KelvinToDegreesC);
        }
    }

    /// <summary>
    /// Small helper class for Distance unit conversions
    /// </summary>
    public static class Distance
    {
        public static double ToAU(double km)
        {
            return km / GameSettings.Units.KmPerAu;
        }

        public static double ToKm(double au)
        {
            return au * GameSettings.Units.KmPerAu;
        }
    }

    /// <summary>
    /// Used for holding a percentage stores as a byte so 255 bits precision.
    /// Takes and Returns 0.0 to 1.0 for easy multiplcation math.
    /// </summary>
    public class PercentValue
    {
        private byte _percent;

        /// <summary>
        /// 0.0f to 1.0f with 255 bits of precision
        /// </summary>
        public float Percent
        {
            get {return _percent / 255f;} 
            set { _percent = (byte)(value * 255); }
        }
    }

    public class WeightedValue<T>
    {
        public double Weight { get; set; }
        public T Value { get; set; }
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
        private List<WeightedValue<T>> m_valueList;

        /// <summary>
        /// Total weights of the list.
        /// </summary>
        public double TotalWeight { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public WeightedList()
        {
            m_valueList = new List<WeightedValue<T>>();
        }

        /// <summary>
        /// Adds a value to the weighted list.
        /// </summary>
        /// <param name="weight">Weight of this value in the list.</param>
        public void Add(double weight, T value)
        {
            var listEntry = new WeightedValue<T> {Weight = weight, Value = value};
            m_valueList.Add(listEntry);
            TotalWeight += weight;
        }

        /// <summary>
        /// Adds the contents of another weighted list to this one.
        /// </summary>
        /// <param name="otherList">The list to add.</param>
        public void AddRange(WeightedList<T> otherList)
        {
            m_valueList.AddRange(otherList.m_valueList);
            TotalWeight += otherList.TotalWeight;
        }

        public IEnumerator<WeightedValue<T>> GetEnumerator() 
        {
            return m_valueList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Selects a value from the list based on the input.
        /// </summary>
        /// <param name="rngValue">Value 0.0 to 1.0 represending the random value selected by the RNG.</param>
        /// <returns></returns>
        public T Select(double rngValue)
        {
            double cumulativeChance = 0;
            foreach (WeightedValue<T> listEntry in m_valueList)
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
            return m_valueList[index].Value;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Values", m_valueList);
        }

        public WeightedList(SerializationInfo info, StreamingContext context)
        {
            m_valueList = (List<WeightedValue<T>>)info.GetValue("Values", typeof(List<WeightedValue<T>>));

            // rebuild total weight:
            TotalWeight = 0;
            foreach (var w in m_valueList)
            {
                TotalWeight += w.Weight;
            }
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
            else if (value < min)
                return min;

            return value;
        }

        /// <summary>
        /// Clamps a number between 0 and 1.
        /// </summary>
        public static double Clamp01(double value)
        {
            return Clamp(value, 0, 1);
        }
    }
}

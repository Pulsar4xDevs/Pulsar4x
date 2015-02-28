using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers.GameMath
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
            return celsius + Constants.Units.DegreesCToKelvin;
        }

        public static float ToKelvin(float celsius)
        {
            return (float)(celsius + Constants.Units.DegreesCToKelvin);
        }

        public static double ToCelsius(double kelvin)
        {
            return kelvin + Constants.Units.KelvinToDegreesC;
        }

        public static float ToCelsius(float kelvin)
        {
            return (float)(kelvin + Constants.Units.KelvinToDegreesC);
        }
    }

    /// <summary>
    /// Small helper class for Distance unit conversions
    /// </summary>
    public static class Distance
    {
        public static double ToAU(double km)
        {
            return km / Constants.Units.KmPerAu;
        }

        public static double ToKm(double au)
        {
            return au * Constants.Units.KmPerAu;
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
    /// 
    public class WeightedList<T> : IEnumerable<WeightedValue<T>>
    {
        List<WeightedValue<T>> m_valueList;

        /// <summary>
        /// Total weights of the list.
        /// </summary>
        public double TotalWeight { get { return m_totalWeight; } }
        double m_totalWeight;

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
            WeightedValue<T> listEntry = new WeightedValue<T>();
            listEntry.Weight = weight;
            listEntry.Value = value;

            m_valueList.Add(listEntry);

            m_totalWeight += weight;
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
                double realChance = listEntry.Weight / m_totalWeight;
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

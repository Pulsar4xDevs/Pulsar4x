using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Pulsar4X.DataStructures
{
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
    [JsonConverter(typeof(WeightedListConverter))]
    public class WeightedList<T> : IEnumerable<WeightedValue<T>>
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

        public WeightedValue<T> this[int index]
        {
            get { return _valueList[index]; }
            set { RemoveAt(index); Add(value); }
        }
    }

    public class WeightedListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(WeightedList<>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);

            JArray? valuesArray;
            if (token is JObject jObject && jObject["$values"] != null)
            {
                valuesArray = jObject["$values"]?.Value<JArray>();
            }
            else if (token is JArray jArrayToken)
            {
                valuesArray = jArrayToken;
            }
            else
            {
                throw new JsonSerializationException($"Unexpected token type: {token.Type}. Expected a JObject or JArray.");
            }

            var weightedListType = typeof(WeightedList<>).MakeGenericType(objectType.GetGenericArguments()[0]);
            var weightedList = Activator.CreateInstance(weightedListType);

            var addMethod = weightedListType.GetMethod("Add", new[] { typeof(WeightedValue<>).MakeGenericType(objectType.GetGenericArguments()[0]) });

            foreach (var value in valuesArray)
            {
                var weightedValue = value.ToObject(typeof(WeightedValue<>).MakeGenericType(objectType.GetGenericArguments()[0]));
                addMethod.Invoke(weightedList, new[] { weightedValue });
            }

            return weightedList;
        }


        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if(value == null) return;
            var jArray = new JArray();
            foreach (var item in (System.Collections.IEnumerable)value)
            {
                jArray.Add(JObject.FromObject(item));
            }

            jArray.WriteTo(writer);
        }
    }
}
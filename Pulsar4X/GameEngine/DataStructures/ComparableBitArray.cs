using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.DataStructures
{
    [JsonConverter(typeof(ComparableBitArrayConverter))]
    public sealed class ComparableBitArray
    {
        [JsonProperty]
        public int[] BackingValues { get; internal set; }
        private const int BitsPerValue = 32;
        public List<int> SetBits;

        public int Length { get; private set; }

        /// <summary>
        /// Quickly ensures bit values are equivilent.
        /// </summary>
        private bool Equals(ComparableBitArray other)
        {
            return other != null && Length == other.Length && BackingValues.SequenceEqual(other.BackingValues);
        }

        /// <summary>
        /// Equality overrides so we are not only checking references.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((ComparableBitArray)obj);
        }

        public static bool operator ==(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (ReferenceEquals(arrayA, arrayB))
                return true;
            return !ReferenceEquals(arrayA, null) && arrayA.Equals(arrayB);
        }

        public static bool operator !=(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            return !(arrayA == arrayB);
        }

        /// <summary>
        /// GetHashCode override so we can effectively store ComparableBitArrays in HashTables.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (BackingValues.GetHashCode() * 397) ^ Length;
            }
        }

        /// <summary>
        /// Overload of the [] operator. Allows ComparableBitArray[index]
        /// </summary>
        public bool this[int index]
        {
            get { return Get(index); }
            set { Set(index, value ? 1 : 0); }
        }

        /// <summary>
        /// Returns the bit value residing at the index location.
        /// </summary>
        private bool Get(int index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException("index", "index cannot be greater than or equal to Length.");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", "index must be a non-negative number.");
            }

            int backingIndex = 0;
            while (index >= BitsPerValue)
            {
                backingIndex++;
                index -= BitsPerValue;
            }

            int backingValue = BackingValues[backingIndex];

            return ((backingValue >> index) & 1) == 1;
        }

        /// <summary>
        /// Sets the bit at the specified index to value.
        /// </summary>
        public void Set(int index, bool value)
        {
            Set(index, value ? 1 : 0);
        }

        private void Set(int index, int value)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException("index", "index cannot be greater than or equal to Length.");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", "index must be a non-negative number.");
            }

            int backingIndex = 0;
            while (index >= BitsPerValue)
            {
                backingIndex++;
                index -= BitsPerValue;
            }

            int backingValue = BackingValues[backingIndex];

            backingValue ^= (-value ^ backingValue) & (1 << index);
            BackingValues[backingIndex] = backingValue;

            int bitIndex = index + (backingIndex * BitsPerValue);

            if (value == 1)
            {
                if (!SetBits.Contains(index))
                    SetBits.Add(bitIndex);
            }
            else
            {
                if (SetBits.Contains(bitIndex))
                    SetBits.Remove(bitIndex);
            }
        }

        /// <summary>
        /// Creates a ComparableBitArray with the specified number of bits.
        /// </summary>
        public ComparableBitArray(int length)
        {
            SetBits = new List<int>();

            int requiredBackingValues = 1;
            while (length > BitsPerValue)
            {
                requiredBackingValues++;
                length -= BitsPerValue;
            }

            BackingValues = new int[requiredBackingValues];

            for (int i = 0; i < BackingValues.Length; i++ )
            {
                BackingValues[i] = 0;
            }

            Length = length + ((requiredBackingValues * BitsPerValue) - BitsPerValue);
        }

        /// <summary>
        /// Operator overload for bitwise AND.
        /// </summary>
        public static ComparableBitArray operator &(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.Length != arrayB.Length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            var combinedValues = new int[arrayA.BackingValues.Length];

            for (int i = 0; i < arrayA.BackingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.BackingValues[i] & arrayB.BackingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.Length);
        }

        /// <summary>
        /// Operator overload for bitwise OR.
        /// </summary>
        public static ComparableBitArray operator |(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.Length != arrayB.Length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            var combinedValues = new int[arrayA.BackingValues.Length];

            for (int i = 0; i < arrayA.BackingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.BackingValues[i] | arrayB.BackingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.Length);
        }

        /// <summary>
        /// Operator overload for bitwise XOR.
        /// </summary>
        public static ComparableBitArray operator ^(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.Length != arrayB.Length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            var combinedValues = new int[arrayA.BackingValues.Length];

            for (int i = 0; i < arrayA.BackingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.BackingValues[i] ^ arrayB.BackingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.Length);
        }

        private ComparableBitArray(int[] backingValues, int length)
        {
            BackingValues = backingValues;
            Length = length;
        }
    }

    public class ComparableBitArrayConverter : JsonConverter<ComparableBitArray>
    {
        public override void WriteJson(JsonWriter writer, ComparableBitArray value, JsonSerializer serializer)
        {
            var jObject = new JObject
            {
                { "BackingValues", JToken.FromObject(value.BackingValues) },
                { "Length", value.Length }
            };
            jObject.WriteTo(writer);
        }

        public override ComparableBitArray ReadJson(JsonReader reader, Type objectType, ComparableBitArray existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            int[] backingValues = jObject["BackingValues"].ToObject<int[]>();
            int length = jObject["Length"].Value<int>();

            // Use the private constructor to create the ComparableBitArray instance
            var bitArrayInstance = (ComparableBitArray)Activator.CreateInstance(typeof(ComparableBitArray), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, 
                null, new object[] { backingValues, length }, null);

            return bitArrayInstance;
        }
    }
}

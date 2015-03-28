using System;
using System.Linq;

namespace Pulsar4X.ECSLib.Helpers
{
    public sealed class ComparableBitArray
    {

        private readonly int[] _backingValues;
        private const int BitsPerValue = 32;

        public int Length { get; private set; }

        /// <summary>
        /// Quickly ensures bit values are equivilent.
        /// </summary>
        private bool Equals(ComparableBitArray other)
        {
            return Length == other.Length && _backingValues.SequenceEqual(other._backingValues);
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
                return (_backingValues.GetHashCode() * 397) ^ Length;
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
            while (index > BitsPerValue)
            {
                backingIndex++;
                index -= BitsPerValue;
            }

            int backingValue = _backingValues[backingIndex];

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
            while (index > BitsPerValue)
            {
                backingIndex++;
                index -= BitsPerValue;
            }

            int backingValue = _backingValues[backingIndex];

            backingValue ^= (-value ^ backingValue) & (1 << index);
            _backingValues[backingIndex] = backingValue;
        }

        /// <summary>
        /// Creates a ComparableBitArray with the specified number of bits.
        /// </summary>
        public ComparableBitArray(int length)
        {
            int requiredBackingValues = 1;
            while (length > BitsPerValue)
            {
                requiredBackingValues++;
                length -= BitsPerValue;
            }

            _backingValues = new int[requiredBackingValues];

            for (int i = 0; i < _backingValues.Length; i++ )
            {
                _backingValues[i] = 0;
            }

            Length = length;
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

            var combinedValues = new int[arrayA._backingValues.Length];

            for (int i = 0; i < arrayA._backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA._backingValues[i] & arrayB._backingValues[i]);
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

            var combinedValues = new int[arrayA._backingValues.Length];

            for (int i = 0; i < arrayA._backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA._backingValues[i] | arrayB._backingValues[i]);
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

            var combinedValues = new int[arrayA._backingValues.Length];

            for (int i = 0; i < arrayA._backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA._backingValues[i] ^ arrayB._backingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.Length);
        }

        private ComparableBitArray(int[] backingValues, int length)
        {
            _backingValues = backingValues;
            Length = length;
        }
    }
}

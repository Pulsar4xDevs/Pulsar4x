using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers
{
    public class ComparableBitArray
    {
        /// <summary>
        /// Overload of the [] operator. Allows ComparableBitArray[index]
        /// </summary>
        public bool this[int index]
        { 
            get { return Get(index); }
            set { Set(index, value); }
        }

        private int[] m_backingValues;
        private const int m_bitsPerValue = 32;

        public int Length { get { return m_length; } }
        private readonly int m_length;

        /// <summary>
        /// Returns the bit value residing at the index location.
        /// </summary>
        public bool Get(int index)
        {
            if (index >= m_length)
            {
                throw new ArgumentOutOfRangeException("index", "index cannot be greater than or equal to Length.");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", "index must be a non-negative number.");
            }

            int backingIndex = 0;
            while (index > m_bitsPerValue)
            {
                backingIndex++;
                index -= m_bitsPerValue;
            }

            int backingValue = m_backingValues[backingIndex];

            if (((backingValue >> index) & 1) == 1)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the bit at the specified index to value.
        /// </summary>
        public void Set(int index, bool value)
        {
            if (value)
            {
                Set(index, 1);
            }
            else
            {
                Set(index, 0);
            }
        }

        private void Set(int index, int value)
        {
            if (index >= m_length)
            {
                throw new ArgumentOutOfRangeException("index", "index cannot be greater than or equal to Length.");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", "index must be a non-negative number.");
            }

            int backingIndex = 0;
            while (index > m_bitsPerValue)
            {
                backingIndex++;
                index -= m_bitsPerValue;
            }

            int backingValue = m_backingValues[backingIndex];

            backingValue ^= (-value ^ backingValue) & (1 << index);
            m_backingValues[backingIndex] = backingValue;
        }

        /// <summary>
        /// Equality override so we are not only checking references.
        /// 
        /// Quickly ensures bit values are equivilent.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            ComparableBitArray bitArray = obj as ComparableBitArray;

            if (Object.Equals(bitArray, null))
            {
                return false;
            }

            if (m_backingValues.Length != bitArray.m_backingValues.Length)
            {
                return false;
            }

            for (int i = 0; i < m_backingValues.Length; i++)
            {
                if (m_backingValues[i] != bitArray.m_backingValues[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Equality override so we are not only checking references.
        /// 
        /// Quickly ensures bit values are equivilent.
        /// </summary>
        public static bool operator ==(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            return arrayA.Equals(arrayB);
        }

        /// <summary>
        /// Equality override so we are not only checking references.
        /// 
        /// Quickly ensures bit values are equivilent.
        /// </summary>
        public static bool operator !=(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            return !(arrayA == arrayB);
        }

        /// <summary>
        /// Creates a ComparableBitArray with the specified number of bits.
        /// </summary>
        public ComparableBitArray(int length)
        {
            int requiredBackingValues = 1;
            while (length > m_bitsPerValue)
            {
                requiredBackingValues++;
                length -= m_bitsPerValue;
            }

            m_backingValues = new int[requiredBackingValues];

            for (int i = 0; i < m_backingValues.Length; i++ )
            {
                m_backingValues[i] = 0;
            }

            m_length = length;
        }

        /// <summary>
        /// Operator overload for bitwise AND.
        /// </summary>
        public static ComparableBitArray operator &(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.m_length != arrayB.m_length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            int[] combinedValues = new int[arrayA.m_backingValues.Length];

            for (int i = 0; i < arrayA.m_backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.m_backingValues[i] & arrayB.m_backingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.m_length);;
        }

        /// <summary>
        /// Operator overload for bitwise OR.
        /// </summary>
        public static ComparableBitArray operator |(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.m_length != arrayB.m_length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            int[] combinedValues = new int[arrayA.m_backingValues.Length];

            for (int i = 0; i < arrayA.m_backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.m_backingValues[i] | arrayB.m_backingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.m_length); ;
        }

        /// <summary>
        /// Operator overload for bitwise XOR.
        /// </summary>
        public static ComparableBitArray operator ^(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            if (arrayA.m_length != arrayB.m_length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            int[] combinedValues = new int[arrayA.m_backingValues.Length];

            for (int i = 0; i < arrayA.m_backingValues.Length; i++)
            {
                combinedValues[i] = (arrayA.m_backingValues[i] ^ arrayB.m_backingValues[i]);
            }

            return new ComparableBitArray(combinedValues, arrayA.m_length); ;
        }

        private ComparableBitArray(int[] backingValues, int length)
        {
            m_backingValues = backingValues;
            m_length = length;
        }
    }
}

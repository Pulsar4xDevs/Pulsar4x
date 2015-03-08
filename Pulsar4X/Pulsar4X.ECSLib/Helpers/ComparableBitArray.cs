using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Helpers
{
    public class ComparableBitArray
    {
        public bool this[int i]
        { 
            get { return Get(i); }
            set { Set(i, value); }
        }

        private int[] m_backingValues;
        private const int m_bitsPerValue = 32;

        public int Length { get { return m_length; } }
        private readonly int m_length;

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

        public static bool operator ==(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            return arrayA.Equals(arrayB);
        }

        public static bool operator !=(ComparableBitArray arrayA, ComparableBitArray arrayB)
        {
            return !(arrayA == arrayB);
        }

        public ComparableBitArray(int Size)
        {
            int requiredBackingValues = 1;
            while (Size > m_bitsPerValue)
            {
                requiredBackingValues++;
                Size -= m_bitsPerValue;
            }

            m_backingValues = new int[requiredBackingValues];

            for (int i = 0; i < m_backingValues.Length; i++ )
            {
                m_backingValues[i] = 0;
            }

            m_length = Size;
        }

        public ComparableBitArray And(ComparableBitArray otherArray)
        {
            if (m_length != otherArray.m_length)
            {
                throw new ArgumentException("Cannot compare bit arrays of different lengths.");
            }

            int[] combinedValues = new int[m_backingValues.Length];

            for (int i = 0; i < m_backingValues.Length; i++)
            {
                combinedValues[i] = (m_backingValues[i] & otherArray.m_backingValues[i]);
            }

            ComparableBitArray retVal = new ComparableBitArray(combinedValues, m_length);

            return retVal;

        }

        private ComparableBitArray(int[] backingValues, int length)
        {
            m_backingValues = backingValues;
            m_length = length;
        }
    }
}

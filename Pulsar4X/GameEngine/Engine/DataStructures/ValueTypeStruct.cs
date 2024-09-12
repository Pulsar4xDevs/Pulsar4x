namespace Pulsar4X.DataStructures
{
    public struct ValueTypeStruct
    {
        public enum ValueTypes
        {
            Power,
            Distance,
            Volume,
            Mass,
            Velocity,
            Force,
            Number,
        }

        public enum ValueSizes
        {
            Pico = -12,
            Nano = -9,
            Micro = -6,
            Milli = - 3,
            Centi = -2,
            Deci = -1,
            BaseUnit = 0,
            Deca = 1,
            Hecto = 2,
            Kilo = 3,
            Mega = 6,
            Giga = 9,
            Tera = 12,
        }

        public ValueTypes ValueType;
        public ValueSizes ValueSize;

        public ValueTypeStruct(ValueTypes type, ValueSizes size)
        {
            ValueType = type;
            ValueSize = size;
        }
    }
}
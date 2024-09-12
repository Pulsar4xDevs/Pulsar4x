using System.Collections.Generic;

namespace Pulsar4X.DataStructures
{
    public class WeightedValue<T>
    {
        public double Weight { get; set; }
        public T Value { get; set; }

        protected bool Equals(WeightedValue<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object? obj)
        {
            if(obj == null) return false;

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
            return Value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(Value);
        }

    }
}
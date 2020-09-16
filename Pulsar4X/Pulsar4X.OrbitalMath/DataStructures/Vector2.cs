using System;

namespace Pulsar4X.Orbital
{
    public struct Vector2
    {
        public double X;
        public double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector2(System.Numerics.Vector2 vector)
        {
            X = vector.X;
            Y = vector.Y;
        }

        public static Vector2 NaN
        {
            get { return new Vector2(double.NaN, double.NaN); }
        }

        /// <summary>
        /// Gets a vector whose 2 elements are equal to one. 
        /// </summary>
        public static Vector2 One
        {
            get { return new Vector2(1, 1); }
        }

        /// <summary>
        /// Gets a vector whose 2 elements are equal to zero. 
        /// </summary>
        public static Vector2 Zero
        {
            get { return new Vector2(0, 0); }
        }
        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector2 Add(Vector2 left, Vector2 right)
        {
            return left + right;
        }

        /// <summary>
        /// Divides the first vector by the second. 
        /// </summary>
        public static Vector2 Divide(Vector2 left, Vector2 right)
        {
            return left / right;
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value. 
        /// </summary>
        public static Vector2 Divide(Vector2 left, double divisor)
        {
            return left / divisor;

        }

        /// <summary>
        /// Returns the magnitude of the vector.
        /// </summary>
        /// 
        public static double Magnitude(Vector2 vector)
        {
            return Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        }

        public static Vector2 Normalise(Vector2 vector)
        {
            double magnatude = Magnitude(vector);
            if (magnatude != 0)
                return vector / magnatude;
            else
                return vector;
        }

        /// <summary>
        /// Returns the dot product of two vectors
        /// </summary>

        public static double Dot(Vector2 left, Vector2 right)
        {
            return left.X * right.X + left.Y * right.Y;
        }


        /// <summary>
        /// Returns the angle between two vectors
        /// </summary>

        public static double AngleBetween(Vector2 left, Vector2 right)
        {
            return Math.Acos(Dot(left, right) / (Magnitude(left) * Magnitude(right)));
        }

        /// <summary>
        /// Returns a value that indicates whether this instance and a specified object are equal. (Overrides ValueType.Equals(Object).)
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Vector2))
                return false;

            return Equals((Vector2)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance and another vector are equal. 
        /// </summary>
        public bool Equals(Vector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        /// <summary>
        /// Returns the hash code for this instance.  (Overrides ValueType.GetHashCode().)
        /// </summary>
        public override int GetHashCode()
        {
            long h = 2166136261;

            h = h * 16777619 ^ X.GetHashCode();
            h = h * 16777619 ^ Y.GetHashCode();

            return (int)h;
        }

        /// <summary>
        /// Returns the length of this vector object. 
        /// </summary>
        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        /// <summary>
        /// Returns the length of the vector squared. 
        /// </summary>
        public double LengthSquared()
        {
            return (X * X) + (Y * Y);
        }

        /// <summary>
        /// Multiplies a scalar value by a specified vector.
        /// </summary>
        public static Vector2 Multiply(double left, Vector2 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together. 
        /// </summary>
        public static Vector2 Multiply(Vector2 left, Vector2 right)
        {
            return left * right;
        }

        /// <summary>
        /// Multiplies a vector by a specified scalar. 
        /// </summary>
        public static Vector2 Multiply(Vector2 left, float right)
        {
            return left * right;
        }

        /// <summary>
        /// Negates a specified vector. 
        /// </summary>
        public static Vector2 Negate(Vector2 value)
        {
            return -value;
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector2 Subtract(Vector2 left, Vector2 right)
        {
            return left - right;
        }

        /// <summary>
        /// Returns the string representation of the current instance using default formatting.  (Overrides ValueType.ToString().)
        /// </summary>
        public override string ToString()
        {
            return String.Format("({0}, {1}", X, Y);
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements. 
        /// </summary>
        public string ToString(string format)
        {
            return String.Format("({0}, {1})", X.ToString(format), Y.ToString(format));
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements 
        /// and the specified format provider to define culture-specific formatting.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format("({0}, {1})", X.ToString(format, formatProvider), Y.ToString(format, formatProvider));
        }



        #region Operators

        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Divides the first vector by the second. 
        /// </summary>
        public static Vector2 operator /(Vector2 left, Vector2 right)
        {
            var temp = new Vector2(left.X / right.X, left.Y / right.Y);
            return temp;
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value.
        /// </summary>
        public static Vector2 operator /(Vector2 left, double right)
        {
            return new Vector2(left.X / right, left.Y / right);
        }

        /// <summary>
        /// Returns a value that indicates whether each pair of elements in two specified vectors is equal. 
        /// </summary>
        public static bool operator ==(Vector2 left, Vector2 right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        /// <summary>
        /// Returns a value that indicates whether two specified vectors are not equal. 
        /// </summary>
        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return left.X != right.X && left.Y != right.Y;
        }

        /// <summary>
        /// Multiples the scalar value by the specified vector. 
        /// </summary>
        public static Vector2 operator *(double left, Vector2 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Multiples the specified vector by the specified scalar value. 
        /// </summary>
        public static Vector2 operator *(Vector2 left, double right)
        {
            return new Vector2(left.X * right, left.Y * right);
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Negates the specified vector. 
        /// </summary>
        public static Vector2 operator -(Vector2 value)
        {
            return new Vector2(-value.X, -value.Y);
        }

        public static explicit operator Vector2(Vector3 v4)  // explicit vector4 to vector2 conversion operator
        {
            return new Vector2(v4.X, v4.Y);
        }

        #endregion

    }

}

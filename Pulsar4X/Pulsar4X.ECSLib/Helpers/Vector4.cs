using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This struct is used for positions and Vectors in Pulsar4x. it containes all the corrosponding Math.
    /// It is designed to be compatible with 3D graphics and with the upcoming (in .Net 4.6) MS System.Numerics.Vector4
    /// See: https://msdn.microsoft.com/en-us/library/system.numerics.vector4(v=vs.110).aspx
    /// </summary>
    /// <typeparam name="T">An int, float, </typeparam>
    public struct Vector4 : IEquatable<Vector4>, IFormattable
    {
        public double X;
        public double Y;
        public double Z;
        public double W;

        #region Constructors

        public Vector4(double single)
        {
            X = single;
            Y = single;
            Z = single;
            W = single;
        }

        public Vector4(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4(Vector4 position)
        {
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            W = position.W;

        }

        #endregion

        #region Static Properties

        /// <summary>
        /// Gets a vector whose 4 elements are equal to one. 
        /// </summary>
        public static Vector4 One
        {
            get { return new Vector4(1,1,1,1); }
        }

        public static explicit operator global::OpenTK.Vector4(Vector4 v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a vector whose 4 elements are equal to zero. 
        /// </summary>
        public static Vector4 Zero
        {
            get { return new Vector4(0,0,0,0); }
        }

        /// <summary>
        /// Gets the vector (1,0,0,0).
        /// </summary>
        public static Vector4 UnitX
        {
            get { return new Vector4(1,0,0,0); }
        }

        /// <summary>
        /// Gets the vector (0,1,0,0).
        /// </summary>
        public static Vector4 UnitY
        {
            get { return new Vector4(0,1,0,0); }
        }

        /// <summary>
        /// Gets the vector (0,0,1,0).
        /// </summary>
        public static Vector4 UnitZ
        {
            get { return new Vector4(0,0,1,0); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector4 Add(Vector4 left, Vector4 right)
        {
            return left + right;
        }

        /// <summary>
        /// Divides the first vector by the second. 
        /// </summary>
        public static Vector4 Divide(Vector4 left, Vector4 right)
        {
            return left / right;
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value. 
        /// </summary>
        public static Vector4 Divide(Vector4 left, double divisor)
        {
            return left / divisor;

        }

        /// <summary>
        /// Returns a value that indicates whether this instance and a specified object are equal. (Overrides ValueType.Equals(Object).)
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Vector4))
                return false;

            return Equals((Vector4)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance and another vector are equal. 
        /// </summary>
        public bool Equals(Vector4 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        }

        /// <summary>
        /// Returns the hash code for this instance.  (Overrides ValueType.GetHashCode().)
        /// </summary>
        public override int GetHashCode()
        {
            long h = 2166136261;

            h = h * 16777619 ^ X.GetHashCode();
            h = h * 16777619 ^ Y.GetHashCode();
            h = h * 16777619 ^ Z.GetHashCode();
            h = h * 16777619 ^ W.GetHashCode();

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
            return X * X + Y * Y + Z * Z + W * W;
        }

        /// <summary>
        /// Multiplies a scalar value by a specified vector.
        /// </summary>
        public static Vector4 Multiply(double left, Vector4 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together. 
        /// </summary>
        public static Vector4 Multiply(Vector4 left, Vector4 right)
        {
            return left * right;
        }

        /// <summary>
        /// Multiplies a vector by a specified scalar. 
        /// </summary>
        public static Vector4 Multiply(Vector4 left, float right)
        {
            return left * right;
        }

        /// <summary>
        /// Negates a specified vector. 
        /// </summary>
        public static Vector4 Negate(Vector4 value)
        {
            return -value;
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector4 Subtract(Vector4 left, Vector4 right)
        {
            return left - right;
        }

        /// <summary>
        /// Returns the string representation of the current instance using default formatting.  (Overrides ValueType.ToString().)
        /// </summary>
        public override string ToString()
        {
            return String.Format("({0}, {1}, {2}, {3})", X, Y, Z, W);
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements. 
        /// </summary>
        public string ToString(string format)
        {
            return String.Format("({0}, {1}, {2}, {3})", X.ToString(format), Y.ToString(format), Z.ToString(format), W.ToString(format));
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements 
        /// and the specified format provider to define culture-specific formatting.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format("({0}, {1}, {2}, {3})", X.ToString(format, formatProvider), Y.ToString(format, formatProvider),
                                                         Z.ToString(format, formatProvider), W.ToString(format, formatProvider));
        }

        #endregion

        #region Operators

        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector4 operator +(Vector4 left, Vector4 right)
        {
            return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
        }

        /// <summary>
        /// Divides the first vector by the second. 
        /// </summary>
        public static Vector4 operator /(Vector4 left, Vector4 right)
        {
            var temp = new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
            return temp;
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value.
        /// </summary>
        public static Vector4 operator /(Vector4 left, double right)
        {
            return new Vector4(left.X / right, left.Y / right, left.Z / right, left.W / right);
        }

        /// <summary>
        /// Returns a value that indicates whether each pair of elements in two specified vectors is equal. 
        /// </summary>
        public static bool operator ==(Vector4 left, Vector4 right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z && left.W == right.W;
        }

        /// <summary>
        /// Returns a value that indicates whether two specified vectors are not equal. 
        /// </summary>
        public static bool operator !=(Vector4 left, Vector4 right)
        {
            return left.X != right.X && left.Y != right.Y && left.Z != right.Z && left.W != right.W;
        }

        /// <summary>
        /// Multiples the scalar value by the specified vector. 
        /// </summary>
        public static Vector4 operator *(double left, Vector4 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        public static Vector4 operator *(Vector4 left, Vector4 right)
        {
            return new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
        }

        /// <summary>
        /// Multiples the specified vector by the specified scalar value. 
        /// </summary>
        public static Vector4 operator *(Vector4 left, double right)
        {
            return new Vector4(left.X * right, left.Y * right, left.Z * right, left.W * right);
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector4 operator -(Vector4 left, Vector4 right)
        {
            return new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
        }

        /// <summary>
        /// Negates the specified vector. 
        /// </summary>
        public static Vector4 operator -(Vector4 value)
        {
            return new Vector4(-value.X, -value.Y, -value.Z, - value.W);
        }

        #endregion
    }
}

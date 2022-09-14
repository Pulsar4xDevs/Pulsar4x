using System;
using System.Numerics;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// This struct is used for positions and Vectors in Pulsar4x. it containes all the corrosponding Math.
    /// It is designed to be compatible with 3D graphics and with the upcoming (in .Net 4.6) MS System.Numerics.Vector4
    /// See: https://msdn.microsoft.com/en-us/library/system.numerics.vector4(v=vs.110).aspx
    /// </summary>
    /// <typeparam name="T">An int, float, </typeparam>
    public struct Vector3 : IEquatable<Vector3>, IFormattable, IComparable<Vector3>
    {

        public double X;
        public double Y;
        public double Z;

        public int CompareTo(Vector3 obj)
        {
            return this.Length().CompareTo(obj.Length());
        }


        #region Constructors

        public Vector3(double single)
        {
            X = single;
            Y = single;
            Z = single;
        }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(Vector3 position)
        {
            X = position.X;
            Y = position.Y;
            Z = position.Z;
        }

        #endregion

        #region Static Properties

        public static Vector3 NaN
        {
            get { return new Vector3(double.NaN, double.NaN, double.NaN); }
        }

        /// <summary>
        /// Gets a vector whose 3 elements are equal to one. 
        /// </summary>
        public static Vector3 One
        {
            get { return new Vector3(1, 1, 1); }
        }

        /// <summary>
        /// Gets a vector whose 3 elements are equal to zero. 
        /// </summary>
        public static Vector3 Zero
        {
            get { return new Vector3(0, 0, 0); }
        }

        /// <summary>
        /// Gets the vector (1,0,0).
        /// </summary>
        public static Vector3 UnitX
        {
            get { return new Vector3(1, 0, 0); }
        }

        /// <summary>
        /// Gets the vector (0,1,0).
        /// </summary>
        public static Vector3 UnitY
        {
            get { return new Vector3(0, 1, 0); }
        }

        /// <summary>
        /// Gets the vector (0,0,1).
        /// </summary>
        public static Vector3 UnitZ
        {
            get { return new Vector3(0, 0, 1); }
        }

        public static Vector3 Random(Random r)
        {
            return new Vector3(r.NextDouble(), r.NextDouble(), r.NextDouble());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector3 Add(Vector3 left, Vector3 right)
        {
            return left + right;
        }

        /// <summary>
        /// Divides the first vector element-wise by the second. 
        /// </summary>
        public static Vector3 Divide(Vector3 left, Vector3 right)
        {
            return left / right;
        }

        /// <summary>
        /// Attempts a more precise result by casting to decimals. 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static (decimal X, decimal Y, decimal Z) DividePrecise(Vector3 left, Vector3 right)
        {
            decimal lx = (decimal)left.X;
            decimal ly = (decimal)left.Y;
            decimal lz = (decimal)left.Z;
            decimal rx = (decimal)right.X;
            decimal ry = (decimal)right.Y;
            decimal rz = (decimal)right.Z;
            return (lx / rx, ly / ry, lz / rz);
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value. 
        /// </summary>
        public static Vector3 Divide(Vector3 left, double divisor)
        {
            return left / divisor;

        }

        /// <summary>
        /// attempts to get a more mathmaticaly precise result by casting to a decimal and back again.
        /// due to not having a built in Sqrt function for decimal, we cast back before that is done.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double MagnitudePrecise(Vector3 vector)
        {
            decimal x = (decimal)vector.X;
            decimal y = (decimal)vector.Y;
            decimal z = (decimal)vector.Z;

            return Math.Sqrt((double)(x * x + y * y + z * z));
        }

        public static Vector3 Normalise(Vector3 vector)
        {
            double magnitude = vector.Length();
            if (magnitude != 0)
                return vector / magnitude;
            else
                return vector;
        }

        /// <summary>
        /// Returns the dot product of two vectors
        /// </summary>
        public static double Dot(Vector3 left, Vector3 right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        /// <summary>
        /// attempts a more precise dotProduct by casting to decimals.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static decimal DotPrecise(Vector3 left, Vector3 right)
        {
            decimal lx = (decimal)left.X;
            decimal ly = (decimal)left.Y;
            decimal lz = (decimal)left.Z;
            decimal rx = (decimal)right.X;
            decimal ry = (decimal)right.Y;
            decimal rz = (decimal)right.Z;
            return (lx * rx + ly * ry + lz * rz);
        }

        /// <summary>
        /// Cross product of left and right.
        /// </summary>
        public static Vector3 Cross(Vector3 left, Vector3 right)
        {
            return new Vector3()
            {
                X = left.Y * right.Z - left.Z * right.Y,
                Y = left.Z * right.X - left.X * right.Z,
                Z = left.X * right.Y - left.Y * right.X
            };
        }

        /// <summary>
        /// attempts a more precise cross product by casting to decimal.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static (decimal X, decimal Y, decimal Z) CrossPrecise(Vector3 left, Vector3 right)
        {
            decimal lx = (decimal)left.X;
            decimal ly = (decimal)left.Y;
            decimal lz = (decimal)left.Z;
            decimal rx = (decimal)right.X;
            decimal ry = (decimal)right.Y;
            decimal rz = (decimal)right.Z;
            decimal newx = ly * rz - lz * ry;
            decimal newy = lz * rx - lx * rz;
            decimal newz = lx * ry - ly * rx;

            return (newx, newy, newz);
        }

        /// <summary>
        /// Returns the angle between two vectors
        /// </summary>
        public static double AngleBetween(Vector3 left, Vector3 right)
        {
            return Math.Acos(Dot(left, right) / (left.Length() * right.Length()));
        }

        /// <summary>
        /// Returns a value that indicates whether this instance and a specified object are equal. (Overrides ValueType.Equals(Object).)
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Vector3))
                return false;

            return Equals((Vector3)obj);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance and another vector are equal. 
        /// </summary>
        public bool Equals(Vector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
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
            return (X * X) + (Y * Y) + (Z * Z);
        }

        /// <summary>
        /// Multiplies a scalar value by a specified vector.
        /// </summary>
        public static Vector3 Multiply(double left, Vector3 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together. 
        /// </summary>
        public static Vector3 Multiply(Vector3 left, Vector3 right)
        {
            return left * right;
        }

        /// <summary>
        /// Attempts a more precise result by casting to a decimal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static (decimal X, decimal Y, decimal Z) MultiplyPrecise(Vector3 left, Vector3 right)
        {
            decimal lx = (decimal)left.X;
            decimal ly = (decimal)left.Y;
            decimal lz = (decimal)left.Z;
            decimal rx = (decimal)right.X;
            decimal ry = (decimal)right.Y;
            decimal rz = (decimal)right.Z;
            return (lx * rx, ly * ry, lz * rz);
        }

        /// <summary>
        /// Multiplies a vector by a specified scalar. 
        /// </summary>
        public static Vector3 Multiply(Vector3 left, float right)
        {
            return left * right;
        }

        /// <summary>
        /// Negates a specified vector. 
        /// </summary>
        public static Vector3 Negate(Vector3 value)
        {
            return -value;
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector3 Subtract(Vector3 left, Vector3 right)
        {
            return left - right;
        }

        /// <summary>
        /// Returns the string representation of the current instance using default formatting.  (Overrides ValueType.ToString().)
        /// </summary>
        public override string ToString()
        {
            return String.Format("({0}, {1}, {2})", X, Y, Z);
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements. 
        /// </summary>
        public string ToString(string format)
        {
            return String.Format("({0}, {1}, {2})", X.ToString(format), Y.ToString(format), Z.ToString(format));
        }

        /// <summary>
        /// Returns the string representation of the current instance using the specified format string to format individual elements 
        /// and the specified format provider to define culture-specific formatting.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format("({0}, {1}, {2})", X.ToString(format, formatProvider), Y.ToString(format, formatProvider),
                                                         Z.ToString(format, formatProvider));
        }

        #endregion

        #region Operators

        /// <summary>
        /// Adds two vectors together. 
        /// </summary>
        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3 operator +(Vector3 left, Vector2 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z);
        }


        /// <summary>
        /// Divides the first vector by the second. 
        /// </summary>
        public static Vector3 operator /(Vector3 left, Vector3 right)
        {
            var temp = new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
            return temp;
        }

        /// <summary>
        /// Divides the specified vector by a specified scalar value.
        /// </summary>
        public static Vector3 operator /(Vector3 left, double right)
        {
            return new Vector3(left.X / right, left.Y / right, left.Z / right);
        }

        /// <summary>
        /// Returns a value that indicates whether each pair of elements in two specified vectors is equal. 
        /// </summary>
        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.X == right.X && left.Y == right.Y && left.Z == right.Z;
        }

        /// <summary>
        /// Returns a value that indicates whether two specified vectors are not equal. 
        /// </summary>
        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return left.X != right.X || left.Y != right.Y || left.Z != right.Z;
        }

        /// <summary>
        /// Multiples the scalar value by the specified vector. 
        /// </summary>
        public static Vector3 operator *(double left, Vector3 right)
        {
            return right * left;
        }

        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        public static Vector3 operator *(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        /// <summary>
        /// Multiples the specified vector by the specified scalar value. 
        /// </summary>
        public static Vector3 operator *(Vector3 left, double right)
        {
            return new Vector3(left.X * right, left.Y * right, left.Z * right);
        }

        /// <summary>
        /// Subtracts the second vector from the first. 
        /// </summary>
        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        /// <summary>
        /// Negates the specified vector. 
        /// </summary>
        public static Vector3 operator -(Vector3 value)
        {
            return new Vector3(-value.X, -value.Y, -value.Z);
        }

        public static explicit operator Vector3(Vector2 v2)  // explicit vector2 to vector4 conversion operator
        {
            return new Vector3(v2.X, v2.Y, 0);
        }

        #endregion
    }
}

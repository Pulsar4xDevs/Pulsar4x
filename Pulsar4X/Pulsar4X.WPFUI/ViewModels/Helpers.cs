using System.Windows;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI
{
    public static class Conversions
    {
        public static Point PointFromVector(Vector vector)
        {
            return new Point(vector.X, vector.Y);
        }
        /// <summary>
        /// looses Z and W
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Point PointFromVector(Vector4 vector)
        {
            return new Point(vector.X, vector.Y);
        }
        /// <summary>
        /// looses Z and W
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector VectorFromVector4(Vector4 vector)
        {
            return new Vector(vector.X, vector.Y);
        }
    }
}

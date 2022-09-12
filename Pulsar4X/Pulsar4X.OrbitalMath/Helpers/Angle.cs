using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// Small Helper Class for Angle unit Conversions
    /// </summary>
    public static class Angle
    {
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        /// <summary>
        /// returns a number between -pi and pi
        /// </summary>
        /// <returns>The radians.</returns>
        /// <param name="radians">Radians.</param>
        public static double NormaliseRadians(double radians)
        {
            radians = NormaliseRadiansPositive(radians);
            if (radians > Math.PI)
            {
                radians -= 2 * Math.PI;
            }

            return radians;
        }

        /// <summary>
        /// returns a number between 0 and 2 * pi
        /// </summary>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static double NormaliseRadiansPositive(double radians)
        {
			radians %= 2 * Math.PI;
			radians = (radians + 2 * Math.PI) % (2 * Math.PI);
			return radians;
        }

        /// <summary>
        /// returns a number between -360 and 360
        /// </summary>
        /// <returns>The degrees.</returns>
        /// <param name="degrees">Degrees.</param>
        public static double NormaliseDegrees(double degrees)
        {
            degrees = degrees % 360;
            return degrees;
        }

        public static double DifferenceBetweenRadians(double a1, double a2)
        {
            return Math.PI - Math.Abs(Math.Abs(a1 - a2) - Math.PI);
        }

        public static double DifferenceBetweenDegrees(double a1, double a2)
        {
            return 180 - Math.Abs(Math.Abs(a1 - a2) - 180);
        }

        public static double RadiansFromVector2(Vector2 vector)
        {
            return Math.Atan2(vector.Y, vector.X);
        }
        
        /// <summary>
        /// currently ignores Z
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double RadiansFromVector3(Vector3 vector)
        {
            return Math.Atan2(vector.Y, vector.X);
        }

    }
}

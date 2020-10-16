using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// Just a container for some general math functions.
    /// </summary>
    public class GeneralMath
    {
        /// <summary>
        /// Clamps a value between the provided man and max.
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        public static double Clamp(double value, MinMaxStruct minMax)
        {
            return Clamp(value, minMax.Min, minMax.Max);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(MinMaxStruct minMax, double selection)
        {
            return minMax.Min + selection * (minMax.Max - minMax.Min);
        }

        /// <summary>
        /// Selects a number from a range based on the selection percentage provided.
        /// </summary>
        public static double SelectFromRange(double min, double max, double selection)
        {
            return min + selection * (max - min);
        }

        /// <summary>
        /// Calculates where the value falls inside the MinMaxStruct.
        /// </summary>
        /// <returns>Value's percent in the MinMaxStruct (Ranged from 0.0 to 1.0)</returns>
        public static double GetPercentage(double value, MinMaxStruct minMax)
        {
            return GetPercentage(value, minMax.Min, minMax.Max);
        }

        /// <summary>
        /// Calculates where the value falls between the min and max.
        /// </summary>
        /// <returns>Value's percent in the MinMaxStruct (Ranged from 0.0 to 1.0)</returns>
        public static double GetPercentage(double value, double min, double max)
        {
            if (min >= max)
            {
                throw new ArgumentOutOfRangeException("min", "Min value must be less than Max value.");
            }
            double adjustedMax = max - min;
            double adjustedValue = value - min;
            return adjustedValue / adjustedMax;
        }

        /// <summary>
        /// Returns the gravitational attraction between two masses.
        /// </summary>
        /// <param name="mass1">Mass of first body. (KG)</param>
        /// <param name="mass2">Mass of second body. (KG)</param>
        /// <param name="distance">Distance between bodies. (M)</param>
        /// <returns>Force (Newtons)</returns>
        public static double GetGravitationalAttraction(double mass1, double mass2, double distance)
        {
            // http://en.wikipedia.org/wiki/Newton%27s_law_of_universal_gravitation
            return UniversalConstants.Science.GravitationalConstant * mass1 * mass2 / (distance * distance);
        }

        /// <summary>
        /// Returns the gravitational attraction of a body at a specified distance.
        /// </summary>
        /// <param name="mass">Mass of the body. (KG)</param>
        /// <param name="distance">Distance to the body. (M)</param>
        /// <returns>Force (Newtons)</returns>
        public static double GetStandardGravitationAttraction(double mass, double distance)
        {
            return GetGravitationalAttraction(mass, 1, distance);
        }

        /// <summary>
        /// Standard Gravitational parameter. in m^3 s^-2
        /// </summary>
        /// <returns>The gravitational parameter.</returns>
        /// <param name="mass">Mass.</param>
        public static double StandardGravitationalParameter(double mass)
        {
            return mass * UniversalConstants.Science.GravitationalConstant;
        }

        public static double GravitationalParameter_Km3s2(double mass)
        {
            return UniversalConstants.Science.GravitationalConstant * mass / 1000000000; // (1000^3)
        }

        public static double GravitiationalParameter_Au3s2(double mass)
        {
            return UniversalConstants.Science.GravitationalConstant * mass / 3.347928976e33; // (149597870700^3)
        }

        /// <summary>
        /// calculates a vector from two positions and a magnatude
        /// </summary>
        /// <returns>The vector.</returns>
        /// <param name="currentPosition">Current position.</param>
        /// <param name="targetPosition">Target position.</param>
        /// <param name="speedMagnitude_AU">Speed magnitude.</param>
        public static Vector3 GetVector(Vector3 currentPosition, Vector3 targetPosition, double speedMagnitude_AU)
        {
            Vector3 speed = new Vector3(0, 0, 0);
            double length;


            Vector3 speedMagInAU = new Vector3(0, 0, 0);

            Vector3 direction = new Vector3(0, 0, 0);
            direction.X = targetPosition.X - currentPosition.X;
            direction.Y = targetPosition.Y - currentPosition.Y;
            direction.Z = targetPosition.Z - currentPosition.Z;

            length = direction.Length(); // Distance between targets in AU
            if (length != 0)
            {
                direction.X = (direction.X / length);
                direction.Y = (direction.Y / length);
                direction.Z = (direction.Z / length);

                speedMagInAU.X = direction.X * speedMagnitude_AU;
                speedMagInAU.Y = direction.Y * speedMagnitude_AU;
                speedMagInAU.Z = direction.Z * speedMagnitude_AU;
            }


            speed.X = (speedMagInAU.X);
            speed.Y = (speedMagInAU.Y);
            speed.Z = (speedMagInAU.Z);

            return speed;
        }

        public static bool LineIntersectsLine(Vector2 l1start, Vector2 l1End, Vector2 l2Start, Vector2 l2End, out Vector2 intersectsAt)
        {
            // calculate the direction of the lines
            var uA = 
                ((l2End.X-l2Start.X)*(l1start.Y-l2Start.Y) - (l2End.Y-l2Start.Y)*(l1start.X-l2Start.X)) / 
                ((l2End.Y-l2Start.Y)*(l1End.X-l1start.X) - (l2End.X-l2Start.X)*(l1End.Y-l1start.Y));
            var uB = 
                ((l1End.X-l1start.X)*(l1start.Y-l2Start.Y) - (l1End.Y-l1start.Y)*(l1start.X-l2Start.X)) / 
                ((l2End.Y-l2Start.Y)*(l1End.X-l1start.X) - (l2End.X-l2Start.X)*(l1End.Y-l1start.Y));

            // if uA and uB are between 0-1, lines are colliding
            if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1) {
                double intersectionX = l1start.X + (uA * (l1End.X-l1start.X));
                double intersectionY = l1start.Y + (uA * (l1End.Y-l1start.Y));
                intersectsAt = new Vector2(intersectionX, intersectionY);
                return true;
            }

            intersectsAt = new Vector2();
            return false;
        }

        /// <summary>
        /// A decimal Sqrt. not as fast as normal Math.Sqrt, but better precision. 
        /// </summary>
        /// <returns>The sqrt. of x</returns>
        /// <param name="x">x</param>
        /// <param name="guess">normaly ignored, this is for the recursion</param>
        public static decimal Sqrt(decimal x, decimal? guess = null)
        {
            var ourGuess = guess.GetValueOrDefault(x / 2m);
            var result = x / ourGuess;
            var average = (ourGuess + result) / 2m;

            if (average == ourGuess) // This checks for the maximum precision possible with a decimal.
                return average;
            else
                return Sqrt(x, average);
        }

    }

    /// <summary>
    /// Small helper struct to make all these min/max dicts. nicer.
    /// </summary>
    public struct MinMaxStruct
    {
        public double Min, Max;

        public MinMaxStruct(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
}

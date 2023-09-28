using System;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// A bunch of convenient functions for calculating various ellipse parameters.
    /// </summary>
    public static class EllipseMath
    {
        /// <summary>
        /// SemiMajorAxis from SGP and SpecificEnergy
        /// </summary>
        /// <returns>The major axis.</returns>
        /// <param name="sgp">Standard Gravitational Parameter</param>
        /// <param name="specificEnergy">Specific energy.</param>
        public static double SemiMajorAxis(double sgp, double specificEnergy)
        {
            return sgp / (2 * specificEnergy);
        }

        public static double SemiMajorAxisFromApsis(double apoapsis, double periapsis)
        {
            return (apoapsis + periapsis) / 2;
        }

        public static double SemiMajorAxisFromLinearEccentricity(double linearEccentricity, double eccentricity)
        {
            return linearEccentricity * eccentricity;
        }

        public static double SemiMinorAxis(double semiMajorAxis, double eccentricity)
        {
            if (eccentricity < 1)
                return semiMajorAxis * Math.Sqrt(1 - eccentricity * eccentricity);
            else
                return semiMajorAxis * Math.Sqrt(eccentricity * eccentricity - 1);
        }

        public static double SemiMinorAxisFromApsis(double apoapsis, double periapsis)
        {
            return Math.Sqrt(Math.Abs(apoapsis * periapsis));
        }

        public static double LinearEccentricity(double apoapsis, double semiMajorAxis)
        {
            return apoapsis - semiMajorAxis;
        }

        public static double LinearEccentricityFromEccentricity(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * eccentricity;
        }

        public static double LinearEccentricityFromAxies(double a, double b)
        {
            return Math.Sqrt(a * a - b * b);
            
        }
        
        public static double Eccentricity(double linearEccentricity, double semiMajorAxis)
        {
            return linearEccentricity / semiMajorAxis;
        }

        public static double EccentricityFromAxies(double a, double b)
        {
            return Math.Sqrt(1 - (b * b) / (a * a));
        }

        public static double Apoapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 + eccentricity) * semiMajorAxis;
        }

        public static double Periapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 - eccentricity) * semiMajorAxis;
        }

        /// <summary>
        /// SemiLatusRectum (often denoted as "p")
        /// Works for circles, ellipses and hypobola
        /// </summary>
        /// <returns>SemiLatusRectum</returns>
        /// <param name="SemiMajorAxis">Semi major axis.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double SemiLatusRectum(double SemiMajorAxis, double eccentricity)
        {
            if (eccentricity == 0)//ie a circle 
                return SemiMajorAxis;
            return SemiMajorAxis * (1 - eccentricity * eccentricity);
        }

        public static double AreaOfEllipseSector(double semiMaj, double semiMin, double firstAngle, double secondAngle)
        {

            var theta1 = firstAngle;
            var theta2 = secondAngle;
            var theta3 = theta2 - theta1;

            //var foo2 = Math.Atan((semiMin - semiMaj) * Math.Sin(2 * theta2) / (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta2)));
            var foo2 = Math.Atan2((semiMin - semiMaj) * Math.Sin(2 * theta2), (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta2)));
            //var foo3 = Math.Atan((semiMin - semiMaj) * Math.Sin(2 * theta1) / (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta1)));
            var foo3 = Math.Atan2((semiMin - semiMaj) * Math.Sin(2 * theta1), (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta1)));

            var area = semiMaj * semiMin / 2 * (theta3 - foo2 + foo3);

            return area;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Ellipse#Polar_form_relative_to_center
        /// </summary>
        /// <param name="b">semi minor</param>
        /// <param name="e">eccentricity</param>
        /// <param name="theta">angle</param>
        /// <returns></returns>
        public static double RadiusFromCenter(double b, double e, double theta)
        {
            return b / Math.Sqrt(1 - (e * Math.Cos(theta)) * (e * Math.Cos(theta)));
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Ellipse#Polar_form_relative_to_focus
        /// </summary>
        /// <param name="a">semi major</param>
        /// <param name="e">eccentricy</param>
        /// <param name="phi">angle from focal 1 to focal 2 (or center)</param>
        /// <param name="theta">angle</param>
        /// <returns></returns>
        public static double RadiusFromFocal(double a, double e, double phi, double theta)
        {
            double dividend = a * (1 - e * e);
            double divisor = 1 + e * Math.Cos(theta - phi);
            double quotent = dividend / divisor;
            return quotent;
        }

    }
}
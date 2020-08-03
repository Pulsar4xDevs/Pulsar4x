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
            return Math.Sqrt(Math.Abs(apoapsis) * Math.Abs(periapsis));
        }

        public static double LinearEccentricity(double appoapsis, double semiMajorAxis)
        {
            return appoapsis - semiMajorAxis;
        }
        public static double LinearEccentricityFromEccentricity(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * eccentricity;
        }
        public static double Eccentricity(double linearEccentricity, double semiMajorAxis)
        {
            return linearEccentricity / semiMajorAxis;
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

    }
}
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
        /// works with ellipse and hyperabola. Plucked from: http://www.bogan.ca/orbits/kepler/orbteqtn.html
        /// </summary>
        /// <returns>The radius from the focal point for a given angle</returns>
        /// <param name="angle">Angle.</param>
        /// <param name="semiLatusRectum">Semi latus rectum.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double RadiusAtAngle(double angle, double semiLatusRectum, double eccentricity)
        {
            return semiLatusRectum / (1 + eccentricity * Math.Cos(angle));
        }
        
        /// <summary>
        /// https://en.wikipedia.org/wiki/Ellipse#Polar_form_relative_to_focus
        /// this is the same as RadiusAtAngle, but allows for phi. 
        /// </summary>
        /// <param name="a">semi major</param>
        /// <param name="e">eccentricy</param>
        /// <param name="phi">angle from focal 1 to focal 2 (or center)</param>
        /// <param name="theta">angle</param>
        /// <returns></returns>
        public static double RadiusFromFocal(double a, double e, double phi, double theta)
        {
            double dividend = a * (1 - e * e); // p semilatus rectum
            double divisor = 1 + e * Math.Cos(theta - phi);
            double quotent = dividend / divisor;
            return quotent;
        }

        /// <summary>
        /// works with ellipse and hyperabola. Plucked from: http://www.bogan.ca/orbits/kepler/orbteqtn.html
        /// </summary>
        /// <returns>The angle from the focal point for a given radius</returns>
        /// <param name="radius">Radius.</param>
        /// <param name="semiLatusRectum">Semi latus rectum.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        public static double AngleAtRadus(double radius, double semiLatusRectum, double eccentricity)
        {
            //r = p / (1 + e * cos(θ))
            //1 + e * cos(θ) = p/r
            //((p / r) -1) / e = cos(θ)
            return Math.Acos((semiLatusRectum / radius - 1) / eccentricity);
        }
        
        /// <summary>
        /// !!I think this is incorrect!!
        /// This is plucked from https://control.asu.edu/Classes/MAE462/462Lecture05.pdf
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="semiLatusRectum"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double AngleAtRadus2(double radius, double semiLatusRectum, double eccentricity)
        {
            return Math.Acos(1 / eccentricity - radius / (eccentricity * semiLatusRectum));
        }
        
        /// <summary>
        /// plucked from a YT comment. 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="semiLatusRectum"></param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double AngleAtRadus3(double radius, double semiLatusRectum, double eccentricity)
        {
            return Math.Acos((semiLatusRectum / radius * eccentricity - 1) / eccentricity);
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

    }
}
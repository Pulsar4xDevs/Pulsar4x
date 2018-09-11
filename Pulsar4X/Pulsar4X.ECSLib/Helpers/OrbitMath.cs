using System;
namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// A struct to hold kepler elements without the need to give a 'parent' as OrbitDB does.
    /// </summary>
    public struct KeplerElements
    {
        public double SemiMajorAxis;
        public double SemiMinorAxis;
        public double Eccentricity;
        public double LinierEccentricity;
        public double Periapsis;
        public double Apoapsis;
        public double LoAN;
        public double AoP;
        public double Inclination;
        public double MeanAnomaly;
        public double TrueAnomaly;
    }

    public class OrbitMath
    {

        public static KeplerElements SetParametersFromVelocityAndPosition(double standardGravParam, Vector4 position, Vector4 velocity)
        {
            KeplerElements ke = new KeplerElements();
            Vector4 angularVelocity = Vector4.Cross(position, velocity);
            Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);

            Vector4 eccentVector = ((velocity.Length() * velocity.Length() - standardGravParam / position.Length()) * position - Vector4.Dot(position, velocity) * velocity) / standardGravParam;

            double eccentricity = eccentVector.Length();

            double specificOrbitalEnergy = velocity.Length() * velocity.Length() * 0.5 - standardGravParam / position.Length();

            double apoaLen;
            double periLen;
            if (Math.Abs(eccentricity - 1.0) > 1e-15)
            {
                apoaLen = -standardGravParam / (2 * specificOrbitalEnergy);
                periLen = apoaLen * (1 - eccentricity * eccentricity);
            }
            else
            {
                periLen = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                apoaLen = double.MaxValue;
            }
            var aplenKM = Distance.AuToKm(apoaLen);
            var perlenKM = Distance.AuToKm(periLen);
            double semiMajorAxis = (apoaLen + periLen) * 0.5;//position.Length() + eccentricity;
            double semiMinorAxis = Math.Sqrt(Math.Abs(apoaLen) * Math.Abs(periLen));

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. 
            if (double.IsNaN(inclination))
                inclination = 0;

            double loANlen = nodeVector.X / nodeVector.Length();
            if (double.IsNaN(loANlen))
                loANlen = 0;
            else
                loANlen = GMath.Clamp(loANlen, -1, 1);
            double longdOfAN = Math.Acos(loANlen); //RAAN or LoAN or Omega letter


            double argOfPeriaps;
            if (longdOfAN == 0)
            {
                argOfPeriaps = Math.Atan2(eccentVector.Y, eccentVector.X);
                if (Vector4.Cross(position, velocity).Z < 0)
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }
            else
            {
                var aopLen = Vector4.Dot(nodeVector, eccentVector);
                aopLen = aopLen / (nodeVector.Length() * eccentricity);
                aopLen = GMath.Clamp(aopLen, -1, 1);
                argOfPeriaps = Math.Acos(aopLen);
                if (eccentVector.Z < 0)
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }


            var dotEccPos = Vector4.Dot(eccentVector, position);
            var talen = eccentVector.Length() * position.Length();
            talen = dotEccPos / talen;
            talen = GMath.Clamp(talen, -1, 1);
            var trueAnomoly = Math.Acos(talen);

            if (Vector4.Dot(position, velocity) < 0)
                trueAnomoly = Math.PI * 2 - trueAnomoly;

            var eccAng = Vector4.Dot(eccentVector, position);
            eccAng = semiMajorAxis / eccAng;
            eccAng = GMath.Clamp(eccAng, -1, 1);
            var eccentricAnomoly = Math.Acos(eccAng);

            var meanAnomaly = eccentricAnomoly - eccentricity * Math.Sin(eccentricAnomoly);


            ke.SemiMajorAxis = semiMajorAxis;
            ke.SemiMinorAxis = semiMinorAxis;
            ke.Eccentricity = eccentricity;
            ke.LinierEccentricity = eccentricity * semiMajorAxis;
            ke.Apoapsis = apoaLen;
            ke.Periapsis = periLen;
            ke.LoAN = longdOfAN;
            ke.AoP = argOfPeriaps;
            ke.Inclination = inclination;
            ke.MeanAnomaly = meanAnomaly;
            ke.TrueAnomaly = trueAnomoly;
            return ke;
        }
    }

    /// <summary>
    /// A bunch of convenient functions for calculating various ellipse parameters.
    /// </summary>
    public static class EllipseMath
    { 
        public static double SemiMinorAxis(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * Math.Sqrt(1 - eccentricity * eccentricity);
        }

        public static double SemiMinorAxisFromApsis(double apoapsis, double periapsis)
        {
            return Math.Sqrt(Math.Abs(apoapsis) * Math.Abs(periapsis));
        }

        public static double LinierEccentricity(double appoapsis, double semiMajorAxis)
        {
            return appoapsis - semiMajorAxis;
        }
        public static double Eccentricity(double linierEccentricity, double semiMajorAxis)
        {
            return linierEccentricity / semiMajorAxis;
        }
    }
}

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// Orbit math.
    /// note multiple simular functions for doing the same thing, some of these are untested.
    /// Take care when using unless the function has a decent test in the tests project. 
    /// Some simular functions with simular inputs left in for future performance testing (ie one of the two might be slightly more performant).
    /// </summary>
    public class OrbitalMath
    {
        private const double Epsilon = 1.0e-15; //TODO: test how low we can go

        /// <summary>
        /// Kepler elements from velocity and position.
        /// Note, to get correct results ensure all Sgp, position, and velocity values are all in the same unit (ie meters, km, or AU)
        /// </summary>
        /// <returns>a struct of Kepler elements.</returns>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="position">Position relative to parent</param>
        /// <param name="velocity">Velocity relative to parent</param>
        public static KeplerElements KeplerFromPositionAndVelocity(double standardGravParam, Vector3 position, Vector3 velocity, DateTime epoch)
        {
            KeplerElements ke = new KeplerElements();
            Vector3 angularVelocity = Vector3.Cross(position, velocity);
            Vector3 nodeVector = Vector3.Cross(Vector3.UnitZ, angularVelocity);

            Vector3 eccentVector = EccentricityVector(standardGravParam, position, velocity);

            double eccentricity = eccentVector.Length();
            double angularSpeed = angularVelocity.Length();

            double specificOrbitalEnergy = GetSpecificOrbitalEnergy(standardGravParam, position, velocity);

            double semiMajorAxis;
            double p; //p is where the ellipse or hypobola crosses a line from the focal point 90 degrees from the sma

			// If we run into negative eccentricity we have big problems
			Debug.Assert(eccentricity >= 0, "Negative eccentricity, this is physically impossible");
            
            //note that a hyperbolic orbit will have a negitive semiMajorAxis
            semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
            if (eccentricity > 1) //hypobola
            {
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else if (eccentricity < 1) //ellipse
            {
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola, currently forcing this to be a hyperbola. 
            {
                p = angularSpeed * angularSpeed / standardGravParam;
                eccentricity += 1.0E-15;
            }

            double semiMinorAxis = EllipseMath.SemiMinorAxis(semiMajorAxis, eccentricity);

            double inclination = Math.Acos(angularVelocity.Z / angularSpeed); //should be 0 in 2d. or pi if counter clockwise orbit. 

            if (double.IsNaN(inclination))
                inclination = 0;

            double trueAnomaly = TrueAnomaly(eccentVector, position, velocity);
            double eccentricAnomaly = GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, eccentricity);
            
            ke.StandardGravParameter = standardGravParam;
            ke.SemiMajorAxis = semiMajorAxis;
            ke.SemiMinorAxis = semiMinorAxis;
            ke.Eccentricity = eccentricity;

            ke.Apoapsis = EllipseMath.Apoapsis(eccentricity, semiMajorAxis);
            ke.Periapsis = EllipseMath.Periapsis(eccentricity, semiMajorAxis);
            ke.LinearEccentricity = EllipseMath.LinearEccentricity(ke.Apoapsis, semiMajorAxis);
            ke.LoAN = CalculateLongitudeOfAscendingNode(nodeVector); ;
            ke.AoP = GetArgumentOfPeriapsis(position, inclination, ke.LoAN, trueAnomaly); ;
            ke.Inclination = inclination;
            ke.MeanMotion = GetMeanMotion(standardGravParam, semiMajorAxis);
            if(eccentricity < 1)
                ke.MeanAnomalyAtEpoch = GetEllipticMeanAnomaly(eccentricity, eccentricAnomaly);
            else
            {
                ke.MeanAnomalyAtEpoch = 0;
            }
            ke.TrueAnomalyAtEpoch = trueAnomaly;
            ke.Period = 2 * Math.PI / ke.MeanMotion;
            ke.Epoch = epoch; //TimeFromPeriapsis(semiMajorAxis, standardGravParam, meanAnomaly);
            //Epoch(semiMajorAxis, semiMinorAxis, eccentricAnomoly, OrbitalPeriod(standardGravParam, semiMajorAxis));

            return ke;
        }

        /// <summary>
        /// returns a boring circular orbit. 
        /// </summary>
        /// <param name="relativePosition"></param>
        /// <param name="sgp"></param>
        /// <param name="epoch"></param>
        /// <returns>a circular orbit</returns>
        public static KeplerElements FromPosition(Vector3 relativePosition, double sgp, DateTime epoch)
        {

            var ralpos = relativePosition;
            var r = ralpos.Length();
            var i = Math.Atan2(ralpos.Z, r);
            var m0 = Math.Atan2(ralpos.Y, ralpos.X);

            var orbit = new KeplerElements()
            {
                SemiMajorAxis = r,
                SemiMinorAxis = r,
                Apoapsis = r,
                Periapsis = r,
                LinearEccentricity = 0,
                Eccentricity = 0,
                Inclination = i,
                LoAN = 0,
                AoP = 0,
                MeanMotion = Math.Sqrt(sgp / Math.Pow(r, 3)),
                MeanAnomalyAtEpoch = m0,
                TrueAnomalyAtEpoch = m0,
                AnomalyAtEpoch = m0,
                Period = 2 * Math.PI * Math.Sqrt(Math.Pow(r, 3) / sgp),
                Epoch = epoch,
                StandardGravParameter = sgp,
            };

            return orbit;
        }
        

        #region Vector Calculations

        public static Vector3 CalculateAngularMomentum(Vector3 position, Vector3 velocity)
        {
            /*
            * position vector       m
            * velocity              m/sec
            */
            var (X, Y, Z) = Vector3.CrossPrecise(position, velocity);
            return new Vector3((double)X, (double)Y, (double)Z);
        }

        public static Vector3 CalculateNode(Vector3 angularVelocity)
        {
            var (X, Y, Z) = Vector3.CrossPrecise(Vector3.UnitZ, angularVelocity);
            return new Vector3((double)X, (double)Y, (double)Z);
        }

        public static double GetSpecificOrbitalEnergy(double sgp, Vector3 position, Vector3 velocity)
        {
            return velocity.LengthSquared() * 0.5 - sgp / position.Length();
            
        }   
        
        
        /// <summary>
        /// In calculation this is referred to as RAAN or LoAN or Ω
        /// </summary>
        /// <param name="nodeVector">The node vector of the Kepler elements</param>
        /// <returns>Radians as a double</returns>
        public static double CalculateLongitudeOfAscendingNode(Vector3 nodeVector)
        {
            double longitudeOfAscendingNodeLength = nodeVector.X / nodeVector.Length();
            if (double.IsNaN(longitudeOfAscendingNodeLength))
                longitudeOfAscendingNodeLength = 0;
            else
                longitudeOfAscendingNodeLength = GeneralMath.Clamp(longitudeOfAscendingNodeLength, -1, 1);

            double longitudeOfAscendingNode = 0;
            if (longitudeOfAscendingNodeLength != 0)
                longitudeOfAscendingNode = Math.Acos(longitudeOfAscendingNodeLength);

            return longitudeOfAscendingNode;
        }

        #endregion

        #region ArgumentOfPeriapsis

        /// <summary>
        /// DO NOT USE! Gives wrong results in testing, needs fixing.
        /// </summary>
        /// <param name="nodeVector"></param>
        /// <param name="eccentricityVector"></param>
        /// <param name="pos"></param>
        /// <param name="vel"></param>
        /// <returns></returns>
        public static double GetArgumentOfPeriapsis1(Vector3 nodeVector, Vector3 eccentricityVector, Vector3 pos, Vector3 vel)
        {
            //throw new Exception("Broken Math Function, This function should not be used.");
            double aop;
            if (nodeVector.Length() == 0)
            {
                aop = Math.Atan2(eccentricityVector.Y, eccentricityVector.X);
                if (Vector3.Cross(pos, vel).Z < 0)
                    aop = 2 * Math.PI + aop;
            }
            else
            {
                var foo = Vector3.Dot(nodeVector, eccentricityVector);
                var foo2 = nodeVector.Length() * eccentricityVector.Length();
                aop = Math.Acos(foo / foo2);
                if (eccentricityVector.Z < 0)
                    aop = 2 * Math.PI + aop;
            }

            aop = Angle.NormaliseRadians(aop);

            return aop;
        }

        public static double GetArgumentOfPeriapsis(Vector3 pos, double incl, double loAN, double trueAnomaly)
        {
            double Sw = 0;
            double Rx = pos.X;
            double Ry = pos.Y;
            double Rz = pos.Z;
            double R = pos.Length();
            double TA = trueAnomaly;
            var Cw = (Rx * Math.Cos(loAN) + Ry * Math.Sin(loAN)) / R;

            if (incl == 0 || incl == Math.PI)
            {
                Sw = (Ry * Math.Cos(loAN) - Rx * Math.Sin(loAN)) / R;
            }
            else
            {
                Sw = Rz / (R * Math.Sin(incl));
            }

            var W = Math.Atan2(Sw, Cw) - TA;
            if (W < 0)
            {
                W = 2 * Math.PI + W;
            }

            return W;
        }

        /// <summary>
        /// DO NOT USE! Gives wrong results in testing, needs fixing.
        /// </summary>
        /// <param name="inclination"></param>
        /// <param name="eccentricityVector"></param>
        /// <param name="nodeVector"></param>
        /// <returns></returns>
        public static double GetArgumentOfPeriapsis3(double inclination, Vector3 eccentricityVector, Vector3 nodeVector)
        {
            //throw new Exception("Broken Math Function, This function shoudl not be used.");
            double aoP = 0;
            double e = eccentricityVector.Length();
            if (Math.Abs(inclination) < Epsilon)
            {
                if (Math.Abs(e) < Epsilon)
                    aoP = 0;
                else
                    aoP = Math.Acos(eccentricityVector.X / e);
            }
            else
            {
                var foo = Vector3.Dot(nodeVector, eccentricityVector);
                var foo2 = nodeVector.Length() * e;
                aoP = Math.Acos(foo / foo2);
            }

            if (Math.Abs(e) > Epsilon && eccentricityVector.Z < 0)
            {
                aoP = 2 * Math.PI - aoP;
            }

            return aoP;
        }




        #endregion

        #region EccentricityVector

        /// <summary>
        /// https://en.wikipedia.org/wiki/Eccentricity_vector
        /// </summary>
        /// <returns>The vector.</returns>
        /// <param name="sgp">StandardGravParam.</param>
        /// <param name="position">Position, relative to parent.</param>
        /// <param name="velocity">Velocity, relative to parent.</param>
        public static Vector3 EccentricityVector(double sgp, Vector3 position, Vector3 velocity)
        {
            Vector3 angularMomentum = Vector3.Cross(position, velocity);
            Vector3 foo1 = Vector3.Cross(velocity, angularMomentum) / sgp;
            Vector3 foo2 = Vector3.Normalise(position);
            Vector3 E = foo1 - foo2;
            
            if (E.Length() < Epsilon)
            {
                return Vector3.Zero;
            }
            else
                return E;
        }

        /// <summary>
        /// Slighty different way of calculating eccentrictyVector.
        /// keep around till profiling pass to see which is faster. 
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="position"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        public static Vector3 EccentricityVector2(double sgp, Vector3 position, Vector3 velocity)
        {
            //var speed = velocity.Length();
            var radius = position.Length();
            var foo1 = (velocity.LengthSquared() - sgp / radius) * position;
            var foo2 = Vector3.Dot(position, velocity) * velocity;
            var E = (foo1 - foo2) / sgp;
            if (E.Length() < Epsilon)
            {
                return Vector3.Zero;
            }
            else
                return E;
        }

        #endregion

        #region TrueAnomaly

        
        /// <summary>
        /// The True Anomaly in radians
        /// https://en.wikipedia.org/wiki/True_anomaly#From_state_vectors
        /// I think this should be eccentricity agnostic (will work with ellipse and hyperbola)
        /// </summary>
        /// <returns>The True Anomaly in radians</returns>
        /// <param name="eccentVector">Eccentricity vector.</param>
        /// <param name="position">Position relative to parent</param>
        /// <param name="velocity">Velocity relative to parent</param>
        public static double TrueAnomaly(Vector3 eccentVector, Vector3 position, Vector3 velocity)
        {
            double e = eccentVector.Length(); //eccentricity
            double r = position.Length();
            if (e < Epsilon)//circular orbit, assume AoP is 0;
            {
                return Angle.NormaliseRadiansPositive(Math.Atan2(position.Y, position.X)); 
            }

                double dotEccPos = Vector3.Dot(eccentVector, position);
                double talen = e * r;
                talen = dotEccPos / talen;
                talen = GeneralMath.Clamp(talen, -1, 1);
                var trueAnomaly = Math.Acos(talen);

                if (Vector3.Dot(position, velocity) < 0)
                    trueAnomaly = Math.PI * 2 - trueAnomaly;

                return Angle.NormaliseRadiansPositive(trueAnomaly);
            
        }


        /// <summary>
        /// The True Anomaly in radians.
        /// </summary>
        /// <returns>The True Anomaly in radians</returns>
        /// <param name="sgp">Sgp.</param>
        /// <param name="position">Position.</param>
        /// <param name="velocity">Velocity.</param>
        public static double TrueAnomaly(double sgp, Vector3 position, Vector3 velocity)
        {
            Vector3 ev = EccentricityVector(sgp, position, velocity);
            return TrueAnomaly(ev, position, velocity);
        }

        /// <summary>
        /// Works with Elliptical and Hyperbolic.
        /// </summary>
        /// <param name="sgp">Standard Gravitational Parameter</param>
        /// <param name="a">SemiMajorAxis</param>
        /// <param name="e">Eccentricity</param>
        /// <param name="m0">Mean Anomaly at Epoch</param>
        /// <param name="s">Seconds from Epoch</param>
        /// <returns></returns>
        public static double TrueAnomalyFromTime(double sgp, double a, double e, double m0, double s)
        {
            double meanMotion = GetMeanMotion(sgp, a);
            if(e < 1)
            {
                var m1 = GetMeanAnomalyFromTime(m0, meanMotion, s);
                GetEccentricAnomalyNewtonsMethod(e, m1, out double E);
                return TrueAnomalyFromEccentricAnomaly(e, E);
            }
            else
            {
                var m1 = GetHyperbolicMeanAnomalyFromTime(meanMotion, s);
                GetHyperbolicAnomalyNewtonsMethod(e, m1, out double F);
                return TrueAnomalyFromHyperbolicAnomaly(e, F);
            }
        }
        
        public static double TrueAmomalyAtRadius(KeplerElements ke, double r)
        {
            var p = EllipseMath.SemiLatusRectum(ke.SemiMajorAxis, ke.Eccentricity);
            return EllipseMath.TrueAnomalyAtRadus(r, p, ke.Eccentricity);
        }

        public static double TrueAnomalyFromEccentricAnomaly(double eccentricity, double eccentricAnomaly)
        {
            var x = Math.Sqrt(1 - Math.Pow(eccentricity, 2)) * Math.Sin(eccentricAnomaly);
            var y = Math.Cos(eccentricAnomaly) - eccentricity;
            var ta = Angle.NormaliseRadiansPositive(Math.Atan2(x, y));
            if (ta == double.NaN)
                throw new Exception("Is NaN");

            return ta;
        }
        
        public static double TrueAnomalyFromHyperbolicAnomaly(double e, double hyperbolicAnomaly)
        {
            var foo = Math.Sqrt((e + 1) / (e - 1));
            var foo2 = Math.Tanh(hyperbolicAnomaly / 2);
            var ta = 2 * Math.Atan(foo * foo2);
            
            if (ta == double.NaN)
                throw new Exception("Is NaN");

            return Angle.NormaliseRadians(ta);
        }

        #endregion

        
        #region Position

        public static Vector3 GetPosition(double a, double e, double loAN, double aoP, double i, double trueAnomaly)
        {
            var p = EllipseMath.SemiLatusRectum(a, e);
            var r = EllipseMath.RadiusAtTrueAnomaly(trueAnomaly, p, e);

            double angleFromLoAN = trueAnomaly + aoP;
            double x = Math.Cos(loAN) * Math.Cos(angleFromLoAN) - Math.Sin(loAN) * Math.Sin(angleFromLoAN) * Math.Cos(i);
            double y = Math.Sin(loAN) * Math.Cos(angleFromLoAN) + Math.Cos(loAN) * Math.Sin(angleFromLoAN) * Math.Cos(i);
            double z = Math.Sin(i) * Math.Sin(angleFromLoAN);

            return new Vector3(x, y, z) * r;
        }
        
        public static Vector3 GetRelativePosition(double lofAN, double aoP, double incl, double trueAnomaly, double radius)
        {
            double angle = trueAnomaly + aoP;
            double x = Math.Cos(lofAN) * Math.Cos(angle) - Math.Sin(lofAN) * Math.Sin(angle) * Math.Cos(incl);
            double y = Math.Sin(lofAN) * Math.Cos(angle) + Math.Cos(lofAN) * Math.Sin(angle) * Math.Cos(incl);
            double z = Math.Sin(incl) * Math.Sin(angle);

            return new Vector3(x, y, z) * radius;
        }

        /// <summary>
        /// Position using time (relatively computationaly expensive)
        /// </summary>
        /// <param name="ke"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static Vector3 GetRelativePosition(KeplerElements ke, DateTime dateTime)
        {
            var secondsFromEpoch = (dateTime - ke.Epoch).TotalSeconds;
            double lofAN = ke.LoAN;
            double i = ke.Inclination;
            double e = ke.Eccentricity;
            double a = ke.SemiMajorAxis;

            double trueAnomaly = 0;
            if(e < 1)
            {
                var meanAnomaly = GetMeanAnomalyFromTime(ke.MeanAnomalyAtEpoch, ke.MeanMotion, secondsFromEpoch);
                GetEccentricAnomalyNewtonsMethod(ke.Eccentricity, meanAnomaly, out var eccAnom);
                trueAnomaly = TrueAnomalyFromEccentricAnomaly(ke.Eccentricity, eccAnom);
            }
            else
            {
                var hyperbolicMeanAnomaly = GetHyperbolicMeanAnomalyFromTime(ke.StandardGravParameter, a, secondsFromEpoch);
                GetHyperbolicAnomalyNewtonsMethod(e, hyperbolicMeanAnomaly, out double hyperbolicAnomalyF);
                trueAnomaly = TrueAnomalyFromHyperbolicAnomaly(e, hyperbolicAnomalyF);
            }
            
            double angle = trueAnomaly + ke.AoP;

            double x = Math.Cos(lofAN) * Math.Cos(angle) - Math.Sin(lofAN) * Math.Sin(angle) * Math.Cos(i);
            double y = Math.Sin(lofAN) * Math.Cos(angle) + Math.Cos(lofAN) * Math.Sin(angle) * Math.Cos(i);
            double z = Math.Sin(i) * Math.Sin(angle);
            double radius = a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly));
            return new Vector3(x, y, z) * radius;
        }

        /// <summary>
        /// Another way of getting position, untested, currently unused, copied from somehwere on the net. 
        /// Untested
        /// </summary>
        /// <returns>The position.</returns>
        /// <param name="combinedMass">Parent + object mass</param>
        /// <param name="semiMajAxis">SemiMajorAxis.</param>
        /// <param name="meanAnomaly">Mean anomaly.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="aoP">ArgumentOfPeriapsis.</param>
        /// <param name="loAN">LongditudeOfAccendingNode.</param>
        /// <param name="i"> inclination </param>
        public static Vector3 Pos(double combinedMass, double semiMajAxis, double meanAnomaly, double eccentricity, double aoP, double loAN, double i)
        {
            var G = 6.6725985e-11;


            double eca = meanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = 0;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - meanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }

            var ceca = Math.Cos(eca);
            var seca = Math.Sin(eca);
            e1 = semiMajAxis * Math.Sqrt(Math.Abs(1 - eccentricity * eccentricity));
            var xw = semiMajAxis * (ceca - eccentricity);
            var yw = e1 * seca;

            var edot = Math.Sqrt((G * combinedMass) / semiMajAxis) / (semiMajAxis * (1 - eccentricity * ceca));
            var xdw = -semiMajAxis * edot * seca;
            var ydw = e1 * edot * ceca;

            var Cw = Math.Cos(aoP);
            var Sw = Math.Sin(aoP);
            var co = Math.Cos(loAN);
            var so = Math.Sin(loAN);
            var ci = Math.Cos(i);
            var si = Math.Sin(i);
            var swci = Sw * ci;
            var cwci = Cw * ci;
            var pX = Cw * co - so * swci;
            var pY = Cw * so + co * swci;
            var pZ = Sw * si;
            var qx = -Sw * co - so * cwci;
            var qy = -Sw * so + co * cwci;
            var qz = Cw * si;

            return new Vector3()
            {
                X = xw * pX + yw * qx,
                Y = xw * pY + yw * qy,
                Z = xw * pZ + yw * qz
            };
        }
        

        #endregion
        
        
        #region VelocityAndSpeed;


        /// <summary>
        /// Instantanious Orbital Veclocity
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="position"></param>
        /// <param name="sma"></param>
        /// <param name="eccentricity"></param>
        /// <param name="trueAnomaly"></param>
        /// <param name="arguemntOfPeriapsis"></param>
        /// <param name="inclination"></param>
        /// <param name="loAN"></param>
        /// <returns></returns>
        public static Vector3 ParentLocalVeclocityVector(double sgp, Vector3 position, double sma, double eccentricity, double trueAnomaly, double arguemntOfPeriapsis, double inclination, double loAN)
        {
            //TODO: is it worth storing the resulting matrix somewhere, and then just doing the transform on it?
            //since loAN and incl don't change, it could be stored in orbitDB if we're doing this often enoguh. 
            var orbitLocal = (Vector3)ObjectLocalVelocityVector(sgp, position, sma, eccentricity, trueAnomaly, arguemntOfPeriapsis);

            var mtxLoAN = Matrix3d.IDRotateZ(-loAN);
            var mtxincl = Matrix3d.IDRotateX(inclination);

            var mtx = mtxLoAN * mtxincl;

            var transformedVector = mtx.Transform(orbitLocal);
            return transformedVector;

        }

        /// <summary>
        /// Converts a prograde relative (Y is prograde, x is radial, z is normal) to it's parent relative vector
        /// (ie psudo north east south west)
        /// </summary>
        /// <param name="progradeVector"></param>
        /// <param name="trueAnomaly"></param>
        /// <param name="aop"></param>
        /// <param name="loAN"></param>
        /// <param name="inclination"></param>
        /// <returns></returns>
        public static Vector3 ProgradeToStateVector(Vector3 progradeVector, double trueAnomaly, double aop, double loAN, double inclination)
        {
            var mtxTruA = Matrix3d.IDRotateZ(-trueAnomaly);
            var mtxaop = Matrix3d.IDRotateZ(-aop);
            var mtxLoAN = Matrix3d.IDRotateZ(-loAN);
            var mtxincl = Matrix3d.IDRotateX(inclination);

            var mtx = mtxLoAN * mtxincl * mtxTruA * mtxaop;

            var transformedVector = mtx.Transform(progradeVector);
            return transformedVector;
        }

        public static Vector3 ProgradeToStateVector(Vector3 progradeVector, KeplerElements ke)
        {
            var mtxTruA = Matrix3d.IDRotateZ(-ke.TrueAnomalyAtEpoch);
            var mtxaop = Matrix3d.IDRotateZ(-ke.Apoapsis);
            var mtxLoAN = Matrix3d.IDRotateZ(-ke.LoAN);
            var mtxincl = Matrix3d.IDRotateX(ke.Inclination);

            var mtx = mtxLoAN * mtxincl * mtxTruA * mtxaop;

            var transformedVector = mtx.Transform(progradeVector);
            return transformedVector;
        }

        /// <summary>
        /// Converts a prograde relative (Y is prograde, x is radial, z is normal) to it's parent relative vector
        /// (ie psudo north east south west)
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="progradeVector"></param>
        /// <param name="position"></param>
        /// <param name="currentVelocityVector"></param>
        /// <returns></returns>
        public static Vector3 ProgradeToStateVector(double sgp, Vector3 progradeVector, Vector3 position, Vector3 currentVelocityVector)
        {
            Vector3 angularVelocity = Vector3.Cross(position, currentVelocityVector);
            Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);
            var loAN = CalculateLongitudeOfAscendingNode(nodeVector);
            var trueAnomaly = OrbitalMath.TrueAnomaly(sgp, position, currentVelocityVector);

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. or pi if counter clockwise orbit. 
            if (double.IsNaN(inclination))
                inclination = 0;
            var aop = OrbitalMath.GetArgumentOfPeriapsis(position, inclination, loAN, trueAnomaly);

            return ProgradeToStateVector(progradeVector, trueAnomaly, aop, loAN, inclination);
        }


        /// <summary>
        /// Converts parent to prograde vector
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="trueAnomaly"></param>
        /// <param name="aop"></param>
        /// <param name="loAN"></param>
        /// <param name="inclination"></param>
        /// <returns></returns>
        public static Vector3 StateToProgradeVector(Vector3 vector, double trueAnomaly, double aop, double loAN, double inclination)
        {
            var mtxTruA = Matrix3d.IDRotateZ(trueAnomaly);
            var mtxaop = Matrix3d.IDRotateZ(aop);
            var mtxLoAN = Matrix3d.IDRotateZ(loAN);
            var mtxincl = Matrix3d.IDRotateX(-inclination);

            var mtx = mtxaop * mtxTruA * mtxincl * mtxLoAN;

            var transformedVector = mtx.Transform(vector);
            return transformedVector;
        }

        public static Vector3 ProgradeVector(KeplerElements ke, DateTime dateTime )
        {
            var secondsFromEpoch = (dateTime - ke.Epoch).TotalSeconds;
            var sgp = ke.StandardGravParameter;
            double lofAN = ke.LoAN;
            double i = ke.Inclination;
            double e = ke.Eccentricity;
            double a = ke.SemiMajorAxis;
            var meanAnomaly = GetMeanAnomalyFromTime(ke.MeanAnomalyAtEpoch, ke.MeanMotion, secondsFromEpoch);
            GetEccentricAnomalyNewtonsMethod(ke.Eccentricity, meanAnomaly, out var eccAnom);
            var trueAnomaly = TrueAnomalyFromEccentricAnomaly(ke.Eccentricity, eccAnom);
            double angleToObj = trueAnomaly + ke.AoP;

            double x = Math.Cos(lofAN) * Math.Cos(angleToObj) - Math.Sin(lofAN) * Math.Sin(angleToObj) * Math.Cos(i);
            double y = Math.Sin(lofAN) * Math.Cos(angleToObj) + Math.Cos(lofAN) * Math.Sin(angleToObj) * Math.Cos(i);
            double z = Math.Sin(i) * Math.Sin(angleToObj);
            double radius = a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly));
            var position = new Vector3(x, y, z) * radius;

            var spd = InstantaneousOrbitalSpeed(sgp, radius, ke.SemiMajorAxis);

            return new Vector3(0, spd, 0);

        }

        /// <summary>
        /// Converts parent to prograde vector
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="orbitLocalVec"></param>
        /// <param name="position"></param>
        /// <param name="currentVelocityVector"></param>
        /// <returns></returns>
        public static Vector3 StateToProgradeVector(double sgp, Vector3 orbitLocalVec, Vector3 position, Vector3 currentVelocityVector)
        {
            Vector3 angularVelocity = Vector3.Cross(position, currentVelocityVector);
            Vector3 nodeVector = Vector3.Cross(new Vector3(0, 0, 1), angularVelocity);
            var loAN = CalculateLongitudeOfAscendingNode(nodeVector);
            var trueAnomaly = OrbitalMath.TrueAnomaly(sgp, position, currentVelocityVector);

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. or pi if counter clockwise orbit. 
            if (double.IsNaN(inclination))
                inclination = 0;
            var aop = OrbitalMath.GetArgumentOfPeriapsis(position, inclination, loAN, trueAnomaly);

            return StateToProgradeVector(orbitLocalVec, trueAnomaly, aop, loAN, inclination);
        }



        /// <summary>
        /// Instantanious Orbital Velocity
        /// </summary>
        /// <returns>The orbital vector relative to the Argument Of Periapsis</returns>
        /// <param name="sgp">Standard Grav Perameter. in AU</param>
        /// <param name="position">relative Position.</param>
        /// <param name="sma">SemiMajorAxis</param>
        public static Vector2 ObjectLocalVelocityVector(double sgp, Vector3 position, double sma, double eccentricity, double trueAnomaly, double argumentOfPeriapsis)
        {
            (double speed, double angle) = ObjectLocalVelocityPolar(sgp, position, sma, eccentricity, trueAnomaly, argumentOfPeriapsis);
            var v = new Vector2()
            {
                X = Math.Cos(angle) * speed,
                Y = Math.Sin(angle) * speed
            };

            if (double.IsNaN(v.X) || double.IsNaN(v.Y))
                throw new Exception("Result is NaN");

            return v;
        }
        

        /// <summary>
        /// returns state vectors, TODO velocity vector should be 3d. TODO Use Orbit.StateVectors
        /// As this uses newtonion method to calculate eccentricAnomaly, this is relatively expensive. 
        /// </summary>
        /// <param name="ke"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (Vector3 position, Vector2 velocity) GetStateVectors(KeplerElements ke, DateTime dateTime)
        {
            var secondsFromEpoch = (dateTime - ke.Epoch).TotalSeconds;
            var sgp = ke.StandardGravParameter;
            double lofAN = ke.LoAN;
            double i = ke.Inclination;
            double e = ke.Eccentricity;
            double a = ke.SemiMajorAxis;
            
            double trueAnomaly;

            if (e < 1)
            {
                double meanAnomaly = GetMeanAnomalyFromTime(ke.MeanAnomalyAtEpoch, ke.MeanMotion, secondsFromEpoch);
                GetEccentricAnomalyNewtonsMethod(ke.Eccentricity, meanAnomaly, out double eccAnom);
                trueAnomaly = TrueAnomalyFromEccentricAnomaly(ke.Eccentricity, eccAnom);
            }
            else
            {
                var quotient = sgp / Math.Pow(-a, 3);
                var hyperbolcMeanMotion = Math.Sqrt(quotient);
                var hyperbolicMeanAnomaly = secondsFromEpoch * hyperbolcMeanMotion;
                GetHyperbolicAnomalyNewtonsMethod(e, hyperbolicMeanAnomaly, out double hyperbolicAnomalyF);
                trueAnomaly = TrueAnomalyFromHyperbolicAnomaly(e, hyperbolicAnomalyF);

            }

            double angleToObj = trueAnomaly + ke.AoP;

            double x = Math.Cos(lofAN) * Math.Cos(angleToObj) - Math.Sin(lofAN) * Math.Sin(angleToObj) * Math.Cos(i);
            double y = Math.Sin(lofAN) * Math.Cos(angleToObj) + Math.Cos(lofAN) * Math.Sin(angleToObj) * Math.Cos(i);
            double z = Math.Sin(i) * Math.Sin(angleToObj);
            double radius = a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly));
            var position = new Vector3(x, y, z) * radius;
            
            (double speed, double headingAngle) = ObjectLocalVelocityPolar(sgp, position, a, e, trueAnomaly, ke.AoP);
            var v = new Vector2()
            {
                X = Math.Cos(headingAngle) * speed,
                Y = Math.Sin(headingAngle) * speed
            };

            if (double.IsNaN(v.X) || double.IsNaN(v.Y))
                throw new Exception("Result is NaN");

            return (position, v);

        }

        /// <summary>
        /// This returns the heading measured from the periapsis (AoP) in radians
        /// Add the LoP to this to get the true heading in a 2d orbit. 
        /// </summary>
        /// <returns>The orbital velocity polar coordinate.</returns>
        /// <param name="sgp">Sgp.</param>
        /// <param name="position">Position.</param>
        /// <param name="semiMajorAxis">Semi major axis.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="trueAnomaly">True anomaly.</param>
        public static (double speed, double heading) ObjectLocalVelocityPolar(double sgp, Vector3 position, double semiMajorAxis, double eccentricity, double trueAnomaly, double argumentOfPeriapsis)
        {
            var radius = position.Length();
            var spd = InstantaneousOrbitalSpeed(sgp, radius, semiMajorAxis);

            var heading = ObjectLocalHeading(position, eccentricity, semiMajorAxis, trueAnomaly, argumentOfPeriapsis);

            return (spd, heading);
        }

        /// <summary>
        /// Heading on the orbital plane.
        /// </summary>
        /// <returns>The from periaps.</returns>
        /// <param name="pos">Position.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="semiMajAxis">Semi major axis.</param>
        /// <param name="trueAnomaly">True anomaly.</param>
        /// <param name="aoP">Argument Of Periapsis</param>
        /// 
        public static double ObjectLocalHeading(Vector3 pos, double eccentricity, double semiMajAxis, double trueAnomaly, double aoP)
        {

            double r = pos.Length();
            double a = semiMajAxis;
            double e = eccentricity;
            double k = r / a;
            double f = trueAnomaly;

            double bar = ((2 - 2 * e * e) / (k * (2 - k))) - 1;
            double foo = GeneralMath.Clamp(bar, -1, 1);
            double alpha = Math.Acos(foo);
            if (trueAnomaly > Math.PI || trueAnomaly < 0)
                alpha = -alpha;
            double heading = ((Math.PI - alpha) / 2) + f;
            heading += aoP;
            Angle.NormaliseRadiansPositive(heading);
            return heading;

        }

        /// <summary>
        /// returns the speed for an object of a given mass at a given radius from a body. this is the vis-viva calculation
        /// </summary>
        /// <returns>The orbital speed, relative to the parent</returns>
        /// <param name="standardGravParameter">standardGravParameter.</param>
        /// <param name="distance">Radius.</param>
        /// <param name="semiMajAxis">Semi maj axis.</param>
        public static double InstantaneousOrbitalSpeed(double standardGravParameter, double distance, double semiMajAxis)
        {
            var foo = Math.Abs(2 / distance - 1 / semiMajAxis);
            var spd = Math.Sqrt(standardGravParameter * foo);
            if (double.IsNaN(spd))
                throw new Exception("Speed Result is NaN");
            return spd;
        }

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future. 
        /// Fairly slow and inefficent. 
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        public static double Hackspeed(KeplerElements orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessorBase.GetPosition(orbit, atDatetime - TimeSpan.FromSeconds(0.5));
            var pos2 = OrbitProcessorBase.GetPosition(orbit, atDatetime + TimeSpan.FromSeconds(0.5));

            return Distance.DistanceBetween(pos1, pos2);
        }

        public static double HackVelocityHeading(KeplerElements orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessorBase.GetPosition(orbit, atDatetime - TimeSpan.FromSeconds(0.5));
            var pos2 = OrbitProcessorBase.GetPosition(orbit, atDatetime + TimeSpan.FromSeconds(0.5));

            Vector3 vector = pos2 - pos1;
            double heading = Math.Atan2(vector.Y, vector.X);
            return heading;
        }

        public static Vector3 HackVelocityVector(KeplerElements orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessorBase.GetPosition(orbit, atDatetime - TimeSpan.FromSeconds(0.5));
            var pos2 = OrbitProcessorBase.GetPosition(orbit, atDatetime + TimeSpan.FromSeconds(0.5));
            //double speed = Distance.DistanceBetween(pos1, pos2);
            return pos2 - pos1;
        }

        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit. 
        /// </summary>
        /// <returns>The orbital velocity in au.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInAU(KeplerElements orbit)
        {
            return Distance.MToAU(MeanOrbitalVelocityInm(orbit));
        }

        public static double MeanOrbitalVelocityInm(KeplerElements orbit)
        {
            double a = orbit.SemiMajorAxis;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.Period;
            double peremeter = Math.PI * (3 * (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return peremeter / orbitalPerodSeconds;
        }

        #endregion

        #region EccentricAnomaly



        /// <summary>
        /// Gets the eccentric anomaly.
        /// This can take a number of itterations to calculate so may not be fast. 
        /// </summary>
        /// <returns>E</returns>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="currentMeanAnomaly">Current mean anomaly.</param>
        public static bool GetEccentricAnomalyNewtonsMethod(double eccentricity, double currentMeanAnomaly, out double eccentricAnomaly)
        {
            bool converges = true;
            //Kepler's Equation
            const int numIterations = 1000;
            var e = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            if (eccentricity > 0.8)
            {
                e[i] = Math.PI;
            }
            else
            {
                e[i] = currentMeanAnomaly;
            }

            do
            {
                // Newton's Method.
                /*                   E(n) - e sin(E(n)) - M(t)
                 * E(n+1) = E(n) - ( ------------------------- )
                 *                        1 - e cos(E(n)
                 * 
                 * E == EccentricAnomaly, e == Eccentricity, M == MeanAnomaly.
                 * http://en.wikipedia.org/wiki/Eccentric_anomaly#From_the_mean_anomaly
                */
                e[i + 1] = e[i] - (e[i] - eccentricity * Math.Sin(e[i]) - currentMeanAnomaly) / (1 - eccentricity * Math.Cos(e[i]));
                i++;
            } while (Math.Abs(e[i] - e[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                converges = false;
            }

            eccentricAnomaly = Angle.NormaliseRadiansPositive( e[i - 1]);
            return converges;
        }

        public static double GetEccentricAnomalyNewtonsMethod2(double eccentricity, double currentMeanAnomaly)
        {
            double eca = currentMeanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = currentMeanAnomaly;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - currentMeanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }
            return eca;
        }

        /// <summary>
        /// Calculates the Eccentric Anomaly given True Anomaly and eccentricity. 
        /// </summary>
        /// <param name="trueAnomaly">Should be a normalised angle between 2pi and -2pi</param>
        /// <param name="eccentricity"></param>
        /// <returns></returns>
        public static double GetEccentricAnomalyFromTrueAnomaly(double trueAnomaly, double eccentricity)
        {
            var E = Math.Acos((Math.Cos(trueAnomaly) + eccentricity) / (1 + eccentricity * Math.Cos(trueAnomaly)));
            if (trueAnomaly > Math.PI || trueAnomaly < 0 && trueAnomaly > -Math.PI)
                E = -E;
            return Angle.NormaliseRadiansPositive(E);
        }

        /*
                public static double GetEccentricAnomalyFromStateVectors(Vector3 position, double semiMajAxis, double linierEccentricity, double aop)
                {
                    var x = (position.X * Math.Cos(aop)) + (position.Y * Math.Sin(aop));
                    x = linierEccentricity + x;
                    double foo = GeneralMath.Clamp(x / semiMajAxis, -1, 1); //because sometimes we were getting a floating point error that resulted in numbers infinatly smaller than -1
                    return Math.Acos(foo);
                }

                /// <summary>
                /// Note: Will fail on circular (eccentricity aproaching 0) orbits (ie will return inconsistant values)
                /// </summary>
                /// <param name="sgp"></param>
                /// <param name="semiMajorAxis"></param>
                /// <param name="position"></param>
                /// <param name="velocity"></param>
                /// <returns></returns>
                public static double GetEccentricAnomalyFromStateVectors2(double sgp, double semiMajorAxis, Vector3 position, Vector3 velocity)
                {
                    var radius = position.Length();
                    var q = Vector3.Dot(position, velocity);
                    var Ex = 1 - radius / semiMajorAxis;
                    var Ey = q / Math.Sqrt(semiMajorAxis * sgp);
                    var E = Math.Atan2(Ey, Ex); // eccentric anomoly 
                    return E;
                }
        */
        
        /// <summary>
        /// known as F.
        /// This can take a number of itterations to calculate so may not be fast. 
        /// </summary>
        /// <returns>H</returns>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="currentMeanAnomaly">Current mean anomaly.</param>
        public static bool GetHyperbolicAnomalyNewtonsMethod(double eccentricity, double hyperbolicMeanAnomaly, out double hyperbolicAnomalyF)
        {
            bool converges = true;
            //Kepler's Equation
            const int numIterations = 1000;
            var F = new double[numIterations];
            const double epsilon = 1E-12; // Plenty of accuracy.
            int i = 0;

            F[i] = hyperbolicMeanAnomaly;

            do
            {
                // Newton's Method.
                /*                   M - e sinh(H(n)) + H(n)
                 * H(n+1) = H(n) + ( ------------------------- )
                 *                        e cosh(H(n) - 1
                 *
                 * H == HyperbolicAnomaly, e == Eccentricity, M == MeanAnomaly.
                 */
                var dividend = hyperbolicMeanAnomaly - eccentricity * Math.Sinh(F[i]) + F[i];
                var divisor = eccentricity * Math.Cosh(F[i]) - 1;
                
                F[i + 1] = F[i] + dividend / divisor;
                i++;
            } while (Math.Abs(F[i] - F[i - 1]) > epsilon && i + 1 < numIterations);

            if (i + 1 >= numIterations)
            {
                converges = false;
            }
            //***!!! hyperbolic Anomaly is NOT an angle and shouldn't be normalised !!!***
            //hyperbolicAnomalyF = Angle.NormaliseRadiansPositive(F[i - 1]);
            hyperbolicAnomalyF = F[i - 1];
            return converges;
        }
        
        #endregion

        #region MeanAnomaly

        /// <summary>
        /// calculates mean motion, works with elliptic and hyperbolic orbits (where a is negitive)
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="a"></param>
        /// <returns>n</returns>
        public static double GetMeanMotion(double sgp, double a)
        {
            double n;
            if(a > 0)
                n = Math.Sqrt(sgp / Math.Pow(a, 3)); // Calculated in radians.
            else
                n = Math.Sqrt(sgp / Math.Pow(-a, 3));
            return n;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="eccentricAnomaly"></param>
        /// <returns>M</returns>
        public static double GetEllipticMeanAnomaly(double e, double eccentricAnomaly)
        {
            return eccentricAnomaly - e * Math.Sin(eccentricAnomaly);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e">eccentricity </param>
        /// <param name="hyperbolicAnomaly">aka H</param>
        /// <returns>Mh</returns>
        public static double GetHyperbolicMeanAnomaly(double e, double hyperbolicAnomaly)
        {
            return e * Math.Sinh(hyperbolicAnomaly) - hyperbolicAnomaly;
        }
        
        
        /// <summary>
        /// Calculates CurrentMeanAnomaly
        /// </summary>
        /// <returns>M</returns>
        /// <param name="meanAnomalyAtEpoch">InRadians.</param>
        /// <param name="meanMotion">InRadians/s.</param>
        /// <param name="secondsFromEpoch">Seconds from epoch.</param>
        public static double GetMeanAnomalyFromTime(double meanAnomalyAtEpoch, double meanMotion, double secondsFromEpoch)
        {
            // http://en.wikipedia.org/wiki/Mean_anomaly (M = M0 + nT)  
            double currentMeanAnomaly = meanAnomalyAtEpoch + (meanMotion * secondsFromEpoch);
            // Large nT can cause meanAnomaly to go past 2*Pi. Roll it down. It shouldn't, because timeSinceEpoch should be tapered above, but it has.
            currentMeanAnomaly = Angle.NormaliseRadiansPositive(currentMeanAnomaly); //currentMeanAnomaly % (Math.PI * 2);
            return currentMeanAnomaly;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="meanMotion"></param>
        /// <param name="secondsFromEpoch"></param>
        /// <returns>Mh</returns>
        public static double GetHyperbolicMeanAnomalyFromTime(double meanMotion, double secondsFromEpoch)
        {
            return  secondsFromEpoch * meanMotion;
        }

        #endregion

        
        #region Time

        public static double GetOrbitalPeriodInSeconds(double sgp, double semiMajorAxis)
        {
            return 2 * Math.PI * Math.Sqrt(Math.Pow(semiMajorAxis, 3) / sgp);
        }
        public static TimeSpan GetOrbitalPeriodAsTimeSpan(double sgp, double SemiMajorAxis)
        {
            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            TimeSpan period;
            double orbitalPeriod = GetOrbitalPeriodInSeconds(sgp, SemiMajorAxis);
            if (orbitalPeriod * 10000000 > long.MaxValue)
            {
                period = TimeSpan.MaxValue;
            }
            else
            {
                period = TimeSpan.FromSeconds(orbitalPeriod);
            }
            return period;
        }

        /// <summary>
        /// Returns the TimeFromPeriapsis
        /// </summary>
        /// <returns>time from periapsis.</returns>
        /// <param name="semiMaj">Semi maj.</param>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="currentMeanAnomaly">Mean anomaly current.</param>
        public static double TimeFromPeriapsis(double semiMaj, double standardGravParam, double currentMeanAnomaly)
        {
            return Math.Pow((Math.Pow(semiMaj, 3) / standardGravParam), 0.5) * currentMeanAnomaly;
        }

        /// <summary>
        /// Alternate way to get TimeFromPeriapsis
        /// Doesn't work with Hypobolic orbits due to period being undefined. 
        /// </summary>
        /// <returns>The epoch.</returns>
        /// <param name="semiMaj">Semi maj.</param>
        /// <param name="semiMin">Semi minimum.</param>
        /// <param name="eccentricAnomaly">Eccentric anomaly.</param>
        /// <param name="Period">Period.</param>
        public static double TimeFromPeriapsis(double semiMaj, double semiMin, double eccentricAnomaly, double Period)
        {

            double areaOfEllipse = semiMaj * semiMin * Math.PI;
            double eccentricAnomalyArea = EllipseMath.AreaOfEllipseSector(semiMaj, semiMaj, 0, eccentricAnomaly); //we get the area as if it's a circile. 
            double trueArea = semiMin / semiMaj * eccentricAnomalyArea; //we then multiply the result by a fraction of b / a
                                                                        //double areaOfSegment = EllipseMath.AreaOfEllipseSector(semiMaj, semiMin, 0, lop + trueAnomaly);

            double t = Period * (trueArea / areaOfEllipse);

            return t;

        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="meanAnomaly">M</param>
        /// <param name="meanMotion">n</param>
        /// <returns>seconds from epoch</returns>
        public static double TimeFromEllipticMeanAnomaly(double meanAomalyAtEpoch, double meanAnomaly, double meanMotion)
        {
            var angle = Angle.NormaliseRadiansPositive(meanAnomaly - meanAomalyAtEpoch);
            return angle / meanMotion;
        }
        
        public static double TimeFromHyperbolicMeanAnomaly(double sgp, double a, double hyperbolicMeanAnomaly)
        {
            var foo = Math.Pow(-a, 3) / sgp;
            return hyperbolicMeanAnomaly * Math.Sqrt(foo);
        }
        public static double TimeFromHyperbolicMeanAnomaly(KeplerElements ke, double hyperbolicMeanAnomaly)
        {
            var foo = Math.Pow(-ke.SemiMajorAxis, 3) / ke.StandardGravParameter;
            return hyperbolicMeanAnomaly * Math.Sqrt(foo);
        }

        public static double TimeHyperbolicFromEpochToPeriaps(KeplerElements ke)
        {
            double p = EllipseMath.SemiLatusRectum(ke.SemiMajorAxis, ke.Eccentricity);
            var trueAnomalyAtPeriaps = EllipseMath.TrueAnomalyAtRadus(ke.Periapsis, p, ke.Eccentricity);
            var ha = GetHyperbolicAnomalyFromTrueAnomaly(ke.Eccentricity, trueAnomalyAtPeriaps);
            var hma = GetHyperbolicMeanAnomaly(ke.Eccentricity, ha);
            return TimeFromHyperbolicMeanAnomaly(ke, hma);
        }

        public static double TimeHyperbolicToTrueAnomalyFromPeriaps(KeplerElements ke, double trueAnomaly)
        {
            var t1 = TimeHyperbolicFromEpochToPeriaps(ke);
            
            var ha = GetHyperbolicAnomalyFromTrueAnomaly(ke.Eccentricity, trueAnomaly);
            var hma = GetHyperbolicMeanAnomaly(ke.Eccentricity, ha);
            var t2 = TimeFromHyperbolicMeanAnomaly(ke, hma);

            return t2 - t1;
        }

        public static double GetLongditudeOfPeriapsis(double inclination, double aoP, double loAN)
        {
            double lop;
            if (inclination > Math.PI * 0.5 && inclination < Math.PI * 1.5)
            {

                lop = loAN - aoP;
            }
            else
            {

                lop = loAN + aoP;
            }
            return lop;
        }
        
        public static double TimeFromTrueAnomalyElliptic(double e, double o_M0, double n, double trueAnomaly)
        {
            if (e < 1)
            {
                var o_E = GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, e);
                var o_M = GetEllipticMeanAnomaly(e, o_E);
                return TimeFromEllipticMeanAnomaly(o_M0, o_M, n);
            }

            throw new Exception("This Function requres e < 1");
        }
        
        public static double TimeFromTrueAnomalyHyperbolic(double sgp, double a, double e, double trueAnomaly)
        {
            if (e >= 1)
            {
                var o_H = GetHyperbolicAnomalyFromTrueAnomaly(e, trueAnomaly);
                var o_Mh = GetHyperbolicMeanAnomaly(e, o_H);
                return TimeFromHyperbolicMeanAnomaly(sgp, a, o_Mh);
            }
            throw new Exception("This Function requres e >= 1");
        }
        
        public static double TimeFromTrueAnomaly(KeplerElements ke, double trueAnomaly)
        {
            double e = ke.Eccentricity;
            double n = ke.MeanMotion;
            double o_M0 = ke.MeanAnomalyAtEpoch;
            double sgp = ke.StandardGravParameter;
            double a = ke.SemiMajorAxis;
            
            if (e < 1)
            {
                var o_E = GetEccentricAnomalyFromTrueAnomaly(trueAnomaly, e);
                var o_M = GetEllipticMeanAnomaly(e, o_E);
                return TimeFromEllipticMeanAnomaly(o_M0, o_M, n);
            }
            else
            {
                var o_H = GetHyperbolicAnomalyFromTrueAnomaly(e, trueAnomaly);
                var o_Mh = GetHyperbolicMeanAnomaly(e, o_H);
                return TimeFromHyperbolicMeanAnomaly(sgp, a, o_Mh);
            }
        }

        #endregion


        #region HyperBolicFunctions
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="trueAnomaly"></param>
        /// <returns>H</returns>
        public static double GetHyperbolicAnomalyFromTrueAnomaly(double e, double trueAnomaly)
        {
            var foo = Math.Sqrt((e - 1) / (e + 1));
            var foo2 = Math.Tan(trueAnomaly / 2);
            var hyperbolicAnomaly = 2 * Math.Atanh(foo * foo2);
            //hyperboilc anomaly is not an angle so don't normalise
            //return Angle.NormaliseRadians(hyperbolicAnomaly);
            return hyperbolicAnomaly;
        }
        public static double GetHyperbolicMeanAnomalyFromTime(double sgp, double a, double secondsFromEpoch)
        {
            var n = GetMeanMotion(sgp, a);
            return  secondsFromEpoch * n;
        }


        
        
        #endregion
        
        /// <summary>
        /// Gets the sphere of influence radius of a given body
        /// </summary>
        /// <returns>The sphere of influence (SOI) radius in whatever units you feed the semiMajorAxis.</returns>
        /// <param name="semiMajorAxis">Semi major axis of the body we're getting soi for and it's parent</param>
        /// <param name="mass">Mass of the body we're getting the soi for</param>
        /// <param name="parentMass">Parent mass. ie the sun</param>
        public static double GetSOI(double semiMajorAxis, double mass, double parentMass)
        {
            return Math.Abs( semiMajorAxis * Math.Pow((mass / parentMass), 0.4));
        }



        /// <summary>
        /// Returns the LoP in 2d space (a retrograde orbits aop will be sign switched)
        /// </summary>
        /// <returns>The of periapsis2d.</returns>
        /// <param name="loAN">Lo an.</param>
        /// <param name="aoP">Ao p.</param>
        /// <param name="inclination">Inclination.</param>
        public static double LonditudeOfPeriapsis2d(double loAN, double aoP, double inclination)
        {
            if (inclination > Math.PI * 0.5 && inclination < Math.PI * 1.5)
            {
                aoP = -aoP;
            }
            return loAN + aoP;
        }

        /// <summary>
        /// Tsiolkovsky's rocket equation.
        /// </summary>
        /// <returns>deltaV</returns>
        /// <param name="wetMass">Wet mass.</param>
        /// <param name="dryMass">Dry mass.</param>
        /// <param name="ve">ExhaustVelocity, not isp</param>
        public static double TsiolkovskyRocketEquation(double wetMass, double dryMass, double ve)
        {
            if (wetMass == 0)
                return 0;
            double deltaV = ve * Math.Log(wetMass / dryMass);
            return deltaV;
        }

        /// <summary>
        /// Tsiolkovsky's rocket equation.
        /// use this to calculate the fuel cost of a given deltaV use.
        /// you will need to know the mass of the ship "after" the burn for the "dryMass"
        /// </summary>
        /// <param name="dryMass">payload in kg</param>
        /// <param name="ve">ExhaustVelocity, not isp</param>
        /// <param name="deltaV">DeltaV to burn</param>
        /// <returns>Fuel Cost</returns>
        public static double TsiolkovskyFuelCost(double dryMass, double ve, double deltaV)
        {
            double wetMass = dryMass * Math.Exp(deltaV / ve);
            double fuelUse = wetMass - dryMass;
            return fuelUse;
        }

        /// <summary>
        /// Tsiolkovsky's rocket equation.
        /// Use this to calculate the amount of fuel you will use when you know how much fuel you have availible.
        /// </summary>
        /// <param name="wetMass">Total mass of ship and fuel</param>
        /// <param name="ve">ExhaustVelocity, not isp</param>
        /// <param name="deltaV">DeltaV to use</param>
        /// <returns>Fuel to burn</returns>
        public static double TsiolkovskyFuelUse(double wetMass, double ve, double deltaV)
        {
            //Step by step math.
            //dv = ve * log(wet/dry)
            //dv / ve = log(wet/dry)
            //dv / log(wet/dry) = ve

            //double b = deltaV / ve;
            //double a = Math.Exp(b);
            //double dryMass = wetMass / a;
            //double fuelUse = wetMass - dryMass;

            double dryMass = wetMass / Math.Exp(deltaV / ve);

            double fuelUse = wetMass - dryMass;

            return fuelUse;
        }
        
        struct Orbit
        {
            public Vector3 position;
            public double T;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moverAbsolutePos"></param>
        /// <param name="speed"></param>
        /// <param name="targetEntity"></param>
        /// <param name="atDateTime"></param>
        /// <param name="offsetPosition">position relative to the target object we wish to stop warp.</param>
        /// <returns></returns>
        public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Vector3 moverAbsolutePos, double speed, EntityBase targetEntity, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
        {

            var pos = moverAbsolutePos;
            double tim = 0;

            var pl = new Orbit()
            {
                position = moverAbsolutePos,
                T = targetEntity.Orbit.Period,
            };

            double a = targetEntity.Orbit.SemiMajorAxis * 2;

            Vector3 p;
            int i;
            double tt, t, dt, a0, a1, T;
            // find orbital position with min error (coarse)
            a1 = -1.0;
            dt = 0.01 * pl.T;


            for (t = 0; t < pl.T; t += dt)
            {
                p = OrbitProcessorBase.GetAbsolutePosition(targetEntity, atDateTime + TimeSpan.FromSeconds(t));  //pl.position(sim_t + t);                     // try time t
                p += offsetPosition;
                tt = (p - pos).Length() / speed;  //length(p - pos) / speed;
                a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                a0 /= pl.T;                                   // remove full periods from the difference
                a0 -= Math.Floor(a0);
                a0 *= pl.T;
                if ((a0 < a1) || (a1 < 0.0))
                {
                    a1 = a0;
                    tim = tt;
                }   // remember best option
            }
            // find orbital position with min error (fine)
            for (i = 0; i < 10; i++)                               // recursive increase of accuracy
                for (a1 = -1.0, t = tim - dt, T = tim + dt, dt *= 0.1; t < T; t += dt)
                {
                    p = OrbitProcessorBase.GetAbsolutePosition(targetEntity, atDateTime + TimeSpan.FromSeconds(t));  //p = pl.position(sim_t + t);                     // try time t
                    p += offsetPosition;
                    tt = (p - pos).Length() / speed;  //tt = length(p - pos) / speed;
                    a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                    a0 /= pl.T;                                   // remove full periods from the difference
                    a0 -= Math.Floor(a0);
                    a0 *= pl.T;
                    if ((a0 < a1) || (a1 < 0.0))
                    {
                        a1 = a0;
                        tim = tt;
                    }   // remember best option
                }
            // direction
            p = OrbitProcessorBase.GetAbsolutePosition(targetEntity, atDateTime + TimeSpan.FromSeconds(tim));//pl.position(sim_t + tim);
            p += offsetPosition;
            //dir = normalize(p - pos);
            return (p, atDateTime + TimeSpan.FromSeconds(tim));
        }
        
        
               /// <summary>
        /// THIS NEEDS TESTING.
        /// Hohmann the specified GravParamOfParent, semiMajAxisCurrentBody and semiMajAxisOfTarget.
        /// </summary>
        /// <returns>two burns with a time in seconds for the second burn</returns>
        /// <param name="sgp">Grav parameter of parent.</param>
        /// <param name="semiMajAxisCurrent">semiMajor axis now</param>
        /// <param name="semiMajAxisOfTarget">target semiMajorAxis</param>
        public static (Vector3 deltaV, double timeInSeconds)[] Hohmann(double sgp, double semiMajAxisCurrent, double semiMajAxisOfTarget)
        {
            double xferOrbitSMA = semiMajAxisCurrent + semiMajAxisOfTarget;
            double velCurrentBody = Math.Sqrt(sgp / semiMajAxisCurrent);
            double velTarg = Math.Sqrt(sgp / semiMajAxisOfTarget);

            double xferVelAtPeriapsis = Math.Sqrt(2 * (-sgp / xferOrbitSMA + sgp / semiMajAxisCurrent));

            double xferVelAtApoaxis = Math.Sqrt(2 * (-sgp / xferOrbitSMA + sgp / semiMajAxisOfTarget));

            double deltaVBurn1 = xferVelAtPeriapsis - velCurrentBody;
            double deltaVBurn2 = xferVelAtApoaxis - velTarg;

            double xferOrbitPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(xferOrbitSMA, 3) / sgp);
            double timeToSecondBurn = xferOrbitPeriod * 0.5;

            var manuvers = new (Vector3 burn1, double timeInSeconds)[2];
            manuvers[0] = (new Vector3(0, deltaVBurn1, 0), 0);
            manuvers[1] = (new Vector3(0, deltaVBurn2, 0), timeToSecondBurn);
            return manuvers;
        }


        /// <summary>
        /// Hohmann transfer manuver, assumes a cicular orbit. 
        /// </summary>
        /// <param name="sgp"></param>
        /// <param name="r1">radius from parent</param>
        /// <param name="r2">radius from parent</param>
        /// <returns>a tuple containing two manuvers with a time in seconds delay for second manuver</returns>
        public static (Vector3 deltaV, double timeInSeconds)[] Hohmann2(double sgp, double r1, double r2)
        {
            var wca1 = Math.Sqrt(sgp / r1);
            var wca2 = Math.Sqrt((2 * r2) / (r1 + r2)) - 1;
            var dva = wca1 * wca2;

            var wcb2 = Math.Sqrt(sgp / r2);
            var wcb3 = 1 - Math.Sqrt((2 * r1) / (r1 + r2));
            var dvb = wcb2 * wcb3;

            var timeTo2ndBurn = Math.PI * Math.Sqrt((Math.Pow(r1 + r2, 3)) / (8 * sgp));
            
            var manuvers = new (Vector3 burn1, double timeInSeconds)[2];
            manuvers[0] = (new Vector3(0, dva, 0), 0);
            manuvers[1] = (new Vector3(0, dvb, 0), timeTo2ndBurn);
            return manuvers;
        }


    }
}
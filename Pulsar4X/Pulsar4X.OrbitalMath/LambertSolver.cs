using System;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// Universal-variable Lambert solver, zero revolutions (Curtis, algorithm 5.2).
    /// Elliptic, parabolic and hyperbolic transfers. Multi-revolution solutions are not solved.
    /// </summary>
    /// <remarks>
    /// <paramref name="mu"/> is <c>OrbitMath.SGP(parent, ship)</c>: the parent's mass plus the ship's,
    /// the same standard gravitational parameter every other transfer in the engine uses.
    /// Units are the caller's, so long as position, <paramref name="mu"/> and time of flight agree
    /// (metres, m³/s² and seconds in the engine). Never throws: a bad input or a miss returns false.
    /// A 180° transfer has no defined plane and returns false; the caller nudges the angle.
    /// Prograde follows the sign of <c>(r1 × r2).Z</c> against the requested sense. Positions stay
    /// three-dimensional; only that sign picks the short or the long way.
    /// </remarks>
    public static class LambertSolver
    {
        const double CollinearSine = 1e-8;
        const int BracketSamples = 64;
        const int BisectIterations = 70;

        /// <summary>
        /// Departure velocity <paramref name="v1"/> and arrival velocity <paramref name="v2"/>
        /// that connect <paramref name="r1"/> to <paramref name="r2"/> in <paramref name="tof"/> seconds.
        /// </summary>
        public static bool TrySolve(double mu, Vector3 r1, Vector3 r2, double tof, bool prograde, out Vector3 v1, out Vector3 v2)
        {
            v1 = default;
            v2 = default;
            if (!(mu > 0) || !(tof > 0) || !Finite(r1) || !Finite(r2) || !double.IsFinite(mu) || !double.IsFinite(tof))
                return false;

            double r1m = r1.Length();
            double r2m = r2.Length();
            if (!(r1m > 0) || !(r2m > 0))
                return false;

            Vector3 cross = Vector3.Cross(r1, r2);
            double sinTheta = cross.Length() / (r1m * r2m);
            if (!(sinTheta > CollinearSine))
                return false;

            double cosTheta = Vector3.Dot(r1, r2) / (r1m * r2m);
            if (cosTheta > 1)
                cosTheta = 1;
            else if (cosTheta < -1)
                cosTheta = -1;
            double dtheta = Math.Acos(cosTheta);
            if (prograde)
            {
                if (cross.Z <= 0)
                    dtheta = 2 * Math.PI - dtheta;
            }
            else if (cross.Z >= 0)
            {
                dtheta = 2 * Math.PI - dtheta;
            }

            double oneMinusCos = 1 - Math.Cos(dtheta);
            if (!(oneMinusCos > 1e-14))
                return false;
            double a = Math.Sin(dtheta) * Math.Sqrt(r1m * r2m / oneMinusCos);
            if (!double.IsFinite(a) || Math.Abs(a) < 1e-6)
                return false;

            if (!TryFindZ(r1m, r2m, a, mu, tof, out double z, out double y))
                return false;

            double g = a * Math.Sqrt(y / mu);
            if (!double.IsFinite(g) || Math.Abs(g) < 1e-8)
                return false;

            double f = 1 - y / r1m;
            double gDot = 1 - y / r2m;
            v1 = (r2 - f * r1) / g;
            v2 = (gDot * r2 - r1) / g;
            if (!Finite(v1) || !Finite(v2))
            {
                v1 = default;
                v2 = default;
                return false;
            }

            return true;
        }

        static bool TryFindZ(double r1, double r2, double a, double mu, double tof, out double z, out double y)
        {
            z = 0;
            y = 0;
            if (!TryPositiveBound(r1, r2, a, out double zMin))
                return false;

            // Zero revolutions live below one full turn. Time of flight grows without bound as z approaches (2π)².
            double zMax = 4 * Math.PI * Math.PI * (1 - 1e-4);
            if (!(zMax > zMin))
                return false;

            const int samples = BracketSamples;
            double bestLo = 0;
            double bestHi = 0;
            double bestTLo = 0;
            double bestTHi = 0;
            bool have = false;
            double bestGap = double.MaxValue;

            if (!TryTime(zMin, r1, r2, a, mu, out double tPrev, out _))
                return false;
            double zPrev = zMin;

            for (int i = 1; i <= samples; i++)
            {
                double zi = zMin + (zMax - zMin) * i / samples;
                if (!TryTime(zi, r1, r2, a, mu, out double ti, out _))
                {
                    zPrev = zi;
                    tPrev = double.NaN;
                    continue;
                }

                if (double.IsFinite(tPrev) && (tPrev - tof) * (ti - tof) <= 0 && ti != tPrev)
                {
                    double gap = Math.Abs(ti - tPrev);
                    if (!have || gap < bestGap)
                    {
                        have = true;
                        bestGap = gap;
                        bestLo = zPrev;
                        bestHi = zi;
                        bestTLo = tPrev;
                        bestTHi = ti;
                    }
                }

                zPrev = zi;
                tPrev = ti;
            }

            if (!have)
                return false;

            double zLo = bestLo;
            double zHi = bestHi;
            double tLo = bestTLo;
            double tHi = bestTHi;
            double yLo = 0;
            TryTime(zLo, r1, r2, a, mu, out _, out yLo);

            for (int n = 0; n < BisectIterations; n++)
            {
                double mid = 0.5 * (zLo + zHi);
                if (!TryTime(mid, r1, r2, a, mu, out double tMid, out double yMid))
                {
                    // A non-finite sample sits in the bracket. Step toward the end that still has a time.
                    if (double.IsFinite(tLo))
                        zHi = mid;
                    else
                        zLo = mid;
                    continue;
                }

                bool midIsLow = (tLo <= tHi && tMid < tof) || (tLo > tHi && tMid > tof);
                if (midIsLow)
                {
                    zLo = mid;
                    tLo = tMid;
                    yLo = yMid;
                }
                else
                {
                    zHi = mid;
                    tHi = tMid;
                }
            }

            z = Math.Abs(tLo - tof) <= Math.Abs(tHi - tof) ? zLo : zHi;
            if (!TryTime(z, r1, r2, a, mu, out double t, out y))
                return false;

            double scale = Math.Max(1, Math.Abs(tof));
            if (Math.Abs(t - tof) / scale > 1e-6)
                return false;
            return true;
        }

        /// <summary>The lower edge of the domain, just where y(z) becomes positive.</summary>
        static bool TryPositiveBound(double r1, double r2, double a, out double zMin)
        {
            zMin = 0;
            if (!TryY(0, r1, r2, a, out _))
                return false;

            double probe = -1;
            while (probe > -1600 && TryY(probe, r1, r2, a, out _))
                probe *= 1.6;

            if (TryY(probe, r1, r2, a, out _))
            {
                zMin = probe;
                return true;
            }

            double lo = probe;
            double hi = 0;
            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                if (TryY(mid, r1, r2, a, out _))
                    hi = mid;
                else
                    lo = mid;
            }

            // A hair inside the positive side so y is safely above zero.
            zMin = hi + (0 - hi) * 1e-6;
            if (zMin <= hi)
                zMin = hi;
            return TryY(zMin, r1, r2, a, out _);
        }

        static bool TryTime(double z, double r1, double r2, double a, double mu, out double time, out double y)
        {
            time = double.NaN;
            if (!TryY(z, r1, r2, a, out y))
                return false;
            double c = StumpffC(z);
            double s = StumpffS(z);
            if (!(c > 0) || !double.IsFinite(s))
                return false;
            double term = Math.Pow(y / c, 1.5) * s + a * Math.Sqrt(y);
            time = term / Math.Sqrt(mu);
            return double.IsFinite(time) && time > 0;
        }

        static bool TryY(double z, double r1, double r2, double a, out double y)
        {
            y = double.NaN;
            double c = StumpffC(z);
            double s = StumpffS(z);
            if (!(c > 0) || !double.IsFinite(s))
                return false;
            y = r1 + r2 + a * (z * s - 1) / Math.Sqrt(c);
            return double.IsFinite(y) && y > 0;
        }

        /// <summary>Stumpff C(z). Series near zero, where the closed form divides by z.</summary>
        static double StumpffC(double z)
        {
            if (Math.Abs(z) < 0.1)
            {
                double z2 = z * z;
                return 0.5 - z / 24.0 + z2 / 720.0 - z2 * z / 40320.0 + z2 * z2 / 3628800.0;
            }

            if (z > 0)
            {
                double s = Math.Sqrt(z);
                return (1 - Math.Cos(s)) / z;
            }

            double sh = Math.Sqrt(-z);
            if (sh > 40)
                return double.NaN;
            return (Math.Cosh(sh) - 1) / (-z);
        }

        /// <summary>Stumpff S(z).</summary>
        static double StumpffS(double z)
        {
            if (Math.Abs(z) < 0.1)
            {
                double z2 = z * z;
                return 1.0 / 6.0 - z / 120.0 + z2 / 5040.0 - z2 * z / 362880.0 + z2 * z2 / 39916800.0;
            }

            if (z > 0)
            {
                double s = Math.Sqrt(z);
                return (s - Math.Sin(s)) / (s * z);
            }

            double sh = Math.Sqrt(-z);
            if (sh > 40)
                return double.NaN;
            return (Math.Sinh(sh) - sh) / (sh * (-z));
        }

        static bool Finite(Vector3 v)
            => double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);
    }
}

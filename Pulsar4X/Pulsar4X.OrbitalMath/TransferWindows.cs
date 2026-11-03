using System;

namespace Pulsar4X.Orbital
{
    /// <summary>
    /// One Lambert transfer between two bodies in the same frame.
    /// Departure and arrival Δv are inertial (parent-relative), not the prograde frame.
    /// </summary>
    public readonly struct TransferWindow
    {
        public readonly DateTime Depart;
        public readonly DateTime Arrive;
        public readonly Vector3 DepartDeltaV;
        public readonly Vector3 ArriveDeltaV;
        public readonly double TotalDeltaV;
        public readonly double VInfDepart;
        public readonly double VInfArrive;

        public TransferWindow(DateTime depart, DateTime arrive, Vector3 departDeltaV, Vector3 arriveDeltaV, double totalDeltaV, double vInfDepart, double vInfArrive)
        {
            Depart = depart;
            Arrive = arrive;
            DepartDeltaV = departDeltaV;
            ArriveDeltaV = arriveDeltaV;
            TotalDeltaV = totalDeltaV;
            VInfDepart = vInfDepart;
            VInfArrive = vInfArrive;
        }

        public bool IsFinite => double.IsFinite(TotalDeltaV);
    }

    /// <summary>
    /// Cost matrix for a departure date against a time of flight.
    /// A failed Lambert solve is <see cref="double.NaN"/>. The grid is at most 400 by 400;
    /// a larger request is clamped.
    /// </summary>
    public sealed class TransferGrid
    {
        public readonly DateTime[] Departures;
        public readonly double[] TimeOfFlightSeconds;
        public readonly double[,] Cost;
        public readonly TransferWindow[,] Cells;

        /// <summary>Lowest total Δv. False when every cell failed.</summary>
        public readonly bool HasBest;
        public readonly TransferWindow Best;

        internal TransferGrid(DateTime[] departures, double[] tof, double[,] cost, TransferWindow[,] cells, bool hasBest, TransferWindow best)
        {
            Departures = departures;
            TimeOfFlightSeconds = tof;
            Cost = cost;
            Cells = cells;
            HasBest = hasBest;
            Best = best;
        }

        /// <summary>
        /// Earliest departure whose total Δv is within <paramref name="budget"/>, in m/s.
        /// Ties at that date take the cheaper cell, then the shorter flight.
        /// </summary>
        public bool TryBestUnderBudget(double budget, out TransferWindow window)
        {
            window = default;
            if (!double.IsFinite(budget) || budget < 0)
                return false;

            bool found = false;
            for (int i = 0; i < Departures.Length; i++)
            {
                for (int j = 0; j < TimeOfFlightSeconds.Length; j++)
                {
                    TransferWindow cell = Cells[i, j];
                    if (!cell.IsFinite || cell.TotalDeltaV > budget)
                        continue;
                    if (!found || Earlier(cell, window))
                    {
                        window = cell;
                        found = true;
                    }
                }
            }

            return found;
        }

        static bool Earlier(TransferWindow cell, TransferWindow soFar)
        {
            if (cell.Depart < soFar.Depart)
                return true;
            if (cell.Depart > soFar.Depart)
                return false;
            if (cell.TotalDeltaV < soFar.TotalDeltaV)
                return true;
            if (cell.TotalDeltaV > soFar.TotalDeltaV)
                return false;
            return cell.Arrive < soFar.Arrive;
        }
    }

    /// <summary>
    /// Porkchop scan. The two delegates return parent-relative state at a date.
    /// The scan does not know whether they came from the engine or from a snapshot.
    /// </summary>
    public static class TransferWindows
    {
        public const int MaxSteps = 400;

        /// <summary>Body-to-body cost in one frame: the v-infinity magnitude, with no parking orbit.</summary>
        public static double MagnitudeCost(Vector3 vInfinity, Vector3 position) => vInfinity.Length();

        /// <summary>One synodic period, seconds. Infinite when the two periods match.</summary>
        public static double SynodicSeconds(double period1Seconds, double period2Seconds)
        {
            if (!(period1Seconds > 0) || !(period2Seconds > 0)
                || !double.IsFinite(period1Seconds) || !double.IsFinite(period2Seconds))
                return double.PositiveInfinity;
            double difference = Math.Abs(1.0 / period1Seconds - 1.0 / period2Seconds);
            if (difference < 1e-30)
                return double.PositiveInfinity;
            return 1.0 / difference;
        }

        /// <summary>Half to one and a half of the Hohmann coast, in seconds.</summary>
        public static (double tofMin, double tofMax) HohmannTofBand(double mu, double r1, double r2)
        {
            double a = 0.5 * (r1 + r2);
            double half = Math.PI * Math.Sqrt(a * a * a / mu);
            return (0.5 * half, 1.5 * half);
        }

        /// <summary>
        /// Each cell solves Lambert, then <c>vInf = vLambert − vBody</c>.
        /// <paramref name="departCost"/> and <paramref name="arriveCost"/> turn <c>(vInfinity, position)</c> into a Δv.
        /// Pass <see cref="MagnitudeCost"/> for a transfer that is already in one frame.
        /// <paramref name="prograde"/> is the sense of the whole grid. False is the retrograde family.
        /// </summary>
        public static TransferGrid Scan(
            double mu,
            Func<DateTime, (Vector3 pos, Vector3 vel)> origin,
            Func<DateTime, (Vector3 pos, Vector3 vel)> target,
            DateTime departFrom,
            DateTime departTo,
            int departSteps,
            double tofMin,
            double tofMax,
            int tofSteps,
            Func<Vector3, Vector3, double> departCost,
            Func<Vector3, Vector3, double> arriveCost,
            bool prograde = true)
        {
            departSteps = ClampSteps(departSteps);
            tofSteps = ClampSteps(tofSteps);
            if (departCost == null)
                departCost = MagnitudeCost;
            if (arriveCost == null)
                arriveCost = MagnitudeCost;

            var departures = new DateTime[departSteps];
            var tofs = new double[tofSteps];
            double departSpan = (departTo - departFrom).TotalSeconds;
            if (!double.IsFinite(departSpan) || departSpan < 0)
                departSpan = 0;
            double tofSpan = tofMax - tofMin;
            if (!double.IsFinite(tofSpan) || tofSpan < 0)
                tofSpan = 0;

            for (int i = 0; i < departSteps; i++)
            {
                double f = departSteps == 1 ? 0 : (double)i / (departSteps - 1);
                departures[i] = departFrom.AddSeconds(departSpan * f);
            }

            for (int j = 0; j < tofSteps; j++)
            {
                double f = tofSteps == 1 ? 0 : (double)j / (tofSteps - 1);
                tofs[j] = tofMin + tofSpan * f;
            }

            var cost = new double[departSteps, tofSteps];
            var cells = new TransferWindow[departSteps, tofSteps];
            bool hasBest = false;
            TransferWindow best = default;

            for (int i = 0; i < departSteps; i++)
            {
                DateTime depart = departures[i];
                var from = origin(depart);
                for (int j = 0; j < tofSteps; j++)
                {
                    double tof = tofs[j];
                    DateTime arrive = depart.AddSeconds(tof);
                    var to = target(arrive);
                    cells[i, j] = new TransferWindow(depart, arrive, default, default, double.NaN, double.NaN, double.NaN);
                    cost[i, j] = double.NaN;

                    if (!LambertSolver.TrySolve(mu, from.pos, to.pos, tof, prograde, out Vector3 v1, out Vector3 v2))
                        continue;

                    Vector3 vInfDepart = v1 - from.vel;
                    Vector3 vInfArrive = v2 - to.vel;
                    double departDv;
                    double arriveDv;
                    try
                    {
                        departDv = departCost(vInfDepart, from.pos);
                        arriveDv = arriveCost(vInfArrive, to.pos);
                    }
                    catch
                    {
                        continue;
                    }

                    double total = departDv + arriveDv;
                    if (!double.IsFinite(total) || total < 0)
                        continue;

                    var window = new TransferWindow(depart, arrive, vInfDepart, vInfArrive, total, vInfDepart.Length(), vInfArrive.Length());
                    cells[i, j] = window;
                    cost[i, j] = total;
                    if (!hasBest || Cheaper(window, best))
                    {
                        best = window;
                        hasBest = true;
                    }
                }
            }

            return new TransferGrid(departures, tofs, cost, cells, hasBest, best);
        }

        static bool Cheaper(TransferWindow cell, TransferWindow soFar)
        {
            if (cell.TotalDeltaV < soFar.TotalDeltaV)
                return true;
            if (cell.TotalDeltaV > soFar.TotalDeltaV)
                return false;
            if (cell.Depart < soFar.Depart)
                return true;
            if (cell.Depart > soFar.Depart)
                return false;
            return cell.Arrive < soFar.Arrive;
        }

        static int ClampSteps(int steps)
        {
            if (steps < 1)
                return 1;
            if (steps > MaxSteps)
                return MaxSteps;
            return steps;
        }
    }
}

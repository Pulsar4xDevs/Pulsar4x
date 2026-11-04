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

        /// <summary>
        /// Earliest departure inside the wait cap whose Δv is within <paramref name="tolerance"/>
        /// of the cheapest affordable cell in that cap. <paramref name="tolerance"/> is a fraction:
        /// 0.10 keeps a cell up to 10% above the floor. Ties use <see cref="TryBestUnderBudget"/>'s order.
        /// </summary>
        public bool TryBestWindow(double budget, double maxWaitSeconds, double tolerance, out TransferWindow window)
        {
            window = default;
            if (!double.IsFinite(budget) || budget < 0 || Departures.Length == 0)
                return false;
            if (!double.IsFinite(maxWaitSeconds) || maxWaitSeconds < 0)
                maxWaitSeconds = 0;
            if (!double.IsFinite(tolerance) || tolerance < 0)
                tolerance = 0;

            DateTime start = Departures[0];
            double floor = double.PositiveInfinity;
            bool any = false;
            for (int i = 0; i < Departures.Length; i++)
            {
                for (int j = 0; j < TimeOfFlightSeconds.Length; j++)
                {
                    TransferWindow cell = Cells[i, j];
                    if (!InCap(cell, start, maxWaitSeconds, budget))
                        continue;
                    any = true;
                    if (cell.TotalDeltaV < floor)
                        floor = cell.TotalDeltaV;
                }
            }

            if (!any || !double.IsFinite(floor))
                return false;

            double limit = floor * (1.0 + tolerance);
            bool found = false;
            for (int i = 0; i < Departures.Length; i++)
            {
                for (int j = 0; j < TimeOfFlightSeconds.Length; j++)
                {
                    TransferWindow cell = Cells[i, j];
                    if (!InCap(cell, start, maxWaitSeconds, budget) || cell.TotalDeltaV > limit)
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

        /// <summary>Cheapest affordable cell in one departure column. Ties take the shorter flight.</summary>
        public bool TryCheapestInColumn(int column, double budget, out TransferWindow window)
        {
            window = default;
            if (!double.IsFinite(budget) || budget < 0
                || column < 0 || column >= Departures.Length)
                return false;

            bool found = false;
            for (int j = 0; j < TimeOfFlightSeconds.Length; j++)
            {
                TransferWindow cell = Cells[column, j];
                if (!cell.IsFinite || cell.TotalDeltaV > budget)
                    continue;
                if (!found || Earlier(cell, window))
                {
                    window = cell;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>A grid the tests can fill by hand. Cost and the absolute cheapest cell are derived.</summary>
        public static TransferGrid FromCells(DateTime[] departures, double[] tof, TransferWindow[,] cells)
        {
            departures ??= Array.Empty<DateTime>();
            tof ??= Array.Empty<double>();
            int ni = departures.Length;
            int nj = tof.Length;
            var cost = new double[ni, nj];
            bool hasBest = false;
            TransferWindow best = default;
            for (int i = 0; i < ni; i++)
            {
                for (int j = 0; j < nj; j++)
                {
                    TransferWindow cell = cells[i, j];
                    cost[i, j] = cell.IsFinite ? cell.TotalDeltaV : double.NaN;
                    if (!cell.IsFinite)
                        continue;
                    if (!hasBest || CheaperCell(cell, best))
                    {
                        best = cell;
                        hasBest = true;
                    }
                }
            }

            return new TransferGrid(departures, tof, cost, cells, hasBest, best);
        }

        static bool InCap(TransferWindow cell, DateTime start, double maxWaitSeconds, double budget)
        {
            if (!cell.IsFinite || cell.TotalDeltaV > budget)
                return false;
            double wait = (cell.Depart - start).TotalSeconds;
            return double.IsFinite(wait) && wait <= maxWaitSeconds + 1e-3;
        }

        static bool CheaperCell(TransferWindow cell, TransferWindow soFar)
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

        /// <summary>A cell within this fraction of the cheapest affordable Δv counts as best.</summary>
        public const double BestWindowDvTolerance = 0.10;

        /// <summary>Wait at most this many Hohmann flight times. The flight is the centre of <see cref="HohmannTofBand"/>.</summary>
        public const double BestWindowWaitCapHohmannFlights = 2;

        /// <summary>
        /// Wait cap in seconds: <see cref="BestWindowWaitCapHohmannFlights"/> times the Hohmann coast,
        /// and never longer than the scan span.
        /// </summary>
        public static double BestWindowWaitCap(double tofMin, double tofMax, double scanSpanSeconds)
        {
            if (!double.IsFinite(tofMin) || !double.IsFinite(tofMax))
                return 0;
            double centre = 0.5 * (tofMin + tofMax);
            double cap = BestWindowWaitCapHohmannFlights * centre;
            if (!double.IsFinite(cap) || cap < 0)
                return 0;
            if (double.IsFinite(scanSpanSeconds) && scanSpanSeconds > 0 && cap > scanSpanSeconds)
                cap = scanSpanSeconds;
            return cap;
        }

        /// <summary>
        /// Spare Δv the patched-conic planner holds back for one mid-course burn, in m/s.
        /// Counted in the budget check, not in the escape-plus-capture total.
        /// </summary>
        public const double CorrectionAllowanceMps = 100;

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

using System;
using Pulsar4X.Api;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>One priced departure from a porkchop, plus the other timing.</summary>
public readonly struct WindowAnswer
{
    public readonly bool Feasible;
    public readonly TransferWindow Window;
    public readonly string Note;
    public readonly bool HasAlt;
    public readonly DepartureTiming AltTiming;
    public readonly TransferWindow AltWindow;
    public readonly string AltNote;
    public readonly bool AltFeasible;
    public readonly double CheapestDv;
    public readonly bool Scanned;

    public WindowAnswer(
        bool feasible, TransferWindow window, string note,
        bool hasAlt, DepartureTiming altTiming, TransferWindow altWindow, string altNote, bool altFeasible,
        double cheapestDv, bool scanned)
    {
        Feasible = feasible;
        Window = window;
        Note = note ?? "";
        HasAlt = hasAlt;
        AltTiming = altTiming;
        AltWindow = altWindow;
        AltNote = altNote ?? "";
        AltFeasible = altFeasible;
        CheapestDv = cheapestDv;
        Scanned = scanned;
    }
}

/// <summary>
/// Builds the two state delegates for <see cref="TransferWindows.Scan"/> from entities.
/// A body is read from its <see cref="OrbitDB"/>. A ship that is thrusting is propagated
/// from its live move blob. The scan itself stays free of engine types.
/// </summary>
public static class TransferPlanner
{
    /// <summary>Planner grid. The plot uses a finer one; this stays small enough to run inside a move decision.</summary>
    public const int PlannerDepartSteps = 32;
    public const int PlannerTofSteps = 20;

    /// <summary>A cell within this fraction of the cheapest affordable Δv counts as best.</summary>
    public const double BestWindowDvTolerance = TransferWindows.BestWindowDvTolerance;

    /// <summary>Wait at most this many Hohmann flights. See <see cref="TransferWindows.BestWindowWaitCap"/>.</summary>
    public const double BestWindowWaitCapHohmannFlights = TransferWindows.BestWindowWaitCapHohmannFlights;

    public static double WaitCapSeconds(double tofMin, double tofMax, double scanSpanSeconds)
        => TransferWindows.BestWindowWaitCap(tofMin, tofMax, scanSpanSeconds);

    public static bool TryState(Entity entity, DateTime at, out (Vector3 pos, Vector3 vel) state)
    {
        state = default;
        if (entity == null)
            return false;
        try
        {
            if (entity.TryGetDataBlob<OrbitDB>(out var orbit))
            {
                var sampled = OrbitMath.GetStateVectors(orbit, at);
                state = (sampled.pos, sampled.Velocity);
            }
            else
            {
                var raw = MoveMath.GetRelativeFutureState(entity, at);
                state = (raw.pos, raw.Velocity);
            }
        }
        catch
        {
            return false;
        }

        return Finite(state.pos) && Finite(state.vel);
    }

    public static TransferGrid Scan(
        Entity origin, Entity target, double mu,
        DateTime departFrom, DateTime departTo, int departSteps,
        double tofMin, double tofMax, int tofSteps,
        bool prograde)
    {
        return TransferWindows.Scan(
            mu,
            at => Sample(origin, at),
            at => Sample(target, at),
            departFrom, departTo, departSteps,
            tofMin, tofMax, tofSteps,
            TransferWindows.MagnitudeCost, TransferWindows.MagnitudeCost,
            prograde);
    }

    /// <summary>
    /// Best window inside <paramref name="budget"/>. <paramref name="bestDv"/> is the cheapest cell,
    /// even when it does not fit, so the caller can name the shortfall.
    /// </summary>
    public static bool TryAffordableWindow(
        Entity origin, Entity target, double mu, DateTime now, double budget,
        out TransferWindow window, out double bestDv)
        => TryAffordableWindow(origin, target, mu, now, budget, DepartureTiming.BestWindow, null,
            out window, out bestDv, out _, out _);

    /// <summary>
    /// One scan, priced for <paramref name="timing"/>. <paramref name="bestDv"/> stays the cheapest cell.
    /// </summary>
    public static bool TryAffordableWindow(
        Entity origin, Entity target, double mu, DateTime now, double budget,
        DepartureTiming timing, TransferWindow? picked,
        out TransferWindow window, out double bestDv, out string note, out WindowAnswer answer)
    {
        window = default;
        bestDv = double.NaN;
        note = "";
        answer = default;
        if (origin == null || target == null || !(mu > 0))
            return false;
        if (!TryState(origin, now, out var from) || !TryState(target, now, out var to))
            return false;

        double r1 = from.pos.Length();
        double r2 = to.pos.Length();
        if (!(r1 > 1) || !(r2 > 1))
            return false;

        double span = DepartureSpan(origin, target, mu);
        var (tofMin, tofMax) = TransferWindows.HohmannTofBand(mu, r1, r2);
        if (!double.IsFinite(tofMin) || !double.IsFinite(tofMax) || tofMax <= tofMin)
            return false;

        bool prograde = AngularMomentumZ(from.pos, from.vel) >= 0;
        TransferGrid grid = Scan(
            origin, target, mu, now, now.AddSeconds(span),
            PlannerDepartSteps, tofMin, tofMax, PlannerTofSteps, prograde);
        if (grid.HasBest)
            bestDv = grid.Best.TotalDeltaV;
        double cap = WaitCapSeconds(tofMin, tofMax, span);
        answer = Choose(grid, budget, cap, timing, picked);
        window = answer.Window;
        note = answer.Note ?? "";
        return answer.Feasible;
    }

    /// <summary>
    /// The departure rule. Best window is the earliest near-cheapest cell inside the wait cap.
    /// Nothing in the cap widens to the earliest the tank can fly. Depart now is the first column.
    /// </summary>
    public static WindowAnswer Choose(
        TransferGrid grid, double budget, double maxWaitSeconds, DepartureTiming timing, TransferWindow? picked)
    {
        double cheapest = grid != null && grid.HasBest ? grid.Best.TotalDeltaV : double.NaN;
        if (grid == null)
            return new WindowAnswer(false, default, "", false, DepartureTiming.BestWindow, default, "", false, cheapest, false);

        if (timing == DepartureTiming.PickOnPorkchop)
        {
            WindowAnswer best = BestOrWiden(grid, budget, maxWaitSeconds, cheapest);
            if (picked is not { } cell || !cell.IsFinite)
            {
                return new WindowAnswer(false, default, "Pick a cell on the porkchop.",
                    best.HasAlt, DepartureTiming.BestWindow, best.Window, best.Note, best.Feasible, cheapest, true);
            }

            if (cell.TotalDeltaV > budget)
            {
                return new WindowAnswer(false, cell, $"needs {cell.TotalDeltaV:N0} m/s Δv, have {budget:N0} m/s",
                    best.HasAlt, DepartureTiming.BestWindow, best.Window, best.Note, best.Feasible, cheapest, true);
            }

            return new WindowAnswer(true, cell, "",
                best.HasAlt, DepartureTiming.BestWindow, best.Window, best.Note, best.Feasible, cheapest, true);
        }

        if (timing == DepartureTiming.DepartNow)
        {
            WindowAnswer best = BestOrWiden(grid, budget, maxWaitSeconds, cheapest);
            if (grid.TryCheapestInColumn(0, budget, out TransferWindow first))
            {
                return new WindowAnswer(true, first, "",
                    best.Feasible, DepartureTiming.BestWindow, best.Window, best.Note, best.Feasible, cheapest, true);
            }

            if (grid.TryBestUnderBudget(budget, out TransferWindow earliest))
            {
                string note = "Cannot leave now; earliest is in " + FormatWait(WaitFrom(grid, earliest));
                return new WindowAnswer(true, earliest, note,
                    best.Feasible, DepartureTiming.BestWindow, best.Window, best.Note, best.Feasible, cheapest, true);
            }

            string shortfall = double.IsFinite(cheapest)
                ? $"needs {cheapest:N0} m/s Δv, have {budget:N0} m/s"
                : "";
            return new WindowAnswer(false, default, shortfall, false, DepartureTiming.BestWindow, default, "", false, cheapest, true);
        }

        WindowAnswer chosen = BestOrWiden(grid, budget, maxWaitSeconds, cheapest);
        bool now = grid.TryCheapestInColumn(0, budget, out TransferWindow nowCell);
        string nowNote = "";
        bool nowFeasible = now;
        if (!now && grid.TryBestUnderBudget(budget, out TransferWindow later))
        {
            nowCell = later;
            now = true;
            nowFeasible = true;
            nowNote = "Cannot leave now; earliest is in " + FormatWait(WaitFrom(grid, later));
        }

        return new WindowAnswer(chosen.Feasible, chosen.Window, chosen.Note,
            now, DepartureTiming.DepartNow, nowCell, nowNote, nowFeasible, cheapest, true);
    }

    /// <summary>under a minute, N min, H h M min, or N d.</summary>
    public static string FormatWait(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 60)
            return "under a minute";
        if (seconds < 3600)
            return $"{Math.Max(1, (int)Math.Round(seconds / 60.0))} min";
        if (seconds < 86400)
        {
            int hours = (int)(seconds / 3600.0);
            int minutes = (int)Math.Round((seconds - hours * 3600.0) / 60.0);
            if (minutes == 60)
            {
                hours++;
                minutes = 0;
            }
            return minutes == 0 ? $"{hours} h" : $"{hours} h {minutes} min";
        }

        return $"{Math.Max(1, (int)Math.Round(seconds / 86400.0))} d";
    }

    static WindowAnswer BestOrWiden(TransferGrid grid, double budget, double maxWaitSeconds, double cheapest)
    {
        if (grid.TryBestWindow(budget, maxWaitSeconds, BestWindowDvTolerance, out TransferWindow window))
            return new WindowAnswer(true, window, "", true, DepartureTiming.BestWindow, window, "", true, cheapest, true);
        if (grid.TryBestUnderBudget(budget, out TransferWindow earliest))
        {
            string note = "No window inside the wait cap; earliest the tank can fly is in " + FormatWait(WaitFrom(grid, earliest));
            return new WindowAnswer(true, earliest, note, true, DepartureTiming.BestWindow, earliest, note, true, cheapest, true);
        }

        return new WindowAnswer(false, default, "", false, DepartureTiming.BestWindow, default, "", false, cheapest, true);
    }

    static double WaitFrom(TransferGrid grid, TransferWindow window)
    {
        if (grid.Departures.Length == 0)
            return 0;
        double wait = (window.Depart - grid.Departures[0]).TotalSeconds;
        return double.IsFinite(wait) && wait > 0 ? wait : 0;
    }

    /// <summary>Parent-relative Δv into the burn frame: X radial, Y prograde, Z normal.</summary>
    public static Vector3 ToProgradeFrame(Vector3 inertial, Vector3 position, Vector3 velocity)
        => OrbitalMath.ToProgradeFrame(inertial, position, velocity);

    static (Vector3 pos, Vector3 vel) Sample(Entity entity, DateTime at)
    {
        if (TryState(entity, at, out var state))
            return state;
        var nan = new Vector3(double.NaN, double.NaN, double.NaN);
        return (nan, nan);
    }

    static double DepartureSpan(Entity origin, Entity target, double mu)
    {
        double cap = TimeSpan.FromDays(4 * 365.25).TotalSeconds;
        double p1 = Period(origin, mu);
        double p2 = Period(target, mu);
        double synodic = TransferWindows.SynodicSeconds(p1, p2);
        if (!double.IsFinite(synodic) || synodic <= 0)
            synodic = TimeSpan.FromDays(365.25).TotalSeconds;
        if (synodic > cap)
            synodic = cap;
        return synodic;
    }

    static double Period(Entity entity, double mu)
    {
        if (!entity.TryGetDataBlob<OrbitDB>(out var orbit) || !(orbit.SemiMajorAxis > 0))
            return double.NaN;
        return OrbitalMath.GetOrbitalPeriodInSeconds(mu, orbit.SemiMajorAxis);
    }

    static double AngularMomentumZ(Vector3 pos, Vector3 vel)
        => pos.X * vel.Y - pos.Y * vel.X;

    static bool Finite(Vector3 v)
        => double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);
}

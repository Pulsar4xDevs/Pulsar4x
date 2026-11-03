using System;
using Pulsar4X.Engine;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

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
    /// Earliest window inside <paramref name="budget"/>. <paramref name="bestDv"/> is the cheapest cell,
    /// even when it does not fit, so the caller can name the shortfall.
    /// </summary>
    public static bool TryAffordableWindow(
        Entity origin, Entity target, double mu, DateTime now, double budget,
        out TransferWindow window, out double bestDv)
    {
        window = default;
        bestDv = double.NaN;
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
        return grid.TryBestUnderBudget(budget, out window);
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

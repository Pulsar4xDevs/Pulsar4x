using System;
using Pulsar4X.Api;
using Pulsar4X.Orbital;

namespace Pulsar4X.Client;

/// <summary>
/// Client-side movement math over snapshot data: state vectors, warp intercepts and transfer
/// maneuvers computed from <see cref="OrbitView"/> elements via the shared orbital-math library.
/// The movement-order windows preview maneuvers with these; the server recomputes anything that
/// matters when the command is submitted.
/// </summary>
public static class SnapshotMoves
{
    /// <summary>The body whose gravity dominates the entity right now.</summary>
    public static EntitySnapshot? GetSoiParent(this EntitySnapshot entity, IClientSystem system)
    {
        int? parentId = entity.GetView<NewtonMoveView>()?.SoiParentId
            ?? entity.GetView<NewtonSimpleMoveView>()?.SoiParentId
            ?? entity.GetView<OrbitView>()?.ParentId
            ?? entity.GetView<PositionView>()?.ParentId;
        return parentId is int id ? system.GetEntity(id) : null;
    }

    /// <summary>The entity's current orbital elements: its orbit, or the trajectory of an active
    /// newtonian maneuver.</summary>
    public static OrbitView? ResolveOrbit(this EntitySnapshot entity)
        => entity.GetView<OrbitView>()
            ?? entity.GetView<NewtonMoveView>()?.Trajectory
            ?? entity.GetView<NewtonSimpleMoveView>()?.CurrentTrajectory;

    /// <summary>The entity's own sphere-of-influence radius (metres); infinite when it has no
    /// orbit to derive one from (the system primary).</summary>
    public static double SoiRadiusM(this EntitySnapshot entity)
    {
        var orbit = entity.ResolveOrbit();
        return orbit is { SoiRadiusM: > 0 } ? orbit.SoiRadiusM : double.PositiveInfinity;
    }

    /// <summary>The lowest sensible orbit over a body (metres from its centre).</summary>
    public static double LowOrbitRadiusM(this EntitySnapshot body)
        => (body.GetView<MassVolumeView>()?.RadiusMetres ?? 0) * 1.1;

    /// <summary>Position and velocity relative to the orbit parent at the given time. Entities
    /// without elements use their last pushed position and zero velocity (at most a tick old).</summary>
    public static (Vector3 pos, Vector3 vel) GetRelativeState(this EntitySnapshot entity, DateTime atTime)
    {
        var orbit = entity.ResolveOrbit();
        if (orbit != null && orbit.StandardGravParameter > 0)
        {
            var state = OrbitalMath.GetStateVectors(orbit.ToKeplerElements(), atTime);
            return (state.position, new Vector3(state.velocity.X, state.velocity.Y, 0));
        }

        var position = entity.GetView<PositionView>();
        return position != null
            ? (new Vector3(position.RelativePosition.X, position.RelativePosition.Y, position.RelativePosition.Z), Vector3.Zero)
            : (Vector3.Zero, Vector3.Zero);
    }

    /// <summary>Absolute position and velocity at the given time, summed up the orbit-parent chain.</summary>
    public static (Vector3 pos, Vector3 vel) GetAbsoluteState(this EntitySnapshot entity, IClientSystem system, DateTime atTime)
    {
        var (pos, vel) = entity.GetRelativeState(atTime);

        var orbit = entity.ResolveOrbit();
        if (orbit != null && orbit.StandardGravParameter > 0)
        {
            if (orbit.ParentId is int parentId && system.GetEntity(parentId) is { } parent)
            {
                var parentState = parent.GetAbsoluteState(system, atTime);
                return (parentState.pos + pos, parentState.vel + vel);
            }
            return (pos, vel);
        }

        var position = entity.GetView<PositionView>();
        return position != null
            ? (new Vector3(position.AbsolutePosition.X, position.AbsolutePosition.Y, position.AbsolutePosition.Z), vel)
            : (pos, vel);
    }

    /// <summary>
    /// Where (and when) a ship warping at the given speed meets the target — the engine's WarpMath
    /// intercept search over snapshot orbits. Targets without an orbit (jump points, anomalies) are
    /// met in a straight line.
    /// </summary>
    public static (Vector3 position, DateTime eti) GetInterceptPosition(
        Vector3 moverAbsolutePos, double speedMps, EntitySnapshot target, IClientSystem system,
        DateTime atDateTime, Vector3 offsetPosition = default)
    {
        var orbit = target.ResolveOrbit();
        if (orbit == null || orbit.StandardGravParameter <= 0 || orbit.OrbitalPeriodSeconds <= 0 || speedMps <= 0)
        {
            var exitPos = target.AbsolutePositionM(system, atDateTime) + offsetPosition;
            double seconds = speedMps > 0 ? (exitPos - moverAbsolutePos).Length() / speedMps : 0;
            return (exitPos, atDateTime + TimeSpan.FromSeconds(seconds));
        }

        double period = orbit.OrbitalPeriodSeconds;
        double tim = 0;

        Vector3 p;
        double tt, t, dt, a0, a1, T;
        // find orbital position with min error (coarse)
        a1 = -1.0;
        dt = 0.01 * period;

        for (t = 0; t < period; t += dt)
        {
            p = target.AbsolutePositionM(system, atDateTime + TimeSpan.FromSeconds(t)) + offsetPosition;
            tt = (p - moverAbsolutePos).Length() / speedMps;
            a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
            a0 /= period;                                     // remove full periods from the difference
            a0 -= Math.Floor(a0);
            a0 *= period;
            if ((a0 < a1) || (a1 < 0.0))
            {
                a1 = a0;
                tim = tt;
            }
        }
        // find orbital position with min error (fine)
        for (int i = 0; i < 10; i++)                          // recursive increase of accuracy
            for (a1 = -1.0, t = tim - dt, T = tim + dt, dt *= 0.1; t < T; t += dt)
            {
                p = target.AbsolutePositionM(system, atDateTime + TimeSpan.FromSeconds(t)) + offsetPosition;
                tt = (p - moverAbsolutePos).Length() / speedMps;
                a0 = tt - t; if (a0 < 0.0) continue;
                a0 /= period;
                a0 -= Math.Floor(a0);
                a0 *= period;
                if ((a0 < a1) || (a1 < 0.0))
                {
                    a1 = a0;
                    tim = tt;
                }
            }

        p = target.AbsolutePositionM(system, atDateTime + TimeSpan.FromSeconds(tim)) + offsetPosition;
        return (p, atDateTime + TimeSpan.FromSeconds(tim));
    }

    /// <summary>The velocity to insert into orbit with at warp exit (the engine's
    /// OrbitProcessor.GetOrbitalInsertionVector over snapshots).</summary>
    public static Vector3 GetOrbitalInsertionVector(Vector3 departureVelocity, EntitySnapshot target,
        IClientSystem system, DateTime arrivalDateTime, bool useRelativeVelocity)
    {
        if (useRelativeVelocity)
            return departureVelocity;

        var targetVelocity = target.GetAbsoluteState(system, arrivalDateTime).vel;
        return departureVelocity - targetVelocity;
    }

}

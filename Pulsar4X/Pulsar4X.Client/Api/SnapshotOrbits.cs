using System;
using Pulsar4X.Api;
using Pulsar4X.Orbital;

namespace Pulsar4X.Client;

/// <summary>
/// Client-side orbit propagation: positions between server pushes are computed each frame from the
/// <see cref="OrbitView"/>'s Keplerian elements via the shared orbital-math library. Entities that
/// move non-Keplerian (warp, newtonian thrust) are re-pushed by the server every clock advance, so
/// their <see cref="PositionView"/> is at most a tick old.
/// </summary>
public static class SnapshotOrbits
{
    public static KeplerElements ToKeplerElements(this OrbitView orbit)
    {
        double a = orbit.SemiMajorAxisM;
        double e = orbit.Eccentricity;
        return new KeplerElements
        {
            StandardGravParameter = orbit.StandardGravParameter,
            SemiMajorAxis = a,
            SemiMinorAxis = a * Math.Sqrt(1 - e * e),
            Eccentricity = e,
            LinearEccentricity = e * a,
            Periapsis = (1 - e) * a,
            Apoapsis = (1 + e) * a,
            LoAN = orbit.LongitudeOfAscendingNodeRad,
            AoP = orbit.ArgumentOfPeriapsisRad,
            Inclination = orbit.InclinationRad,
            MeanMotion = orbit.MeanMotionRadPerSec,
            Period = orbit.OrbitalPeriodSeconds,
            MeanAnomalyAtEpoch = orbit.MeanAnomalyAtEpochRad,
            Epoch = orbit.Epoch,
        };
    }

    /// <summary>The position relative to the orbit parent at the given time, in metres.</summary>
    public static Vector3 RelativePositionM(this OrbitView orbit, DateTime atTime)
    {
        return OrbitalMath.GetPosition(orbit.ToKeplerElements(), atTime);
    }
        

    /// <summary>The entity's absolute position at the given time, propagated up the orbit-parent
    /// chain; entities without an orbit use their last pushed position.</summary>
    public static Vector3 AbsolutePositionM(this EntitySnapshot entity, IClientSystem system, DateTime atTime)
    {
        var orbit = entity.GetView<OrbitView>();
        if (orbit != null && orbit.StandardGravParameter > 0)
        {
            var relative = orbit.RelativePositionM(atTime);
            if (orbit.ParentId is int parentId && system.GetEntity(parentId) is { } parent)
                return parent.AbsolutePositionM(system, atTime) + relative;
            return relative;
        }

        var position = entity.GetView<PositionView>();
        return position != null
            ? new Vector3(position.AbsolutePosition.X, position.AbsolutePosition.Y, position.AbsolutePosition.Z)
            : Vector3.Zero;
    }
}

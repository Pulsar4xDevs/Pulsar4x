using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;

namespace Pulsar4X.Movement;

/// <summary>
/// Frame sums for a patched-conic transfer. One level each side of the common ancestor:
/// Earth to Mars, not a moon to another planet.
/// </summary>
internal static class PatchedConic
{
    const int ChainGuard = 16;

    /// <summary>
    /// Deepest body that is on both chains. The walk starts at the ship's orbit parent,
    /// so a ship is not an ancestor of its own target.
    /// </summary>
    internal static Entity? FindCommonAncestor(Entity ship, Entity target)
    {
        if (ship == null || target == null)
            return null;

        var chain = new List<Entity>(8);
        for (var node = ship.GetSOIParentEntity(); node != null && chain.Count < ChainGuard; node = node.GetSOIParentEntity())
            chain.Add(node);

        int guard = 0;
        for (var node = target; node != null && guard < ChainGuard; node = node.GetSOIParentEntity(), guard++)
        {
            if (chain.Contains(node))
                return node;
        }

        return null;
    }

    /// <summary>
    /// The ship's parent and the target are different children of <see cref="FindCommonAncestor"/>.
    /// A moon and another planet share the star, and this returns false for that.
    /// </summary>
    internal static bool IsOneLevelTransfer(Entity ship, Entity target)
    {
        var ancestor = FindCommonAncestor(ship, target);
        if (ancestor == null)
            return false;
        var shipParent = ship.GetSOIParentEntity();
        if (shipParent == null || shipParent == ancestor || shipParent == target)
            return false;
        if (shipParent.GetSOIParentEntity() != ancestor)
            return false;
        if (target.GetSOIParentEntity() != ancestor)
            return false;
        return true;
    }

    /// <summary>
    /// Position and velocity of <paramref name="entity"/> in <paramref name="ancestor"/>'s frame.
    /// The ancestor's own state is left out. A stationary star at the origin then matches
    /// <see cref="MoveMath.GetAbsoluteFuturePosition"/>.
    /// </summary>
    internal static (Vector3 pos, Vector3 vel) StateRelativeToAncestor(Entity entity, Entity ancestor, DateTime at)
    {
        Vector3 pos = Vector3.Zero;
        Vector3 vel = Vector3.Zero;
        Entity? node = entity;
        for (int guard = 0; node != null && node != ancestor && guard < ChainGuard; guard++)
        {
            if (node.TryGetDataBlob<OrbitDB>(out var orbit) && !orbit.IsStationary)
            {
                pos += orbit.GetPosition(at);
                vel += OrbitMath.InstantaneousOrbitalVelocityVector_m(orbit, at);
                node = orbit.Parent;
            }
            else
            {
                node = node.GetSOIParentEntity();
            }
        }

        return (pos, vel);
    }

    /// <summary>
    /// Sphere exit in the parent's parent's frame: the same sum <c>ChangeSOIProcessor</c> uses.
    /// <paramref name="escape"/> is the hyperbola about <paramref name="parent"/>. Its epoch is
    /// periapsis passage. <see cref="OrbitMath.TimeToRadius"/> ignores a hyperbola's epoch mean
    /// anomaly, so a burn away from periapsis would not match the processor.
    /// </summary>
    internal static bool ExitStateAtSphere(
        KeplerElements escape, Entity parent, double shipMass,
        out DateTime at, out Vector3 position, out Vector3 velocity)
    {
        at = default;
        position = default;
        velocity = default;
        if (parent == null || escape.Epoch == default || !(shipMass >= 0))
            return false;
        if (!parent.TryGetDataBlob<OrbitDB>(out var parentOrbit))
            return false;
        if (parent.GetSOIParentEntity() == null)
            return false;
        double soi = parent.GetSOI_m();
        if (!double.IsFinite(soi) || !(soi > 0))
            return false;
        if (!(escape.Eccentricity >= 1) || !double.IsFinite(escape.SemiMajorAxis))
            return false;

        try
        {
            var hyperbola = OrbitDB.FromKeplerElements(parent, shipMass, escape, escape.Epoch);
            at = OrbitMath.TimeToRadius(hyperbola, soi);
            var shipPos = hyperbola.GetPosition(at);
            var shipVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(hyperbola, at);
            var parentPos = parentOrbit.GetPosition(at);
            var parentVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(parentOrbit, at);
            if (!Finite(shipPos) || !Finite(shipVel) || !Finite(parentPos) || !Finite(parentVel))
                return false;
            position = parentPos + shipPos;
            velocity = parentVel + shipVel;
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }

    static bool Finite(Vector3 v) =>
        double.IsFinite(v.X) && double.IsFinite(v.Y) && double.IsFinite(v.Z);
}

using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.Movement;


public enum MoveMode
{
    /// <summary>Close enough already; the plan is no actions at all.</summary>
    AlreadyThere,

    /// <summary>Reaction-drive transfer within the current SOI — phasing or Hohmann rendezvous.</summary>
    NewtonianTransfer,

    /// <summary>Alcubierre bubble.</summary>
    Warp,
}

public class MoveToPlan : IGoalPlanner
{
    public GoalType Type => GoalType.MoveTo;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanSubGoals(managedEntity, goal);
        if (managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanActions(managedEntity, goal, atDateTime);

        return PlanResult.Fail("Non supported entity");
    }

    /// <summary>
    /// Leaf: resolve target, pick move mode, emit actions. Does not mutate <paramref name="goal"/>.
    /// </summary>
    public PlanResult PlanActions(Entity ship, Goal goal, DateTime atDateTime)
    {
        if (!ship.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out Entity requested))
            return PlanResult.Fail("Target not found");

        // Already queued for this goal — roll up or wait.
        if (ship.TryGetDataBlob<ActionQueueDB>(out var actionQueue))
        {
            var actionsForGoal = actionQueue.ActionsFor(goal);
            if (actionsForGoal.Count > 0)
            {
                if (actionsForGoal.Exists(a => a.Status == ActionStatus.Failed))
                    return PlanResult.Fail("a move action failed");
                if (actionsForGoal.TrueForAll(a => a.Status == ActionStatus.Succeeded))
                    return PlanResult.Done();
                return PlanResult.Continue(new List<EntityAction>());
            }
        }

        if (!MovePlanner.TryBuildMoveActions(ship, requested, out var actions, out string reason, atDateTime))
            return PlanResult.Fail(reason);

        if (actions.Count == 0)
            return PlanResult.Done(reason);

        return PlanResult.Continue(actions, reason);
    }

    /// <summary>
    /// Fleet: every capable free subunit gets the same destination.
    /// Mode (warp vs newton) is chosen by each ship's own PlanActions.
    /// Re-entrant: skips children already working this parent goal.
    /// </summary>
    public PlanResult PlanSubGoals(Entity fleet, Goal goal)
    {
        if (!fleet.TryGetDataBlob<FleetDB>(out FleetDB? db))
            return PlanResult.Fail("We have no subordinates to manage");

        if (!fleet.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out _))
            return PlanResult.Fail("Target not found");

        var subGoals = new List<(Entity subordinate, Goal goal)>();
        int capable = 0;
        int alreadyWorking = 0;

        foreach (var subunit in db.Children)
        {
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            capable++;

            if (subunit.TryGetDataBlob<GoalsDB>(out var childGoals)
                && childGoals.ActiveGoal != null
                && childGoals.ActiveGoal.ParentGoalId == goal.Id
                && childGoals.ActiveGoal.Status is not (GoalStatus.Completed or GoalStatus.Failed))
            {
                alreadyWorking++;
                continue;
            }

            subGoals.Add((subunit, new Goal(GoalType.MoveTo)
            {
                TargetEntityID = goal.TargetEntityID,
                ParentGoalId = goal.Id,
            }));
        }

        if (capable == 0)
            return PlanResult.Fail("no subordinates can move");

        if (subGoals.Count == 0)
        {
            // Everyone who can move is already assigned (or finished — agent rollup handles complete).
            return alreadyWorking > 0
                ? PlanResult.Continue(new List<(Entity subordinate, Goal goal)>())
                : PlanResult.Done("all subordinates already at target or idle");
        }

        return PlanResult.Continue(subGoals);
    }
}



/// <summary>
/// Turns "what the player clicked on" into "what we can actually plot a course to".
///
/// This is deliberately movement-mode agnostic: a warp plot and a newtonian transfer both
/// want the parent body rather than the colony sitting on it, and both want a warping ship's
/// destination rather than the ship itself, since there is no useful way to rendezvous with a
/// point on somebody else's warp line.
/// </summary>
public static class MoveTargeting
{
    /// <summary>
    /// Depth guard. A chasing B chasing A would otherwise recurse forever; the visited set
    /// catches cycles and this catches pathological chains.
    /// </summary>
    const int MaxHops = 8;

    public static bool TryResolve(Entity requested, out Entity resolved, out string reason)
    {
        resolved = requested;
        reason = string.Empty;
        var seen = new HashSet<int>();

        for (int hop = 0; hop <= MaxHops; hop++)
        {
            if (!seen.Add(resolved.Id))
            {
                reason = "Target chain loops back on itself.";
                return false;
            }

            // A colony is an abstraction living on a body. The body is the thing in space.
            if (resolved.TryGetDataBlob<ColonyInfoDB>(out var colony))
            {
                resolved = colony.PlanetEntity;
                continue;
            }

            if (!resolved.TryGetDataBlob<PositionDB>(out var posDB))
            {
                reason = "Target has no position.";
                return false;
            }

            // Anything that isn't warping is a point we can at least attempt to predict.
            // (Newtonian movers get as far as here and are then rejected per-mode below,
            // because it's the *prediction* that's missing, not the identity.)
            if (posDB.MoveType != PositionDB.MoveTypes.Warp)
                return true;

            // Chase where they're going, not where they are.
            // TODO intel: this reads another faction's destination straight out of the engine.
            // It should be gated on what we actually know about the target, and guessed
            // (badly, from observed heading) when we don't.
            if (!resolved.TryGetDataBlob<WarpMovingDB>(out var warping) || warping.TargetEntity == null)
            {
                reason = "Target is in warp with no readable destination.";
                return false;
            }

            resolved = warping.TargetEntity;
        }

        reason = $"Target chain deeper than {MaxHops} hops.";
        return false;
    }
}



/// <summary>
/// One candidate way of getting there, with what it would cost. Infeasible options carry the
/// reason so a failed goal can say *why* rather than just "can't".
/// </summary>
public readonly struct MoveOption
{
    public readonly MoveMode Mode;
    public readonly bool Feasible;
    public readonly string Reason;

    /// <summary>Seconds from now until we arrive.</summary>
    public readonly double EtaSeconds;
    /// <summary>Reaction mass budget in m/s. Zero for warp.</summary>
    public readonly double DeltaV;
    /// <summary>Warp bubble energy. Zero for newtonian.</summary>
    public readonly double EnergyCost;

    /// <summary>Prograde-frame burns and their cumulative offsets from now, in seconds.</summary>
    public readonly (Vector3 deltaV, double timeInSeconds)[] Manuvers;

    MoveOption(MoveMode mode, bool feasible, string reason, double eta, double deltaV, double energy,
        (Vector3 deltaV, double timeInSeconds)[] manuvers)
    {
        Mode = mode;
        Feasible = feasible;
        Reason = reason;
        EtaSeconds = eta;
        DeltaV = deltaV;
        EnergyCost = energy;
        Manuvers = manuvers ?? Array.Empty<(Vector3, double)>();
    }

    public static MoveOption No(MoveMode mode, string reason)
        => new MoveOption(mode, false, reason, double.PositiveInfinity, 0, 0, null);

    public static MoveOption Yes(MoveMode mode, double etaSeconds, double deltaV = 0, double energyCost = 0,
        (Vector3 deltaV, double timeInSeconds)[] manuvers = null, string message = "")
        => new MoveOption(mode, true, message, etaSeconds, deltaV, energyCost, manuvers);
}

/// <summary>
/// The movement-mode decision tree: given a ship and an already-resolved target, work out
/// which ways of getting there are possible and pick one.
///
/// This is the layer that used to be missing — every caller went straight to
/// <see cref="WarpMoveAction"/> and so had already decided to warp before asking whether warp
/// was the right answer (or available at all).
/// </summary>
public static class MovePlanner
{
    // ---------------------------------------------------------------------
    // Tuning. All placeholder values; none of this is balanced.
    // ---------------------------------------------------------------------

    /// <summary>Count as arrived when within this many low-orbit radii of a body.</summary>
    const double ArrivedWithinLowOrbits = 3.0;

    /// <summary>...or this close to something with no meaningful low orbit (jump point, ship).</summary>
    const double ArrivedWithin_m = 10_000;

    /// <summary>
    /// Euclidean-near is not "there" on a leftover hyperbola. Bound and this circular
    /// around the drop-in parent (or the target, if we made it into its SOI).
    /// </summary>
    const double ArrivedMaxEccentricity = 0.05;

    /// <summary>
    /// When we cannot enter the target's SOI (Phobos), "alongside" is this fraction of the
    /// target's SMA around the parent — 3× Phobos low-orbit is only ~37 km.
    /// </summary>
    const double ArrivedAlongOrbitFraction = 0.01;

    /// <summary>
    /// Two orbits are "the same orbit" (so: phase, don't transfer) when their radii are within
    /// this fraction of each other.
    /// </summary>
    const double CoOrbitalRadiusTolerance = 0.001;

    /// <summary>The transfer maths we have all assume circular orbits.</summary>
    const double MaxEccentricityForTransfer = 0.01;

    // ---------------------------------------------------------------------

    /// <summary>
    /// Does this entity have any means of moving itself at all? The early game may have no warp
    /// drive; a barge may have no reaction drive either.
    /// </summary>
    public static bool CanMove(Entity ship, out string reason)
    {
        bool warp = ship.HasDataBlob<WarpAbilityDB>();
        bool newt = ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) && thrust.ThrustInNewtons > 0;

        reason = warp || newt ? string.Empty : "no warp drive and no reaction drive";
        return warp || newt;
    }

    /// <summary>Evaluate every mode we know about. Infeasible ones are returned too, for the reason.</summary>
    public static List<MoveOption> Evaluate(Entity ship, Entity target, DateTime now)
    {
        return new List<MoveOption>
        {
            EvaluateAlreadyThere(ship, target, now),
            EvaluateNewtonian(ship, target, now),
            EvaluateWarp(ship, target, now),
        };
    }

    /// <summary>
    /// Pick one. PLACEHOLDER POLICY — the eventual design is a utility comparison over
    /// (eta, deltaV, energy) weighted by the owning goal: a Trade run should value fuel, an
    /// Intercept should value time, a ship on DontRunOutOfFuel should refuse an expensive option
    /// outright. Until the goal layer can express that, the ordering is:
    ///
    ///   1. already there — do nothing
    ///   2. newtonian — match orbits in the current well (phase/Hohmann; circularise first
    ///      if leftover e is high). Same-orbit and next-moon-over; do not lose to warp on ETA
    ///   3. warp — when the target is under a different parent (interplanetary)
    /// </summary>
    public static MoveOption Select(IReadOnlyList<MoveOption> options)
    {
        MoveOption? arrived = Find(options, MoveMode.AlreadyThere);
        if (arrived.HasValue && arrived.Value.Feasible) return arrived.Value;

        MoveOption? newt = Find(options, MoveMode.NewtonianTransfer);
        MoveOption? warp = Find(options, MoveMode.Warp);

        bool newtOk = newt.HasValue && newt.Value.Feasible;
        bool warpOk = warp.HasValue && warp.Value.Feasible;

        // Newtonian is only feasible in the current well (match the moon's orbit around
        // the parent, or phase/Hohmann with a co-orbital). Warp is seconds; a Phobos
        // phasing burn is hours and would always lose a 3× ETA comparison — which is
        // how MoveTo Phobos kept warping and never completed.
        if (newtOk) return newt.Value;
        if (warpOk) return warp.Value;

        // Nothing works. Hand back the collected reasons so the goal can say why.
        var why = new List<string>();
        foreach (var o in options)
            if (!o.Feasible && !string.IsNullOrEmpty(o.Reason))
                why.Add($"{o.Mode}: {o.Reason}");

        return MoveOption.No(MoveMode.Warp, string.Join("; ", why));
    }

    static MoveOption? Find(IReadOnlyList<MoveOption> options, MoveMode mode)
    {
        foreach (var o in options)
            if (o.Mode == mode)
                return o;
        return null;
    }

    // ---------------------------------------------------------------------
    // Candidates
    // ---------------------------------------------------------------------

    static MoveOption EvaluateAlreadyThere(Entity ship, Entity target, DateTime now)
    {
        if (!ship.TryGetDataBlob<PositionDB>(out var shipPos) || !target.TryGetDataBlob<PositionDB>(out var tgtPos))
            return MoveOption.No(MoveMode.AlreadyThere, "no position");

        var parent = ship.GetSOIParentEntity();
        var dropInParent = PredictDropInParent(target, PlannedWarpExitOffsetLength(target));
        if (parent == null || (parent != dropInParent && parent != target))
            return MoveOption.No(MoveMode.AlreadyThere, "not in the destination gravity well");

        // Prefer the attached OrbitDB: reconstructing Kepler from a state vector
        // can disagree with the orbit we just wrote (mixed frames / blob velocity).
        // Fall back to the state vector when there is no stable orbit yet.
        double eccentricity;
        double shipRadius;
        Vector3 posRel;
        if (ship.TryGetDataBlob<OrbitDB>(out var shipOrbit) && shipOrbit.Parent == parent)
        {
            eccentricity = shipOrbit.Eccentricity;
            shipRadius = shipOrbit.SemiMajorAxis;
            posRel = (Vector3)MoveMath.GetRelativeFuturePosition(ship, now);
        }
        else
        {
            if (!TryCurrentRelativeState(ship, now, out var state))
                return MoveOption.No(MoveMode.AlreadyThere, "no state vector");
            double sgp = OrbitMath.SGP(parent, ship);
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, state.pos, state.vel, now);
            eccentricity = ke.Eccentricity;
            shipRadius = ke.SemiMajorAxis;
            posRel = state.pos;
        }

        if (!double.IsFinite(eccentricity) || eccentricity >= ArrivedMaxEccentricity)
            return MoveOption.No(MoveMode.AlreadyThere, "not in a circular orbit");

        // Blobs lag the orbit hotloop. Evaluate where we actually are at the plan instant.
        double separation = ((Vector3)MoveMath.GetAbsoluteFuturePosition(ship, now)
                             - (Vector3)MoveMath.GetAbsoluteFuturePosition(target, now)).Length();

        // In the body's own SOI: close to the body in a circular orbit around it.
        if (dropInParent == target || parent == target)
        {
            double tolerance = target.HasDataBlob<MassVolumeDB>()
                ? OrbitMath.LowOrbitRadius(target) * ArrivedWithinLowOrbits
                : ArrivedWithin_m;
            return separation <= tolerance
                ? MoveOption.Yes(MoveMode.AlreadyThere, 0, message: $"Already at {NameOf(target, ship)}")
                : MoveOption.No(MoveMode.AlreadyThere, "not there yet");
        }

        // Cannot enter the target's SOI (Phobos): arrived when we match its orbit around
        // the parent and are alongside it, not when we merely parked circular at leftover r.
        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit) || targetOrbit.Parent != parent)
            return MoveOption.No(MoveMode.AlreadyThere, "target orbit is not around this parent");

        double targetRadius = targetOrbit.SemiMajorAxis;
        if (!IsCoOrbital(shipRadius, targetRadius, target)
            && !IsCoOrbital(posRel.Length(), targetRadius, target))
            return MoveOption.No(MoveMode.AlreadyThere, "not in the target's orbit");

        double along = AlongTrackArrivedSeparation(target, targetRadius);
        return separation <= along
            ? MoveOption.Yes(MoveMode.AlreadyThere, 0, message: $"Matching {NameOf(target, ship)}'s orbit")
            : MoveOption.No(MoveMode.AlreadyThere, "in the orbit but not with the target yet");
    }

    /// <summary>
    /// Reaction-drive transfer. Only the cases we have working maths for; everything else is
    /// rejected with a reason rather than plotted badly.
    /// </summary>
    static MoveOption EvaluateNewtonian(Entity ship, Entity target, DateTime now)
    {
        const MoveMode mode = MoveMode.NewtonianTransfer;

        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) || thrust.ThrustInNewtons <= 0)
            return MoveOption.No(mode, "no reaction drive");

        // We plot from a known orbit. A ship mid-burn or mid-warp has no OrbitDB.
        // MISSING MATHS: deriving a transfer from an arbitrary state vector. Until we have that,
        // a manoeuvring ship has to reach a stable orbit before it can plan a newtonian move.
        if (!ship.TryGetDataBlob<OrbitDB>(out var shipOrbit))
            return MoveOption.No(mode, "ship is not in a stable orbit to transfer from");

        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit))
            return MoveOption.No(mode, "target is not in a stable orbit; cannot predict where it will be");

        var parent = shipOrbit.Parent;
        var dropInParent = PredictDropInParent(target, PlannedWarpExitOffsetLength(target));
        if (parent == null)
            return MoveOption.No(mode, "no SOI parent");

        if (!TryCurrentRelativeState(ship, now, out var state))
            return MoveOption.No(mode, "no state vector");

        double r = state.pos.Length();
        if (!double.IsFinite(r) || r < 1)
            return MoveOption.No(mode, "no usable radius");
        if (parent.TryGetDataBlob<MassVolumeDB>(out var parentMass) && r <= parentMass.RadiusInM)
            return MoveOption.No(mode, "inside the parent body");

        double sgp = OrbitMath.SGP(parent, ship);
        bool needsCircularise = shipOrbit.Eccentricity > MaxEccentricityForTransfer;
        double circulariseDV = 0;
        if (needsCircularise)
        {
            var circKE = OrbitMath.KeplerCircularFromPosition(sgp, state.pos, now);
            var circV = OrbitMath.GetStateVectors(circKE, now).velocity;
            circulariseDV = (state.vel - (Vector3)circV).Length();
            if (!double.IsFinite(circulariseDV))
                return MoveOption.No(mode, "circularise Δv is not finite");
        }

        // Already in the destination body's SOI (warp-to-Mars drop-in). Circularise leftover,
        // then Hohmann to low orbit — do not match the planet's solar orbit and do not warp again.
        if (parent == target)
        {
            if (needsCircularise)
            {
                if (circulariseDV > thrust.DeltaV)
                    return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
                return MoveOption.Yes(mode, 0, circulariseDV,
                    message: $"Circularise at {NameOf(target, ship)}");
            }

            double parkingR = OrbitMath.LowOrbitRadius(target);
            if (!ChangeOrbitalAltitudeAction.TryPlanBurns(ship, parkingR, now, out var parkingBurns, out var parkingReason))
                return MoveOption.No(mode, parkingReason);
            if (parkingBurns.Count == 0)
                return MoveOption.No(mode, "already at parking altitude");

            double parkDv = 0;
            double parkEta = 0;
            foreach (var b in parkingBurns)
            {
                var v0 = OrbitMath.GetStateVectors(b.start, b.at).velocity;
                var v1 = OrbitMath.GetStateVectors(b.end, b.at).velocity;
                parkDv += ((Vector3)v1 - (Vector3)v0).Length();
                parkEta = Math.Max(parkEta, (b.at - now).TotalSeconds);
            }
            if (parkDv > thrust.DeltaV)
                return MoveOption.No(mode, $"needs {parkDv:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
            return MoveOption.Yes(mode, parkEta, parkDv,
                message: $"Change altitude at {NameOf(target, ship)}, {FormatArrival(now.AddSeconds(parkEta))}",
                manuvers: new (Vector3, double)[parkingBurns.Count]);
        }

        // Match the target's orbit around the shared drop-in parent (Phobos around Mars).
        if (dropInParent == target)
            return MoveOption.No(mode, "target body has a usable SOI; warp");
        if (parent != dropInParent || targetOrbit.Parent != dropInParent)
        {
            return MoveOption.No(mode, "target is under a different SOI parent; no interplanetary transfer maths yet");
        }

        if (targetOrbit.Eccentricity > MaxEccentricityForTransfer)
            return MoveOption.No(mode, "transfer maths assume circular orbits");

        // Hyperbolic SMA is negative; Hohmann/phasing run from the circular radius we'll have.
        double shipRadius = needsCircularise ? r : shipOrbit.SemiMajorAxis;
        double targetRadius = targetOrbit.SemiMajorAxis;

        // True longitude in the reference plane. The sim is effectively 2D for these transfers,
        // and this matches how InterceptCalcs measures the same angles.
        double shipAngle = AngleOf(ship, now);
        double targetAngle = AngleOf(target, now);

        (Vector3 deltaV, double timeInSeconds)[] manuvers;
        double phaseAngle = NormaliseAngle(targetAngle - shipAngle);
        double along = AlongTrackArrivedSeparation(target, targetRadius);
        double phaseTol = targetRadius > 0 ? along / targetRadius : 0;

        if (IsCoOrbital(shipRadius, targetRadius, target) && Math.Abs(phaseAngle) <= phaseTol)
        {
            if (!needsCircularise)
                return MoveOption.No(mode, "co-orbital and co-located");
            // Leftover hyperbola at the moon: circularise to match its orbit, then replan.
            if (circulariseDV > thrust.DeltaV)
                return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
            return MoveOption.Yes(mode, 0, circulariseDV,
                message: $"Circularise to match {NameOf(target, ship)}");
        }

        if (needsCircularise)
        {
            // Hohmann/phasing assume circular. Circularise at current r first; the agent
            // replans the match-orbit burns from that circular orbit.
            if (circulariseDV > thrust.DeltaV)
                return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
            return MoveOption.Yes(mode, 0, circulariseDV,
                message: $"Circularise to match {NameOf(target, ship)}");
        }

        if (IsCoOrbital(shipRadius, targetRadius, target))
        {
            // Same orbit, wrong place in it: drop into a phasing orbit and come back.
            // NOTE: OrbitPhasingManuvers' sign convention for phaseAngle is unverified against
            // the target-ahead/target-behind cases. Worth a test before this is trusted.
            manuvers = OrbitalMath.OrbitPhasingManuvers(shipOrbit.GetElements(), sgp, now, phaseAngle);
        }
        else
        {
            // Different radii around the same parent: Hohmann, but the *OE* variant, which waits
            // for the launch window so we arrive where the target actually is rather than merely
            // at the right altitude.
            manuvers = OrbitalMath.HohmannOE(sgp, shipRadius, shipAngle, targetRadius, targetAngle);

            // HohmannOE returns burn 2's time relative to burn 1; every consumer here wants
            // cumulative offsets from now.
            manuvers[1].timeInSeconds += manuvers[0].timeInSeconds;
        }

        double totalDV = 0;
        double eta = 0;
        foreach (var m in manuvers)
        {
            totalDV += m.deltaV.Length();
            eta = Math.Max(eta, m.timeInSeconds);
        }

        // The closed-form solutions happily return NaN/Infinity for degenerate inputs rather than
        // throwing, so this is the only thing standing between a bad orbit and a bad burn.
        if (!double.IsFinite(totalDV) || !double.IsFinite(eta) || eta < 0)
            return MoveOption.No(mode, "transfer solution did not converge");

        if (totalDV > thrust.DeltaV)
            return MoveOption.No(mode, $"needs {totalDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");

        string match = IsCoOrbital(shipRadius, targetRadius, target)
            ? $"Phasing to {NameOf(target, ship)}"
            : $"Hohmann to {NameOf(target, ship)}";
        return MoveOption.Yes(mode, eta, totalDV, 0, manuvers,
            $"{match}, {FormatArrival(now.AddSeconds(eta))}");
    }

    static MoveOption EvaluateWarp(Entity ship, Entity target, DateTime now)
    {
        const MoveMode mode = MoveMode.Warp;

        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warpDB))
            return MoveOption.No(mode, "no warp drive");
        if (warpDB.MaxSpeed <= 0)
            return MoveOption.No(mode, "warp drive produces no speed");

        // Already in the well we would drop into (Earth→Mars leftover, or Phobos
        // around Mars). Warping again is a short hop to the same offset and the
        // leftover-v dump repeats forever. Circularise / match-orbit instead.
        var parent = ship.GetSOIParentEntity();
        var dropInParent = PredictDropInParent(target, PlannedWarpExitOffsetLength(target));
        if (parent != null && (parent == target || parent == dropInParent))
            return MoveOption.No(mode, "already in destination gravity well");

        // WarpMath.GetInterceptPosition only solves against a fixed point or a keplerian orbit.
        var moveType = target.GetDataBlob<PositionDB>().MoveType;
        if (moveType != PositionDB.MoveTypes.None && moveType != PositionDB.MoveTypes.Orbit)
        {
            // MISSING MATHS: intercepting a target under thrust. Its future path depends on burns
            // we may not know about, so this is as much an intel problem as a maths one.
            return MoveOption.No(mode, $"cannot solve a warp intercept against a {moveType} target");
        }

        var intercept = WarpMath.GetInterceptPosition(ship, target, now);
        double eta = (intercept.etiDateTime - now).TotalSeconds;
        if (double.IsNaN(eta) || eta < 0)
            return MoveOption.No(mode, "warp intercept did not converge");

        // Sustain and collapse costs are charged by WarpMoveProcessor as the bubble runs; only
        // the up-front cost is knowable here. WarpMoveAction holds the goal on "Charging
        // batteries" if we can't afford even that, so it isn't a feasibility gate.
        return MoveOption.Yes(mode, eta, 0, warpDB.BubbleCreationCost,
            message: $"Warp to {NameOf(target, ship)}, {FormatArrival(intercept.etiDateTime)}");
    }

    // ---------------------------------------------------------------------
    // Action construction
    // ---------------------------------------------------------------------
    // MovementGoalsAndActions / MovePlanner
    
    /// <summary>
    /// Build the movement actions only. Never touches goal.Status.
    /// Returns false (with reason) when the move is impossible.
    /// AlreadyThere yields an empty list and true.
    /// </summary>
    public static bool TryBuildMoveActions(Entity ship, Entity targetEntity,
                                           out List<EntityAction> actions, out string reason)
        => TryBuildMoveActions(ship, targetEntity, out actions, out reason, ship.StarSysDateTime);

    public static bool TryBuildMoveActions(Entity ship, Entity targetEntity,
                                           out List<EntityAction> actions, out string reason, DateTime now)
    {
        actions = new List<EntityAction>();
        reason = string.Empty;
        
        if (!MoveTargeting.TryResolve(targetEntity, out var target, out reason))
            return false;
        if (!CanMove(ship, out reason))
            return false;

        var moveOptions = Evaluate(ship, target, now);
        var chosen = Select(moveOptions);
        if (!chosen.Feasible)
        {
            reason = chosen.Reason;
            return false;
        }
        reason = chosen.Reason;
        if (chosen.Mode == MoveMode.AlreadyThere)
            return true;

        if (chosen.Mode == MoveMode.Warp)
            actions.AddRange(BuildWarpAndCircularise(ship, target, now));
        else if (chosen.Manuvers.Length == 0)
            actions.Add(CirculariseAction.CreateCommand(ship, now));
        else if (ship.GetSOIParentEntity() == target)
            actions.Add(ChangeOrbitalAltitudeAction.CreateCommand(ship, OrbitMath.LowOrbitRadius(target), now));
        else
            actions.Add(MatchOrbitAction.CreateCommand(ship, target, now));

        return true;
    }

    private static List<EntityAction> BuildWarpAndCircularise(Entity orderEntity, Entity targetEntity, DateTime now)
    {
        var actions = new List<EntityAction>();
        var lowOrbitRadius = OrbitMath.LowOrbitRadius(targetEntity);
        
        (Vector3 pos, Vector3 vel) departureState;
        if(orderEntity.Manager.Game.Settings.UseRelativeVelocity)
        {
            departureState = MoveMath.GetRelativeFutureState(orderEntity, now);
        }
        else
            departureState = MoveMath.GetAbsoluteState(orderEntity, now);        
        var perpVec = Vector3.Normalise(new Vector3(departureState.vel.Y * -1, departureState.vel.X, 0));
        var endWarpPos = perpVec * lowOrbitRadius;
        
        
        actions.Add(WarpMoveAction.CreateWarpOnly(orderEntity, targetEntity, now, endWarpPos));
        if (targetEntity.GetDataBlob<PositionDB>().MoveType == PositionDB.MoveTypes.Orbit)
            actions.Add(CirculariseAction.CreateCommand(orderEntity));
        return actions;
    }

    /// <summary>
    /// Parent the ship will have after warp drop-in: the target if the exit offset is inside
    /// its SOI, otherwise the target's SOI parent. Matches SetOrbitHereSimpleNewt.
    /// </summary>
    static Entity PredictDropInParent(Entity target, double exitOffsetLength)
    {
        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit) || targetOrbit.Parent == null)
            return target;

        double soi = OrbitMath.GetSOIRadius(targetOrbit);
        if (soi > exitOffsetLength)
            return target;

        return target.GetSOIParentEntity() ?? target;
    }

    static double PlannedWarpExitOffsetLength(Entity target)
    {
        return target.HasDataBlob<MassVolumeDB>()
            ? OrbitMath.LowOrbitRadius(target)
            : ArrivedWithin_m;
    }

    static bool IsCoOrbital(double shipRadius, double targetRadius, Entity target)
    {
        if (!double.IsFinite(shipRadius) || !double.IsFinite(targetRadius) || targetRadius <= 0)
            return false;
        double tol = Math.Max(targetRadius * CoOrbitalRadiusTolerance, PlannedWarpExitOffsetLength(target) * 2);
        return Math.Abs(shipRadius - targetRadius) <= tol;
    }

    static double AlongTrackArrivedSeparation(Entity target, double targetRadius)
    {
        double close = target.HasDataBlob<MassVolumeDB>()
            ? OrbitMath.LowOrbitRadius(target) * ArrivedWithinLowOrbits
            : ArrivedWithin_m;
        if (!double.IsFinite(targetRadius) || targetRadius <= 0)
            return close;
        return Math.Max(close, targetRadius * ArrivedAlongOrbitFraction);
    }

    static bool TryCurrentRelativeState(Entity ship, DateTime at, out (Vector3 pos, Vector3 vel) state)
    {
        state = default;
        if (!ship.HasDataBlob<PositionDB>())
            return false;
        if (!ship.HasDataBlob<OrbitDB>()
            && !ship.HasDataBlob<OrbitUpdateOftenDB>()
            && !ship.HasDataBlob<NewtonSimpleMoveDB>()
            && !ship.HasDataBlob<NewtonMoveDB>()
            && !ship.HasDataBlob<WarpMovingDB>())
            return false;

        var raw = MoveMath.GetRelativeFutureState(ship, at);
        state = (raw.pos, raw.Velocity);
        return double.IsFinite(state.pos.X) && double.IsFinite(state.vel.X);
    }

    // ---------------------------------------------------------------------

    static string NameOf(Entity target, Entity ship)
        => target.GetName(ship.FactionOwnerID);

    static string FormatArrival(DateTime at)
        => at.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>True longitude of an entity about its SOI parent, in the reference plane.</summary>
    static double AngleOf(Entity entity, DateTime atDateTime)
    {
        var pos = MoveMath.GetRelativeFuturePosition(entity, atDateTime);
        return Math.Atan2(pos.Y, pos.X);
    }

    /// <summary>Wrap to (-pi, pi].</summary>
    static double NormaliseAngle(double radians)
    {
        while (radians > Math.PI) radians -= 2 * Math.PI;
        while (radians <= -Math.PI) radians += 2 * Math.PI;
        return radians;
    }
}





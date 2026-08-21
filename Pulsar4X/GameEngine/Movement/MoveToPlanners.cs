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

/// <summary>
/// This whole thing was written by AI, and needs checking/rewrite.
/// I'm suspicious for example that the BuildBurns is taking DeltaV instead of KeplerElements.
/// </summary>

public class MoveToPlan : IGoalPlanner
{
    public GoalType Type => GoalType.MoveTo;

    public PlanResult Plan(Entity managedEntity, Goal goal)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanSubGoals(managedEntity, goal);
        if (managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanActions(managedEntity, goal);

        return PlanResult.Fail("Non supported entity");
    }

    /// <summary>
    /// Leaf: resolve target, pick move mode, emit actions. Does not mutate <paramref name="goal"/>.
    /// </summary>
    public PlanResult PlanActions(Entity ship, Goal goal)
    {
        if (!ship.Manager.TryGetGlobalEntityById(goal.TargetEntityID, out Entity requested))
            return PlanResult.Fail("Target not found");

        if (!MoveTargeting.TryResolve(requested, out var target, out var resolveFailure))
            return PlanResult.Fail(resolveFailure);

        if (!MovePlanner.CanMove(ship, out var immobile))
            return PlanResult.Fail(immobile);

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

        DateTime now = ship.StarSysDateTime;
        var chosen = MovePlanner.Select(MovePlanner.Evaluate(ship, target, now));

        if (!chosen.Feasible)
            return PlanResult.Fail(chosen.Reason);

        if (chosen.Mode == MoveMode.AlreadyThere)
            return PlanResult.Done("Already at target");

        var actions = MovePlanner.BuildActions(ship, target, now, chosen);
        return PlanResult.Continue(actions);
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
        (Vector3 deltaV, double timeInSeconds)[] manuvers = null)
        => new MoveOption(mode, true, string.Empty, etaSeconds, deltaV, energyCost, manuvers);
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
    /// Two orbits are "the same orbit" (so: phase, don't transfer) when their radii are within
    /// this fraction of each other.
    /// </summary>
    const double CoOrbitalRadiusTolerance = 0.001;

    /// <summary>The transfer maths we have all assume circular orbits.</summary>
    const double MaxEccentricityForTransfer = 0.01;

    /// <summary>
    /// Take the newtonian option unless it's more than this multiple of the warp ETA. A phasing
    /// manoeuvre can easily cost most of an orbital period, which is fine for a freighter and
    /// absurd for an intercept.
    /// </summary>
    const double NewtonianEtaTolerance = 3.0;

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
            EvaluateAlreadyThere(ship, target),
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
    ///   2. newtonian — no bubble energy, no warp tech needed, and it is the sane answer for the
    ///      same-orbit and next-moon-over cases
    ///   3. warp — unless newtonian would be dramatically slower, in which case warp wins
    /// </summary>
    public static MoveOption Select(IReadOnlyList<MoveOption> options)
    {
        MoveOption? arrived = Find(options, MoveMode.AlreadyThere);
        if (arrived.HasValue && arrived.Value.Feasible) return arrived.Value;

        MoveOption? newt = Find(options, MoveMode.NewtonianTransfer);
        MoveOption? warp = Find(options, MoveMode.Warp);

        bool newtOk = newt.HasValue && newt.Value.Feasible;
        bool warpOk = warp.HasValue && warp.Value.Feasible;

        if (newtOk && warpOk)
            return newt.Value.EtaSeconds > warp.Value.EtaSeconds * NewtonianEtaTolerance
                ? warp.Value
                : newt.Value;

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

    static MoveOption EvaluateAlreadyThere(Entity ship, Entity target)
    {
        if (!ship.TryGetDataBlob<PositionDB>(out var shipPos) || !target.TryGetDataBlob<PositionDB>(out var tgtPos))
            return MoveOption.No(MoveMode.AlreadyThere, "no position");

        double separation = (shipPos.AbsolutePosition - tgtPos.AbsolutePosition).Length();

        double tolerance = target.HasDataBlob<MassVolumeDB>()
            ? OrbitMath.LowOrbitRadius(target) * ArrivedWithinLowOrbits
            : ArrivedWithin_m;

        return separation <= tolerance
            ? MoveOption.Yes(MoveMode.AlreadyThere, 0)
            : MoveOption.No(MoveMode.AlreadyThere, "not there yet");
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
        if (parent == null || targetOrbit.Parent != parent)
        {
            // MISSING MATHS: leaving one SOI for another needs an escape burn, a transfer around
            // the grandparent, and a capture burn at the far end.
            // InterceptCalcs.InterPlanetaryHohmann is a sketch of this — it is untested, its
            // phasing is ad-hoc, and NewtonSimpleMoveDB does not do SOI transitions — so we do
            // not use it. Warp handles interplanetary for now.
            return MoveOption.No(mode, "target is under a different SOI parent; no interplanetary transfer maths yet");
        }

        if (shipOrbit.Eccentricity > MaxEccentricityForTransfer || targetOrbit.Eccentricity > MaxEccentricityForTransfer)
        {
            // MISSING MATHS: Hohmann2/HohmannOE/OrbitPhasingManuvers all assume circular orbits.
            // A general solution wants a Lambert solver, or a circularise-first leg.
            return MoveOption.No(mode, "transfer maths assume circular orbits");
        }

        if (target.HasDataBlob<SystemBodyInfoDB>())
        {
            // MISSING MATHS: arriving co-located with a body means falling into its SOI, and we
            // have no capture/insertion burn. NewtonSimpleProcessor does not transition SOI
            // parents either (NewtonianMovementProcessor does, but that is the NewtonComplex path
            // — see the TODO in agents-and-goals-design.md).
            // Warp already plots a low orbit and its insertion ΔV, so bodies go to warp for now.
            // Once there is a capture burn this restriction lifts and moon hops go newtonian.
            return MoveOption.No(mode, "no orbital capture maths for arriving at a body");
        }

        double sgp = shipOrbit.GravitationalParameter_m3S2;
        double shipRadius = shipOrbit.SemiMajorAxis;
        double targetRadius = targetOrbit.SemiMajorAxis;

        // True longitude in the reference plane. The sim is effectively 2D for these transfers,
        // and this matches how InterceptCalcs measures the same angles.
        double shipAngle = AngleOf(ship, now);
        double targetAngle = AngleOf(target, now);

        (Vector3 deltaV, double timeInSeconds)[] manuvers;

        if (Math.Abs(shipRadius - targetRadius) <= targetRadius * CoOrbitalRadiusTolerance)
        {
            // Same orbit, wrong place in it: drop into a phasing orbit and come back.
            // This is the case that should never have been a warp — matching orbits with
            // something we're already flying alongside.
            double phaseAngle = NormaliseAngle(targetAngle - shipAngle);
            if (phaseAngle == 0)
                return MoveOption.No(mode, "co-orbital and co-located");

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

        return MoveOption.Yes(mode, eta, totalDV, 0, manuvers);
    }

    static MoveOption EvaluateWarp(Entity ship, Entity target, DateTime now)
    {
        const MoveMode mode = MoveMode.Warp;

        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warpDB))
            return MoveOption.No(mode, "no warp drive");
        if (warpDB.MaxSpeed <= 0)
            return MoveOption.No(mode, "warp drive produces no speed");

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
        return MoveOption.Yes(mode, eta, 0, warpDB.BubbleCreationCost);
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
    public static bool TryBuildMoveActions(Entity ship, int targetId,
                                           out List<EntityAction> actions, out string reason)
    {
        actions = new List<EntityAction>();
        reason = string.Empty;

        if (!ship.Manager.TryGetGlobalEntityById(targetId, out var requested))
        {
            reason = "Target not found";
            return false;
        }
        if (!MoveTargeting.TryResolve(requested, out var target, out reason))
            return false;
        if (!CanMove(ship, out reason))
            return false;

        var chosen = Select(Evaluate(ship, target, ship.StarSysDateTime));
        if (!chosen.Feasible)
        {
            reason = chosen.Reason;
            return false;
        }
        if (chosen.Mode == MoveMode.AlreadyThere)
            return true;                       // empty actions, success

        actions = BuildActions(ship, target, ship.StarSysDateTime, chosen);
        return true;
    }
    /// <summary>
    /// Turn the chosen option into queued actions. Returns an empty list for AlreadyThere.
    /// </summary>
    internal static List<EntityAction> BuildActions(Entity ship, Entity target, DateTime now, MoveOption chosen)
    {
        var actions = new List<EntityAction>();

        switch (chosen.Mode)
        {
            case MoveMode.AlreadyThere:
                break;
            
            case MoveMode.Warp:
            {
                actions.AddRange(BuildWarpAndCircularise(ship, target, now));
                break;
            }
            
            case MoveMode.NewtonianTransfer:
                actions.AddRange(BuildBurns(ship, now, chosen.Manuvers));
                break;
        }

        return actions;
    }

    internal static List<EntityAction> BuildWarpAndCircularise(Entity orderEntity, Entity targetEntity, DateTime now)
    {
        var actions = new List<EntityAction>();
        // 1. pure transit
        var lowOrbitRadius = OrbitMath.LowOrbitRadius(targetEntity);
        targetEntity.TryGetDataBlob<OrbitDB>(out var orbitDB);
        var soi = OrbitMath.GetSOIRadius(orbitDB);
        
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
        
        switch (targetEntity.GetDataBlob<PositionDB>().MoveType) //if the targetEntity's movetype is this:
        {
            case PositionDB.MoveTypes.None: //this means it's a grav anomaly, jump point
            {
                break;
            }
            case PositionDB.MoveTypes.Orbit:
            {
                var sgp = OrbitMath.SGP(targetEntity, orderEntity);

                (Vector3 pos, DateTime eti) targetIntercept  = WarpMath.GetInterceptPosition(orderEntity, targetEntity, now, endWarpPos);
                var targetOrbit = OrbitMath.KeplerCircularFromPosition(sgp, endWarpPos, targetIntercept.eti);
                var lowOrbitState = OrbitMath.GetStateVectors(targetOrbit, targetIntercept.eti);
                var targetEntityOrbitDb = targetEntity.GetDataBlob<OrbitDB>();
                Vector3 insertionVector = OrbitProcessor.GetOrbitalInsertionVector(departureState.vel, targetEntityOrbitDb, targetIntercept.eti);
                var deltaVRequired = insertionVector - (Vector3)lowOrbitState.velocity;
                var endWarpOrbit = OrbitMath.KeplerFromPositionAndVelocity(sgp, endWarpPos, insertionVector, targetIntercept.eti);

                actions.Add(NewtonSimpleAction.CreateCommand(orderEntity.FactionOwnerID, orderEntity, targetIntercept.eti, endWarpOrbit, targetOrbit));
                
                break;
            }
            case PositionDB.MoveTypes.NewtonSimple:
            case PositionDB.MoveTypes.NewtonComplex:
                // A targetEntity under thrust has no closed-form future position, so WarpMath can't
                // solve the intercept. MovePlanner rejects these before we get here; if we're
                // reached anyway it's a bug in the caller, not a case to guess at.
                throw new NotImplementedException(
                    $"No warp intercept solution against a {targetEntity.GetDataBlob<PositionDB>().MoveType} targetEntity.");

            case PositionDB.MoveTypes.Warp:
                // MoveTargeting.TryResolve chases warping targets to their destination, so a
                // warping targetEntity should be impossible by this point.
                throw new InvalidOperationException("Warp targetEntity was not resolved to its destination.");

            default:
                throw new NotImplementedException();
        }
    
        
        
        
        return actions;

    }
    
    /// <summary>
    /// Prograde-frame burns become NewtonSimpleActions: each carries the orbit it starts from and
    /// the orbit it should end in, and NewtonSimpleProcessor charges the fuel for the difference.
    ///
    /// We use the Simple path rather than NewtonThrustAction deliberately — NewtonThrustAction
    /// goes through NewtonMoveDB / the NewtonComplex integrator, which is the older
    /// full-thrust-simulation code and is not currently trusted.
    /// </summary>
    static List<EntityAction> BuildBurns(Entity ship, DateTime now, (Vector3 deltaV, double timeInSeconds)[] manuvers)
    {
        var actions = new List<EntityAction>();
        if (manuvers.Length == 0) return actions;

        var orbit = ship.GetDataBlob<OrbitDB>();
        double sgp = orbit.GravitationalParameter_m3S2;

        // Each burn starts from the orbit the previous burn put us in.
        KeplerElements startKE = orbit.GetElements();

        foreach (var manuver in manuvers)
        {
            DateTime nodeTime = now + TimeSpan.FromSeconds(manuver.timeInSeconds);

            var state = OrbitalMath.GetStateVectors(startKE, nodeTime);
            var position = state.position;
            var velocity = (Vector3)state.velocity;

            // Manoeuvres come out of the orbital maths in the prograde/radial/normal frame.
            var deltaV = OrbitalMath.ProgradeToStateVector(sgp, manuver.deltaV, position, velocity);
            var endKE = OrbitMath.KeplerFromPositionAndVelocity(sgp, position, velocity + deltaV, nodeTime);

            actions.Add(NewtonSimpleAction.CreateCommand(ship.FactionOwnerID, ship, nodeTime, startKE, endKE));

            startKE = endKE;
        }

        return actions;
    }

    // ---------------------------------------------------------------------

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





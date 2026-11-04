using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.People;
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

    public void Cancel(Entity unit, Goal goal, DateTime at, CancelTiming timing)
    {
        if (timing == CancelTiming.Now || !BubbleUp(unit))
        {
            GoalCancel.Apply(unit, goal, at, timing);
            return;
        }

        if (!unit.TryGetDataBlob<ActionQueueDB>(out var queue))
            return;

        // The bubble is one transit. Keep the warp and the circularise behind it.
        foreach (var action in queue.ActionsFor(goal))
        {
            if (action is WarpMoveAction or CirculariseAction)
                continue;
            action.Stop(at);
            queue.ActionList.Remove(action);
        }
    }

    static bool BubbleUp(Entity unit)
        => unit.TryGetDataBlob<WarpMovingDB>(out var warp) && warp.HasStarted && !warp.IsAtTarget;

    public PlanResult Plan(Entity managedEntity, Goal goal, DateTime atDateTime)
    {
        if (managedEntity.HasDataBlob<FleetDB>())
            return PlanSubGoals(managedEntity, goal);
        if (managedEntity.HasDataBlob<ShipInfoDB>())
            return PlanActions(managedEntity, goal, atDateTime);

        return PlanResult.Fail("Non supported entity");
    }

    /// <summary>
    /// Leaf: resolve target, pick move mode, emit actions. Does not write <paramref name="goal"/>.Status.
    /// A brake step may set <see cref="Goal.BrakeSteps"/> once.
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
                {
                    foreach (var action in actionsForGoal)
                    {
                        if (action.Details.StartsWith("Elliptical orbit"))
                            return PlanResult.Done(action.Details);
                    }
                    return PlanResult.Done();
                }
                return PlanResult.Continue(new List<EntityAction>());
            }
        }

        if (!MovePlanner.TryBuildMoveActions(ship, requested, out var actions, out string reason, atDateTime, goal))
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
        int deferred = 0;

        foreach (var subunit in db.Children)
        {
            if (!MovePlanner.CanMove(subunit, out _))
                continue;
            capable++;

            if (FleetChildDuty.WorkingThisGoal(subunit, goal))
            {
                alreadyWorking++;
                continue;
            }

            // Own order, or a plot the player queued on an empty chair. Retry later.
            if (FleetChildDuty.BusyWithOwnWork(subunit, goal))
            {
                deferred++;
                continue;
            }

            subGoals.Add((subunit, new Goal(GoalType.MoveTo)
            {
                TargetEntityID = goal.TargetEntityID,
                ParentGoalId = goal.Id,
                ArrivalPeriapsis = goal.ArrivalPeriapsis,
                ArrivalApoapsis = goal.ArrivalApoapsis,
            }));
        }

        if (capable == 0)
            return PlanResult.Fail("no subordinates can move");

        if (subGoals.Count == 0)
        {
            // Already on this order, or away on their own work. Stay active and retry.
            return alreadyWorking > 0 || deferred > 0
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
/// Timing rows and route legs copied onto a <see cref="TripPreview"/>. Default means the option
/// has no porkchop (a Hohmann, a warp, or already there).
/// </summary>
public struct PreviewFacts
{
    public bool HasTiming;
    public DepartureTiming Timing;
    public double WaitSeconds;
    public string Note;
    public bool HasAlt;
    public DepartureTiming AltTiming;
    public double AltWaitSeconds;
    public double AltEtaSeconds;
    public double AltDeltaV;
    public bool AltFeasible;
    public string AltNote;
    public bool CanPick;
    public TripLeg[] Legs;
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
    /// <summary>Seconds from now until the departure burn. Zero when it starts now.</summary>
    public readonly double DepartSeconds;
    /// <summary>Reaction mass budget in m/s. Zero for warp.</summary>
    public readonly double DeltaV;
    /// <summary>Warp bubble energy. Zero for newtonian.</summary>
    public readonly double EnergyCost;

    /// <summary>Prograde-frame burns and their cumulative offsets from now, in seconds.</summary>
    public readonly (Vector3 deltaV, double timeInSeconds)[] Manuvers;

    /// <summary>One-level sphere change. <see cref="MovePlanner.TryBuildMoveActions"/> queues a transfer, not a match.</summary>
    public readonly bool SoiTransfer;

    /// <summary>Set when the Hohmann branch took a Lambert window instead of the closed form.</summary>
    public readonly TransferWindow? Window;

    /// <summary>
    /// Patched-conic trip between two planets. Warp stays ahead of this when both are feasible.
    /// A transfer inside one sphere leaves it false.
    /// </summary>
    public readonly bool Interplanetary;

    public bool HasWindow => Window is { IsFinite: true };

    /// <summary>Set when this option came from a porkchop. Empty for Hohmann and warp.</summary>
    public readonly PreviewFacts Facts;

    MoveOption(MoveMode mode, bool feasible, string reason, double eta, double deltaV, double energy,
        (Vector3 deltaV, double timeInSeconds)[] manuvers, bool soiTransfer = false, TransferWindow? window = null,
        bool interplanetary = false, double departSeconds = 0, PreviewFacts facts = default)
    {
        Mode = mode;
        Feasible = feasible;
        Reason = reason;
        EtaSeconds = eta;
        DeltaV = deltaV;
        EnergyCost = energy;
        Manuvers = manuvers ?? Array.Empty<(Vector3, double)>();
        SoiTransfer = soiTransfer;
        Window = window;
        Interplanetary = interplanetary;
        DepartSeconds = double.IsFinite(departSeconds) && departSeconds > 0 ? departSeconds : 0;
        Facts = facts;
    }

    public static MoveOption No(MoveMode mode, string reason, PreviewFacts facts = default)
        => new MoveOption(mode, false, reason, double.PositiveInfinity, 0, 0, null, facts: facts);

    public static MoveOption Yes(MoveMode mode, double etaSeconds, double deltaV = 0, double energyCost = 0,
        (Vector3 deltaV, double timeInSeconds)[] manuvers = null, string message = "", bool soiTransfer = false,
        TransferWindow? window = null, bool interplanetary = false, double departSeconds = 0, PreviewFacts facts = default)
        => new MoveOption(mode, true, message, etaSeconds, deltaV, energyCost, manuvers, soiTransfer, window, interplanetary, departSeconds, facts);
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

    /// <summary>Above this, HohmannOE is the wrong tool and the Lambert scan answers instead.</summary>
    const double ClosedFormEccentricity = 0.05;
    const double ClosedFormInclination = 2.0 * Math.PI / 180.0;

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
    public static List<MoveOption> Evaluate(Entity ship, Entity target, DateTime now,
        double arrivalPeriapsis = 0, double arrivalApoapsis = 0, MoveOrderOptions order = null)
    {
        order ??= new MoveOrderOptions(Periapsis: arrivalPeriapsis, Apoapsis: arrivalApoapsis);
        return new List<MoveOption>
        {
            EvaluateAlreadyThere(ship, target, now),
            EvaluateNewtonian(ship, target, now, order),
            EvaluateWarp(ship, target, now),
        };
    }

    /// <summary>
    /// The plan the Orders panel shows for one clicked target. Does not start a burn.
    /// A short tank for a one-level transfer also gets up to three arrival orbits that fit.
    /// </summary>
    public static TripPreview Preview(Entity ship, Entity target, DateTime now, MoveOrderOptions order = null)
    {
        if (ship == null || target == null)
            return TripPreview.None("target not found");
        order ??= new MoveOrderOptions();
        if (order.Arrival != ArrivalKind.Orbit)
            return TripPreview.None("not built");
        if (!CanMove(ship, out string cannot))
            return TripPreview.None(cannot);

        var options = Evaluate(ship, target, now, order.Periapsis, order.Apoapsis, order);
        var chosen = Choose(options, order.Mode, ship, target);
        double have = ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) ? thrust.DeltaV : 0;
        string reason = chosen.Reason;
        var newton = Find(options, MoveMode.NewtonianTransfer);
        if (!chosen.Feasible && newton.HasValue && newton.Value.Reason.Contains("Δv")
            && order.Mode != MoveDriveChoice.Warp)
            reason = newton.Value.Reason;
        bool dvRefuse = !chosen.Feasible && (reason ?? "").Contains("Δv");
        var alternatives = dvRefuse
            ? FindArrivals(ship, target, now, have)
            : Array.Empty<ArrivalChoice>();
        double depart = chosen.Feasible ? chosen.DepartSeconds : 0;
        bool warp = chosen.Mode == MoveMode.Warp;
        bool showTiming = chosen.Mode == MoveMode.NewtonianTransfer && chosen.Facts.HasTiming;
        var timings = showTiming ? TimingRows(ship, chosen) : null;
        string timingNote = showTiming ? chosen.Facts.Note ?? "" : "";
        double wait = showTiming ? chosen.Facts.WaitSeconds : 0;
        DepartureTiming timing = showTiming ? chosen.Facts.Timing : order.Timing;
        bool canPick = chosen.Mode == MoveMode.NewtonianTransfer && chosen.Facts.CanPick;
        TripLeg[] legs = LegsOf(chosen);
        return new TripPreview(
            chosen.Feasible,
            reason ?? "",
            ModeName(chosen.Mode),
            chosen.Feasible ? chosen.DeltaV : DeltaVFrom(reason),
            have,
            chosen.Feasible ? chosen.EtaSeconds : 0,
            dvRefuse,
            alternatives,
            depart,
            OtherModes(options, chosen.Mode),
            legs,
            timing,
            wait,
            timings,
            timingNote,
            warp && chosen.Feasible,
            canPick);
    }

    static ModeOffer[] OtherModes(IReadOnlyList<MoveOption> options, MoveMode chosen)
    {
        var list = new List<ModeOffer>();
        foreach (var option in options)
        {
            if (option.Mode == chosen)
                continue;
            if (option.Mode == MoveMode.AlreadyThere && !option.Feasible)
                continue;
            list.Add(new ModeOffer(
                ModeName(option.Mode),
                option.Feasible,
                option.Feasible ? option.EtaSeconds : 0,
                option.Feasible ? option.DeltaV : 0,
                option.Reason ?? ""));
        }
        return list.ToArray();
    }

    static TimingOffer[] TimingRows(Entity ship, MoveOption chosen)
    {
        var facts = chosen.Facts;
        var rows = new List<TimingOffer>
        {
            new TimingOffer(facts.Timing, facts.WaitSeconds, chosen.Feasible ? chosen.EtaSeconds : 0,
                chosen.Feasible ? chosen.DeltaV : 0, FuelKg(ship, chosen.DeltaV), chosen.Feasible, facts.Note ?? ""),
        };
        if (facts.HasAlt)
        {
            rows.Add(new TimingOffer(facts.AltTiming, facts.AltWaitSeconds, facts.AltEtaSeconds, facts.AltDeltaV,
                FuelKg(ship, facts.AltDeltaV), facts.AltFeasible, facts.AltNote ?? ""));
        }
        return rows.ToArray();
    }

    static double FuelKg(Entity ship, double deltaV)
    {
        if (!(deltaV > 0) || ship == null)
            return double.NaN;
        if (!ship.TryGetDataBlob<MassVolumeDB>(out var mass) || !(mass.MassTotal > 0))
            return double.NaN;
        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) || !(thrust.ExhaustVelocity > 0))
            return double.NaN;
        double fuel = OrbitMath.TsiolkovskyFuelUse(mass.MassTotal, thrust.ExhaustVelocity, deltaV);
        return double.IsFinite(fuel) && fuel >= 0 ? fuel : double.NaN;
    }

    static TripLeg[] LegsOf(MoveOption chosen)
    {
        if (chosen.Facts.Legs is { Length: > 0 })
            return chosen.Facts.Legs;
        if (chosen.Mode == MoveMode.Warp && chosen.Feasible)
            return new[] { new TripLeg("warp", 0, 0, chosen.EtaSeconds) };
        if (chosen.Manuvers == null || chosen.Manuvers.Length == 0)
            return null;
        var legs = new TripLeg[chosen.Manuvers.Length];
        for (int i = 0; i < chosen.Manuvers.Length; i++)
        {
            double at = chosen.Manuvers[i].timeInSeconds;
            if (!double.IsFinite(at) || at < 0)
                at = 0;
            string kind = chosen.Manuvers.Length == 2 ? (i == 0 ? "departure" : "arrival") : "burn";
            legs[i] = new TripLeg(kind, chosen.Manuvers[i].deltaV.Length(), at, at);
        }
        return legs;
    }

    static string ModeName(MoveMode mode) => mode switch
    {
        MoveMode.AlreadyThere => "Already there",
        MoveMode.NewtonianTransfer => "Newtonian",
        MoveMode.Warp => "Warp",
        _ => mode.ToString(),
    };

    static double DeltaVFrom(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            return 0;
        int at = reason.IndexOf("needs ", StringComparison.Ordinal);
        if (at < 0)
            return 0;
        int start = at + "needs ".Length;
        int end = start;
        while (end < reason.Length && (char.IsDigit(reason[end]) || reason[end] == ','))
            end++;
        if (end == start)
            return 0;
        string digits = reason.Substring(start, end - start).Replace(",", "");
        return double.TryParse(digits, out double dv) ? dv : 0;
    }

    static ArrivalChoice[] FindArrivals(Entity ship, Entity target, DateTime now, double have)
    {
        if (!OneLevel(ship, target, out var parent, out var shipOrbit, out var targetOrbit))
            return Array.Empty<ArrivalChoice>();
        double low = OrbitMath.LowOrbitRadius(target);
        double soi = target.GetSOI_m();
        if (!(low > 1) || !double.IsFinite(soi) || soi < low * 2)
            return Array.Empty<ArrivalChoice>();
        double apoCap = soi * OrbitMath.SoiCaptureFraction;

        var found = new List<ArrivalChoice>();
        void Consider(double peri, double apo, string label)
        {
            var plan = SoiTransferMath.Evaluate(
                ship, target, parent, shipOrbit, targetOrbit, now, double.PositiveInfinity, peri, apo);
            if (plan.Kind != SoiTransferMath.Kind.Ready || !(plan.DeltaV <= have) || !(plan.DeltaV > 0))
                return;
            foreach (var prior in found)
            {
                if (Math.Abs(prior.DeltaV - plan.DeltaV) < 50)
                    return;
            }
            found.Add(new ArrivalChoice(peri, apo, plan.DeltaV, label));
        }

        Consider(low, apoCap, "Ellipse");
        double hi = Math.Min(apoCap * 0.5, low * 30);
        for (double radius = low * 2; radius < hi; radius *= 2)
            Consider(radius, 0, "Circular");

        found.Sort((a, b) => a.DeltaV.CompareTo(b.DeltaV));
        if (found.Count > 3)
            found.RemoveRange(3, found.Count - 3);
        return found.ToArray();
    }

    static bool OneLevel(Entity ship, Entity target, out Entity parent, out OrbitDB shipOrbit, out OrbitDB targetOrbit)
    {
        parent = ship.GetSOIParentEntity();
        shipOrbit = null;
        targetOrbit = null;
        if (parent == null
            || !ship.TryGetDataBlob(out shipOrbit)
            || !target.TryGetDataBlob(out targetOrbit)
            || targetOrbit.Parent == null)
            return false;
        if (targetOrbit.Parent == parent && parent != target)
            return true;
        return parent.TryGetDataBlob<OrbitDB>(out var parentOrbit) && parentOrbit.Parent == target;
    }

    /// <summary>
    /// Pick one. PLACEHOLDER POLICY — the eventual design is a utility comparison over
    /// (eta, deltaV, energy) weighted by the owning goal: a Trade run should value fuel, an
    /// Intercept should value time, a ship on DontRunOutOfFuel should refuse an expensive option
    /// outright. Until the goal layer can express that, the ordering is:
    ///
    ///   1. already there — do nothing
    ///   2. warp — when the newtonian option is an interplanetary patched conic and warp can fly
    ///   3. newtonian — inside one sphere, and interplanetary when warp cannot
    /// </summary>
    public static MoveOption Select(IReadOnlyList<MoveOption> options)
    {
        MoveOption? arrived = Find(options, MoveMode.AlreadyThere);
        if (arrived.HasValue && arrived.Value.Feasible) return arrived.Value;

        MoveOption? newt = Find(options, MoveMode.NewtonianTransfer);
        MoveOption? warp = Find(options, MoveMode.Warp);

        bool newtOk = newt.HasValue && newt.Value.Feasible;
        bool warpOk = warp.HasValue && warp.Value.Feasible;

        // Inside one sphere, Newtonian stays first. A Phobos phasing burn is hours and
        // warp is seconds; losing that comparison is how MoveTo Phobos kept warping.
        // Between planets, warp stays the default when the ship has both.
        if (newtOk && warpOk && newt.Value.Interplanetary)
            return warp.Value;
        if (newtOk) return newt.Value;
        if (warpOk) return warp.Value;

        // Nothing works. Hand back the collected reasons so the goal can say why.
        var why = new List<string>();
        foreach (var o in options)
            if (!o.Feasible && !string.IsNullOrEmpty(o.Reason))
                why.Add($"{o.Mode}: {o.Reason}");

        return MoveOption.No(MoveMode.Warp, string.Join("\n", why));
    }

    /// <summary>
    /// <see cref="Select"/> for Auto. Warp and Newtonian force that mode.
    /// A forced warp that is already in the destination well falls through to Newtonian,
    /// so the post-warp replan still uses the order's timing.
    /// </summary>
    public static MoveOption Choose(IReadOnlyList<MoveOption> options, MoveDriveChoice mode, Entity ship, Entity target)
    {
        if (mode == MoveDriveChoice.Newtonian)
            return Find(options, MoveMode.NewtonianTransfer) ?? MoveOption.No(MoveMode.NewtonianTransfer, "no reaction drive");
        if (mode == MoveDriveChoice.Warp)
        {
            MoveOption? warp = Find(options, MoveMode.Warp);
            if (warp.HasValue && warp.Value.Feasible)
                return warp.Value;
            if (ship != null && target != null && InsideDropIn(ship, target))
            {
                MoveOption? newt = Find(options, MoveMode.NewtonianTransfer);
                if (newt.HasValue)
                    return newt.Value;
            }
            return warp ?? MoveOption.No(MoveMode.Warp, "no warp drive");
        }

        return Select(options);
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

        // Static sites (grav anomalies, jump points) have no circular orbit to match.
        // Close enough is the same 100 km JPSurveyProcessor uses to apply points.
        if (tgtPos.MoveType == PositionDB.MoveTypes.None)
        {
            double siteSep = ((Vector3)MoveMath.GetAbsoluteFuturePosition(ship, now)
                              - (Vector3)MoveMath.GetAbsoluteFuturePosition(target, now)).Length();
            return siteSep <= 100_000
                ? MoveOption.Yes(MoveMode.AlreadyThere, 0, message: $"Already at {NameOf(target, ship)}")
                : MoveOption.No(MoveMode.AlreadyThere, "not there yet");
        }

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
        {
            // Bound, and the tank cannot buy a tighter orbit. The goal completes
            // on that ellipse instead of queueing a burn the processor would snap.
            if (parent == target && eccentricity < 1)
            {
                DateTime sampleAt = CirculariseAction.SampleAt(ship, now);
                double budget = ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var tank) ? tank.DeltaV : 0;
                if (TryCurrentRelativeState(ship, sampleAt, out var holdState)
                    && TryArrivalBurn(ship, parent, holdState.pos, holdState.vel, budget, sampleAt,
                        out _, out bool hold, out _, out double shortfall)
                    && hold)
                    return MoveOption.Yes(MoveMode.AlreadyThere, 0,
                        message: EllipticalOrbitDetails(parent, ship, shortfall));
            }
            return MoveOption.No(MoveMode.AlreadyThere, "not in a circular orbit");
        }

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

        // Cannot enter the target's SOI (Phobos, ship, station). Warp drop-in is
        // dest+offset, so we are already next to them; leftover circular SMA can
        // differ from their a by a*e. Don't Hohmann to SMA from 13 km away.
        if (!target.TryGetDataBlob<OrbitDB>(out var targetOrbit) || targetOrbit.Parent != parent)
            return MoveOption.No(MoveMode.AlreadyThere, "target orbit is not around this parent");

        double targetRadius = targetOrbit.SemiMajorAxis;
        double along = AlongTrackArrivedSeparation(target, targetRadius);
        if (separation <= along)
            return MoveOption.Yes(MoveMode.AlreadyThere, 0, message: $"Matching {NameOf(target, ship)}'s orbit");

        if (!IsCoOrbital(shipRadius, targetRadius, target)
            && !IsCoOrbital(posRel.Length(), targetRadius, target))
            return MoveOption.No(MoveMode.AlreadyThere, "not in the target's orbit");

        return MoveOption.No(MoveMode.AlreadyThere, "in the orbit but not with the target yet");
    }

    /// <summary>
    /// Reaction-drive transfer. Only the cases we have working maths for; everything else is
    /// rejected with a reason rather than plotted badly.
    /// </summary>
    static MoveOption EvaluateNewtonian(Entity ship, Entity target, DateTime now, MoveOrderOptions order)
    {
        const MoveMode mode = MoveMode.NewtonianTransfer;
        order ??= new MoveOrderOptions();
        double arrivalPeriapsis = order.Periapsis;
        double arrivalApoapsis = order.Apoapsis;

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

        DateTime sampleAt = CirculariseAction.SampleAt(ship, now);
        if (!TryCurrentRelativeState(ship, sampleAt, out var state))
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
            var circKE = OrbitMath.KeplerCircularFromPosition(sgp, state.pos, sampleAt);
            var circV = OrbitMath.GetStateVectors(circKE, sampleAt).velocity;
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
                // Warp arrival replans from this well after the preview circularise is
                // dropped. A tank that can capture but not circularise still queues the burn.
                if (!TryArrivalBurn(ship, parent, state.pos, state.vel, thrust.DeltaV, sampleAt,
                        out bool circular, out bool hold, out Vector3 slowed, out _))
                    return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");

                double dv = circular || hold
                    ? circulariseDV
                    : state.vel.Length() - slowed.Length();
                if (!double.IsFinite(dv) || dv < 0)
                    dv = 0;
                return MoveOption.Yes(mode, 0, dv,
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

        // Earth to Mars, one level each side of the star. A moon to another planet is
        // two escapes and stays on the refusal below.
        if (PatchedConic.IsOneLevelTransfer(ship, target))
            return EvaluatePatched(ship, target, now, thrust.DeltaV, order);

        // One level in or out (Earth to Luna, Luna to Earth).
        if (dropInParent == target)
        {
            var soi = SoiTransferMath.Evaluate(
                ship, target, parent, shipOrbit, targetOrbit, now, thrust.DeltaV,
                arrivalPeriapsis, arrivalApoapsis);
            if (soi.Kind == SoiTransferMath.Kind.NotApplicable)
                return MoveOption.No(mode, "target body has a usable SOI; warp");
            if (soi.Kind == SoiTransferMath.Kind.Circularise)
            {
                if (circulariseDV > thrust.DeltaV)
                    return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
                return MoveOption.Yes(mode, 0, circulariseDV, message: soi.Reason);
            }
            if (soi.Kind != SoiTransferMath.Kind.Ready)
                return MoveOption.No(mode, soi.Reason);
            double departAt = ForwardSeconds(soi.DepartAt, now);
            var leg = new PreviewFacts
            {
                Legs = new[] { new TripLeg("transfer", soi.DeltaV, departAt, soi.EtaSeconds) },
            };
            return MoveOption.Yes(mode, soi.EtaSeconds, soi.DeltaV,
                manuvers: new (Vector3, double)[] { (Vector3.Zero, soi.EtaSeconds) },
                message: soi.Reason,
                soiTransfer: true,
                departSeconds: departAt,
                facts: leg);
        }
        if (parent != dropInParent || targetOrbit.Parent != dropInParent)
        {
            return MoveOption.No(mode, "target is under a different SOI parent; no interplanetary transfer maths yet");
        }

        // Leftover dump is a hyperbola. A near-circular coplanar pair keeps HohmannOE.
        // Anything eccentric or inclined asks the Lambert scan, which is the case the
        // closed form cannot answer. A short tank that cannot fly the window still circularises.
        bool closedForm = !needsCircularise
            && targetOrbit.Eccentricity <= ClosedFormEccentricity
            && RelativeInclination(shipOrbit, targetOrbit) <= ClosedFormInclination;
        if (!closedForm)
        {
            if (TryLambertWindow(ship, target, parent, now, thrust.DeltaV, order, out MoveOption windowed))
                return windowed;
            if (needsCircularise)
            {
                if (circulariseDV > thrust.DeltaV)
                    return MoveOption.No(mode, $"needs {circulariseDV:N0} m/s Δv, have {thrust.DeltaV:N0} m/s");
                return MoveOption.Yes(mode, 0, circulariseDV,
                    message: $"Circularise to match {NameOf(target, ship)}");
            }

            return MoveOption.No(mode, "transfer solution did not converge");
        }

        // Hyperbolic SMA is negative; Hohmann/phasing run from the circular radius we'll have.
        double shipRadius = shipOrbit.SemiMajorAxis;
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
            return MoveOption.No(mode, "co-orbital and co-located");

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

        bool hohmann = !IsCoOrbital(shipRadius, targetRadius, target);
        double depart = 0;
        if (hohmann && manuvers.Length > 0 && double.IsFinite(manuvers[0].timeInSeconds) && manuvers[0].timeInSeconds > 0)
            depart = manuvers[0].timeInSeconds;
        string match = hohmann
            ? $"Hohmann to {NameOf(target, ship)}"
            : $"Phasing to {NameOf(target, ship)}";
        string when = depart > 60
            ? $"depart {FormatArrival(now.AddSeconds(depart))}, arrive {FormatArrival(now.AddSeconds(eta))}"
            : FormatArrival(now.AddSeconds(eta));
        return MoveOption.Yes(mode, eta, totalDV, 0, manuvers,
            $"{match}, {when}", departSeconds: depart);
    }

    static MoveOption EvaluatePatched(Entity ship, Entity target, DateTime now, double budget, MoveOrderOptions order)
    {
        const MoveMode mode = MoveMode.NewtonianTransfer;
        order ??= new MoveOrderOptions();
        TransferWindow? picked = ToWindow(order.PickedWindow);
        var plan = PatchedConicPlanner.TryPlan(
            ship, target, now, budget, order.Periapsis, order.Apoapsis, order.Timing, picked);
        var facts = PatchedFacts(plan, now);
        if (!plan.Feasible)
            return MoveOption.No(mode, string.IsNullOrEmpty(plan.Reason) ? "transfer did not converge" : plan.Reason, facts);

        double eta = (plan.ArriveAt - now).TotalSeconds;
        if (!double.IsFinite(eta) || eta < 0)
            eta = 0;
        string message = $"Transfer to {NameOf(target, ship)}, depart {FormatArrival(plan.DepartAt)}, arrive {FormatArrival(plan.ArriveAt)}";
        if (!string.IsNullOrEmpty(plan.Reason))
            message += ", " + plan.Reason;
        TransferWindow? flown = plan.HasFlownWindow ? plan.Flown : null;
        return MoveOption.Yes(mode, eta, plan.TotalDeltaV,
            message: message, soiTransfer: true, window: flown, interplanetary: true,
            departSeconds: ForwardSeconds(plan.DepartAt, now), facts: facts);
    }

    static PreviewFacts PatchedFacts(PatchedConicPlanner.Plan plan, DateTime now)
    {
        double depart = ForwardSeconds(plan.DepartAt, now);
        double exit = ForwardSeconds(plan.ExitAt, now);
        double arrive = ForwardSeconds(plan.ArriveAt, now);
        TripLeg[] legs = plan.Feasible
            ? new[]
            {
                new TripLeg("escape", plan.EscapeDeltaV, depart, exit),
                new TripLeg("cruise", plan.PlaneChange, exit, arrive),
                new TripLeg("capture", plan.CaptureDeltaV, arrive, arrive),
            }
            : null;
        if (!plan.HasTiming)
        {
            return new PreviewFacts { CanPick = plan.CanPick, Legs = legs };
        }

        var answer = plan.TimingAnswer;
        bool hasWindow = plan.Feasible;
        bool hasAlt = answer.HasAlt && answer.AltWindow.IsFinite;
        return new PreviewFacts
        {
            HasTiming = true,
            Timing = plan.Timing,
            WaitSeconds = hasWindow ? depart : 0,
            Note = plan.TimingNote ?? "",
            HasAlt = hasAlt,
            AltTiming = answer.AltTiming,
            AltWaitSeconds = hasAlt ? NonNeg((answer.AltWindow.Depart - now).TotalSeconds) : 0,
            AltEtaSeconds = hasAlt ? NonNeg((answer.AltWindow.Arrive - now).TotalSeconds) : 0,
            AltDeltaV = hasAlt ? answer.AltWindow.TotalDeltaV : 0,
            AltFeasible = hasAlt && answer.AltFeasible,
            AltNote = answer.AltNote ?? "",
            CanPick = true,
            Legs = legs,
        };
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

        (Vector3 pos, Vector3 vel) departureState;
        if (ship.Manager.Game.Settings.UseRelativeVelocity)
            departureState = MoveMath.GetRelativeFutureState(ship, now);
        else
            departureState = MoveMath.GetAbsoluteState(ship, now);
        var offset = PlannedWarpExitOffset(ship, target, departureState.vel);
        var cruise = WarpMath.PriceCruise(ship, target, now, offset);
        double eta = (cruise.exitTime - now).TotalSeconds;
        if (double.IsNaN(eta) || eta < 0)
            return MoveOption.No(mode, "warp intercept did not converge");

        // Sustain and collapse costs are charged by WarpMoveProcessor as the bubble runs; only
        // the up-front cost is knowable here. WarpMoveAction holds the goal on "Charging
        // batteries" if we can't afford even that, so it isn't a feasibility gate.
        // The leg carries cruise speed. DeltaV stays 0: warp does not spend reaction mass.
        double legSpeed = cruise.speed > 0 && double.IsFinite(cruise.speed) ? cruise.speed : warpDB.MaxSpeed;
        var warpLeg = new PreviewFacts
        {
            Legs = new[] { new TripLeg("warp", legSpeed, 0, eta) },
        };
        return MoveOption.Yes(mode, eta, 0, warpDB.BubbleCreationCost,
            message: $"Warp to {NameOf(target, ship)}, {FormatArrival(cruise.exitTime)}",
            facts: warpLeg);
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
                                           out List<EntityAction> actions, out string reason, Goal goal = null)
        => TryBuildMoveActions(ship, targetEntity, out actions, out reason, ship.StarSysDateTime, goal);

    public static bool TryBuildMoveActions(Entity ship, Entity targetEntity,
                                           out List<EntityAction> actions, out string reason, DateTime now, Goal goal = null,
                                           double arrivalPeriapsis = 0, double arrivalApoapsis = 0,
                                           MoveOrderOptions options = null)
    {
        actions = new List<EntityAction>();
        reason = string.Empty;

        if (!MoveTargeting.TryResolve(targetEntity, out var target, out reason))
            return false;
        if (!CanMove(ship, out reason))
            return false;

        MoveOrderOptions order = OrderFrom(goal, options, arrivalPeriapsis, arrivalApoapsis);
        if (order.Arrival != ArrivalKind.Orbit)
        {
            reason = "not built";
            return false;
        }

        double peri = order.Periapsis;
        double apo = order.Apoapsis;
        var moveOptions = Evaluate(ship, target, now, peri, apo, order);
        var chosen = Choose(moveOptions, order.Mode, ship, target);
        bool allowBrake = order.Mode != MoveDriveChoice.Newtonian;
        if (chosen.Feasible && chosen.Mode == MoveMode.AlreadyThere)
        {
            reason = chosen.Reason;
            return true;
        }

        if (chosen.Feasible && chosen.Mode != MoveMode.Warp)
        {
            reason = chosen.Reason;
            if (chosen.HasWindow && chosen.Window is { } hinted)
                return PlanFromWindow(ship, target, hinted, now, out actions, out reason);
            if (chosen.SoiTransfer)
                actions.Add(SoiTransferAction.CreateCommand(ship, target, now, peri, apo));
            else if (chosen.Manuvers.Length == 0)
                actions.Add(CirculariseAction.CreateCommand(ship, now));
            else if (ship.GetSOIParentEntity() == target)
                actions.Add(ChangeOrbitalAltitudeAction.CreateCommand(ship, OrbitMath.LowOrbitRadius(target), now));
            else
                actions.Add(MatchOrbitAction.CreateCommand(ship, target, now));
            return true;
        }

        if (chosen.Feasible && chosen.Mode == MoveMode.Warp)
        {
            if (CaptureAllowsWarp(ship, target, now, out string captureReason))
            {
                reason = chosen.Reason;
                actions.AddRange(BuildWarpAndCircularise(ship, target, now));
                return true;
            }

            // A hand plot does not replan, so it does not open the first bubble.
            if (goal != null
                && CommanderTechniques.KnowsWarpBrake(ship)
                && TryBrakeForecast(ship, target, now, out var entry)
                && entry.Kind == BrakeKind.Warp)
                return EmitBrakeWarp(ship, target, now, goal, entry, false, actions, out reason);

            reason = captureReason;
            return false;
        }

        if (allowBrake
            && goal != null
            && InsideDropIn(ship, target)
            && CommanderTechniques.KnowsWarpBrake(ship)
            && TryBrakeForecast(ship, target, now, out var brake))
        {
            if (brake.Kind == BrakeKind.Coast)
                return EmitCoast(ship, now, goal, brake, actions, out reason);
            if (brake.Kind == BrakeKind.Warp)
                return EmitBrakeWarp(ship, target, now, goal, brake, true, actions, out reason);
        }

        if (allowBrake
            && InsideDropIn(ship, target)
            && CommanderTechniques.KnowsWarpBrake(ship)
            && !CaptureAllowsWarp(ship, target, now, out string offered))
        {
            reason = offered;
            return false;
        }

        reason = chosen.Reason;
        return false;
    }

    public static List<EntityAction> BuildWarpAndCircularise(Entity orderEntity, Entity targetEntity, DateTime now)
    {
        var actions = new List<EntityAction>();
        (Vector3 pos, Vector3 vel) departureState;
        if(orderEntity.Manager.Game.Settings.UseRelativeVelocity)
        {
            departureState = MoveMath.GetRelativeFutureState(orderEntity, now);
        }
        else
            departureState = MoveMath.GetAbsoluteState(orderEntity, now);

        var endWarpPos = PlannedWarpExitOffset(orderEntity, targetEntity, departureState.vel);

        actions.Add(WarpMoveAction.CreateWarpOnly(orderEntity, targetEntity, now, endWarpPos));
        if (targetEntity.GetDataBlob<PositionDB>().MoveType == PositionDB.MoveTypes.Orbit)
            actions.Add(CirculariseAction.CreateCommand(orderEntity));
        return actions;
    }

    /// <summary>
    /// Best arrival the tank can finish at this position and velocity.
    /// Circular: the full circularise fits. Hold: already on an acceptable ellipse, do not burn.
    /// Otherwise <paramref name="slowed"/> is the same direction, shed by up to the budget,
    /// and still at least circular speed. False when no capture orbit exists or the budget
    /// cannot reach apoapsis at 90% of the parent's sphere of influence.
    /// </summary>
    internal static bool TryArrivalBurn(
        Entity ship, Entity parent, Vector3 pos, Vector3 vel, double budget, DateTime at,
        out bool circular, out bool hold, out Vector3 slowed, out double shortfall)
    {
        circular = false;
        hold = false;
        slowed = vel;
        shortfall = 0;
        try
        {
            if (parent == null || ship == null)
                return false;
            if (!double.IsFinite(budget) || budget < 0)
                budget = 0;

            double r = pos.Length();
            double v = vel.Length();
            if (!(r > 1) || !double.IsFinite(v) || v < 0)
                return false;
            if (!TryCaptureSpeed(parent, ship, r, out double vCap, out double mu))
                return false;

            var circKE = OrbitMath.KeplerCircularFromPosition(mu, pos, at);
            var circVel = (Vector3)OrbitMath.GetStateVectors(circKE, at).velocity;
            double vCirc = circVel.Length();
            if (!double.IsFinite(vCirc) || vCirc <= 0)
                return false;

            double toCircular = (vel - circVel).Length();
            if (!double.IsFinite(toCircular))
                return false;
            if (budget + 1e-6 >= toCircular)
            {
                if (toCircular < 0.01 && ApoapsisInside(parent, mu, pos, vel, at))
                {
                    hold = true;
                    shortfall = toCircular;
                    return true;
                }
                circular = true;
                return true;
            }

            // Slower than circular already. Slowing further raises the apoapsis.
            if (v + 1e-6 < vCirc)
            {
                if (!ApoapsisInside(parent, mu, pos, vel, at))
                    return false;
                hold = true;
                shortfall = toCircular;
                return true;
            }

            if (budget + 1e-6 < v - vCap)
                return false;

            double targetSpeed = Math.Max(vCirc, v - budget);
            double dv = v - targetSpeed;
            if (dv < 0.01)
            {
                if (!ApoapsisInside(parent, mu, pos, vel, at))
                    return false;
                hold = true;
                shortfall = toCircular;
                return true;
            }

            slowed = Vector3.Normalise(vel) * targetSpeed;
            shortfall = (slowed - circVel).Length();
            return double.IsFinite(shortfall);
        }
        catch
        {
            circular = false;
            hold = false;
            slowed = vel;
            shortfall = 0;
            return false;
        }
    }

    internal static string EllipticalOrbitDetails(Entity parent, Entity ship, double shortfall)
        => "Elliptical orbit around " + parent.GetName(ship.FactionOwnerID)
           + ", " + Stringify.Velocity(shortfall) + " short of circular";

    const int MaxBrakeWarps = 3;

    internal enum BrakeKind { Warp, Coast }

    internal struct BrakeForecast
    {
        public BrakeKind Kind;
        public int Bubbles;
        public int StillToOpen;
        public double ArrivalSpeed;
        public double CapRadius;
        public Entity DropIn;
    }

    internal static bool TryBrakeForecast(Entity ship, Entity target, DateTime now, out BrakeForecast forecast)
    {
        forecast = default;
        if (ship == null || target == null || !target.HasDataBlob<MassVolumeDB>())
            return false;
        double rLow = OrbitMath.LowOrbitRadius(target);
        if (!(rLow > 1) || !double.IsFinite(rLow))
            return false;

        var dropIn = PredictDropInParent(target, rLow);
        if (!TryCaptureSpeed(dropIn, ship, rLow, out _, out double mu))
            return false;
        if (!dropIn.TryGetDataBlob<OrbitDB>(out var dropOrbit))
            return false;
        double rCap = OrbitMath.SoiCaptureFraction * OrbitMath.GetSOIRadius(dropOrbit);
        if (!(rCap > rLow * 1.05) || !double.IsFinite(rCap))
            return false;

        if (!ship.TryGetDataBlob<PositionDB>(out _))
            return false;
        var live = MoveMath.GetRelativeFutureState(ship, now);
        double speed = live.Velocity.Length();
        double radius = live.pos.Length();
        if (!double.IsFinite(speed) || speed < 0 || !double.IsFinite(radius) || radius < 1)
            return false;

        var parent = ship.GetSOIParentEntity();
        double midpoint = (rLow + rCap) / 2.0;
        double budget = ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) ? thrust.DeltaV : 0;
        int bubbles = 0;
        int openedBeforeFirst = -1;
        BrakeKind first = BrakeKind.Coast;
        bool simulated = false;

        for (int guard = 0; guard < 8; guard++)
        {
            bool inside = parent != null && (parent == target || parent == dropIn);
            if (inside && radius <= rLow * 1.02)
            {
                Vector3 pos = simulated ? new Vector3(radius, 0, 0) : live.pos;
                Vector3 vel = simulated ? new Vector3(0, speed, 0) : live.Velocity;
                if (TryArrivalBurn(ship, dropIn, pos, vel, budget, now, out _, out _, out _, out _))
                {
                    if (openedBeforeFirst < 0 || bubbles < 1)
                        return false;
                    forecast = new BrakeForecast
                    {
                        Kind = first,
                        Bubbles = bubbles,
                        StillToOpen = bubbles - openedBeforeFirst,
                        ArrivalSpeed = speed,
                        CapRadius = rCap,
                        DropIn = dropIn,
                    };
                    return forecast.StillToOpen >= 1;
                }
            }

            if (!inside)
            {
                if (bubbles + 1 > MaxBrakeWarps)
                    return false;
                if (openedBeforeFirst < 0)
                {
                    openedBeforeFirst = bubbles;
                    first = BrakeKind.Warp;
                }
                bubbles++;
                parent = dropIn;
                radius = rLow;
                simulated = true;
                continue;
            }

            if (radius < midpoint)
            {
                if (!SpeedAt(speed, radius, rCap, mu, out double slowed) || speed - slowed < 1)
                    return false;
                if (openedBeforeFirst < 0)
                {
                    openedBeforeFirst = bubbles;
                    first = BrakeKind.Coast;
                }
                speed = slowed;
                radius = rCap;
                simulated = true;
                continue;
            }

            if (!SpeedAt(speed, radius, rLow, mu, out double atLow) || atLow - speed < 1)
                return false;
            if (bubbles + 1 > MaxBrakeWarps)
                return false;
            if (openedBeforeFirst < 0)
            {
                openedBeforeFirst = bubbles;
                first = BrakeKind.Warp;
            }
            bubbles++;
            radius = rLow;
            parent = dropIn;
            simulated = true;
        }

        return false;
    }

    static bool SpeedAt(double speed, double rFrom, double rTo, double mu, out double next)
    {
        next = 0;
        if (!(rFrom > 1) || !(rTo > 1) || !(mu > 0) || !double.IsFinite(speed))
            return false;
        double v2 = speed * speed + 2.0 * mu * (1.0 / rTo - 1.0 / rFrom);
        if (!double.IsFinite(v2) || v2 <= 0)
            return false;
        next = Math.Sqrt(v2);
        return double.IsFinite(next);
    }

    static bool InsideDropIn(Entity ship, Entity target)
    {
        double radius = PlannedWarpExitOffsetLength(target);
        var dropIn = PredictDropInParent(target, radius);
        var parent = ship.GetSOIParentEntity();
        return parent != null && (parent == target || parent == dropIn);
    }

    static void RememberBrake(Goal goal, BrakeForecast forecast)
    {
        if (goal != null && goal.BrakeSteps == 0)
            goal.BrakeSteps = forecast.Bubbles;
    }

    static string BrakeLine(Goal goal, BrakeForecast forecast)
    {
        int n = goal != null && goal.BrakeSteps > 0 ? goal.BrakeSteps : forecast.Bubbles;
        int k = n - forecast.StillToOpen + 1;
        if (n < 1)
            n = 1;
        if (k < 1)
            k = 1;
        if (k > n)
            k = n;
        return "Shedding speed, warp " + k + " of " + n;
    }

    static bool EmitCoast(Entity ship, DateTime now, Goal goal, BrakeForecast forecast,
        List<EntityAction> actions, out string reason)
    {
        RememberBrake(goal, forecast);
        reason = BrakeLine(goal, forecast);
        actions.Add(ShedSpeedAction.Create(ship, forecast.DropIn.Id, forecast.CapRadius, reason, now));
        return true;
    }

    static bool EmitBrakeWarp(Entity ship, Entity target, DateTime now, Goal goal, BrakeForecast forecast,
        bool fromInside, List<EntityAction> actions, out string reason)
    {
        Vector3 departureVel;
        if (fromInside)
            departureVel = MoveMath.GetRelativeFutureState(ship, now).Velocity;
        else if (ship.Manager.Game.Settings.UseRelativeVelocity)
            departureVel = MoveMath.GetRelativeFutureState(ship, now).Velocity;
        else
            departureVel = MoveMath.GetAbsoluteState(ship, now).velocity;

        var end = PlannedWarpExitOffset(ship, target, departureVel);
        actions.Add(WarpMoveAction.CreateWarpOnly(ship, target, now, end));
        RememberBrake(goal, forecast);
        reason = BrakeLine(goal, forecast);
        return true;
    }

    /// <summary>
    /// A captained ship does not open the warp when the pasted parent-relative speed
    /// cannot be captured. An empty chair is not gated. A fleet commander who is not
    /// aboard this ship does not count.
    /// </summary>
    static bool CaptureAllowsWarp(Entity ship, Entity target, DateTime now, out string reason)
    {
        reason = string.Empty;
        if (!CommanderSkills.TryGetCommander(ship, out _, out _))
            return true;

        double radius = PlannedWarpExitOffsetLength(target);
        if (!(radius > 1) || !double.IsFinite(radius))
            radius = 1;
        var dropIn = PredictDropInParent(target, radius);
        var rel = MoveMath.GetRelativeFutureState(ship, now);
        double speed = rel.Velocity.Length();
        if (!double.IsFinite(speed) || speed < 0)
            speed = 0;
        var pos = new Vector3(radius, 0, 0);
        var vel = new Vector3(0, speed, 0);
        // Nothing to capture into (jump point, anomaly, orbitless body): there is no capture
        // burn to pay for, so the gate does not apply.
        if (!TryCaptureSpeed(dropIn, ship, radius, out _, out _))
            return true;
        double budget = ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) ? thrust.DeltaV : 0;
        if (TryArrivalBurn(ship, dropIn, pos, vel, budget, now, out _, out _, out _, out _))
            return true;

        double need = CaptureNeed(ship, dropIn, radius, speed);
        reason = "Not enough Δv to orbit " + NameOf(dropIn, ship)
                 + " (need " + Stringify.Velocity(need) + ", have " + Stringify.Velocity(budget) + ")";
        return false;
    }

    static double CaptureNeed(Entity ship, Entity parent, double r, double speed)
    {
        if (TryCaptureSpeed(parent, ship, r, out double vCap, out _))
            return Math.Max(0, speed - vCap);
        return speed;
    }

    /// <summary>
    /// Speed at <paramref name="r"/> whose apoapsis is <see cref="OrbitMath.SoiCaptureFraction"/> of the parent's sphere.
    /// A parent with no orbit, or a sphere that does not contain <paramref name="r"/>, has none.
    /// </summary>
    static bool TryCaptureSpeed(Entity parent, Entity ship, double r, out double vCap, out double mu)
    {
        vCap = 0;
        mu = 0;
        if (parent == null || !parent.TryGetDataBlob<OrbitDB>(out var orbit))
            return false;
        double soi = OrbitMath.GetSOIRadius(orbit);
        if (!(soi > r) || !(r > 1))
            return false;
        double ra = OrbitMath.SoiCaptureFraction * soi;
        if (!(ra > r))
            return false;
        mu = OrbitMath.SGP(parent, ship);
        if (!(mu > 0) || !double.IsFinite(mu))
            return false;
        double a = (r + ra) / 2.0;
        vCap = Math.Sqrt(mu * (2.0 / r - 1.0 / a));
        return double.IsFinite(vCap) && vCap > 0;
    }

    static bool ApoapsisInside(Entity parent, double mu, Vector3 pos, Vector3 vel, DateTime at)
    {
        if (!parent.TryGetDataBlob<OrbitDB>(out var orbit))
            return false;
        double limit = OrbitMath.SoiCaptureFraction * OrbitMath.GetSOIRadius(orbit);
        var ke = OrbitMath.KeplerFromPositionAndVelocity(mu, pos, vel, at);
        return ke.Eccentricity < 1 && double.IsFinite(ke.Apoapsis) && ke.Apoapsis <= limit;
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

    /// <summary>
    /// Low-orbit drop-in, 90° to departure velocity. Nav quality above 1.0 leads
    /// along-track by a fraction of v_circ·t_burn so a finite circularise finishes
    /// nearer circular. Quality 1.0 (no commander) is today's 90° offset.
    /// </summary>
    public static Vector3 PlannedWarpExitOffset(
        Entity ship, Entity target, Vector3 departureVel, float? navQuality = null)
    {
        // Jump points and grav anomalies have no body radius. Arrive on the point.
        if (!target.HasDataBlob<MassVolumeDB>())
            return Vector3.Zero;

        double r = OrbitMath.LowOrbitRadius(target);
        if (!(r > 0) || !double.IsFinite(r))
            return Vector3.Zero;

        var velXy = new Vector3(departureVel.X, departureVel.Y, 0);
        if (velXy.Length() < 1)
            return new Vector3(r, 0, 0);

        var radial = Vector3.Normalise(new Vector3(-velXy.Y, velXy.X, 0)) * r;
        float q = navQuality
                  ?? (CommanderSkills.TryGetCommander(ship, out _, out var db)
                      ? CommanderSkills.Quality(db, SkillDomain.Nav)
                      : 1f);
        double lead = Math.Clamp(q - 1f, 0, 1);
        if (lead <= 0)
            return radial;

        double tBurn = EstimateCirculariseBurnSeconds(ship, target, r, velXy.Length());
        if (tBurn <= 0)
            return radial;

        double sgp = OrbitMath.SGP(target, ship);
        double vCirc = Math.Sqrt(sgp / r);
        if (!double.IsFinite(vCirc) || vCirc <= 0)
            return radial;

        double theta = Math.Min(vCirc * tBurn * lead / r, 0.2);
        double c = Math.Cos(-theta);
        double s = Math.Sin(-theta);
        return new Vector3(radial.X * c - radial.Y * s, radial.X * s + radial.Y * c, radial.Z);
    }

    static double EstimateCirculariseBurnSeconds(Entity ship, Entity target, double r, double vIn)
    {
        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) || thrust.ThrustInNewtons < 1)
            return 0;
        if (!ship.TryGetDataBlob<MassVolumeDB>(out var mv) || mv.MassTotal < 1)
            return 0;
        double sgp = OrbitMath.SGP(target, ship);
        double vCirc = Math.Sqrt(sgp / r);
        if (!double.IsFinite(vCirc))
            return 0;
        double dv = Math.Abs(vIn - vCirc);
        if (!double.IsFinite(dv) || dv < 0.01)
            return 0;
        return Math.Min(dv * mv.MassTotal / thrust.ThrustInNewtons, 600);
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

    internal static double AlongTrackArrivedSeparation(Entity target, double targetRadius)
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

    /// <summary>
    /// Angle between two orbit planes. Zero when they share a plane, including both equatorial.
    /// </summary>
    static double RelativeInclination(OrbitDB a, OrbitDB b)
    {
        double cos = Math.Cos(a.Inclination) * Math.Cos(b.Inclination)
            + Math.Sin(a.Inclination) * Math.Sin(b.Inclination) * Math.Cos(a.LongitudeOfAscendingNode - b.LongitudeOfAscendingNode);
        if (cos > 1)
            cos = 1;
        if (cos < -1)
            cos = -1;
        return Math.Acos(cos);
    }

    /// <summary>
    /// Best Lambert window the tank can fly. Returns false when the scan has nothing to say,
    /// so an eccentric ship can still fall back to circularise. A circular ship that found a
    /// window it cannot afford gets the usual Δv sentence.
    /// </summary>
    static bool TryLambertWindow(Entity ship, Entity target, Entity parent, DateTime now, double budget, MoveOrderOptions order, out MoveOption option)
    {
        option = default;
        const MoveMode mode = MoveMode.NewtonianTransfer;
        order ??= new MoveOrderOptions();
        double mu = OrbitMath.SGP(parent, ship);
        TransferWindow? picked = ToWindow(order.PickedWindow);
        bool affordable = TransferPlanner.TryAffordableWindow(
            ship, target, mu, now, budget, order.Timing, picked,
            out TransferWindow window, out double bestDv, out _, out WindowAnswer answer);
        var facts = LambertFacts(answer, now, order.Timing);
        if (!affordable)
        {
            if (order.Timing == DepartureTiming.PickOnPorkchop && !string.IsNullOrEmpty(answer.Note))
            {
                option = MoveOption.No(mode, answer.Note, facts);
                return true;
            }

            bool shipEccentric = ship.TryGetDataBlob<OrbitDB>(out var shipOrbit)
                && shipOrbit.Eccentricity > MaxEccentricityForTransfer;
            if (!shipEccentric && double.IsFinite(bestDv) && bestDv > budget)
            {
                string sentence = string.IsNullOrEmpty(answer.Note)
                    ? $"needs {bestDv:N0} m/s Δv, have {budget:N0} m/s"
                    : answer.Note;
                option = MoveOption.No(mode, sentence, facts);
                return true;
            }

            if (answer.Scanned && !string.IsNullOrEmpty(answer.Note))
            {
                option = MoveOption.No(mode, answer.Note, facts);
                return true;
            }

            return false;
        }

        var manuvers = new (Vector3 deltaV, double timeInSeconds)[2];
        if (TransferPlanner.TryState(ship, window.Depart, out var from)
            && TransferPlanner.TryState(target, window.Arrive, out var to))
        {
            Vector3 depart = TransferPlanner.ToProgradeFrame(window.DepartDeltaV, from.pos, from.vel);
            Vector3 arrivalVelocity = to.vel + window.ArriveDeltaV;
            Vector3 arrive = TransferPlanner.ToProgradeFrame(-window.ArriveDeltaV, to.pos, arrivalVelocity);
            manuvers[0] = (depart, Math.Max(0, (window.Depart - now).TotalSeconds));
            manuvers[1] = (arrive, (window.Arrive - now).TotalSeconds);
        }

        facts.Legs = new[]
        {
            new TripLeg("departure", manuvers[0].deltaV.Length(), manuvers[0].timeInSeconds, manuvers[0].timeInSeconds),
            new TripLeg("arrival", manuvers[1].deltaV.Length(), manuvers[1].timeInSeconds, manuvers[1].timeInSeconds),
        };
        option = MoveOption.Yes(mode, (window.Arrive - now).TotalSeconds, window.TotalDeltaV, 0, manuvers,
            $"Transfer to {NameOf(target, ship)}, depart {FormatArrival(window.Depart)}, arrive {FormatArrival(window.Arrive)}",
            window: window, departSeconds: ForwardSeconds(window.Depart, now), facts: facts);
        return true;
    }

    static PreviewFacts LambertFacts(WindowAnswer answer, DateTime now, DepartureTiming timing)
    {
        if (!answer.Scanned)
            return default;
        bool hasAlt = answer.HasAlt && answer.AltWindow.IsFinite;
        double wait = answer.Feasible && answer.Window.IsFinite
            ? NonNeg((answer.Window.Depart - now).TotalSeconds)
            : 0;
        return new PreviewFacts
        {
            HasTiming = true,
            Timing = timing,
            WaitSeconds = wait,
            Note = answer.Note ?? "",
            HasAlt = hasAlt,
            AltTiming = answer.AltTiming,
            AltWaitSeconds = hasAlt ? NonNeg((answer.AltWindow.Depart - now).TotalSeconds) : 0,
            AltEtaSeconds = hasAlt ? NonNeg((answer.AltWindow.Arrive - now).TotalSeconds) : 0,
            AltDeltaV = hasAlt ? answer.AltWindow.TotalDeltaV : 0,
            AltFeasible = hasAlt && answer.AltFeasible,
            AltNote = answer.AltNote ?? "",
            CanPick = true,
        };
    }

    static TransferWindow? ToWindow(WindowChoice pick)
    {
        if (pick == null)
            return null;
        var depart = new Vector3(pick.DepartDeltaV.X, pick.DepartDeltaV.Y, pick.DepartDeltaV.Z);
        var arrive = new Vector3(pick.ArriveDeltaV.X, pick.ArriveDeltaV.Y, pick.ArriveDeltaV.Z);
        double total = depart.Length() + arrive.Length();
        return new TransferWindow(pick.Depart, pick.Arrive, depart, arrive, total, depart.Length(), arrive.Length());
    }

    static double NonNeg(double seconds)
        => double.IsFinite(seconds) && seconds > 0 ? seconds : 0;

    static MoveOrderOptions OrderFrom(Goal goal, MoveOrderOptions options, double periArg, double apoArg)
    {
        var order = options ?? new MoveOrderOptions();
        if (periArg > 0)
            order = order with { Periapsis = periArg };
        if (apoArg > 0)
            order = order with { Apoapsis = apoArg };
        if (goal == null)
            return order;
        WindowChoice pick = null;
        if (goal.HasPickedWindow)
        {
            pick = new WindowChoice(
                goal.PickedDepart, goal.PickedArrive,
                new Vec3(goal.PickedDepartDx, goal.PickedDepartDy, goal.PickedDepartDz),
                new Vec3(goal.PickedArriveDx, goal.PickedArriveDy, goal.PickedArriveDz));
        }

        double peri = goal.ArrivalPeriapsis > 0 ? goal.ArrivalPeriapsis : order.Periapsis;
        double apo = goal.ArrivalApoapsis > 0 ? goal.ArrivalApoapsis : order.Apoapsis;
        return new MoveOrderOptions(goal.Mode, goal.Timing, pick, peri, apo, goal.Arrival);
    }

    /// <summary>
    /// Build the two burns for a chosen window. The window is a hint: the burns are solved
    /// again from the propagated state at its departure and arrival.
    /// </summary>
    public static bool PlanFromWindow(Entity ship, Entity target, TransferWindow window,
        out List<EntityAction> actions, out string reason)
    {
        DateTime now = ship != null ? ship.StarSysDateTime : default;
        return PlanFromWindow(ship, target, window, now, out actions, out reason);
    }

    public static bool PlanFromWindow(Entity ship, Entity target, TransferWindow window, DateTime now,
        out List<EntityAction> actions, out string reason)
    {
        actions = new List<EntityAction>();
        reason = "transfer solution did not converge";
        if (ship == null || target == null || !window.IsFinite)
            return false;
        if (!ship.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrust) || thrust.ThrustInNewtons <= 0)
        {
            reason = "no reaction drive";
            return false;
        }

        var parent = ship.GetSOIParentEntity();
        if (parent == null)
        {
            reason = "no SOI parent";
            return false;
        }

        if (PatchedConic.IsOneLevelTransfer(ship, target))
        {
            var plan = PatchedConicPlanner.TryPlanFromWindow(ship, target, now, thrust.DeltaV, window);
            if (!plan.Feasible)
            {
                reason = string.IsNullOrEmpty(plan.Reason) ? "transfer did not converge" : plan.Reason;
                return false;
            }

            actions.Add(SoiTransferAction.CreateCommand(ship, target, now, 0, 0, window));
            reason = $"Transfer to {NameOf(target, ship)}, depart {FormatArrival(plan.DepartAt)}, arrive {FormatArrival(plan.ArriveAt)}";
            if (!string.IsNullOrEmpty(plan.Reason))
                reason += ", " + plan.Reason;
            return true;
        }

        bool sameParent = target.GetSOIParentEntity() == parent
            || (target.TryGetDataBlob<OrbitDB>(out var targetOrbit) && targetOrbit.Parent == parent);
        if (!sameParent)
        {
            reason = "target is under a different SOI parent; no interplanetary transfer maths yet";
            return false;
        }

        double mu = OrbitMath.SGP(parent, ship);
        double tof = (window.Arrive - window.Depart).TotalSeconds;
        if (!(mu > 0) || !(tof > 0))
            return false;
        if (!TransferPlanner.TryState(ship, window.Depart, out var from)
            || !TransferPlanner.TryState(target, window.Arrive, out var to))
            return false;

        bool prograde = from.pos.X * from.vel.Y - from.pos.Y * from.vel.X >= 0;
        bool solved = LambertSolver.TrySolve(mu, from.pos, to.pos, tof, prograde, out Vector3 v1, out Vector3 v2)
            || LambertSolver.TrySolve(mu, from.pos, to.pos, tof, !prograde, out v1, out v2);
        if (!solved)
            return false;

        Vector3 dvDepart = v1 - from.vel;
        Vector3 dvArrive = to.vel - v2;
        double total = dvDepart.Length() + dvArrive.Length();
        if (!double.IsFinite(total))
            return false;
        if (total > thrust.DeltaV)
        {
            reason = $"needs {total:N0} m/s Δv, have {thrust.DeltaV:N0} m/s";
            return false;
        }

        Vector3 burn1 = TransferPlanner.ToProgradeFrame(dvDepart, from.pos, from.vel);
        Vector3 burn2 = TransferPlanner.ToProgradeFrame(dvArrive, to.pos, v2);
        double mass = ship.TryGetDataBlob<MassVolumeDB>(out var massDb) ? massDb.MassTotal : 0;
        double seconds1 = BurnSeconds(mass, burn1.Length(), thrust);
        double fuel1 = thrust.FuelBurnRate > 0 ? seconds1 * thrust.FuelBurnRate : 0;
        double seconds2 = BurnSeconds(mass - fuel1, burn2.Length(), thrust);

        actions.Add(NewtonThrustAction.CreateCommand(
            ship.FactionOwnerID, ship, window.Depart, burn1, seconds1, "Transfer window"));
        actions.Add(NewtonThrustAction.CreateCommand(
            ship.FactionOwnerID, ship, window.Arrive, burn2, seconds2, "Transfer window"));
        reason = $"Transfer to {NameOf(target, ship)}, depart {FormatArrival(window.Depart)}, arrive {FormatArrival(window.Arrive)}";
        return true;
    }

    static double BurnSeconds(double mass, double deltaV, NewtonThrustAbilityDB thrust)
    {
        if (!(mass > 0) || !(deltaV > 0) || !(thrust.FuelBurnRate > 0) || !(thrust.ExhaustVelocity > 0))
            return 0;
        double fuel = OrbitMath.TsiolkovskyFuelUse(mass, thrust.ExhaustVelocity, deltaV);
        if (!double.IsFinite(fuel) || fuel < 0)
            return 0;
        return fuel / thrust.FuelBurnRate;
    }

    // ---------------------------------------------------------------------

    static string NameOf(Entity target, Entity ship)
        => target.GetName(ship.FactionOwnerID);

    static string FormatArrival(DateTime at)
        => at.ToString("yyyy-MM-dd HH:mm:ss");

    static double ForwardSeconds(DateTime at, DateTime now)
    {
        double seconds = (at - now).TotalSeconds;
        return double.IsFinite(seconds) && seconds > 0 ? seconds : 0;
    }

    /// <summary>
    /// Angle about the SOI parent, increasing in the direction of motion.
    /// A retrograde orbit's longitude runs the other way, so the sign is flipped.
    /// </summary>
    static double AngleOf(Entity entity, DateTime atDateTime)
    {
        Vector3 pos;
        Vector3 vel;
        if (entity.TryGetDataBlob<OrbitDB>(out var orbit))
        {
            var state = OrbitMath.GetStateVectors(orbit, atDateTime);
            pos = state.pos;
            vel = state.Velocity;
        }
        else
        {
            var raw = MoveMath.GetRelativeFutureState(entity, atDateTime);
            pos = raw.pos;
            vel = raw.Velocity;
        }
        double longitude = Math.Atan2(pos.Y, pos.X);
        double hz = pos.X * vel.Y - pos.Y * vel.X;
        return hz < 0 ? -longitude : longitude;
    }

    /// <summary>Wrap to (-pi, pi].</summary>
    static double NormaliseAngle(double radians)
    {
        while (radians > Math.PI) radians -= 2 * Math.PI;
        while (radians <= -Math.PI) radians += 2 * Math.PI;
        return radians;
    }
}





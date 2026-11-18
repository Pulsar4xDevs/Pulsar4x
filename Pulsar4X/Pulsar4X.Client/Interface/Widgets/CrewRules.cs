using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;

namespace Pulsar4X.Client.Interface.Widgets;

/// <summary>
/// Client-side crew-rules display and design checks. Automated order and span wording comes
/// from <see cref="CrewText"/>. Other refusal strings must stay in step with
/// <c>CommandTranslator</c> and <c>ShipCrew.DesignCrewWarnings</c>.
/// </summary>
public static class CrewRules
{
    public const string AutomatedState = "Automated, no crew needed";
    public const string AutomatedLabel = "Automated";

    public const string AutomatedShipNoOrders = CrewText.AutomatedNoOrders;

    public const string AssignCaptainAutomated =
        "This ship has no bridge: it is automated and has no captain.";

    public const string AssignCaptainNoSlot =
        "This ship's bridge has no officer slot for a captain.";

    public const string TopUpAutomated =
        "This ship has no bridge: it is automated and carries no crew.";

    public const string SetFlagshipAutomated =
        "An automated ship (no bridge) cannot be a flagship. It may follow one.";

    /// <summary>One design component for the designer totals and warn-only checks.</summary>
    public readonly record struct DesignPart(int CrewRequired, int Berths, int OfficerSlots, bool IsBridge, int Count = 1);

    /// <summary>
    /// Designer totals. Crew is the parts' crew when any part is a bridge, else 0.
    /// Bunks and officer slots are the sums of those fields. Warnings never block saving.
    /// </summary>
    public readonly record struct DesignTotals(int Crew, int Bunks, int OfficerSlots, bool HasBridge)
    {
        public bool Automated => !HasBridge;

        /// <summary>People the design can house: crew the bunks hold, plus the captain when a slot exists.</summary>
        public int PeopleCapacity => Automated ? 0 : Math.Min(Crew, Bunks) + (OfficerSlots >= 1 ? 1 : 0);
    }

    public static DesignTotals Totals(IEnumerable<DesignPart> parts)
    {
        int crew = 0, bunks = 0, slots = 0;
        bool hasBridge = false;
        foreach (var p in parts ?? Enumerable.Empty<DesignPart>())
        {
            if (p.Count <= 0)
                continue;
            if (p.IsBridge)
                hasBridge = true;
            crew += p.CrewRequired * p.Count;
            bunks += p.Berths * p.Count;
            slots += p.OfficerSlots * p.Count;
        }
        if (!hasBridge)
            crew = 0;
        return new DesignTotals(crew, bunks, slots, hasBridge);
    }

    /// <summary>
    /// Warn-only: bunks fewer than crew, and a bridge with no officer slot. Empty when automated.
    /// Wording matches <c>ShipCrew.DesignCrewWarnings</c>.
    /// </summary>
    public static IReadOnlyList<string> DesignWarnings(DesignTotals totals)
    {
        if (!totals.HasBridge)
            return Array.Empty<string>();
        var list = new List<string>();
        if (totals.Bunks < totals.Crew)
            list.Add($"Crew {totals.Crew} but only {totals.Bunks} crew bunks: add crew quarters for {totals.Crew - totals.Bunks} more.");
        if (totals.OfficerSlots < 1)
            list.Add("The bridge has no officer slot for a captain.");
        return list;
    }

    public static IReadOnlyList<string> DesignWarnings(IEnumerable<DesignPart> parts)
        => DesignWarnings(Totals(parts));

    public static string CrewLine(int aboard, int required, int bunks)
        => $"Crew {aboard:N0}/{required:N0} ({bunks:N0} {(bunks == 1 ? "bunk" : "bunks")})";

    public static string OfficersLine(int aboard, int slots)
        => $"Officers {aboard:N0}/{slots:N0}";

    public static string PeopleAboardLine(int people)
        => $"Life support: {people:N0} {(people == 1 ? "person" : "people")} aboard";

    /// <summary>
    /// Player actions that may be gated on an automated ship. Goal, move, well/system
    /// freight (Trade, Haul, Move goods) and destination picks use <see cref="GoalOrMove"/>.
    /// Cargo transfer, refit, fire control and cancel stay allowed.
    /// </summary>
    public enum ShipAction
    {
        GoalOrMove,
        SetFlagship,
        AssignCaptain,
        TopUp,
        CargoTransfer,
        Refit,
        FireControl,
        Cancel,
    }

    /// <summary>Refusal tooltip for <paramref name="action"/> on this ship, or null if allowed.</summary>
    public static string? RefusalFor(ShipAction action, ShipSnapshot? ship)
    {
        bool automated = ship is { Automated: true };
        return action switch
        {
            ShipAction.GoalOrMove => automated ? CrewText.AutomatedNoOrders : null,
            ShipAction.SetFlagship => automated ? SetFlagshipAutomated : null,
            ShipAction.AssignCaptain => ship == null ? null : AssignCaptainRefusal(ship),
            ShipAction.TopUp => automated ? TopUpAutomated : null,
            ShipAction.CargoTransfer or ShipAction.Refit or ShipAction.FireControl or ShipAction.Cancel => null,
            _ => automated ? CrewText.AutomatedNoOrders : null,
        };
    }

    public static string? OrderRefusal(ShipSnapshot? ship)
        => RefusalFor(ShipAction.GoalOrMove, ship);

    public static string? SetFlagshipRefusal(ShipSnapshot ship)
        => ship.Automated ? SetFlagshipAutomated : null;

    public static string? AssignCaptainRefusal(bool automated, int officerSlots)
    {
        if (automated)
            return AssignCaptainAutomated;
        if (officerSlots < 1)
            return AssignCaptainNoSlot;
        return null;
    }

    public static string? AssignCaptainRefusal(ShipSnapshot ship)
        => AssignCaptainRefusal(ship.Automated, ship.OfficerSlots);

    public static string? AssignCaptainRefusal(ShipView ship)
        => AssignCaptainRefusal(ship.Automated, ship.OfficerSlots);
}

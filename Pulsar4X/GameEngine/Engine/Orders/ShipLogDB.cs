using System;
using System.Collections.Generic;
using Pulsar4X.Api;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Fleets;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.People.Orders;
using Pulsar4X.Ships;
using Pulsar4X.Colonies;
using Pulsar4X.Technology;

namespace GameEngine.Engine.Orders;

/// <summary>
/// One line in a ship's log. Appended only. Names are written at the decision.
/// </summary>
public class ShipLogEntry
{
    public DateTime At { get; set; }
    public ShipLogKind Kind { get; set; }
    public int OtherEntityId { get; set; } = -1;
    public string Text { get; set; } = "";
}

/// <summary>
/// The book for one ship, fleet, or colony. Kept for the life of the game.
/// </summary>
public class ShipLogDB : BaseDataBlob
{
    public List<ShipLogEntry> Entries { get; set; } = new();

    public override object Clone()
    {
        var copy = new ShipLogDB();
        foreach (var entry in Entries)
        {
            copy.Entries.Add(new ShipLogEntry
            {
                At = entry.At,
                Kind = entry.Kind,
                OtherEntityId = entry.OtherEntityId,
                Text = entry.Text,
            });
        }
        return copy;
    }
}

/// <summary>
/// Writes a line when a decision changes. A quiet wake does not come here.
/// The same text as the previous line is not written again.
/// </summary>
static class ShipLogBook
{
    public static void Append(Entity entity, DateTime at, ShipLogKind kind, string text, int otherId = -1)
    {
        if (entity == null || string.IsNullOrWhiteSpace(text))
            return;

        if (!entity.TryGetDataBlob<ShipLogDB>(out var book))
        {
            book = new ShipLogDB();
            entity.SetDataBlob(book);
        }

        if (book.Entries.Count > 0 && book.Entries[^1].Text == text)
            return;

        book.Entries.Add(new ShipLogEntry
        {
            At = at,
            Kind = kind,
            OtherEntityId = otherId,
            Text = text,
        });
    }

    public static void NoteOrdered(Entity entity, Goal goal, DateTime at)
        => Append(entity, at, ShipLogKind.Own, "Ordered: " + Phrase(entity, goal) + ".");

    public static void NoteRoute(Entity entity, Goal goal, IReadOnlyList<EntityAction> actions, DateTime at)
    {
        if (entity.HasDataBlob<FleetDB>())
            return;
        Append(entity, at, ShipLogKind.Own, RouteSentence(entity, goal, actions));
    }

    /// <summary>One action, on a ship or a colony. A fleet does not list these.</summary>
    public static void NoteQueued(EntityAction action)
    {
        if (action?.EntityCommanding == null)
            return;
        var entity = action.EntityCommanding;
        if (entity.HasDataBlob<FleetDB>())
            return;
        // A posting has its own sentence, written when the chair actually changes.
        if (action is AssignAdministratorOrder or UnassignAdministratorOrder
            or AssignScientistOrder or UnassignScientistOrder)
            return;
        if (!entity.HasDataBlob<ShipInfoDB>() && !entity.HasDataBlob<ColonyInfoDB>())
            return;

        DateTime at = action.CreatedDate != default
            ? action.CreatedDate
            : entity.StarSysDateTime;
        Append(entity, at, ShipLogKind.Action, ActionText(action));
    }

    /// <summary>
    /// One failed action, on a ship or a colony. A fleet does not list these.
    /// The reason is the one the order already shows.
    /// </summary>
    public static void NoteFailed(EntityAction action)
    {
        if (action?.EntityCommanding == null)
            return;
        var entity = action.EntityCommanding;
        if (entity.HasDataBlob<FleetDB>())
            return;
        if (action is AssignAdministratorOrder or UnassignAdministratorOrder
            or AssignScientistOrder or UnassignScientistOrder)
            return;
        if (!entity.HasDataBlob<ShipInfoDB>() && !entity.HasDataBlob<ColonyInfoDB>())
            return;

        string name = (action.Name ?? "").Trim();
        if (name.Length == 0)
            name = "Action";
        string reason = (action.FailureMessage ?? "").Trim();
        string text = reason.Length == 0 ? name + " failed" : name + " — " + reason;
        DateTime at = entity.Manager != null ? entity.StarSysDateTime : action.CreatedDate;
        Append(entity, at, ShipLogKind.Action, EndSentence(text));
    }

    /// <summary>A commander sat this chair. <paramref name="post"/> is "command", "the post", or "the lab".</summary>
    public static void NoteTookPost(Entity entity, Entity officer, DateTime at, string post)
        => Append(entity, at, ShipLogKind.Own, OfficerLabel(officer) + " took " + post + ".");

    /// <summary>A commander left this chair.</summary>
    public static void NoteLeftPost(Entity entity, Entity officer, DateTime at, string post)
        => Append(entity, at, ShipLogKind.Own, OfficerLabel(officer) + " left " + post + ".");

    /// <summary>
    /// The colony or ship a lab sits on. The lab entity itself has no log tab.
    /// </summary>
    public static Entity? LabBook(Entity lab)
    {
        if (lab == null)
            return null;
        if (lab.HasDataBlob<ColonyInfoDB>() || lab.HasDataBlob<ShipInfoDB>())
            return lab;
        if (lab.TryGetDataBlob<ResearcherDB>(out var research)
            && research.LocationId >= 0
            && lab.Manager != null
            && lab.Manager.TryGetGlobalEntityById(research.LocationId, out var place)
            && (place.HasDataBlob<ColonyInfoDB>() || place.HasDataBlob<ShipInfoDB>()))
            return place;
        return null;
    }

    static string OfficerLabel(Entity officer)
    {
        if (officer == null)
            return "An officer";

        string name = officer.GetName(officer.FactionOwnerID);
        if (name == "Unknown")
            name = "";
        officer.TryGetDataBlob<CommanderDB>(out var commander);
        if (commander != null && !string.IsNullOrWhiteSpace(commander.Name))
            name = commander.Name;
        if (name.Length == 0)
            return "An officer";

        if (commander != null
            && commander.Type == CommanderTypes.Navy
            && officer.Manager?.Game is Game game
            && game.Themes != null
            && game.Themes.TryGetValue(game.Settings.CurrentTheme, out var theme)
            && theme.NavyRanks != null
            && theme.NavyRanks.TryGetValue(commander.Rank, out var title)
            && !string.IsNullOrWhiteSpace(title))
            return title + " " + name;

        return name;
    }

    public static void NoteOutcome(Entity entity, Goal goal, DateTime at)
    {
        string result = Outcome(entity, goal);
        Append(entity, at, ShipLogKind.Own, result);

        if (goal.IssuerEntityId < 0 || entity.Manager == null)
            return;
        if (!entity.Manager.TryGetGlobalEntityById(goal.IssuerEntityId, out var parent))
            return;

        string child = entity.GetName(entity.FactionOwnerID);
        Append(parent, at, ShipLogKind.From, "From " + child + " — " + result, entity.Id);
    }

    public static string Phrase(Entity entity, Goal goal)
    {
        string name = goal.Name?.Trim() ?? "";
        bool raw = name.Length == 0 || name == goal.Type.ToString();
        if (!raw)
            return name;

        string target = TargetName(entity, goal.TargetEntityID);
        return goal.Type switch
        {
            GoalType.MoveTo => target.Length > 0 ? "Move to " + target : "Move",
            GoalType.ServeyBodies => target.Length > 0 ? "Survey " + target : "Survey",
            GoalType.ScanAnomalies => target.Length > 0 ? "Survey " + target : "Grav survey",
            GoalType.SurveySystem => "Survey the system",
            GoalType.SurveyStanding => "Survey",
            GoalType.Mine or GoalType.MineAsteroids or GoalType.MineStanding
                => target.Length > 0 ? "Mine " + target : "Mine",
            GoalType.RefuelAt or GoalType.BuyFuel => target.Length > 0 ? "Refuel at " + target : "Refuel",
            GoalType.Colonise => target.Length > 0 ? "Colonise " + target : "Colonise",
            GoalType.Trade => "Trade",
            GoalType.Freighter => "Haul",
            GoalType.RunIndustry => "Refine",
            GoalType.OfferStock => "Offer stock",
            GoalType.RunMarket => "Run the market",
            GoalType.MoveGoods or GoalType.FleetMoveGoods => "Move goods",
            _ => name.Length > 0 ? name : goal.Type.ToString(),
        };
    }

    static string RouteSentence(Entity entity, Goal goal, IReadOnlyList<EntityAction> actions)
    {
        int targetId = goal.TargetEntityID >= 0 ? goal.TargetEntityID : FirstMoveTarget(actions);
        string target = TargetName(entity, targetId);
        return goal.Type switch
        {
            GoalType.MoveTo => target.Length > 0 ? "Moving to " + target + "." : "Moving.",
            GoalType.Mine or GoalType.MineAsteroids or GoalType.MineStanding
                => target.Length > 0 ? "Mining " + target + "." : "Mining.",
            GoalType.RefuelAt or GoalType.BuyFuel
                => target.Length > 0 ? "Refuel at " + target + "." : "Refuel.",
            GoalType.ServeyBodies or GoalType.ScanAnomalies or GoalType.SurveySystem or GoalType.SurveyStanding
                => target.Length > 0 ? "Moving to " + target + "." : "Surveying.",
            _ => Phrase(entity, goal) + ".",
        };
    }

    static int FirstMoveTarget(IReadOnlyList<EntityAction> actions)
    {
        if (actions == null)
            return -1;
        foreach (var action in actions)
        {
            if (action is WarpMoveAction warp && warp.TargetEntityGuid >= 0)
                return warp.TargetEntityGuid;
        }
        return -1;
    }

    static string Outcome(Entity entity, Goal goal)
    {
        if (goal.PlayerCancelled)
            return "Cancelled.";

        string target = TargetName(entity, goal.TargetEntityID);
        if (goal.Status == GoalStatus.Failed)
        {
            string lead;
            if (!IsReach(goal.Message))
                lead = "Could not finish.";
            else if (TargetIsAsteroid(entity, goal.TargetEntityID))
                lead = "Could not reach the asteroid.";
            else
                lead = target.Length > 0 ? "Could not reach " + target + "." : "Could not reach the target.";

            string why = PlayerReason(goal.Message);
            return why.Length == 0 ? lead : lead + " " + why;
        }

        return goal.Type switch
        {
            GoalType.ServeyBodies or GoalType.SurveySystem or GoalType.SurveyStanding or GoalType.ScanAnomalies
                => target.Length > 0 ? target + " surveyed." : "Surveyed.",
            GoalType.Mine or GoalType.MineAsteroids or GoalType.MineStanding
                => target.Length > 0 ? target + " mined." : "Mined.",
            GoalType.MoveTo => target.Length > 0 ? "Arrived at " + target + "." : "Arrived.",
            GoalType.RefuelAt or GoalType.BuyFuel
                => target.Length > 0 ? "Refuelled at " + target + "." : "Refuelled.",
            _ => Phrase(entity, goal) + ".",
        };
    }

    static readonly string[] InternalReasons =
    {
        "no path",
        "fleet only",
        "ship only",
        "non supported entity",
        "unknown fail",
        "a move action failed",
        "a survey action failed",
        "a mining action failed",
        "a fuel buy failed",
        "a refuel action failed",
        "an action failed",
        "invalid target",
        "not a valid target",
        "we have no subordinates to manage",
        "no subordinates can move",
        "no action queue",
        "no geo-survey capability",
        "no grav-survey capability",
    };

    static bool IsReach(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return false;
        return message.Contains("path", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Target not found", StringComparison.Ordinal)
            || message.Contains("move", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ΔV", StringComparison.Ordinal)
            || message.Contains("warp", StringComparison.OrdinalIgnoreCase)
            || message.Contains("power", StringComparison.OrdinalIgnoreCase)
            || message.Contains("reach", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The player-facing reason, capitalised and ended. An internal planner token is left off.
    /// </summary>
    static string PlayerReason(string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || IsInternal(message))
            return "";
        string text = EndSentence(message.Trim());
        if (text.Length == 0 || IsInternal(text))
            return "";
        if (char.IsLower(text[0]))
            text = char.ToUpperInvariant(text[0]) + text.Substring(1);
        return text;
    }

    static bool IsInternal(string message)
    {
        string text = message.Trim();
        if (text.EndsWith('.'))
            text = text.Substring(0, text.Length - 1).Trim();
        if (text.StartsWith("no planner", StringComparison.OrdinalIgnoreCase))
            return true;
        foreach (var token in InternalReasons)
        {
            if (text.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static bool TargetIsAsteroid(Entity entity, int id)
    {
        if (id < 0 || entity.Manager == null)
            return false;
        if (!entity.Manager.TryGetGlobalEntityById(id, out var body))
            return false;
        return body.TryGetDataBlob<SystemBodyInfoDB>(out var info)
            && info.BodyType == BodyType.Asteroid;
    }

    static string TargetName(Entity entity, int id)
    {
        if (id < 0 || entity.Manager == null)
            return "";
        if (!entity.Manager.TryGetGlobalEntityById(id, out var target))
            return "";
        string name = target.GetName(entity.FactionOwnerID);
        return name == "Unknown" ? "" : name;
    }

    static string ActionText(EntityAction action)
    {
        string name = (action.Name ?? "").Trim();
        if (name.Length == 0)
            name = "Action";

        string detail = (action.Details ?? "").Trim();

        if (name == "Add To Construction Queue" && detail.StartsWith("Install ", StringComparison.Ordinal))
            return EndSentence(detail);
        if (detail.Length == 0
            || detail.Equals(name, StringComparison.OrdinalIgnoreCase)
            || detail == "Instant"
            || IsStatus(detail))
            return EndSentence(name);
        return EndSentence(name + " — " + detail);
    }

    static bool IsStatus(string detail)
        => detail.StartsWith("Waiting", StringComparison.Ordinal)
            || detail.StartsWith("Charging", StringComparison.Ordinal)
            || detail.StartsWith("Warping", StringComparison.Ordinal)
            || detail.StartsWith("Arriving", StringComparison.Ordinal)
            || detail.StartsWith("Circularis", StringComparison.Ordinal)
            || detail.EndsWith("%", StringComparison.Ordinal);

    static string EndSentence(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return "";
        return text.EndsWith('.') ? text : text + ".";
    }
}

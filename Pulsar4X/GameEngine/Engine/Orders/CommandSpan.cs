using System;
using System.Collections.Generic;
using GameEngine.People;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.People;
using Pulsar4X.Ships;

namespace GameEngine.Engine.Orders;

/// <summary>
/// How far an issued goal may fan out from <c>TargetEntityID</c>.
/// <see cref="Of"/> is the bridge. <see cref="Effective"/> is the narrower of that and the seated officer's rank.
/// </summary>
public enum CommandSpanKind
{
    Body,
    Well,
    System,
    /// <summary>This star system and systems one known jump away. Trade, freight, and supply only.</summary>
    Neighbor,
}

public static class CommandSpan
{
    public static CommandSpanKind FromAdminLevel(AdminLevel level) => level switch
    {
        AdminLevel.Colony or AdminLevel.Planet or AdminLevel.SOI => CommandSpanKind.Well,
        AdminLevel.System or AdminLevel.Sector or AdminLevel.Empire => CommandSpanKind.System,
        _ => CommandSpanKind.Body,
    };

    /// <summary>
    /// Fleet → flagship bridge; ship → own bridge. No seats → Body.
    /// </summary>
    public static CommandSpanKind Of(Entity unit)
    {
        if (!TryGetCommandBridge(unit, out var admin))
            return CommandSpanKind.Body;
        var max = AdminLevel.Ship;
        bool any = false;
        foreach (var seat in admin.CommanderSeats)
        {
            any = true;
            if (seat.SeatType > max)
                max = seat.SeatType;
        }
        return any ? FromAdminLevel(max) : CommandSpanKind.Body;
    }

    /// <summary>Ranks 1–3 are Body, 4–5 are Well, 6 and above are System.</summary>
    public static CommandSpanKind RankSpan(int rank)
    {
        if (rank >= 6)
            return CommandSpanKind.System;
        if (rank >= 4)
            return CommandSpanKind.Well;
        return CommandSpanKind.Body;
    }

    /// <summary>
    /// Ships and fleets use the narrower of the bridge and the seated rank.
    /// No seated officer is Body. A colony keeps the hardware reading.
    /// </summary>
    public static CommandSpanKind Effective(Entity unit)
    {
        var hardware = Of(unit);
        if (unit == null)
            return CommandSpanKind.Body;
        if (!unit.HasDataBlob<ShipInfoDB>() && !unit.HasDataBlob<FleetDB>())
            return hardware;
        if (!CommanderSkills.TryGetCommander(unit, out _, out var commander))
            return CommandSpanKind.Body;
        return Narrower(hardware, RankSpan(commander.Rank));
    }

    public static CommandSpanKind Narrower(CommandSpanKind hardware, CommandSpanKind rank)
    {
        if (hardware == CommandSpanKind.Body || rank == CommandSpanKind.Body)
            return CommandSpanKind.Body;
        if (hardware == CommandSpanKind.Well || rank == CommandSpanKind.Well)
            return CommandSpanKind.Well;
        return CommandSpanKind.System;
    }

    /// <summary>
    /// One line. "Body. This bridge can cover a system. Ensign Kim covers this ship."
    /// </summary>
    public static string Describe(Entity unit)
    {
        var hardware = unit == null ? CommandSpanKind.Body : Of(unit);
        var effective = unit == null ? CommandSpanKind.Body : Effective(unit);
        string line = Word(effective) + ". " + BridgeClause(hardware) + " " + OfficerClause(unit, effective);
        string limit = LimitClause(unit == null ? SpanLimiter.None : LimiterOf(unit));
        return limit.Length == 0 ? line : line + " " + limit;
    }

    /// <summary>What caps a ship's reach. Flags, since the bridge and the chair can both be the cap.</summary>
    [Flags]
    public enum SpanLimiter
    {
        None = 0,
        /// <summary>The bridge hardware is no wider than the effective span.</summary>
        Bridge = 1,
        /// <summary>A seated officer's rank is no wider than the effective span.</summary>
        Rank = 2,
        /// <summary>Nobody sits the chair.</summary>
        EmptyChair = 4,
    }

    /// <summary>
    /// Which part holds reach below System. Pure: a bridge, whether an officer sits, and that officer's rank.
    /// A chair with nobody in it is the cap, not the rank.
    /// </summary>
    public static SpanLimiter LimiterOf(CommandSpanKind bridge, bool seated, int rank)
    {
        var rankSpan = seated ? RankSpan(rank) : CommandSpanKind.Body;
        var effective = Narrower(bridge, rankSpan);
        if (effective == CommandSpanKind.System)
            return SpanLimiter.None;
        var found = SpanLimiter.None;
        if (Order(bridge) == Order(effective))
            found |= SpanLimiter.Bridge;
        if (Order(rankSpan) == Order(effective))
            found |= seated ? SpanLimiter.Rank : SpanLimiter.EmptyChair;
        return found;
    }

    public static SpanLimiter LimiterOf(Entity unit)
    {
        if (unit == null)
            return SpanLimiter.None;
        if (!unit.HasDataBlob<ShipInfoDB>() && !unit.HasDataBlob<FleetDB>())
            return SpanLimiter.None;
        bool seated = CommanderSkills.TryGetCommander(unit, out _, out var commander);
        return LimiterOf(Of(unit), seated, seated ? commander.Rank : 0);
    }

    /// <summary>One sentence naming the part that limits reach. Empty when nothing below System does.</summary>
    public static string LimitClause(SpanLimiter limiter)
    {
        bool bridge = limiter.HasFlag(SpanLimiter.Bridge);
        if (limiter.HasFlag(SpanLimiter.EmptyChair))
            return bridge ? "The bridge and the empty chair limit reach." : "The empty chair limits reach.";
        if (limiter.HasFlag(SpanLimiter.Rank))
            return bridge ? "The bridge and the captain's rank limit reach." : "The captain's rank limits reach.";
        return bridge ? "The bridge limits reach." : "";
    }

    static int Order(CommandSpanKind span) => span switch
    {
        CommandSpanKind.Body => 0,
        CommandSpanKind.Well => 1,
        _ => 2,
    };

    /// <summary>The lowest rank whose span reaches <paramref name="wanted"/>.</summary>
    public static int RankFor(CommandSpanKind wanted) => Order(wanted) >= 2 ? 6 : Order(wanted) == 1 ? 4 : 1;

    /// <summary>
    /// What to change so reach grows to <paramref name="wanted"/>. Null when it already does.
    /// Pure. <paramref name="titleOf"/> turns a rank into its theme title and may be null.
    /// </summary>
    public static string? Fix(CommandSpanKind bridge, bool seated, int rank, CommandSpanKind wanted, Func<int, string?>? titleOf = null)
    {
        int want = Order(wanted);
        var rankSpan = seated ? RankSpan(rank) : CommandSpanKind.Body;
        bool bridgeShort = Order(bridge) < want;
        bool rankShort = Order(rankSpan) < want;
        if (!bridgeShort && !rankShort)
            return null;

        string bridgeFix = want >= 2 ? "Fit a System Command Bridge" : "Fit a Planetary Command Bridge";
        int need = RankFor(wanted);
        string? title = titleOf?.Invoke(need);
        string officer = "an officer of rank " + need + "+" + (string.IsNullOrWhiteSpace(title) ? "" : " (" + title + ")");
        string seat = seated ? "Promote the captain or seat " + officer : "Seat " + officer;

        if (bridgeShort && rankShort)
            return (seated ? "The bridge and the captain's rank both limit reach. " : "The bridge and the empty chair both limit reach. ")
                + bridgeFix + ". " + seat + ".";
        if (bridgeShort)
            return "The bridge limits reach. " + bridgeFix + ".";
        return (seated ? "The captain's rank limits reach. " : "The chair is empty. ") + seat + ".";
    }

    public static string? Fix(Entity unit, CommandSpanKind wanted)
    {
        if (unit == null)
            return null;
        bool seated = CommanderSkills.TryGetCommander(unit, out _, out var commander);
        return Fix(Of(unit), seated, seated ? commander.Rank : 0, wanted, rank => TitleFor(unit, rank));
    }

    static string? TitleFor(Entity unit, int rank)
    {
        if (unit.Manager?.Game is Game game
            && game.Themes != null
            && game.Themes.TryGetValue(game.Settings.CurrentTheme, out var theme)
            && theme.NavyRanks != null
            && theme.NavyRanks.TryGetValue(rank, out var title)
            && !string.IsNullOrWhiteSpace(title))
            return title;
        return null;
    }

    static string Word(CommandSpanKind span) => span switch
    {
        CommandSpanKind.Well => "Well",
        CommandSpanKind.System => "System",
        CommandSpanKind.Neighbor => "Neighbor",
        _ => "Body",
    };

    static string BridgeClause(CommandSpanKind hardware) => hardware switch
    {
        CommandSpanKind.Well => "This bridge can cover a well.",
        CommandSpanKind.System => "This bridge can cover a system.",
        _ => "This bridge covers this ship.",
    };

    static string OfficerClause(Entity? unit, CommandSpanKind effective)
    {
        if (unit == null || !CommanderSkills.TryGetCommander(unit, out var commander, out var db))
            return "No officer is seated.";
        string name = string.IsNullOrWhiteSpace(db.Name) ? "the officer" : db.Name;
        string rank = RankTitle(commander, db);
        string reach = effective switch
        {
            CommandSpanKind.Well => "covers this well.",
            CommandSpanKind.System => "covers this system.",
            _ => "covers this ship.",
        };
        return rank + " " + name + " " + reach;
    }

    static string RankTitle(Entity commander, CommanderDB db)
    {
        if (db.Type == CommanderTypes.Navy
            && commander.Manager?.Game is Game game
            && game.Themes != null
            && game.Themes.TryGetValue(game.Settings.CurrentTheme, out var theme)
            && theme.NavyRanks != null
            && theme.NavyRanks.TryGetValue(db.Rank, out var title)
            && !string.IsNullOrWhiteSpace(title))
            return title;
        return "Rank " + db.Rank;
    }

    public static bool TryGetCommandBridge(Entity unit, out AdminSpaceDB admin)
    {
        admin = null!;
        Entity bridge = unit;
        if (unit.TryGetDataBlob<FleetDB>(out var fleet) && fleet.FlagShipID >= 0
            && unit.Manager != null
            && unit.Manager.TryGetEntityById(fleet.FlagShipID, out var flag))
            bridge = flag;
        return bridge.TryGetDataBlob(out admin);
    }

    /// <summary>
    /// Root first, then children of <paramref name="root"/> allowed by <paramref name="span"/>.
    /// Body: root only. Well: direct <see cref="PositionDB.Children"/>.
    /// System: all descendants. Never a parent or sibling of <paramref name="root"/>.
    /// Geo/grav pass their own <paramref name="canInclude"/>.
    /// </summary>
    public static List<Entity> Expand(Entity root, CommandSpanKind span, Func<Entity, bool> canInclude)
    {
        var list = new List<Entity>();
        if (canInclude(root))
            list.Add(root);

        int maxDepth = span switch
        {
            CommandSpanKind.Well => 1,
            CommandSpanKind.System => int.MaxValue,
            _ => 0,
        };
        if (maxDepth == 0 || !root.TryGetDataBlob<PositionDB>(out var rootPos))
            return list;

        var pending = new Queue<(Entity entity, int depth)>();
        foreach (var child in rootPos.Children)
            pending.Enqueue((child, 1));

        var seen = new HashSet<int> { root.Id };
        while (pending.Count > 0)
        {
            var (entity, depth) = pending.Dequeue();
            if (!seen.Add(entity.Id))
                continue;
            if (canInclude(entity))
                list.Add(entity);
            if (depth >= maxDepth || !entity.TryGetDataBlob<PositionDB>(out var childPos))
                continue;
            foreach (var grandchild in childPos.Children)
                pending.Enqueue((grandchild, depth + 1));
        }

        return list;
    }
}

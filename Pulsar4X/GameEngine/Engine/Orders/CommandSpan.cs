using System;
using System.Collections.Generic;
using GameEngine.People;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;

namespace GameEngine.Engine.Orders;

/// <summary>
/// How far an issued goal may fan out from <c>TargetEntityID</c>.
/// Comes from the flagship / ship command bridge (<see cref="AdminSpaceDB"/>), not skill.
/// </summary>
public enum CommandSpanKind
{
    Body,
    Well,
    System,
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

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
    /// Root first, then extra POIs allowed by <paramref name="span"/>.
    /// Geo/grav pass their own <paramref name="canInclude"/>.
    /// </summary>
    public static List<Entity> Expand(Entity root, CommandSpanKind span, Func<Entity, bool> canInclude)
    {
        var list = new List<Entity>();
        if (span == CommandSpanKind.System && root.Manager != null)
        {
            foreach (var e in root.Manager.GetAllEntitiesWithDataBlob<PositionDB>())
            {
                if (canInclude(e))
                    list.Add(e);
            }
            return list;
        }

        if (canInclude(root))
            list.Add(root);

        if (span == CommandSpanKind.Well
            && root.TryGetDataBlob<PositionDB>(out var pos))
        {
            foreach (var child in pos.Children)
            {
                if (canInclude(child))
                    list.Add(child);
            }
        }

        return list;
    }
}

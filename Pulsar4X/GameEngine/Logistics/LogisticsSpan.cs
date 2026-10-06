using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;

namespace Pulsar4X.Logistics;

/// <summary>
/// Trade, freight, and supply read the command bridge one step wider than survey.
/// A ship bridge covers the well. A colony, planet, or sphere-of-influence bridge covers this
/// star system. A system bridge, and anything wider, covers this system and one known jump.
/// </summary>
public static class LogisticsSpan
{
    public static CommandSpanKind Of(Entity unit)
    {
        return CommandSpan.Of(unit) switch
        {
            CommandSpanKind.Body => CommandSpanKind.Well,
            CommandSpanKind.Well => CommandSpanKind.System,
            _ => CommandSpanKind.Neighbor,
        };
    }

    /// <summary>The unit's system, then each system one remembered jump away when the reach is <see cref="CommandSpanKind.Neighbor"/>.</summary>
    public static List<string> SystemIds(Entity unit)
    {
        var ids = new List<string>();
        if (unit?.Manager == null)
            return ids;
        ids.Add(unit.Manager.ManagerID);
        if (Of(unit) != CommandSpanKind.Neighbor)
            return ids;
        foreach (var id in JumpDestinations(unit))
        {
            if (!ids.Contains(id))
                ids.Add(id);
        }
        return ids;
    }

    public static bool ReachesManager(Entity unit, EntityManager? manager)
    {
        if (unit?.Manager == null || manager == null)
            return false;
        if (manager == unit.Manager)
            return true;
        if (Of(unit) != CommandSpanKind.Neighbor)
            return false;
        return JumpDestinations(unit).Contains(manager.ManagerID);
    }

    /// <summary>Both contract ends sit inside the fleet's logistics reach.</summary>
    public static bool ContractReaches(Entity fleet, Entity source, Entity dest)
    {
        if (fleet?.Manager == null || source?.Manager == null || dest?.Manager == null)
            return false;
        var span = Of(fleet);
        if (span == CommandSpanKind.Neighbor)
            return ReachesManager(fleet, source.Manager) && ReachesManager(fleet, dest.Manager);
        if (source.Manager != fleet.Manager || dest.Manager != fleet.Manager)
            return false;
        if (span == CommandSpanKind.System)
            return true;
        return ShareWell(source, dest);
    }

    /// <summary>
    /// Same body, a body and its moon, or two moons of that body.
    /// Two planets of the same star do not share a well.
    /// </summary>
    public static bool ShareWell(Entity left, Entity right)
    {
        var a = BodyOf(left);
        var b = BodyOf(right);
        if (a == null || b == null)
            return false;
        if (a.Id == b.Id)
            return true;
        var parentA = Parent(a);
        var parentB = Parent(b);
        if (parentB != null && parentB.Id == a.Id)
            return true;
        if (parentA != null && parentA.Id == b.Id)
            return true;
        return parentA != null && parentB != null && parentA.Id == parentB.Id && !IsSystemRoot(parentA);
    }

    static Entity? BodyOf(Entity entity)
    {
        if (entity.TryGetDataBlob<ColonyInfoDB>(out var info) && info.PlanetEntity != null && info.PlanetEntity.IsValid)
            return info.PlanetEntity;
        if (entity.TryGetDataBlob<PositionDB>(out var pos) && pos.Parent != null)
            return pos.Parent;
        return entity;
    }

    static Entity? Parent(Entity body)
        => body.TryGetDataBlob<PositionDB>(out var pos) ? pos.Parent : null;

    static bool IsSystemRoot(Entity body)
        => !body.TryGetDataBlob<PositionDB>(out var pos) || pos.Parent == null;

    static List<string> JumpDestinations(Entity unit)
    {
        var found = new List<string>();
        if (unit.Manager == null)
            return found;
        if (!unit.Manager.Game.Factions.TryGetValue(unit.FactionOwnerID, out var faction))
            return found;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return found;
        if (!info.KnownJumpPoints.TryGetValue(unit.Manager.ManagerID, out var gates))
            return found;

        foreach (var gate in gates)
        {
            if (gate == null || !gate.TryGetDataBlob<JumpPointDB>(out var jump))
                continue;
            if (!unit.Manager.TryGetGlobalEntityById(jump.DestinationId, out var dest) || dest.Manager == null)
                continue;
            string there = dest.Manager.ManagerID;
            if (there != unit.Manager.ManagerID && !found.Contains(there))
                found.Add(there);
        }
        return found;
    }
}

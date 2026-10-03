using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;

namespace Pulsar4X.Logistics;

/// <summary>
/// Known-jump path between two entities, and the one leg a ship can fly this wake.
/// </summary>
static class JumpRoute
{
    /// <summary>Hours added to the in-system travel term for each jump.</summary>
    public const double HopHours = 1.0;

    /// <summary>Same close-enough distance MovePlanner uses for a static site.</summary>
    const double AtGateMeters = 100_000;

    public static bool TryTravelHours(Entity ship, Entity from, Entity to, out double hours)
    {
        hours = 0;
        if (!TryConnect(ship, from, to, out var hops, out var meters, out _))
            return false;
        if (!ship.TryGetDataBlob<WarpAbilityDB>(out var warp) || warp.MaxSpeed <= 0)
            return false;

        hours = meters / warp.MaxSpeed / 3600.0 + hops * HopHours;
        return double.IsFinite(hours) && hours >= 0;
    }

    public static bool TryConnect(Entity ship, Entity from, Entity to, out int hops, out double meters, out Stack<Node> path)
    {
        hops = 0;
        meters = 0;
        path = new Stack<Node>();
        if (from?.Manager == null || to?.Manager == null)
            return false;
        if (!ship.Manager.Game.Factions.TryGetValue(ship.FactionOwnerID, out var faction))
            return false;

        var finder = new PathfindingManager(ship.Manager.Game);
        var graph = finder.GetPathfindingGraph(faction, ship.Manager.ManagerID);
        var sourceNode = new JPNode(from, from, new List<EdgeToNeighbor>());
        var destNode = new JPNode(to, to, new List<EdgeToNeighbor>());
        graph.AddNode(sourceNode);
        graph.AddNode(destNode);

        try
        {
            path = finder.GetPath(sourceNode, destNode, graph, out var cost);
            if (path == null || path.Count < 2 || !double.IsFinite(cost) || cost >= double.MaxValue)
                return false;

            hops = path.Count - 2;
            meters = cost;
            return true;
        }
        catch (InvalidOperationException)
        {
            path = new Stack<Node>();
            return false;
        }
    }

    /// <summary>
    /// The place is in the ship's system, or a known jump path reaches it.
    /// A pair in a known system with no path is not a candidate.
    /// </summary>
    public static bool CanReach(Entity ship, Entity place)
    {
        if (ship?.Manager == null || place?.Manager == null)
            return false;
        if (place.Manager == ship.Manager)
            return true;
        return TryTravelHours(ship, ship, place, out _);
    }

    /// <summary>
    /// Current leg toward a market in another system: warp to the next known gate, or transit it.
    /// </summary>
    public static PlanResult LegToward(Entity ship, Entity market)
    {
        if (!TryConnect(ship, ship, market, out _, out _, out var path))
            return PlanResult.Fail("no path");

        path.Pop();
        if (path.Count == 0 || path.Peek() is not JPNode next)
            return PlanResult.Fail("no path");

        var gate = GateInSystem(next, ship.Manager);
        if (gate == null || !gate.TryGetDataBlob<JumpPointDB>(out var jump))
            return PlanResult.Fail("no path");

        if (!AtGate(ship, gate))
        {
            if (!MovePlanner.TryBuildMoveActions(ship, gate, out var actions, out var reason))
                return PlanResult.Fail(string.IsNullOrEmpty(reason) ? "no path" : reason);
            if (actions.Count > 0)
                return PlanResult.Continue(actions);
        }

        return PlanResult.Continue(ShipJumpAction.Create(ship, jump));
    }

    static Entity GateInSystem(JPNode node, EntityManager system)
    {
        if (InSystem(node.Data.Item1, system) && node.Data.Item1.HasDataBlob<JumpPointDB>())
            return node.Data.Item1;
        if (InSystem(node.Data.Item2, system) && node.Data.Item2.HasDataBlob<JumpPointDB>())
            return node.Data.Item2;
        return null;
    }

    static bool InSystem(Entity entity, EntityManager system)
        => entity != null && entity.Manager == system;

    static bool AtGate(Entity ship, Entity gate)
    {
        if (ship.TryGetDataBlob<PositionDB>(out var shipPos) && shipPos.Parent == gate)
            return true;
        double sep = MarketRun.DistanceMeters(ship, gate);
        return sep <= AtGateMeters;
    }
}

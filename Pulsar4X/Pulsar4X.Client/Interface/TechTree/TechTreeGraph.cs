using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Client;

/// <summary>One tech blueprint as the visualiser sees it. No engine types.</summary>
public sealed record TechTreeSpec(
    string Id,
    string Name,
    string Description,
    string CategoryId,
    string CategoryName,
    string CostFormula,
    int MaxLevel,
    IReadOnlyList<TechTreeUnlock> Unlocks);

/// <summary>An id listed in a tech's <c>Unlocks</c> at a given level.</summary>
public sealed record TechTreeUnlock(int Level, string Id);

/// <summary>A non-tech id the tree can resolve (component template, armour, good, …).</summary>
public sealed record TechTreeUnlockable(string Id, string Name, string Kind);

/// <summary>The loaded tech blueprints plus the catalog of things those techs can unlock.</summary>
public sealed record TechTreeInput(
    IReadOnlyList<TechTreeSpec> Techs,
    IReadOnlyList<TechTreeUnlockable> Unlockables);

public enum TechTreeUnlockKind
{
    Tech,
    Component,
    Other,
    Missing,
}

public sealed record TechTreeUnlockTarget(int Level, string Id, string Name, TechTreeUnlockKind Kind);

public sealed class TechTreeNode
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string CostFormula { get; init; }
    public int MaxLevel { get; init; }
    public int Column { get; set; }
    public int Row { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public IReadOnlyList<string> Prerequisites { get; set; } = Array.Empty<string>();
    public IReadOnlyList<TechTreeUnlockTarget> Unlocks { get; set; } = Array.Empty<TechTreeUnlockTarget>();
}

public sealed record TechTreeEdge(string From, string To, int AtLevel);

public sealed record TechTreeCycle(IReadOnlyList<string> Ids);

public sealed class TechTreeWarnings
{
    public IReadOnlyList<string> MissingIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TechTreeCycle> Cycles { get; init; } = Array.Empty<TechTreeCycle>();
    public IReadOnlyList<string> Orphans { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> NoComponentTechs { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Layered left-to-right layout and analysis for the tech tree. No ImGui.
/// Column is the length of the longest prerequisite chain (no prereqs → column 0).
/// Cycle edges are dropped from the layout DAG so column assignment cannot hang.
/// </summary>
public sealed class TechTreeGraph
{
    public const float NodeWidth = 200f;
    public const float NodeHeight = 64f;
    public const float ColumnGap = 80f;
    public const float RowGap = 20f;

    readonly Dictionary<string, TechTreeNode> _nodes;
    readonly Dictionary<string, List<string>> _forward;
    readonly Dictionary<string, List<string>> _backward;
    readonly IReadOnlyList<TechTreeEdge> _edges;

    public IReadOnlyList<TechTreeNode> Nodes { get; }
    public IReadOnlyList<TechTreeEdge> Edges => _edges;
    public TechTreeWarnings Warnings { get; }

    TechTreeGraph(
        IReadOnlyList<TechTreeNode> nodes,
        IReadOnlyList<TechTreeEdge> edges,
        Dictionary<string, List<string>> forward,
        Dictionary<string, List<string>> backward,
        TechTreeWarnings warnings)
    {
        Nodes = nodes;
        _edges = edges;
        _nodes = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        _forward = forward;
        _backward = backward;
        Warnings = warnings;
    }

    public TechTreeNode? Find(string id)
        => id != null && _nodes.TryGetValue(id, out var node) ? node : null;

    /// <summary>This tech and every tech it needs, walking the original prereq graph.</summary>
    public IReadOnlyList<string> Upstream(string id) => Walk(_backward, id);

    /// <summary>This tech and every tech it opens, walking the original unlock graph.</summary>
    public IReadOnlyList<string> Downstream(string id) => Walk(_forward, id);

    /// <summary>Name or category substring, ordinal ignore-case, in layout order.</summary>
    public IReadOnlyList<TechTreeNode> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<TechTreeNode>();
        string needle = query.Trim();
        return Nodes
            .Where(node => Contains(node.Name, needle)
                || Contains(node.CategoryName, needle)
                || Contains(node.CategoryId, needle)
                || Contains(node.Id, needle))
            .ToList();
    }

    public static TechTreeGraph Build(TechTreeInput? input)
    {
        var techs = input?.Techs ?? Array.Empty<TechTreeSpec>();
        var unlockables = (input?.Unlockables ?? Array.Empty<TechTreeUnlockable>())
            .Where(u => !string.IsNullOrEmpty(u.Id))
            .GroupBy(u => u.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var nodes = new Dictionary<string, TechTreeNode>(StringComparer.Ordinal);
        foreach (var spec in techs)
        {
            if (string.IsNullOrEmpty(spec.Id) || nodes.ContainsKey(spec.Id))
                continue;
            nodes[spec.Id] = new TechTreeNode
            {
                Id = spec.Id,
                Name = spec.Name ?? spec.Id,
                Description = spec.Description ?? "",
                CategoryId = spec.CategoryId ?? "",
                CategoryName = string.IsNullOrEmpty(spec.CategoryName) ? (spec.CategoryId ?? "") : spec.CategoryName,
                CostFormula = spec.CostFormula ?? "",
                MaxLevel = spec.MaxLevel,
            };
        }

        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var originalEdges = new List<TechTreeEdge>();
        var forward = NewAdj(nodes.Keys);
        var backward = NewAdj(nodes.Keys);

        foreach (var spec in techs)
        {
            if (string.IsNullOrEmpty(spec.Id) || !nodes.TryGetValue(spec.Id, out var node))
                continue;
            var targets = new List<TechTreeUnlockTarget>();
            foreach (var unlock in spec.Unlocks ?? Array.Empty<TechTreeUnlock>())
            {
                if (string.IsNullOrEmpty(unlock.Id))
                    continue;
                if (nodes.ContainsKey(unlock.Id))
                {
                    targets.Add(new TechTreeUnlockTarget(unlock.Level, unlock.Id, nodes[unlock.Id].Name, TechTreeUnlockKind.Tech));
                    originalEdges.Add(new TechTreeEdge(spec.Id, unlock.Id, unlock.Level));
                    AddUnique(forward[spec.Id], unlock.Id);
                    AddUnique(backward[unlock.Id], spec.Id);
                }
                else if (unlockables.TryGetValue(unlock.Id, out var known))
                {
                    var kind = string.Equals(known.Kind, "component", StringComparison.OrdinalIgnoreCase)
                        ? TechTreeUnlockKind.Component
                        : TechTreeUnlockKind.Other;
                    targets.Add(new TechTreeUnlockTarget(unlock.Level, unlock.Id, known.Name, kind));
                }
                else
                {
                    missing.Add(unlock.Id);
                    targets.Add(new TechTreeUnlockTarget(unlock.Level, unlock.Id, unlock.Id, TechTreeUnlockKind.Missing));
                }
            }
            node.Unlocks = targets;
        }

        foreach (var node in nodes.Values)
            node.Prerequisites = (backward[node.Id]).ToList();

        var (dagEdges, cycles) = BreakCycles(nodes.Keys, originalEdges);
        AssignColumns(nodes, dagEdges);
        OrderRows(nodes.Values, dagEdges);
        Place(nodes.Values);

        var orphans = nodes.Values
            .Where(n => n.Prerequisites.Count == 0 && !n.Unlocks.Any(u => u.Kind == TechTreeUnlockKind.Tech))
            .Select(n => n.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var noComponents = nodes.Values
            .Where(n => n.Unlocks.All(u => u.Kind != TechTreeUnlockKind.Component))
            .Select(n => n.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var warnings = new TechTreeWarnings
        {
            MissingIds = missing.ToList(),
            Cycles = cycles,
            Orphans = orphans,
            NoComponentTechs = noComponents,
        };

        var ordered = nodes.Values
            .OrderBy(n => n.Column)
            .ThenBy(n => n.Row)
            .ThenBy(n => n.Id, StringComparer.Ordinal)
            .ToList();

        return new TechTreeGraph(ordered, originalEdges, forward, backward, warnings);
    }

    static Dictionary<string, List<string>> NewAdj(IEnumerable<string> ids)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var id in ids)
            map[id] = new List<string>();
        return map;
    }

    static void AddUnique(List<string> list, string id)
    {
        if (!list.Contains(id, StringComparer.Ordinal))
            list.Add(id);
    }

    static (List<TechTreeEdge> Dag, List<TechTreeCycle> Cycles) BreakCycles(
        IEnumerable<string> ids,
        IReadOnlyList<TechTreeEdge> edges)
    {
        var outgoing = edges.ToLookup(e => e.From, StringComparer.Ordinal);
        const int White = 0, Gray = 1, Black = 2;
        var color = new Dictionary<string, int>(StringComparer.Ordinal);
        var parent = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var id in ids)
        {
            color[id] = White;
            parent[id] = null;
        }

        var dropped = new HashSet<(string From, string To)>();
        var cycles = new List<TechTreeCycle>();
        var seenCycles = new HashSet<string>(StringComparer.Ordinal);
        int steps = 0;
        const int StepCap = 1_000_000;

        void Visit(string u)
        {
            if (steps++ > StepCap)
                return;
            color[u] = Gray;
            foreach (var edge in outgoing[u])
            {
                string v = edge.To;
                if (!color.ContainsKey(v))
                    continue;
                if (color[v] == Gray)
                {
                    dropped.Add((u, v));
                    var cycle = TraceCycle(parent, u, v);
                    if (cycle.Count > 0 && seenCycles.Add(CanonicalCycle(cycle)))
                        cycles.Add(new TechTreeCycle(cycle));
                    continue;
                }
                if (color[v] == White)
                {
                    parent[v] = u;
                    Visit(v);
                }
            }
            color[u] = Black;
        }

        foreach (var id in color.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (color[id] == White)
                Visit(id);
        }

        var dag = edges.Where(e => !dropped.Contains((e.From, e.To))).ToList();
        return (dag, cycles);
    }

    static List<string> TraceCycle(Dictionary<string, string?> parent, string from, string backTo)
    {
        var path = new List<string> { backTo };
        string? at = from;
        int guard = parent.Count + 2;
        while (at != null && at != backTo && guard-- > 0)
        {
            path.Add(at);
            at = parent.TryGetValue(at, out var p) ? p : null;
        }
        path.Add(backTo);
        path.Reverse();
        return path;
    }

    static string CanonicalCycle(IReadOnlyList<string> cycle)
    {
        // cycle is [start, ..., start]; ignore the repeated last id for the key.
        var core = cycle.Count > 1 && cycle[0] == cycle[cycle.Count - 1]
            ? cycle.Take(cycle.Count - 1).ToList()
            : cycle.ToList();
        if (core.Count == 0)
            return "";
        int min = 0;
        for (int i = 1; i < core.Count; i++)
        {
            if (string.Compare(core[i], core[min], StringComparison.Ordinal) < 0)
                min = i;
        }
        var rotated = core.Skip(min).Concat(core.Take(min));
        return string.Join(">", rotated);
    }

    static void AssignColumns(Dictionary<string, TechTreeNode> nodes, IReadOnlyList<TechTreeEdge> dag)
    {
        var outgoing = dag.ToLookup(e => e.From, StringComparer.Ordinal);
        var indegree = nodes.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in dag)
        {
            if (indegree.ContainsKey(edge.To))
                indegree[edge.To]++;
        }

        foreach (var node in nodes.Values)
            node.Column = 0;

        var queue = new Queue<string>();
        foreach (var id in indegree.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (indegree[id] == 0)
                queue.Enqueue(id);
        }

        int seen = 0;
        int cap = nodes.Count * 2 + 8;
        while (queue.Count > 0 && seen < cap)
        {
            seen++;
            string u = queue.Dequeue();
            int next = nodes[u].Column + 1;
            foreach (var edge in outgoing[u])
            {
                if (!nodes.TryGetValue(edge.To, out var dest))
                    continue;
                if (dest.Column < next)
                    dest.Column = next;
                indegree[edge.To]--;
                if (indegree[edge.To] == 0)
                    queue.Enqueue(edge.To);
            }
        }
    }

    static void OrderRows(IEnumerable<TechTreeNode> all, IReadOnlyList<TechTreeEdge> dag)
    {
        var columns = all
            .GroupBy(n => n.Column)
            .OrderBy(g => g.Key)
            .Select(g => g.ToList())
            .ToList();

        foreach (var col in columns)
        {
            col.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            AssignRowIndex(col);
        }

        var preds = dag.ToLookup(e => e.To, StringComparer.Ordinal);
        var succs = dag.ToLookup(e => e.From, StringComparer.Ordinal);
        var byId = all.ToDictionary(n => n.Id, StringComparer.Ordinal);

        for (int pass = 0; pass < 2; pass++)
        {
            for (int c = 1; c < columns.Count; c++)
                SortByBarycentre(columns[c], preds, byId, fromSide: true);
            for (int c = columns.Count - 2; c >= 0; c--)
                SortByBarycentre(columns[c], succs, byId, fromSide: false);
        }
    }

    static void SortByBarycentre(
        List<TechTreeNode> column,
        ILookup<string, TechTreeEdge> neighbours,
        Dictionary<string, TechTreeNode> byId,
        bool fromSide)
    {
        float Bary(TechTreeNode node)
        {
            float sum = 0;
            int count = 0;
            foreach (var edge in neighbours[node.Id])
            {
                string other = fromSide ? edge.From : edge.To;
                if (byId.TryGetValue(other, out var n))
                {
                    sum += n.Row;
                    count++;
                }
            }
            return count == 0 ? node.Row : sum / count;
        }

        column.Sort((a, b) =>
        {
            int cmp = Bary(a).CompareTo(Bary(b));
            if (cmp != 0)
                return cmp;
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });
        AssignRowIndex(column);
    }

    static void AssignRowIndex(List<TechTreeNode> column)
    {
        for (int i = 0; i < column.Count; i++)
            column[i].Row = i;
    }

    static void Place(IEnumerable<TechTreeNode> nodes)
    {
        float dx = NodeWidth + ColumnGap;
        float dy = NodeHeight + RowGap;
        foreach (var node in nodes)
        {
            node.X = node.Column * dx;
            node.Y = node.Row * dy;
        }
    }

    IReadOnlyList<string> Walk(Dictionary<string, List<string>> adj, string start)
    {
        if (start == null || !_nodes.ContainsKey(start))
            return Array.Empty<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        int cap = _nodes.Count * 2 + 8;
        int steps = 0;
        while (queue.Count > 0 && steps++ < cap)
        {
            string u = queue.Dequeue();
            if (!adj.TryGetValue(u, out var next))
                continue;
            foreach (var v in next)
            {
                if (seen.Add(v))
                    queue.Enqueue(v);
            }
        }
        return seen.OrderBy(id => id, StringComparer.Ordinal).ToList();
    }

    static bool Contains(string? text, string needle)
        => !string.IsNullOrEmpty(text) && text.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

using System;
using System.Collections.Generic;
using System.Globalization;
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

/// <summary>
/// Cost of a <c>CostFormula</c> at a given NCalc <c>Level</c> parameter.
/// The first researchable level uses Level=0, so game level k costs <c>CostFormula(Level=k-1)</c>.
/// Return null when the formula cannot be evaluated.
/// </summary>
public delegate long? TechCostEvaluate(string formula, int formulaLevel);

public enum TechTreeUnlockKind
{
    Tech,
    Component,
    Other,
    Missing,
}

public sealed record TechTreeUnlockTarget(int Level, string Id, string Name, TechTreeUnlockKind Kind);

/// <summary>One researchable level of a tech, with its own cost and running cumulative RP.</summary>
public sealed record TechTreeLevel(int Level, long Cost, long CumulativeRp, bool CostFailed);

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

    /// <summary>Cheapest cumulative RP at which this tech can start: 0 for a root, otherwise the
    /// minimum over parents of the parent's cumulative RP at the Unlocks level that opens this tech.</summary>
    public long UnlockRp { get; set; }

    /// <summary>Levels 1..MaxLevel, each ending at that level's cumulative RP.</summary>
    public IReadOnlyList<TechTreeLevel> Levels { get; set; } = Array.Empty<TechTreeLevel>();

    public int RpRow { get; set; }
    public float RpX { get; set; }
    public float RpWidth { get; set; }
    public float RpY { get; set; }

    /// <summary>Cumulative RP at the end of <paramref name="level"/>, or <see cref="UnlockRp"/> when level is 0.</summary>
    public long CumulativeRpAt(int level)
    {
        if (level <= 0 || Levels.Count == 0)
            return UnlockRp;
        int index = Math.Min(level, Levels.Count) - 1;
        return Levels[index].CumulativeRp;
    }
}

public sealed record TechTreeEdge(string From, string To, int AtLevel);

public sealed record TechTreeCycle(IReadOnlyList<string> Ids);

public sealed class TechTreeWarnings
{
    public IReadOnlyList<string> MissingIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<TechTreeCycle> Cycles { get; init; } = Array.Empty<TechTreeCycle>();
    public IReadOnlyList<string> Orphans { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> NoComponentTechs { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FailedCostFormulas { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Layered left-to-right layout and analysis for the tech tree. No ImGui.
/// Column is the length of the longest prerequisite chain (no prereqs → column 0).
/// Cycle edges are dropped from the layout DAG so column assignment cannot hang.
/// The RP-graph layout places each tech on a log RP axis; see <see cref="RpLogOffset"/>.
/// </summary>
public sealed class TechTreeGraph
{
    public const float NodeWidth = 200f;
    public const float NodeHeight = 64f;
    public const float ColumnGap = 80f;
    public const float RowGap = 20f;

    /// <summary>
    /// Log-axis offset in RP. A tech at cumulative RP r is placed at
    /// <c>log10(r + 250) - log10(250)</c> decades, so roots at 0 sit at x=0 and small
    /// costs still have a visible width. A hard floor would collapse every bar below the floor.
    /// </summary>
    public const double RpLogOffset = 250;

    public const float RpPixelsPerDecade = 280f;
    public const float RpBarHeight = 28f;
    public const float RpRowGap = 8f;
    public const float RpPackGap = 6f;

    readonly Dictionary<string, TechTreeNode> _nodes;
    readonly Dictionary<string, List<string>> _forward;
    readonly Dictionary<string, List<string>> _backward;
    readonly IReadOnlyList<TechTreeEdge> _edges;

    public IReadOnlyList<TechTreeNode> Nodes { get; }
    public IReadOnlyList<TechTreeEdge> Edges => _edges;
    public TechTreeWarnings Warnings { get; }
    public long MaxCumulativeRp { get; }

    TechTreeGraph(
        IReadOnlyList<TechTreeNode> nodes,
        IReadOnlyList<TechTreeEdge> edges,
        Dictionary<string, List<string>> forward,
        Dictionary<string, List<string>> backward,
        TechTreeWarnings warnings,
        long maxCumulativeRp)
    {
        Nodes = nodes;
        _edges = edges;
        _nodes = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        _forward = forward;
        _backward = backward;
        Warnings = warnings;
        MaxCumulativeRp = maxCumulativeRp;
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

    public static TechTreeGraph Build(TechTreeInput? input, TechCostEvaluate? costEvaluate = null)
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
        var failedCosts = AssignResearchPoints(nodes, dagEdges, costEvaluate);
        PackRpRows(nodes.Values, dagEdges);

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
            FailedCostFormulas = failedCosts,
        };

        var ordered = nodes.Values
            .OrderBy(n => n.Column)
            .ThenBy(n => n.Row)
            .ThenBy(n => n.Id, StringComparer.Ordinal)
            .ToList();

        long maxRp = 0;
        foreach (var node in ordered)
            maxRp = Math.Max(maxRp, node.CumulativeRpAt(node.Levels.Count));
        return new TechTreeGraph(ordered, originalEdges, forward, backward, warnings, maxRp);
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


    /// <summary>Layout X for a cumulative RP value on the offset-log axis. RP 0 maps to 0.</summary>
    public static float RpToX(double rp)
    {
        double value = Math.Max(0, rp);
        return (float)((Math.Log10(value + RpLogOffset) - Math.Log10(RpLogOffset)) * RpPixelsPerDecade);
    }

    /// <summary>Inverse of <see cref="RpToX"/>.</summary>
    public static double XToRp(float x)
        => Math.Pow(10, (x / RpPixelsPerDecade) + Math.Log10(RpLogOffset)) - RpLogOffset;

    /// <summary>
    /// 1-2-5 ticks starting at 500 (500, 1k, 2k, 5k, …) far enough to cover <paramref name="maxRp"/>.
    /// </summary>
    public static IReadOnlyList<long> RulerTicks(double maxRp)
    {
        var ticks = new List<long>();
        double cap = Math.Max(maxRp, 500);
        for (int exp = 2; exp <= 18; exp++)
        {
            long mag = 1;
            for (int i = 0; i < exp; i++)
                mag *= 10;
            foreach (long step in new long[] { 1, 2, 5 })
            {
                long tick = step * mag;
                if (tick < 500)
                    continue;
                ticks.Add(tick);
                if (tick >= cap)
                    return ticks;
            }
        }
        return ticks;
    }

    static List<string> AssignResearchPoints(
        Dictionary<string, TechTreeNode> nodes,
        IReadOnlyList<TechTreeEdge> dag,
        TechCostEvaluate? costEvaluate)
    {
        TechCostEvaluate evaluate = costEvaluate ?? ParseConstantCost;
        var incoming = dag.ToLookup(e => e.To, StringComparer.Ordinal);
        var outgoing = dag.ToLookup(e => e.From, StringComparer.Ordinal);
        var indegree = nodes.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in dag)
        {
            if (indegree.ContainsKey(edge.To))
                indegree[edge.To]++;
        }

        var queue = new Queue<string>();
        foreach (var id in indegree.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (indegree[id] == 0)
                queue.Enqueue(id);
        }

        var failed = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new HashSet<string>(nodes.Keys, StringComparer.Ordinal);
        int cap = nodes.Count * 2 + 8;
        int seen = 0;
        while ((queue.Count > 0 || pending.Count > 0) && seen < cap)
        {
            seen++;
            string id;
            if (queue.Count > 0)
                id = queue.Dequeue();
            else
            {
                id = pending.OrderBy(k => k, StringComparer.Ordinal).First();
                queue.Enqueue(id);
                continue;
            }
            if (!pending.Remove(id))
                continue;

            var node = nodes[id];
            long cheapest = long.MaxValue;
            int parents = 0;
            foreach (var edge in incoming[id])
            {
                if (!nodes.TryGetValue(edge.From, out var parent))
                    continue;
                parents++;
                cheapest = Math.Min(cheapest, parent.CumulativeRpAt(Math.Max(1, edge.AtLevel)));
            }
            node.UnlockRp = parents == 0 ? 0 : cheapest;

            int levelCount = Math.Max(1, node.MaxLevel);
            var levels = new List<TechTreeLevel>(levelCount);
            long running = node.UnlockRp;
            for (int level = 1; level <= levelCount; level++)
            {
                long? cost = evaluate(node.CostFormula ?? "", level - 1);
                bool failedCost = cost == null;
                if (failedCost)
                {
                    failed.Add(node.Id);
                    cost = 0;
                }
                if (cost < 0)
                    cost = 0;
                running += cost.Value;
                levels.Add(new TechTreeLevel(level, cost.Value, running, failedCost));
            }
            node.Levels = levels;
            node.RpX = RpToX(node.UnlockRp);
            node.RpWidth = Math.Max(0, RpToX(running) - node.RpX);

            foreach (var edge in outgoing[id])
            {
                if (!indegree.ContainsKey(edge.To))
                    continue;
                indegree[edge.To]--;
                if (indegree[edge.To] == 0)
                    queue.Enqueue(edge.To);
            }
        }

        return failed.ToList();
    }

    static long? ParseConstantCost(string formula, int _)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return null;
        if (long.TryParse(formula.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            return value < 0 ? 0 : value;
        return null;
    }

    /// <summary>
    /// Pack RP bars by family: walk each DAG root's descendants (cheapest-parent first),
    /// greedily stack bars in that family so they do not overlap on X, then place the next
    /// family on the rows below. This keeps branches together and is readable on a wide axis.
    /// </summary>
    static void PackRpRows(IEnumerable<TechTreeNode> all, IReadOnlyList<TechTreeEdge> dag)
    {
        var nodes = all.ToDictionary(n => n.Id, StringComparer.Ordinal);
        var outgoing = dag.ToLookup(e => e.From, StringComparer.Ordinal);
        var indegree = nodes.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
        foreach (var edge in dag)
        {
            if (indegree.ContainsKey(edge.To))
                indegree[edge.To]++;
        }

        var roots = nodes.Values
            .Where(n => indegree[n.Id] == 0)
            .OrderBy(n => n.Name, StringComparer.Ordinal)
            .ThenBy(n => n.Id, StringComparer.Ordinal)
            .ToList();

        var visited = new HashSet<string>(StringComparer.Ordinal);
        int rowBase = 0;
        foreach (var root in roots)
        {
            var family = new List<TechTreeNode>();
            CollectFamily(root.Id, nodes, outgoing, visited, family);
            if (family.Count == 0)
                continue;
            int used = PackFamily(family);
            foreach (var node in family)
            {
                node.RpRow += rowBase;
                node.RpY = node.RpRow * (RpBarHeight + RpRowGap);
            }
            rowBase += used;
        }

        // Any leftover (should be none) gets its own row.
        foreach (var node in nodes.Values.OrderBy(n => n.Name, StringComparer.Ordinal))
        {
            if (visited.Contains(node.Id))
                continue;
            node.RpRow = rowBase;
            node.RpY = node.RpRow * (RpBarHeight + RpRowGap);
            node.RpX = RpToX(node.UnlockRp);
            long end = node.CumulativeRpAt(node.Levels.Count);
            node.RpWidth = Math.Max(0, RpToX(end) - node.RpX);
            rowBase++;
        }
    }

    static void CollectFamily(
        string start,
        Dictionary<string, TechTreeNode> nodes,
        ILookup<string, TechTreeEdge> outgoing,
        HashSet<string> visited,
        List<TechTreeNode> family)
    {
        if (!visited.Add(start) || !nodes.TryGetValue(start, out var node))
            return;
        family.Add(node);
        var children = outgoing[start]
            .Select(e => e.To)
            .Distinct(StringComparer.Ordinal)
            .Where(id => nodes.ContainsKey(id) && !visited.Contains(id))
            .Select(id => nodes[id])
            .OrderBy(n => n.UnlockRp)
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .ThenBy(n => n.Id, StringComparer.Ordinal)
            .ToList();
        foreach (var child in children)
            CollectFamily(child.Id, nodes, outgoing, visited, family);
    }

    static int PackFamily(List<TechTreeNode> family)
    {
        var rows = new List<List<(float X0, float X1)>>();
        foreach (var node in family)
        {
            long endRp = node.CumulativeRpAt(node.Levels.Count);
            node.RpX = RpToX(node.UnlockRp);
            node.RpWidth = Math.Max(0, RpToX(endRp) - node.RpX);
            float x0 = node.RpX;
            float x1 = node.RpX + node.RpWidth;
            int row = 0;
            for (; row < rows.Count; row++)
            {
                if (rows[row].All(span => x1 <= span.X0 + 0.01f || x0 >= span.X1 - 0.01f))
                    break;
            }
            if (row == rows.Count)
                rows.Add(new List<(float, float)>());
            rows[row].Add((x0, x1));
            node.RpRow = row;
        }
        return rows.Count;
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

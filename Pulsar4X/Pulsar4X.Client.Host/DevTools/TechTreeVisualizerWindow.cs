using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client.Interface.Windows;

/// <summary>Debug tech-tree visualiser: pan, zoom, focus, search, and a warnings panel.</summary>
public sealed class TechTreeVisualizerWindow : UniquePulsarGuiWindow<TechTreeVisualizerWindow>
{
    const float MinZoom = 0.25f;
    const float MaxZoom = 2.5f;
    const float SideWidth = 280f;
    const float DragThreshold = 4f;

    TechTreeGraph? _graph;
    bool _rpGraph = true;
    string _source = "";
    string? _selectedId;
    string? _pendingCentreId;
    Vector2 _pan;
    float _zoom = 1f;
    readonly byte[] _searchBuffer = new byte[128];
    int _searchIndex;
    Vector2 _dragStart;
    bool _dragging;

    static readonly Vector4[] CategoryPalette =
    {
        new(0.35f, 0.62f, 0.85f, 1f),
        new(0.45f, 0.78f, 0.55f, 1f),
        new(0.88f, 0.62f, 0.35f, 1f),
        new(0.78f, 0.45f, 0.70f, 1f),
        new(0.85f, 0.78f, 0.35f, 1f),
        new(0.50f, 0.70f, 0.80f, 1f),
        new(0.80f, 0.40f, 0.40f, 1f),
        new(0.55f, 0.50f, 0.85f, 1f),
        new(0.40f, 0.80f, 0.75f, 1f),
        new(0.75f, 0.70f, 0.55f, 1f),
    };

    TechTreeVisualizerWindow()
    {
    }

    internal static TechTreeVisualizerWindow GetInstance()
    {
        if (_uiState.TryGetUniqueWindow<TechTreeVisualizerWindow>(out var window))
            return window;
        return _uiState.AddUniqueWindow(new TechTreeVisualizerWindow());
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        ImGui.SetNextWindowSize(new Vector2(1280, 800), ImGuiCond.FirstUseEver);
        using (var w = Window.Scope("Tech Tree Visualiser", ref IsActive, _flags))
        {
            if (!w.Visible)
                return;

            if (_graph == null)
                Reload();

            DrawToolbar();

            if (_graph == null)
            {
                ImGui.TextUnformatted("No tech blueprints loaded. Start a game or enable a mod that contains techs.");
                return;
            }

            Vector2 avail = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("##tech-tree-warnings", new Vector2(SideWidth, avail.Y), ImGuiChildFlags.Borders))
                DrawWarnings();
            ImGui.EndChild();

            ImGui.SameLine();
            float canvasWidth = Math.Max(120f, avail.X - SideWidth * 2f - ImGui.GetStyle().ItemSpacing.X * 2f);
            if (ImGui.BeginChild("##tech-tree-canvas-host", new Vector2(canvasWidth, avail.Y), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                DrawCanvas();
            ImGui.EndChild();

            ImGui.SameLine();
            if (ImGui.BeginChild("##tech-tree-detail", new Vector2(SideWidth, avail.Y), ImGuiChildFlags.Borders))
                DrawDetail();
            ImGui.EndChild();
        }
    }

    void DrawToolbar()
    {
        if (ImGui.Button("Reload"))
            Reload();
        ImGui.SameLine();
        if (ImGui.Button("Reset view"))
        {
            _zoom = 1f;
            _pan = Vector2.Zero;
            if (_graph?.Nodes.Count > 0)
                _pendingCentreId = _selectedId ?? _graph.Nodes[0].Id;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("RP graph", _rpGraph))
        {
            if (!_rpGraph)
            {
                _rpGraph = true;
                _zoom = 1f;
                _pan = Vector2.Zero;
                if (_graph?.Nodes.Count > 0)
                    _pendingCentreId = _selectedId ?? _graph.Nodes[0].Id;
            }
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Column view", !_rpGraph))
        {
            if (_rpGraph)
            {
                _rpGraph = false;
                _zoom = 1f;
                _pan = Vector2.Zero;
                if (_graph?.Nodes.Count > 0)
                    _pendingCentreId = _selectedId ?? _graph.Nodes[0].Id;
            }
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("Source: " + _source + (_graph == null ? "" : " (" + _graph.Nodes.Count + " techs)"));

        ImGui.TextUnformatted("Search");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(260);
        bool changed = ImGui.InputText("##tech-tree-search", _searchBuffer, (uint)_searchBuffer.Length);
        ImGui.SameLine();
        if (ImGui.Button("Next") || (changed && FirstSearchMatch() != null))
            JumpToSearch(changed);
    }

    void Reload()
    {
        var (input, source) = TechTreeData.Load();
        _source = source;
        _graph = input == null ? null : TechTreeGraph.Build(input, TechTreeData.EvaluateCost);
        _searchIndex = 0;
        if (_graph?.Nodes.Count > 0 && _selectedId == null)
            _pendingCentreId = _graph.Nodes[0].Id;
    }

    void JumpToSearch(bool fromTyping)
    {
        if (_graph == null)
            return;
        var hits = _graph.Search(FilterText(_searchBuffer));
        if (hits.Count == 0)
            return;
        if (fromTyping)
            _searchIndex = 0;
        else
            _searchIndex = (_searchIndex + 1) % hits.Count;
        var node = hits[_searchIndex];
        _selectedId = node.Id;
        _pendingCentreId = node.Id;
    }

    TechTreeNode? FirstSearchMatch()
    {
        if (_graph == null)
            return null;
        var hits = _graph.Search(FilterText(_searchBuffer));
        return hits.Count == 0 ? null : hits[0];
    }

    void DrawWarnings()
    {
        var warnings = _graph!.Warnings;
        DisplayHelpers.Header("Warnings");
        ImGui.TextUnformatted("Click a line to focus that tech.");
        WarningList("Missing prereq ids (" + warnings.MissingIds.Count + ")", warnings.MissingIds, id =>
        {
            var owner = _graph.Nodes.FirstOrDefault(n => n.Unlocks.Any(u => u.Id == id));
            if (owner != null)
            {
                _selectedId = owner.Id;
                _pendingCentreId = owner.Id;
            }
        });
        if (ImGui.CollapsingHeader("Cycles (" + warnings.Cycles.Count + ")", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (warnings.Cycles.Count == 0)
                ImGui.TextDisabled("None.");
            for (int i = 0; i < warnings.Cycles.Count; i++)
            {
                var cycle = warnings.Cycles[i];
                string label = string.Join(" → ", cycle.Ids);
                if (ImGui.Selectable(label + "###cycle-" + i))
                {
                    _selectedId = cycle.Ids[0];
                    _pendingCentreId = cycle.Ids[0];
                }
            }
        }
        WarningList("Orphans (" + warnings.Orphans.Count + ")", warnings.Orphans, FocusId);
        WarningList("Unlock no components (" + warnings.NoComponentTechs.Count + ")", warnings.NoComponentTechs, FocusId);
        WarningList("Cost formula failed (" + warnings.FailedCostFormulas.Count + ")", warnings.FailedCostFormulas, FocusId);
    }

    void WarningList(string header, IReadOnlyList<string> ids, Action<string> onPick)
    {
        if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ids.Count == 0)
                ImGui.TextDisabled("None.");
            foreach (var id in ids)
            {
                if (ImGui.Selectable(id + "###warn-" + header + "-" + id))
                    onPick(id);
            }
        }
    }

    void FocusId(string id)
    {
        _selectedId = id;
        _pendingCentreId = id;
    }

    void DrawCanvas()
    {
        var graph = _graph!;
        Vector2 origin = ImGui.GetCursorScreenPos();
        Vector2 size = ImGui.GetContentRegionAvail();
        if (size.X < 8 || size.Y < 8)
            return;

        ImGui.InvisibleButton("##tech-tree-canvas", size);
        bool hovered = ImGui.IsItemHovered();
        var io = ImGui.GetIO();

        if (hovered && Math.Abs(io.MouseWheel) > 0.01f)
        {
            Vector2 mouse = io.MousePos;
            Vector2 worldBefore = ScreenToWorld(mouse, origin);
            _zoom = Math.Clamp(_zoom * (io.MouseWheel > 0 ? 1.1f : 1f / 1.1f), MinZoom, MaxZoom);
            _pan = (mouse - origin) / _zoom - worldBefore;
        }

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _dragStart = io.MousePos;
            _dragging = false;
        }
        if (hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            Vector2 delta = io.MousePos - _dragStart;
            if (delta.LengthSquared() > DragThreshold * DragThreshold)
            {
                if (_dragging)
                    _pan += io.MouseDelta / _zoom;
                else
                {
                    _dragging = true;
                    _pan += delta / _zoom;
                }
                _dragStart = io.MousePos;
            }
        }

        if (_pendingCentreId != null && graph.Find(_pendingCentreId) is { } centre)
        {
            Vector2 mid = NodeCentre(centre);
            _pan = (size * 0.5f) / _zoom - mid;
            _pendingCentreId = null;
        }

        if (hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !_dragging)
        {
            var hit = HitTestAt(graph, io.MousePos, origin);
            _selectedId = hit?.Id;
        }

        var focus = FocusSet(graph, _selectedId);
        var searchHits = new HashSet<string>(graph.Search(FilterText(_searchBuffer)).Select(n => n.Id), StringComparer.Ordinal);

        var draw = ImGui.GetWindowDrawList();
        draw.PushClipRect(origin, origin + size, true);
        draw.AddRectFilled(origin, origin + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.08f, 0.08f, 0.10f, 1f)));

        if (_rpGraph)
        {
            DrawRpGrid(draw, graph, origin, size);
            DrawRpEdges(draw, graph, origin, focus);
            foreach (var node in graph.Nodes)
                DrawRpBar(draw, node, origin, focus, searchHits);
            DrawRpRuler(draw, graph, origin, size);
        }
        else
        {
            foreach (var edge in graph.Edges)
            {
                var from = graph.Find(edge.From);
                var to = graph.Find(edge.To);
                if (from == null || to == null)
                    continue;
                bool lit = focus == null || (focus.Contains(from.Id) && focus.Contains(to.Id));
                var a = WorldToScreen(new Vector2(from.X + TechTreeGraph.NodeWidth, from.Y + TechTreeGraph.NodeHeight * 0.5f), origin);
                var b = WorldToScreen(new Vector2(to.X, to.Y + TechTreeGraph.NodeHeight * 0.5f), origin);
                DrawEdge(draw, a, b, lit);
            }

            foreach (var node in graph.Nodes)
            {
                bool lit = focus == null || focus.Contains(node.Id);
                bool selected = node.Id == _selectedId;
                bool match = searchHits.Contains(node.Id);
                Vector2 p0 = WorldToScreen(new Vector2(node.X, node.Y), origin);
                Vector2 p1 = WorldToScreen(new Vector2(node.X + TechTreeGraph.NodeWidth, node.Y + TechTreeGraph.NodeHeight), origin);
                DrawBox(draw, p0, p1, CategoryColour(node.CategoryId), lit, selected, match, node.Name, node.CostFormula);
            }
        }
        draw.PopClipRect();

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    void DrawDetail()
    {
        DisplayHelpers.Header("Selected tech");
        if (_selectedId == null || _graph?.Find(_selectedId) is not { } node)
        {
            ImGui.TextDisabled("Click a tech, or click empty space to clear.");
            return;
        }

        ImGui.TextUnformatted(node.Name);
        ImGui.TextDisabled(node.Id);
        ImGui.TextUnformatted(node.CategoryName);
        ImGui.TextUnformatted("Max level: " + node.MaxLevel);
        if (!string.IsNullOrEmpty(node.CostFormula))
            ImGui.TextUnformatted("Cost: " + node.CostFormula);
        ImGui.TextUnformatted("Unlocks at: " + FormatRp(node.UnlockRp) + " RP");
        if (node.Levels.Count > 0)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Levels");
            foreach (var level in node.Levels)
            {
                string cost = level.CostFailed ? "formula failed" : FormatRp(level.Cost);
                ImGui.TextUnformatted("L" + level.Level + ": " + cost + "  (cumulative " + FormatRp(level.CumulativeRp) + ")");
            }
        }
        if (!string.IsNullOrEmpty(node.Description))
        {
            ImGui.Separator();
            ImGui.TextUnformatted(node.Description);
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Prerequisites");
        if (node.Prerequisites.Count == 0)
            ImGui.TextDisabled("None.");
        foreach (var id in node.Prerequisites)
        {
            string label = _graph.Find(id)?.Name ?? id;
            if (ImGui.Selectable(label + "###prereq-" + id))
                FocusId(id);
        }

        ImGui.Separator();
        ImGui.TextUnformatted("Unlocks");
        if (node.Unlocks.Count == 0)
            ImGui.TextDisabled("None.");
        foreach (var unlock in node.Unlocks)
        {
            string tag = unlock.Kind switch
            {
                TechTreeUnlockKind.Tech => "tech",
                TechTreeUnlockKind.Component => "component",
                TechTreeUnlockKind.Missing => "missing",
                _ => "other",
            };
            string label = "L" + unlock.Level + " [" + tag + "] " + unlock.Name;
            if (ImGui.Selectable(label + "###unlock-" + unlock.Id + "-" + unlock.Level))
            {
                if (unlock.Kind == TechTreeUnlockKind.Tech)
                    FocusId(unlock.Id);
            }
        }
    }

    static HashSet<string>? FocusSet(TechTreeGraph graph, string? selectedId)
    {
        if (selectedId == null)
            return null;
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in graph.Upstream(selectedId))
            set.Add(id);
        foreach (var id in graph.Downstream(selectedId))
            set.Add(id);
        return set;
    }

    TechTreeNode? HitTestAt(TechTreeGraph graph, Vector2 mouse, Vector2 origin)
    {
        foreach (var node in graph.Nodes)
        {
            (Vector2 p0, Vector2 p1) = NodeScreenRect(node, origin);
            if (mouse.X >= p0.X && mouse.X <= p1.X && mouse.Y >= p0.Y && mouse.Y <= p1.Y)
                return node;
        }
        return null;
    }

    (Vector2, Vector2) NodeScreenRect(TechTreeNode node, Vector2 origin)
    {
        if (_rpGraph)
        {
            Vector2 p0 = WorldToScreen(new Vector2(node.RpX, node.RpY), origin);
            Vector2 p1 = WorldToScreen(new Vector2(node.RpX + node.RpWidth, node.RpY + TechTreeGraph.RpBarHeight), origin);
            return (p0, p1);
        }
        Vector2 a = WorldToScreen(new Vector2(node.X, node.Y), origin);
        Vector2 b = WorldToScreen(new Vector2(node.X + TechTreeGraph.NodeWidth, node.Y + TechTreeGraph.NodeHeight), origin);
        return (a, b);
    }

    Vector2 NodeCentre(TechTreeNode node)
    {
        if (_rpGraph)
            return new Vector2(node.RpX + node.RpWidth * 0.5f, node.RpY + TechTreeGraph.RpBarHeight * 0.5f);
        return new Vector2(node.X + TechTreeGraph.NodeWidth * 0.5f, node.Y + TechTreeGraph.NodeHeight * 0.5f);
    }

    void DrawRpGrid(ImDrawListPtr draw, TechTreeGraph graph, Vector2 origin, Vector2 size)
    {
        uint line = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.06f));
        foreach (long tick in TechTreeGraph.RulerTicks(graph.MaxCumulativeRp))
        {
            float x = WorldToScreen(new Vector2(TechTreeGraph.RpToX(tick), 0), origin).X;
            if (x < origin.X || x > origin.X + size.X)
                continue;
            draw.AddLine(new Vector2(x, origin.Y), new Vector2(x, origin.Y + size.Y), line);
        }
    }

    void DrawRpEdges(ImDrawListPtr draw, TechTreeGraph graph, Vector2 origin, HashSet<string>? focus)
    {
        foreach (var edge in graph.Edges)
        {
            var from = graph.Find(edge.From);
            var to = graph.Find(edge.To);
            if (from == null || to == null)
                continue;
            bool lit = focus == null || (focus.Contains(from.Id) && focus.Contains(to.Id));
            float startX = TechTreeGraph.RpToX(from.CumulativeRpAt(edge.AtLevel));
            var a = WorldToScreen(new Vector2(startX, from.RpY + TechTreeGraph.RpBarHeight * 0.5f), origin);
            var b = WorldToScreen(new Vector2(to.RpX, to.RpY + TechTreeGraph.RpBarHeight * 0.5f), origin);
            DrawEdge(draw, a, b, lit);
        }
    }

    void DrawRpBar(ImDrawListPtr draw, TechTreeNode node, Vector2 origin, HashSet<string>? focus, HashSet<string> searchHits)
    {
        bool lit = focus == null || focus.Contains(node.Id);
        bool selected = node.Id == _selectedId;
        bool match = searchHits.Contains(node.Id);
        var baseFill = CategoryColour(node.CategoryId);
        Vector2 p0 = WorldToScreen(new Vector2(node.RpX, node.RpY), origin);
        Vector2 p1 = WorldToScreen(new Vector2(node.RpX + node.RpWidth, node.RpY + TechTreeGraph.RpBarHeight), origin);
        var border = selected ? new Vector4(1f, 1f, 1f, 1f) : match ? new Vector4(1f, 0.9f, 0.4f, 1f) : new Vector4(0.1f, 0.1f, 0.12f, lit ? 0.9f : 0.25f);

        long previous = node.UnlockRp;
        for (int i = 0; i < node.Levels.Count; i++)
        {
            var level = node.Levels[i];
            Vector2 s0 = WorldToScreen(new Vector2(TechTreeGraph.RpToX(previous), node.RpY), origin);
            Vector2 s1 = WorldToScreen(new Vector2(TechTreeGraph.RpToX(level.CumulativeRp), node.RpY + TechTreeGraph.RpBarHeight), origin);
            var fill = baseFill;
            float shade = (i % 2 == 0) ? 1f : 0.78f;
            fill = new Vector4(fill.X * shade, fill.Y * shade, fill.Z * shade, lit ? 0.9f : 0.18f);
            draw.AddRectFilled(s0, s1, ImGui.ColorConvertFloat4ToU32(fill));
            if ((s1.X - s0.X) >= 10f * _zoom)
            {
                uint tickCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, lit ? 0.35f : 0.12f));
                draw.AddLine(new Vector2(s1.X, s0.Y), new Vector2(s1.X, s1.Y), tickCol);
                if ((s1.X - s0.X) >= 14f * _zoom && (s1.Y - s0.Y) >= 12f)
                    draw.AddText(s0 + new Vector2(2f * _zoom, 2f * _zoom), ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.06f, 0.08f, lit ? 0.9f : 0.3f)), level.Level.ToString());
            }
            previous = level.CumulativeRp;
        }
        draw.AddRect(p0, p1, ImGui.ColorConvertFloat4ToU32(border), 3f, ImDrawFlags.None, selected ? 2.5f : 1.2f);
        if ((p1.Y - p0.Y) >= 14f)
        {
            uint textCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.06f, 0.08f, lit ? 1f : 0.35f));
            float pad = 4f * _zoom;
            draw.AddText(p0 + new Vector2(pad, Math.Max(0, (p1.Y - p0.Y - ImGui.GetTextLineHeight()) * 0.5f)), textCol, Truncate(node.Name, 32));
        }
    }

    void DrawRpRuler(ImDrawListPtr draw, TechTreeGraph graph, Vector2 origin, Vector2 size)
    {
        float height = 28f;
        Vector2 top = new(origin.X, origin.Y + size.Y - height);
        draw.AddRectFilled(top, origin + size, ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.14f, 0.95f)));
        uint ink = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.85f, 0.88f, 0.95f));
        // Root/zero sits at x=0 on the offset-log axis.
        float zeroX = WorldToScreen(new Vector2(TechTreeGraph.RpToX(0), 0), origin).X;
        if (zeroX >= origin.X && zeroX <= origin.X + size.X)
        {
            draw.AddLine(new Vector2(zeroX, top.Y), new Vector2(zeroX, origin.Y + size.Y), ink);
            draw.AddText(new Vector2(zeroX + 3, top.Y + 8), ink, "0");
        }
        foreach (long tick in TechTreeGraph.RulerTicks(graph.MaxCumulativeRp))
        {
            float x = WorldToScreen(new Vector2(TechTreeGraph.RpToX(tick), 0), origin).X;
            if (x < origin.X || x > origin.X + size.X)
                continue;
            draw.AddLine(new Vector2(x, top.Y), new Vector2(x, origin.Y + size.Y), ink);
            draw.AddText(new Vector2(x + 3, top.Y + 8), ink, FormatRp(tick));
        }
    }

    static void DrawEdge(ImDrawListPtr draw, Vector2 a, Vector2 b, bool lit)
    {
        float pull = Math.Max(40f, Math.Abs(b.X - a.X) * 0.45f);
        var c1 = new Vector2(a.X + pull, a.Y);
        var c2 = new Vector2(b.X - pull, b.Y);
        Vector4 colour = lit
            ? new Vector4(0.75f, 0.78f, 0.85f, 0.9f)
            : new Vector4(0.45f, 0.48f, 0.55f, 0.18f);
        draw.AddBezierCubic(a, c1, c2, b, ImGui.ColorConvertFloat4ToU32(colour), lit ? 2.2f : 1.2f);
    }

    static void DrawBox(ImDrawListPtr draw, Vector2 p0, Vector2 p1, Vector4 fill, bool lit, bool selected, bool match, string name, string costFormula)
    {
        fill.W = lit ? 0.85f : 0.18f;
        var border = selected ? new Vector4(1f, 1f, 1f, 1f) : match ? new Vector4(1f, 0.9f, 0.4f, 1f) : new Vector4(0.1f, 0.1f, 0.12f, lit ? 0.9f : 0.25f);
        draw.AddRectFilled(p0, p1, ImGui.ColorConvertFloat4ToU32(fill), 4f);
        draw.AddRect(p0, p1, ImGui.ColorConvertFloat4ToU32(border), 4f, ImDrawFlags.None, selected ? 2.5f : 1.2f);
        if ((p1.Y - p0.Y) >= 18f)
        {
            uint textCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.06f, 0.06f, 0.08f, lit ? 1f : 0.35f));
            draw.AddText(p0 + new Vector2(6f, 6f), textCol, Truncate(name, 28));
            if ((p1.Y - p0.Y) >= 36f && !string.IsNullOrEmpty(costFormula))
                draw.AddText(p0 + new Vector2(6f, 6f + ImGui.GetTextLineHeight()), textCol, "Cost " + Truncate(costFormula, 22));
        }
    }

    static string FormatRp(long rp)
    {
        if (rp >= 1_000_000)
            return (rp / 1_000_000d).ToString("0.##") + "M";
        if (rp >= 1000)
            return (rp / 1000d).ToString("0.##") + "k";
        return rp.ToString();
    }


    Vector2 WorldToScreen(Vector2 world, Vector2 origin) => origin + (world + _pan) * _zoom;

    Vector2 ScreenToWorld(Vector2 screen, Vector2 origin) => (screen - origin) / _zoom - _pan;

    static Vector4 CategoryColour(string categoryId)
    {
        int hash = 0;
        foreach (char c in categoryId ?? "")
            hash = unchecked(hash * 31 + c);
        int index = Math.Abs(hash) % CategoryPalette.Length;
        return CategoryPalette[index];
    }

    static string Truncate(string text, int max)
        => text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 1)) + "…";

    static string FilterText(byte[] buffer)
    {
        int n = Array.IndexOf(buffer, (byte)0);
        if (n < 0)
            n = buffer.Length;
        return n == 0 ? "" : Encoding.UTF8.GetString(buffer, 0, n);
    }
}

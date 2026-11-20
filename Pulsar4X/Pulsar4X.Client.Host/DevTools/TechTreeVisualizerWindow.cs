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
                _pendingCentreId = _graph.Nodes[0].Id;
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
        _graph = input == null ? null : TechTreeGraph.Build(input);
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
            Vector2 mid = new(centre.X + TechTreeGraph.NodeWidth * 0.5f, centre.Y + TechTreeGraph.NodeHeight * 0.5f);
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

        foreach (var edge in graph.Edges)
        {
            var from = graph.Find(edge.From);
            var to = graph.Find(edge.To);
            if (from == null || to == null)
                continue;
            bool lit = focus == null || (focus.Contains(from.Id) && focus.Contains(to.Id));
            var a = WorldToScreen(new Vector2(from.X + TechTreeGraph.NodeWidth, from.Y + TechTreeGraph.NodeHeight * 0.5f), origin);
            var b = WorldToScreen(new Vector2(to.X, to.Y + TechTreeGraph.NodeHeight * 0.5f), origin);
            float pull = Math.Max(40f * _zoom, Math.Abs(b.X - a.X) * 0.45f);
            var c1 = new Vector2(a.X + pull, a.Y);
            var c2 = new Vector2(b.X - pull, b.Y);
            Vector4 colour = lit
                ? new Vector4(0.75f, 0.78f, 0.85f, 0.9f)
                : new Vector4(0.45f, 0.48f, 0.55f, 0.18f);
            draw.AddBezierCubic(a, c1, c2, b, ImGui.ColorConvertFloat4ToU32(colour), lit ? 2.2f : 1.2f);
        }

        foreach (var node in graph.Nodes)
        {
            bool lit = focus == null || focus.Contains(node.Id);
            bool selected = node.Id == _selectedId;
            bool match = searchHits.Contains(node.Id);
            Vector2 p0 = WorldToScreen(new Vector2(node.X, node.Y), origin);
            Vector2 p1 = WorldToScreen(new Vector2(node.X + TechTreeGraph.NodeWidth, node.Y + TechTreeGraph.NodeHeight), origin);
            var fill = CategoryColour(node.CategoryId);
            fill.W = lit ? 0.85f : 0.18f;
            var border = selected ? new Vector4(1f, 1f, 1f, 1f) : match ? new Vector4(1f, 0.9f, 0.4f, 1f) : new Vector4(0.1f, 0.1f, 0.12f, lit ? 0.9f : 0.25f);
            draw.AddRectFilled(p0, p1, ImGui.ColorConvertFloat4ToU32(fill), 4f);
            draw.AddRect(p0, p1, ImGui.ColorConvertFloat4ToU32(border), 4f, ImDrawFlags.None, selected ? 2.5f : 1.2f);

            if ((p1.Y - p0.Y) >= 18f)
            {
                var text = lit ? new Vector4(0.06f, 0.06f, 0.08f, 1f) : new Vector4(0.06f, 0.06f, 0.08f, 0.35f);
                uint textCol = ImGui.ColorConvertFloat4ToU32(text);
                float pad = 6f * _zoom;
                draw.AddText(p0 + new Vector2(pad, pad), textCol, Truncate(node.Name, 28));
                if ((p1.Y - p0.Y) >= 36f)
                {
                    string cost = string.IsNullOrEmpty(node.CostFormula) ? "" : "Cost " + Truncate(node.CostFormula, 22);
                    draw.AddText(p0 + new Vector2(pad, pad + ImGui.GetTextLineHeight()), textCol, cost);
                }
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
            Vector2 p0 = WorldToScreen(new Vector2(node.X, node.Y), origin);
            Vector2 p1 = WorldToScreen(new Vector2(node.X + TechTreeGraph.NodeWidth, node.Y + TechTreeGraph.NodeHeight), origin);
            if (mouse.X >= p0.X && mouse.X <= p1.X && mouse.Y >= p0.Y && mouse.Y <= p1.Y)
                return node;
        }
        return null;
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

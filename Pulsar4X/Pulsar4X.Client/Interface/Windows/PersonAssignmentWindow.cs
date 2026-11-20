using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client;

/// <summary>
/// Shared person-assignment chooser: title, post, name filter, a scrolling portrait list,
/// and Assign / Unseat. Resizable, with a 70% viewport height cap.
/// </summary>
public sealed class PersonAssignmentWindow : UniquePulsarGuiWindow<PersonAssignmentWindow>
{
    public const string ChildId = "##person-assignment-list";

    string _title = "Assign";
    string _post = "";
    Func<IReadOnlyList<CommanderSnapshot>> _candidates = () => Array.Empty<CommanderSnapshot>();
    Func<CommanderSnapshot, string>? _rowLabel;
    int _currentId = -1;
    int _selectedId = -1;
    byte[] _filterBuffer = new byte[64];
    Action<int>? _onAssign;
    Action? _onUnseat;
    Action? _onClose;
    Action? _footer;
    bool _sessionOpen;

    PersonAssignmentWindow()
    {
    }

    internal static PersonAssignmentWindow GetInstance()
    {
        if (_uiState.TryGetUniqueWindow<PersonAssignmentWindow>(out var window))
            return window;
        return _uiState.AddUniqueWindow(new PersonAssignmentWindow());
    }

    /// <summary>Open or refresh the chooser. Call each frame while the post should stay open, or once when opening.</summary>
    public void Present(
        string title,
        string post,
        Func<IReadOnlyList<CommanderSnapshot>> candidates,
        int currentId,
        Action<int> onAssign,
        Action? onUnseat,
        Action? onClose,
        Func<CommanderSnapshot, string>? rowLabel = null,
        Action? footer = null)
    {
        bool newly = !_sessionOpen;
        _title = title;
        _post = post ?? "";
        _candidates = candidates ?? (() => Array.Empty<CommanderSnapshot>());
        _currentId = currentId;
        _onAssign = onAssign;
        _onUnseat = onUnseat;
        _onClose = onClose;
        _rowLabel = rowLabel;
        _footer = footer;
        if (newly)
        {
            Array.Clear(_filterBuffer, 0, _filterBuffer.Length);
            _selectedId = -1;
            _sessionOpen = true;
            IsActive = true;
        }
    }

    internal override void Display()
    {
        if (!IsActive)
        {
            CloseSession();
            return;
        }

        var viewport = ImGui.GetMainViewport().WorkSize;
        ImGui.SetNextWindowSize(PersonAssignmentUi.DefaultSize, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(PersonAssignmentUi.MinSize, PersonAssignmentUi.MaxSize(viewport));

        using (var w = Window.Scope(_title + "###person-assignment", ref IsActive, _flags))
        {
            if (!w.Visible)
                return;

            if (!string.IsNullOrEmpty(_post))
            {
                ImGui.TextUnformatted("Post");
                ImGui.SameLine();
                ImGui.TextUnformatted(_post);
            }

            ImGui.TextUnformatted("Filter");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##person-assignment-filter", _filterBuffer, (uint)_filterBuffer.Length);

            var people = PersonAssignmentUi.FilterByName(
                PersonAssignmentUi.ExcludeCurrent(_candidates(), _currentId),
                FilterText());

            Vector2 avail = ImGui.GetContentRegionAvail();
            float buttons = ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2;
            float footerReserve = _footer == null ? 0f : 140f;
            float listHeight = Math.Max(120f, avail.Y - buttons - footerReserve);

            if (ImGui.BeginChild(ChildId, new Vector2(0, listHeight), ImGuiChildFlags.Borders))
            {
                if (people.Count == 0)
                    ImGui.TextUnformatted("No eligible people.");

                foreach (var person in people)
                {
                    bool selected = _selectedId == person.Id;
                    ImGui.PushID(person.Id);
                    PortraitRenderer.For(_uiState).DrawPerson(person, PortraitRenderer.SmallSize);
                    ImGui.SameLine();
                    string label = _rowLabel != null ? _rowLabel(person) : person.Name;
                    if (ImGui.Selectable(label + "###row", selected, ImGuiSelectableFlags.AllowOverlap, new Vector2(0, PortraitRenderer.SmallSize)))
                        _selectedId = person.Id;
                    if (ImGui.IsItemHovered() && person.IsAssigned && !string.IsNullOrEmpty(person.AssignmentName))
                        ImGui.SetTooltip("Currently " + person.AssignmentName + ". Choosing them moves them.");
                    ImGui.PopID();
                }
            }
            ImGui.EndChild();

            bool canAssign = _selectedId >= 0;
            if (!canAssign)
                ImGui.BeginDisabled();
            if (ImGui.Button("Assign") && canAssign)
            {
                int assigned = _selectedId;
                _onAssign?.Invoke(assigned);
                CloseAndDeactivate();
            }
            if (!canAssign)
                ImGui.EndDisabled();

            ImGui.SameLine();
            bool canUnseat = _currentId >= 0 && _onUnseat != null;
            if (!canUnseat)
                ImGui.BeginDisabled();
            if (ImGui.Button("Unseat") && canUnseat)
            {
                _onUnseat?.Invoke();
                CloseAndDeactivate();
            }
            if (!canUnseat)
                ImGui.EndDisabled();

            if (_footer != null)
            {
                if (ImGui.BeginChild("##person-assignment-hire", new Vector2(0, 0), ImGuiChildFlags.Borders))
                    _footer.Invoke();
                ImGui.EndChild();
            }
        }

        if (!IsActive)
            CloseSession();
    }

    static string FilterText(byte[] buffer)
    {
        int n = Array.IndexOf(buffer, (byte)0);
        if (n < 0)
            n = buffer.Length;
        return n == 0 ? "" : Encoding.UTF8.GetString(buffer, 0, n);
    }

    string FilterText() => FilterText(_filterBuffer);

    public void Dismiss() => CloseAndDeactivate();

    void CloseAndDeactivate()
    {
        IsActive = false;
        CloseSession();
    }

    void CloseSession()
    {
        if (!_sessionOpen)
            return;
        _sessionOpen = false;
        var close = _onClose;
        _onAssign = null;
        _onUnseat = null;
        _onClose = null;
        _footer = null;
        close?.Invoke();
    }
}

using System;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Orbital;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.Client;

/// <summary>
/// A compact ImGui overlay panel anchored to a maneuver node's screen position.
/// Allows the player to adjust prograde/radial delta-v and commit the burn.
/// Supports both creating new maneuvers and editing existing orders.
/// </summary>
public class ManeuverNodePanel
{
    private GlobalUIState _uiState;
    private readonly int _entityId;
    private readonly string _systemId;
    private ManuverLinesComplete _manuverLines;
    private ManuverNode _node;

    private float _progradeDV;
    private float _radialDV;
    private bool _isActive;
    private bool _isInteracting;

    /// <summary>
    /// When editing an existing order, the id of the queued order being edited (the commit
    /// cancels it and submits a replacement). Null when creating a new maneuver.
    /// </summary>
    private readonly string? _editingOrderId;

    /// <summary>
    /// Screen position where the node marker is drawn. Updated each frame.
    /// </summary>
    public Vector2 ScreenPosition;

    public bool IsActive => _isActive;

    /// <summary>
    /// True when the panel is editing an existing order rather than creating a new one.
    /// </summary>
    public bool IsEditing => _editingOrderId != null;

    public ManeuverNodePanel(GlobalUIState uiState, int entityId, string systemId,
        ManuverLinesComplete manuverLines, ManuverNode node, string? editingOrderId = null)
    {
        _uiState = uiState;
        _entityId = entityId;
        _systemId = systemId;
        _manuverLines = manuverLines;
        _node = node;
        _progradeDV = (float)node.Prograde;
        _radialDV = (float)node.Radial;
        _editingOrderId = editingOrderId;
        _isActive = true;
    }

    public void Display()
    {
        if (!_isActive)
            return;

        var system = _uiState.GameClient?.Galaxy.GetSystem(_systemId);
        var entity = system?.GetEntity(_entityId);
        if (system == null || entity == null)
        {
            ClosePanel();
            return;
        }

        // Update screen position from node world position
        UpdateScreenPosition(entity, system);

        // Position the window near the node, but freeze position while user is dragging a slider
        // to prevent the window from moving out from under the mouse (which breaks the drag).
        if (!_isInteracting)
            ImGui.SetNextWindowPos(new Vector2(ScreenPosition.X + 15, ScreenPosition.Y - 30), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(280, 0)); // auto-height

        var flags = ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoResize
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoSavedSettings;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));

        if (ImGui.Begin("##ManeuverNode", flags))
        {
            // Close button in top-right
            var windowWidth = ImGui.GetWindowWidth();
            ImGui.SameLine(windowWidth - 25);
            if (ImGui.SmallButton("X"))
            {
                ClosePanel();
                ImGui.End();
                ImGui.PopStyleVar(2);
                return;
            }
            ImGui.Separator();

            // Get max DV from ship
            float maxDV = 100f;
            if (entity.GetView<ThrustView>() is { } thrust)
                maxDV = (float)thrust.DeltaVMps;

            float maxProgradeDV = Math.Max(1f, maxDV - Math.Abs(_radialDV));
            float maxRadialDV = Math.Max(1f, maxDV - Math.Abs(_progradeDV));

            // Prograde controls
            bool changes = false;

            if (ImGui.Button("-1##pg"))
            {
                _progradeDV -= 1;
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Decrease prograde by 1 m/s");
            ImGui.SameLine();
            if (ImGui.Button("+1##pg"))
            {
                _progradeDV += 1;
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Increase prograde by 1 m/s");
            ImGui.SameLine();
            if (ImGui.DragFloat("Prograde", ref _progradeDV, 0.5f, -maxProgradeDV, maxProgradeDV, "%.1f m/s"))
            {
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Delta-v along the direction of travel.\nPositive = speed up, Negative = slow down.\nDrag or Ctrl+click to type a value.");

            // Radial controls
            if (ImGui.Button("-1##rd"))
            {
                _radialDV -= 1;
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Decrease radial by 1 m/s");
            ImGui.SameLine();
            if (ImGui.Button("+1##rd"))
            {
                _radialDV += 1;
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Increase radial by 1 m/s");
            ImGui.SameLine();
            if (ImGui.DragFloat("Radial", ref _radialDV, 0.5f, -maxRadialDV, maxRadialDV, "%.1f m/s"))
            {
                changes = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Delta-v perpendicular to the direction of travel.\nPositive = away from parent, Negative = toward parent.\nDrag or Ctrl+click to type a value.");

            if (changes)
            {
                // Reset and re-apply to get correct absolute values
                _node.SetNode(_progradeDV, _radialDV, 0, _node.NodeTime);

                // Ensure maneuver lines are in the render list
                if (_uiState.SelectedSysMapRender != null && !_uiState.SelectedSysMapRender.SelectedEntityExtras.Contains(_manuverLines))
                    _uiState.SelectedSysMapRender.SelectedEntityExtras.Add(_manuverLines);
            }

            ImGui.Separator();

            // Info display
            double dvCost = Math.Sqrt(_progradeDV * _progradeDV + _radialDV * _radialDV);
            ImGui.Text("Dv cost: " + Stringify.Velocity(dvCost));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Total delta-v magnitude of this maneuver");
            ImGui.Text("Burn: " + Stringify.Quantity(_node.BurnTimeTotal, "0.#") + " s");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Estimated burn duration at full thrust");
            ImGui.Text("Time: " + _node.NodeTime.ToString("yyyy-MM-dd HH:mm:ss"));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("When the burn will be centered.\nDrag the node marker on the orbit to change.");

            // Encounter predictions
            if (_node.Encounters != null && _node.Encounters.Length > 0)
            {
                ImGui.Separator();
                ImGui.Text("Encounters:");
                for (int i = 0; i < _node.Encounters.Length; i++)
                {
                    var enc = _node.Encounters[i];
                    string distText = FormatEncounterDistance(enc.ClosestApproach_m);
                    if (enc.EntersSOI)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f));
                        ImGui.Text(">> " + enc.BodyName + "  " + distText);
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.Text("   " + enc.BodyName + "  " + distText);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Closest approach: " + distText +
                            "\nTime: " + enc.EncounterTime.ToString("yyyy-MM-dd HH:mm:ss") +
                            (enc.EntersSOI ? "\nEnters sphere of influence" : "\nNear miss"));
                }
            }

            // Trajectory segment summary
            if (_node.Segments != null && _node.Segments.Length > 0)
            {
                ImGui.Separator();
                ImGui.Text("Trajectory:");
                for (int i = 0; i < _node.Segments.Length; i++)
                {
                    var seg = _node.Segments[i];
                    TimeSpan duration = seg.EndTime - seg.StartTime;
                    string durText = FormatDuration(duration);

                    if (seg.EntersSOI)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f));
                        ImGui.Text("  > " + seg.ParentName + " orbit -> SOI (" + durText + ")");
                        ImGui.PopStyleColor();
                    }
                    else if (seg.ExitsSOI)
                    {
                        string peText = FormatEncounterDistance(seg.Orbit.Periapsis);
                        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 0.7f, 1f, 1f));
                        ImGui.Text("  > " + seg.ParentName + " flyby, Pe: " + peText);
                        ImGui.PopStyleColor();
                    }
                    else if (i > 0 && seg.Orbit.Eccentricity < 1)
                    {
                        string peText = FormatEncounterDistance(seg.Orbit.Periapsis);
                        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 1f, 0.4f, 1f));
                        ImGui.Text("  > " + seg.ParentName + " capture, Pe: " + peText);
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.Text("  > " + seg.ParentName + " orbit (" + durText + ")");
                    }
                }
            }

            ImGui.Separator();

            // Action buttons - different labels for edit mode vs new mode
            if (_editingOrderId != null)
            {
                if (ImGui.Button("Update"))
                {
                    CommitNode(entity);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Update the existing thrust order with new values");
                ImGui.SameLine();
                if (ImGui.Button("Delete Order"))
                {
                    DeleteOrder();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove this thrust order from the ship's queue");
            }
            else
            {
                if (ImGui.Button("Commit"))
                {
                    CommitNode(entity);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Issue the thrust command to the ship");
                ImGui.SameLine();
                if (ImGui.Button("Delete"))
                {
                    ClosePanel();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Discard this maneuver node");
            }

            // Track whether a widget is being actively dragged/edited this frame
            _isInteracting = ImGui.IsAnyItemActive();
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    /// <summary>
    /// Repositions the node to a new time on the orbit, preserving current delta-v settings.
    /// Called when user clicks a different point on the orbit while the panel is open.
    /// </summary>
    public void RepositionNode(DateTime newNodeTime)
    {
        // Re-create the node at the new time, keeping existing prograde/radial
        _manuverLines.EditingNodes = new ManuverNode[1];
        _manuverLines.EditingNodes[0] = new ManuverNode(_uiState, _systemId, _entityId, newNodeTime);
        _node = _manuverLines.EditingNodes[0];

        // Re-apply any existing delta-v
        if (_progradeDV != 0 || _radialDV != 0)
        {
            _node.SetNode(_progradeDV, _radialDV, 0, newNodeTime);
        }
    }

    private void UpdateScreenPosition(EntitySnapshot entity, IClientSystem system)
    {
        // Convert node world position (relative to SOI parent) to absolute, then to screen
        var soiParent = entity.GetSoiParent(system);
        if (soiParent == null)
            return;

        var absPos = soiParent.AbsolutePositionM(system, _uiState.PrimarySystemDateTime);
        var nodeWorldPos = new Orbital.Vector2(
            absPos.X + _node.NodePosition.X,
            absPos.Y + _node.NodePosition.Y);

        var screenPos = _uiState.Camera.ViewCoordinateV2_m(nodeWorldPos);
        ScreenPosition = new Vector2((float)screenPos.X, (float)screenPos.Y);
    }

    private void CommitNode(EntitySnapshot entity)
    {
        if (entity.GetView<ThrustView>() == null)
            return;

        // If editing, remove the old order first
        if (_editingOrderId != null)
        {
            _uiState.GameClient?.SubmitCommandAsync(new CancelOrderCommand(_entityId, _editingOrderId));
        }

        var deltaV = new Vec3(_radialDV, _progradeDV, 0);
        _uiState.GameClient?.SubmitCommandAsync(new Pulsar4X.Api.NewtonThrustCommand(_entityId, _node.NodeTime, deltaV));

        // Add to the maneuver tree
        _node.NodeName = "Thrust";
        _manuverLines.AddSequence("Thrust Manuver");

        ClosePanel();
    }

    /// <summary>
    /// Removes the existing order from the ship's queue and closes the panel.
    /// </summary>
    private void DeleteOrder()
    {
        if (_editingOrderId != null)
        {
            _uiState.GameClient?.SubmitCommandAsync(new CancelOrderCommand(_entityId, _editingOrderId));
        }
        ClosePanel();
    }

    /// <summary>
    /// Closes the panel and clears the editing node.
    /// </summary>
    public void ClosePanel()
    {
        _isActive = false;
        _manuverLines.EditingNodes = new ManuverNode[0];
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return ((int)ts.TotalDays) + "d " + ts.Hours + "h";
        if (ts.TotalHours >= 1)
            return ((int)ts.TotalHours) + "h " + ts.Minutes + "m";
        return ((int)ts.TotalMinutes) + "m";
    }

    private static string FormatEncounterDistance(double meters)
    {
        double au = Pulsar4X.Orbital.Distance.MToAU(meters);
        if (au >= 0.01)
            return au.ToString("F2") + " AU";
        double km = Pulsar4X.Orbital.Distance.MToKm(meters);
        return km.ToString("N0") + " km";
    }
}

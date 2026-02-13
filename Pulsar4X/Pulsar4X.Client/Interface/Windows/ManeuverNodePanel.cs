using System;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Orbital;
using Pulsar4X.Movement;
using Pulsar4X.Orbits;
using Vector2 = System.Numerics.Vector2;

namespace Pulsar4X.Client;

/// <summary>
/// A compact ImGui overlay panel anchored to a maneuver node's screen position.
/// Allows the player to adjust prograde/radial delta-v and commit the burn.
/// </summary>
public class ManeuverNodePanel
{
    private GlobalUIState _uiState;
    private Entity _orderEntity;
    private ManuverLinesComplete _manuverLines;
    private ManuverNode _node;

    private float _progradeDV;
    private float _radialDV;
    private bool _isActive;

    /// <summary>
    /// Screen position where the node marker is drawn. Updated each frame.
    /// </summary>
    public Vector2 ScreenPosition;

    public bool IsActive => _isActive;

    public ManeuverNodePanel(GlobalUIState uiState, Entity orderEntity, ManuverLinesComplete manuverLines, ManuverNode node)
    {
        _uiState = uiState;
        _orderEntity = orderEntity;
        _manuverLines = manuverLines;
        _node = node;
        _progradeDV = (float)node.Prograde;
        _radialDV = (float)node.Radial;
        _isActive = true;
    }

    public void Display()
    {
        if (!_isActive)
            return;

        // Update screen position from node world position
        UpdateScreenPosition();

        // Position the window near the node, offset slightly so it doesn't overlap the marker
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
            if (_orderEntity.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrustDB))
                maxDV = (float)thrustDB.DeltaV;

            float maxProgradeDV = Math.Max(1f, maxDV - Math.Abs(_radialDV));
            float maxRadialDV = Math.Max(1f, maxDV - Math.Abs(_progradeDV));

            // Prograde controls
            bool changes = false;

            if (ImGui.Button("-1##pg"))
            {
                _progradeDV -= 1;
                changes = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("+1##pg"))
            {
                _progradeDV += 1;
                changes = true;
            }
            ImGui.SameLine();
            if (ImGui.DragFloat("Prograde", ref _progradeDV, 0.5f, -maxProgradeDV, maxProgradeDV, "%.1f m/s"))
            {
                changes = true;
            }

            // Radial controls
            if (ImGui.Button("-1##rd"))
            {
                _radialDV -= 1;
                changes = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("+1##rd"))
            {
                _radialDV += 1;
                changes = true;
            }
            ImGui.SameLine();
            if (ImGui.DragFloat("Radial", ref _radialDV, 0.5f, -maxRadialDV, maxRadialDV, "%.1f m/s"))
            {
                changes = true;
            }

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
            ImGui.Text("Burn: " + Stringify.Quantity(_node.BurnTimeTotal, "0.#") + " s");
            ImGui.Text("Time: " + _node.NodeTime.ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.Separator();

            // Action buttons
            if (ImGui.Button("Commit"))
            {
                CommitNode();
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete"))
            {
                ClosePanel();
            }
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
        _manuverLines.EditingNodes[0] = new ManuverNode(_orderEntity, newNodeTime);
        _node = _manuverLines.EditingNodes[0];

        // Re-apply any existing delta-v
        if (_progradeDV != 0 || _radialDV != 0)
        {
            _node.SetNode(_progradeDV, _radialDV, 0, newNodeTime);
        }
    }

    private void UpdateScreenPosition()
    {
        // Convert node world position (relative to SOI parent) to absolute, then to screen
        var soiParentPos = MoveMath.GetSOIParentPositionDB(_orderEntity);
        if (soiParentPos == null)
            return;

        var absPos = soiParentPos.AbsolutePosition;
        var nodeWorldPos = new Orbital.Vector2(
            absPos.X + _node.NodePosition.X,
            absPos.Y + _node.NodePosition.Y);

        var screenPos = _uiState.Camera.ViewCoordinateV2_m(nodeWorldPos);
        ScreenPosition = new Vector2((float)screenPos.X, (float)screenPos.Y);
    }

    private void CommitNode()
    {
        if (!_orderEntity.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrustDB))
            return;
        if (!_orderEntity.TryGetDataBlob<MassVolumeDB>(out var massDB))
            return;

        double totalMass = massDB.MassTotal;
        double exhaustVelocity = thrustDB.ExhaustVelocity;
        double burnRate = thrustDB.FuelBurnRate;
        double dvMag = Math.Sqrt(_progradeDV * _progradeDV + _radialDV * _radialDV);

        double fuelBurned = OrbitMath.TsiolkovskyFuelUse(totalMass, exhaustVelocity, dvMag);
        double secondsBurn = fuelBurned / burnRate;

        var deltaV = new Orbital.Vector3(_radialDV, _progradeDV, 0);
        var order = NewtonThrustCommand.CreateCommand(
            _orderEntity.FactionOwnerID,
            _orderEntity,
            _node.NodeTime,
            deltaV,
            secondsBurn);

        _uiState.Game?.OrderHandler.HandleOrder(order);

        // Add to the maneuver tree
        _node.NodeName = "Thrust";
        _manuverLines.AddSequence("Thrust Manuver");

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
}

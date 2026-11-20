using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>Colony Production Recycle section: ships at a shipyard, warehouse items at a factory.</summary>
public static class RecycleDisplay
{
    static readonly Dictionary<string, int> Quantities = new(StringComparer.Ordinal);
    static string? _pendingRefusal;
    static volatile bool _pendingReady;

    public static void TakePendingRefusal(ref string? refusal)
    {
        if (!_pendingReady)
            return;
        refusal = _pendingRefusal;
        _pendingRefusal = null;
        _pendingReady = false;
    }

    public static bool HasRows(IndustryView industry)
        => industry.RecycleShips.Count > 0 || industry.RecycleItems.Count > 0;

    public static void Draw(int colonyId, IndustryView industry, GlobalUIState uiState, ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        if (!ImGui.CollapsingHeader("Recycle", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawRefusal(refusal);
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
        ImGui.TextUnformatted("Ships go to a shipyard, warehouse items to a factory. Returns are "
            + RecycleUi.ReturnFractionText() + " of the build resources.");
        ImGui.PopStyleColor();

        if (industry.RecycleShips.Count > 0)
        {
            ImGui.TextUnformatted("Ships");
            foreach (var ship in industry.RecycleShips)
                DrawShipRow(colonyId, ship, uiState, ref refusal);
        }

        if (industry.RecycleItems.Count > 0)
        {
            ImGui.TextUnformatted("Warehouse items");
            foreach (var item in industry.RecycleItems)
                DrawItemRow(colonyId, item, uiState, ref refusal);
        }

        DrawRefusal(refusal);
    }

    static void DrawShipRow(int colonyId, RecycleCandidateView ship, GlobalUIState uiState, ref string? refusal)
    {
        ImGui.PushID("recycle-ship-" + ship.ShipEntityId);
        ImGui.TextUnformatted(ship.Name);
        ImGui.SameLine();
        ImGui.TextUnformatted(RecycleUi.ReturnsText(ship.Returns));
        ImGui.SameLine();
        ImGui.TextUnformatted(RecycleUi.PointCostText(ship.PointCost));
        ImGui.SameLine();
        RecycleButton(colonyId, ship, 1, uiState, ref refusal);
        ImGui.PopID();
    }

    static void DrawItemRow(int colonyId, RecycleCandidateView item, GlobalUIState uiState, ref string? refusal)
    {
        ImGui.PushID("recycle-item-" + item.DesignId);
        if (!Quantities.TryGetValue(item.DesignId, out int quantity))
            quantity = 1;
        quantity = RecycleUi.ClampQuantity(quantity, item.Available);

        ImGui.TextUnformatted(item.Name);
        ImGui.SameLine();
        ImGui.TextUnformatted(item.Available.ToString("N0") + " available");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80f);
        if (ImGui.InputInt("##qty", ref quantity))
            quantity = RecycleUi.ClampQuantity(quantity, item.Available);
        Quantities[item.DesignId] = quantity;
        ImGui.SameLine();
        ImGui.TextUnformatted(RecycleUi.ReturnsText(RecycleUi.ReturnsForQuantity(item.Returns, quantity)));
        ImGui.SameLine();
        ImGui.TextUnformatted(RecycleUi.PointCostText(item.PointCost, quantity));
        ImGui.SameLine();
        RecycleButton(colonyId, item, quantity, uiState, ref refusal);
        ImGui.PopID();
    }

    static void RecycleButton(
        int colonyId,
        RecycleCandidateView candidate,
        int quantity,
        GlobalUIState uiState,
        ref string? refusal)
    {
        bool can = RecycleUi.CanQueue(candidate);
        if (!can)
            ImGui.BeginDisabled();
        if (ImGui.Button("Recycle") && can)
        {
            var command = RecycleUi.Command(colonyId, candidate, quantity);
            if (command != null)
            {
                Submit(uiState, command);
                TakePendingRefusal(ref refusal);
            }
        }
        if (!can)
            ImGui.EndDisabled();
        if (!can && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(candidate.Refusal);
    }

    static void DrawRefusal(string? refusal)
    {
        if (string.IsNullOrEmpty(refusal))
            return;
        ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
        ImGui.TextUnformatted(refusal);
        ImGui.PopStyleColor();
    }

    static void Submit(GlobalUIState uiState, GameCommand command)
    {
        var task = uiState.GameClient?.SubmitCommandAsync(command);
        if (task == null)
            return;
        if (task.IsCompleted)
        {
            _pendingRefusal = RecycleUi.RefusalMessage(task.Result);
            _pendingReady = true;
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = RecycleUi.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}

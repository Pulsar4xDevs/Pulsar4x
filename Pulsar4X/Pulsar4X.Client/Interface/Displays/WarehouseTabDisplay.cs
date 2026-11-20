using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Warehouse tab: per-item Min and Max stock sliders, using the same SliderInt + InputInt
/// pattern as the component-designer integer (infrastructure) range slider.
/// </summary>
public static class WarehouseTabDisplay
{
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

    public static void Display(
        int colonyId,
        CargoStorageView? storage,
        IReadOnlyList<MarketGoodChoice>? addable,
        GlobalUIState uiState,
        ref string? refusal)
    {
        TakePendingRefusal(ref refusal);

        if (storage == null)
        {
            ImGui.TextUnformatted("This colony has no warehouse.");
            DrawRefusal(refusal);
            return;
        }

        bool showMin = WarehouseStockUi.ShowMin(storage.IndustrySeatFilled);
        bool showMax = WarehouseStockUi.ShowMax(storage.MarketSeatFilled);
        if (WarehouseStockUi.MinHint(storage.IndustrySeatFilled) is { } minHint)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextUnformatted(minHint);
            ImGui.PopStyleColor();
        }
        if (WarehouseStockUi.MaxHint(storage.MarketSeatFilled) is { } maxHint)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextUnformatted(maxHint);
            ImGui.PopStyleColor();
        }

        var rows = WarehouseStockUi.Rows(storage, addable);
        if (rows.Count == 0)
        {
            ImGui.TextUnformatted("No warehouse items yet. Cargo and stock rules will appear here.");
            DrawRefusal(refusal);
            return;
        }

        if (ImGui.BeginChild("##warehouse-stock-rows"))
        {
            foreach (var row in rows)
                DrawRow(colonyId, row, showMin, showMax, uiState, ref refusal);
        }
        ImGui.EndChild();
        DrawRefusal(refusal);
    }

    static void DrawRow(
        int colonyId,
        WarehouseStockRow row,
        bool showMin,
        bool showMax,
        GlobalUIState uiState,
        ref string? refusal)
    {
        ImGui.PushID(row.CargoId);
        ImGui.TextUnformatted(row.Name);
        ImGui.SameLine();
        ImGui.TextUnformatted(WarehouseStockUi.StockQueuedText(row.Stock, row.Queued));

        int ceiling = WarehouseStockUi.SliderCeiling(row.Stock, row.Queued, row.Min, row.Max);
        long? min = row.Min;
        long? max = row.Max;
        bool changed = false;

        if (showMin)
        {
            int value = (int)Math.Clamp(min ?? 0, 0, ceiling);
            if (DrawBoundSlider("Min", "min", ref value, ceiling))
            {
                (min, max) = WarehouseStockUi.WithMin(min, max, value);
                changed = true;
            }
            if (min != null && ClearButton("Clear Min###clear-min"))
            {
                (min, max) = WarehouseStockUi.WithMin(min, max, null);
                changed = true;
            }
        }

        if (showMax)
        {
            int value = (int)Math.Clamp(max ?? 0, 0, ceiling);
            if (DrawBoundSlider("Max", "max", ref value, ceiling))
            {
                (min, max) = WarehouseStockUi.WithMax(min, max, value);
                changed = true;
            }
            if (max != null && ClearButton("Clear Max###clear-max"))
            {
                (min, max) = WarehouseStockUi.WithMax(min, max, null);
                changed = true;
            }
        }

        if (row.HasRule)
        {
            if (ClearButton("Clear rule###clear-rule"))
            {
                min = null;
                max = null;
                changed = true;
            }
        }

        if (changed)
        {
            Submit(uiState, WarehouseStockUi.Command(colonyId, row.CargoId, min, max));
            TakePendingRefusal(ref refusal);
        }

        ImGui.PopID();
        ImGui.Separator();
    }

    /// <summary>SliderInt + InputInt, matching the designer integer min/max slider.</summary>
    static bool DrawBoundSlider(string label, string id, ref int value, int ceiling)
    {
        int before = value;
        float width = Math.Max(80f, ImGui.GetContentRegionAvail().X * 0.45f);
        ImGui.SetNextItemWidth(width);
        ImGui.SliderInt("##slider-" + id, ref value, 0, ceiling);
        bool sliderDone = ImGui.IsItemDeactivatedAfterEdit();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90f);
        bool typed = ImGui.InputInt("##input-" + id, ref value);
        value = Math.Max(0, value);
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        return (sliderDone || typed) && value != before;
    }

    static bool ClearButton(string label)
    {
        ImGui.SameLine();
        return ImGui.SmallButton(label);
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
            _pendingRefusal = WarehouseStockUi.RefusalMessage(task.Result);
            _pendingReady = true;
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = WarehouseStockUi.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}

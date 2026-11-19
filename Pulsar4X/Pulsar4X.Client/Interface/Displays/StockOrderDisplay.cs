using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>New-job and job-row chrome for a stock-keeping level. Begin/End is not needed for the inputs themselves.</summary>
public static class StockOrderDisplay
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

    public static void DrawRefusal(ref string? refusal)
    {
        TakePendingRefusal(ref refusal);
        if (string.IsNullOrEmpty(refusal))
            return;
        ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
        ImGui.TextUnformatted(refusal);
        ImGui.PopStyleColor();
    }

    public static void DrawNewJobField(ref string text)
    {
        ImGui.SameLine();
        ImGui.TextUnformatted(StockOrderUi.FieldLabel);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80f);
        ImGui.InputTextWithHint("##stockbelow", "X", ref text, 20);
        if (!StockOrderUi.TryParseLevel(text, out _, out var error) && error != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
            ImGui.TextUnformatted(error);
            ImGui.PopStyleColor();
        }
    }

    public static bool DrawJobEdit(
        int colonyId,
        ProductionLineView line,
        IndustryJobView job,
        Dictionary<string, string> edits,
        GlobalUIState uiState,
        ref string? refusal)
    {
        string canonical = StockOrderUi.FormatLevel(job.StockBelow);
        if (!edits.TryGetValue(job.JobId, out var text))
            text = canonical;

        ImGui.SameLine();
        ImGui.TextUnformatted("below");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(64f);
        bool enter = ImGui.InputTextWithHint("##stock-edit-" + job.JobId, "X", ref text, 20, ImGuiInputTextFlags.EnterReturnsTrue);
        bool active = ImGui.IsItemActive();
        ImGui.SameLine();
        bool set = ImGui.Button("Set##stock-set-" + job.JobId);
        if (enter || set)
        {
            if (!StockOrderUi.TryParseLevel(text, out var level, out var error))
            {
                refusal = error;
                edits[job.JobId] = text;
                return false;
            }

            Submit(uiState, StockOrderUi.EditCommand(colonyId, line.Id, job.JobId, job.NumberOrdered, job.Repeat, level), ref refusal);
            edits[job.JobId] = StockOrderUi.FormatLevel(level);
            return true;
        }

        if (!active)
            text = canonical;
        edits[job.JobId] = text;
        return false;
    }

    public static void Submit(GlobalUIState uiState, GameCommand command, ref string? refusal)
    {
        var task = uiState.GameClient?.SubmitCommandAsync(command);
        if (task == null)
            return;
        if (task.IsCompleted)
        {
            refusal = StockOrderUi.RefusalMessage(task.Result);
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = StockOrderUi.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}

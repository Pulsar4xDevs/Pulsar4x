using System;
using System.Collections.Generic;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Persistent Missing Inputs footer for a pinned production job. Sits outside the new-job pane.
/// </summary>
public static class MissingInputsPanel
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

    /// <summary>Height to reserve under the production columns, based on the pinned rows.</summary>
    public static float ReservedHeight(IReadOnlyList<IndustryJobView> pinnedJobs, float availableHeight)
    {
        float line = ImGui.GetTextLineHeightWithSpacing();
        int rows = 0;
        foreach (var job in pinnedJobs)
            rows += Math.Max(1, job.MissingInputs.Count);
        float wanted = line * (rows + 3 + pinnedJobs.Count * 2) + 24f;
        float max = Math.Max(line * 6f, availableHeight * 0.4f);
        return Math.Clamp(wanted, line * 6f, max);
    }

    public static void Draw(
        int colonyId,
        IndustryView industry,
        IReadOnlyList<IndustryJobView> pinnedJobs,
        MissingInputsPin pins,
        GlobalUIState uiState,
        ref string? refusal,
        float height)
    {
        TakePendingRefusal(ref refusal);
        if (ImGui.BeginChild("MissingInputsPanel", new System.Numerics.Vector2(0, height), ImGuiChildFlags.Borders))
        {
            DisplayHelpers.Header("Missing inputs");
            ImGui.TextUnformatted(MissingInputsPin.HeadText(industry.IndustryHeadId, industry.IndustryHeadName));

            foreach (var job in pinnedJobs)
            {
                ImGui.PushID(job.JobId);
                ImGui.TextUnformatted(RefitJobDisplay.Name(job));
                ImGui.SameLine();
                if (ImGui.Button("Unpin"))
                    pins.Unpin(colonyId, job.JobId);

                if (job.MissingInputs.Count == 0)
                    ImGui.TextDisabled("No missing inputs.");
                else if (ImGui.BeginTable("missing-inputs-" + job.JobId, 4, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.None, 0.28f);
                    ImGui.TableSetupColumn("Short / required (in stock)", ImGuiTableColumnFlags.None, 0.36f);
                    ImGui.TableSetupColumn("Line", ImGuiTableColumnFlags.None, 0.18f);
                    ImGui.TableSetupColumn("Queue", ImGuiTableColumnFlags.None, 0.18f);
                    ImGui.TableHeadersRow();

                    foreach (var input in job.MissingInputs)
                    {
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(input.Name);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(MissingInputsPin.StockText(input));
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(MissingInputsPin.LineText(input));
                        ImGui.TableNextColumn();
                        if (MissingInputsPin.ShowQueue(industry.IndustryHeadId, input))
                        {
                            if (ImGui.Button(SafeButtonLabel(MissingInputsPin.QueueButtonText(input), job.JobId, input.ItemId)))
                                Submit(uiState, MissingInputsPin.QueueCommand(colonyId, input), ref refusal);
                        }
                        ImGui.TableNextRow();
                    }
                    ImGui.EndTable();
                }

                ImGui.PopID();
            }

            if (!string.IsNullOrEmpty(refusal))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                ImGui.TextUnformatted(refusal);
                ImGui.PopStyleColor();
            }
        }
        ImGui.EndChild();
    }

    static string SafeButtonLabel(string text, string jobId, string itemId)
        => text.Replace("%", "%%", StringComparison.Ordinal) + "###queue-" + jobId + "-" + itemId;

    static void Submit(GlobalUIState uiState, GameCommand command, ref string? refusal)
    {
        var task = uiState.GameClient?.SubmitCommandAsync(command);
        if (task == null)
            return;
        if (task.IsCompleted)
        {
            refusal = MissingInputsPin.RefusalMessage(task.Result);
            return;
        }
        task.ContinueWith(t =>
        {
            if (!t.IsCompletedSuccessfully)
                return;
            _pendingRefusal = MissingInputsPin.RefusalMessage(t.Result);
            _pendingReady = true;
        });
    }
}

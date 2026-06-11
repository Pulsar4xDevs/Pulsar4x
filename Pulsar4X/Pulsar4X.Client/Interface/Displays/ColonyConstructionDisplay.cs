using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Snapshot-based local-construction UI (the API-layer port of <see cref="ConstructionDisplay"/>):
    /// renders an entity's <see cref="ConstructionView"/> and submits construction-queue commands.
    /// </summary>
    public sealed class ColonyConstructionDisplay
    {
        private static ColonyConstructionDisplay? instance = null;

        private int _selectedDesignIndex = -1;

        private ColonyConstructionDisplay() { }

        internal static ColonyConstructionDisplay GetInstance()
        {
            return instance ??= new ColonyConstructionDisplay();
        }

        public void Display(int entityId, ConstructionView? construction, GlobalUIState uiState)
        {
            if (construction == null)
            {
                Vector2 topSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginChild("NoConstructionAvailable", new Vector2(topSize.X, 56f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                    ImGui.Text("No local construction capability available at this colony.\n\nBuild installations with Local Construction capability to enable this feature.");
                    ImGui.PopStyleColor();
                }
                ImGui.EndChild();
                return;
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            // Header with construction points
            DisplayConstructionHeader(construction);

            // Two column layout: Queue on left, Available designs on right
            if (ImGui.BeginChild("ConstructionQueue", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y - 50), ImGuiChildFlags.Borders))
            {
                DisplayQueue(entityId, construction, uiState);
            }
            ImGui.EndChild();

            ImGui.SameLine();

            if (ImGui.BeginChild("AvailableDesigns", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y - 50), ImGuiChildFlags.Borders))
            {
                DisplayAvailableDesigns(entityId, construction, uiState);
            }
            ImGui.EndChild();
        }

        private void DisplayConstructionHeader(ConstructionView construction)
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ConstructionHeader", new Vector2(topSize.X, 35f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Indent(8);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

                ImGui.Text("Construction Points Per Day:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(construction.PointsPerDay.ToString(Styles.IntFormat));
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Amount of construction progress applied to the build queue each day.");
                }

                ImGui.SameLine(ImGui.GetWindowWidth() - 200);

                int queueCount = construction.BuildQueue.Count;
                ImGui.Text("Items in Queue:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, queueCount > 0 ? Styles.GoodColor : Styles.DescriptiveColor);
                ImGui.Text(queueCount.ToString());
                ImGui.PopStyleColor();

                ImGui.Unindent(8);
            }
            ImGui.EndChild();
        }

        private void DisplayQueue(int entityId, ConstructionView construction, GlobalUIState uiState)
        {
            DisplayHelpers.Header("Build Queue");

            if (construction.BuildQueue.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextWrapped("\nQueue is empty. Select a design from the right panel to add it to the queue.");
                ImGui.PopStyleColor();
                return;
            }

            int queueCount = construction.BuildQueue.Count;
            long pointsAccumulatedBefore = 0;

            for (int i = 0; i < queueCount; i++)
            {
                var job = construction.BuildQueue[i];
                ImGui.PushID(i);

                // Layout: arrows | content | remove button
                if (ImGui.BeginTable($"QueueItem{i}", 3, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Arrows", ImGuiTableColumnFlags.WidthFixed, 26f);
                    ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 24f);

                    ImGui.TableNextRow();

                    // Arrow buttons column
                    ImGui.TableNextColumn();

                    bool canMoveUp = i > 0;
                    bool canMoveDown = i < queueCount - 1;

                    if (!canMoveUp) ImGui.BeginDisabled();
                    if (ImGui.ArrowButton("up", ImGuiDir.Up))
                    {
                        uiState.GameClient?.SubmitCommandAsync(
                            new MoveConstructionJobCommand(entityId, i, MoveUp: true));
                    }
                    if (!canMoveUp) ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(canMoveUp ? "Move up in queue" : "Already at top");

                    if (!canMoveDown) ImGui.BeginDisabled();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down))
                    {
                        uiState.GameClient?.SubmitCommandAsync(
                            new MoveConstructionJobCommand(entityId, i, MoveUp: false));
                    }
                    if (!canMoveDown) ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(canMoveDown ? "Move down in queue" : "Already at bottom");

                    // Content column
                    ImGui.TableNextColumn();

                    // Item name
                    ImGui.Text(job.Name);

                    // Component type
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(job.ComponentType);
                    ImGui.PopStyleColor();

                    // Progress bar
                    float progress = (float)job.Progress;
                    string progressText = $"{job.PointsAccumulated:N0} / {job.IndustryPointCosts:N0}";

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.4f, 0.8f, 0.4f, 0.8f));
                    ImGui.ProgressBar(progress, new Vector2(-1, 0), progressText);
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Progress: {progress * 100:F1}%\n{job.PointsAccumulated:N0} of {job.IndustryPointCosts:N0} construction points");
                    }

                    // Estimated completion time
                    if (construction.PointsPerDay > 0)
                    {
                        long pointsRemaining = job.IndustryPointCosts - job.PointsAccumulated;
                        double totalDays = (double)(pointsAccumulatedBefore + pointsRemaining) / construction.PointsPerDay;

                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.Text("Est:");
                        ImGui.PopStyleColor();
                        ImGui.SameLine();

                        if (totalDays < 1)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.GoodColor);
                            ImGui.Text("< 1 day");
                            ImGui.PopStyleColor();
                        }
                        else if (totalDays < 365)
                        {
                            ImGui.Text($"{totalDays:F1} days");
                        }
                        else
                        {
                            double years = totalDays / 365.0;
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                            ImGui.Text($"{years:F1} years");
                            ImGui.PopStyleColor();
                        }
                    }

                    // Remove button column
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Button, Styles.BadColor);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.35f, 0.35f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.15f, 0.15f, 1f));
                    if (ImGui.Button("X"))
                    {
                        uiState.GameClient?.SubmitCommandAsync(
                            new RemoveConstructionJobCommand(entityId, i));
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Remove from queue");

                    ImGui.EndTable();
                }

                // Add separator between items
                ImGui.Spacing();
                if (i < queueCount - 1)
                {
                    ImGui.Separator();
                    ImGui.Spacing();
                }

                ImGui.PopID();

                pointsAccumulatedBefore += job.IndustryPointCosts - job.PointsAccumulated;
            }
        }

        private void DisplayAvailableDesigns(int entityId, ConstructionView construction, GlobalUIState uiState)
        {
            DisplayHelpers.Header("Available Component Designs");

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextWrapped("Select a design and click 'Add to Queue' to begin construction.");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            var designs = construction.AvailableDesigns;

            // Calculate space for button at bottom (button height + spacing + separator)
            float buttonAreaHeight = 50f;
            Vector2 tableSize = new Vector2(-1, ImGui.GetContentRegionAvail().Y - buttonAreaHeight);

            // Scrollable table area
            if (ImGui.BeginChild("DesignsTableArea", tableSize, ImGuiChildFlags.None))
            {
                if (ImGui.BeginTable("DesignsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Design Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 120);
                    ImGui.TableSetupColumn("Cost", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < designs.Count; i++)
                    {
                        var design = designs[i];
                        ImGui.TableNextRow();

                        // Design name
                        ImGui.TableNextColumn();
                        bool isSelected = i == _selectedDesignIndex;

                        if (ImGui.Selectable($"{design.Name}###{i}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            _selectedDesignIndex = i;
                        }

                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(design.ComponentType))
                        {
                            ImGui.SetTooltip($"{design.Name}\nType: {design.ComponentType}\nCost: {design.IndustryPointCosts:N0} points");
                        }

                        // Type
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.Text(design.ComponentType ?? "Unknown");
                        ImGui.PopStyleColor();

                        // Cost
                        ImGui.TableNextColumn();
                        ImGui.Text($"{design.IndustryPointCosts:N0}");
                    }

                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();

            // Add to queue button - always visible at bottom
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool hasSelection = _selectedDesignIndex >= 0 && _selectedDesignIndex < designs.Count;

            if (!hasSelection)
            {
                ImGui.BeginDisabled();
            }

            var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, 30);
            ImGui.PushStyleColor(ImGuiCol.Button, Styles.GoodColor * new Vector4(0.6f, 0.6f, 0.6f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.GoodColor * new Vector4(0.8f, 0.8f, 0.8f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.GoodColor * new Vector4(0.5f, 0.5f, 0.5f, 1f));

            if (ImGui.Button("Add to Queue", buttonSize) && hasSelection)
            {
                uiState.GameClient?.SubmitCommandAsync(
                    new AddToConstructionQueueCommand(entityId, designs[_selectedDesignIndex].DesignId));
            }

            ImGui.PopStyleColor(3);

            if (!hasSelection)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("Select a design from the table above");
            }
            else if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Add {designs[_selectedDesignIndex].Name} to the build queue");
            }
        }
    }
}

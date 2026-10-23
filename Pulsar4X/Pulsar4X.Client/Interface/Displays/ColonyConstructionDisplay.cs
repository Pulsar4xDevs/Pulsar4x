using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Snapshot-based local-construction UI (the API-layer port of the old engine-backed ConstructionDisplay):
    /// renders an entity's <see cref="ConstructionView"/> and submits construction-queue commands.
    /// A ship uses the same section and picks one colony in transfer range. The cargo menu reads that pick.
    /// </summary>
    public sealed class ColonyConstructionDisplay
    {
        private int _selectedCrateId = -1;
        private string _selectedSiteId = "";
        static readonly Dictionary<int, int> PickedColonies = new();

        internal ColonyConstructionDisplay() { }

        /// <summary>The colony the ship window last picked. Defaults to the first colony in range.</summary>
        public static int PickedColony(int shipId, IReadOnlyList<int> colonyIds)
        {
            if (colonyIds.Count == 0)
                return 0;
            if (PickedColonies.TryGetValue(shipId, out int picked))
            {
                for (int i = 0; i < colonyIds.Count; i++)
                {
                    if (colonyIds[i] == picked)
                        return picked;
                }
            }
            PickedColonies[shipId] = colonyIds[0];
            return colonyIds[0];
        }

        public static void PickColony(int shipId, int colonyId)
        {
            PickedColonies[shipId] = colonyId;
        }

        public void Display(int entityId, ConstructionView? construction, GlobalUIState uiState)
        {
            if (construction == null)
            {
                Vector2 topSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginChild("NoConstructionAvailable", new Vector2(topSize.X, 72f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                    ImGui.Text("This colony needs a Construction Office.\n\nMount Construction Services to install components here.");
                    ImGui.PopStyleColor();
                }
                ImGui.EndChild();
                return;
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            if (construction.IsShip)
                DisplayColonyPicker(entityId, construction);

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

        private static void DisplayColonyPicker(int entityId, ConstructionView construction)
        {
            var ids = new List<int>(construction.ColoniesInRange.Count);
            foreach (var colony in construction.ColoniesInRange)
                ids.Add(colony.EntityId);

            int picked = PickedColony(entityId, ids);
            string label = "No colony in transfer range";
            foreach (var colony in construction.ColoniesInRange)
            {
                if (colony.EntityId == picked)
                    label = colony.Name;
            }

            if (ids.Count == 0)
                ImGui.BeginDisabled();
            if (ImGui.BeginCombo("Colony##construction-target", label))
            {
                foreach (var colony in construction.ColoniesInRange)
                {
                    if (ImGui.Selectable(colony.Name + "##colony" + colony.EntityId, colony.EntityId == picked))
                        PickColony(entityId, colony.EntityId);
                }
                ImGui.EndCombo();
            }
            if (ids.Count == 0)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip("No colony in transfer range.");
            }
        }

        private void DisplayConstructionHeader(ConstructionView construction)
        {
            Vector2 topSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ConstructionHeader", new Vector2(topSize.X, 35f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Indent(8);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

                ImGui.Text("Installation points per day:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(construction.PointsPerDay.ToString(Styles.IntFormat));
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Installation points applied to the build queue each day.");
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
                ImGui.TextWrapped("\nQueue is empty. Select a crate or a site-only design from the right panel.");
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

                    // Item name and whether this row mounts a crate or assembles a design.
                    ImGui.Text(job.Name);
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(job.TargetName.Length > 0 ? job.Kind + " on " + job.TargetName : job.Kind);
                    ImGui.PopStyleColor();

                    // Progress bar. The denominator is the cost copied onto the job.
                    float progress = (float)job.Progress;
                    string progressText = $"{job.PointsAccumulated:N0} / {job.PointCost:N0}";

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.4f, 0.8f, 0.4f, 0.8f));
                    ImGui.ProgressBar(progress, new Vector2(-1, 0), progressText);
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Progress: {progress * 100:F1}%\n{job.PointsAccumulated:N0} of {job.PointCost:N0} installation points");
                    }

                    // Estimated completion time
                    if (construction.PointsPerDay > 0)
                    {
                        long pointsRemaining = job.PointCost - job.PointsAccumulated;
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

                pointsAccumulatedBefore += job.PointCost - job.PointsAccumulated;
            }
        }

        private void DisplayAvailableDesigns(int entityId, ConstructionView construction, GlobalUIState uiState)
        {
            DisplayHelpers.Header("Installations");

            string where = construction.IsShip ? "hold" : "warehouse";
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextWrapped($"Mount a {where} crate, or assemble a design that cannot be cargo.");
            ImGui.PopStyleColor();
            ImGui.Spacing();

            ConstructionCrateView? selectedCrate = null;
            foreach (var crate in construction.Crates)
            {
                if (crate.ComponentId == _selectedCrateId)
                    selectedCrate = crate;
            }
            ConstructionSiteView? selectedSite = null;
            if (selectedCrate == null)
            {
                foreach (var site in construction.SiteDesigns)
                {
                    if (site.DesignId == _selectedSiteId)
                        selectedSite = site;
                }
            }

            float buttonAreaHeight = 50f;
            Vector2 tableSize = new Vector2(-1, ImGui.GetContentRegionAvail().Y - buttonAreaHeight);

            if (ImGui.BeginChild("DesignsTableArea", tableSize, ImGuiChildFlags.None))
            {
                DisplayHelpers.Header(construction.IsShip ? "Hold" : "Warehouse Crates");
                if (construction.Crates.Count == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextWrapped(construction.IsShip
                        ? "No installation crates in the hold."
                        : "No installation crates in the warehouse.");
                    ImGui.PopStyleColor();
                }
                else if (ImGui.BeginTable("CratesTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, 140)))
                {
                    ImGui.TableSetupColumn("Crate", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Mount cost", ImGuiTableColumnFlags.WidthFixed, 90);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var crate in construction.Crates)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        bool isSelected = selectedCrate != null && selectedCrate.ComponentId == crate.ComponentId;
                        if (ImGui.Selectable($"{crate.Name}###crate{crate.ComponentId}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            _selectedCrateId = crate.ComponentId;
                            _selectedSiteId = "";
                        }
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip($"{crate.Name}\nMount cost: {crate.MountCost:N0} installation points");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{crate.MountCost:N0}");
                    }
                    ImGui.EndTable();
                }

                ImGui.Spacing();
                DisplayHelpers.Header("Site-only Designs");
                if (construction.SiteDesigns.Count == 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextWrapped("No site-only designs.");
                    ImGui.PopStyleColor();
                }
                else if (ImGui.BeginTable("SitesTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("Design", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Cost", ImGuiTableColumnFlags.WidthFixed, 70);
                    ImGui.TableSetupColumn("Missing", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var site in construction.SiteDesigns)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        bool isSelected = selectedSite != null && selectedSite.DesignId == site.DesignId;
                        if (ImGui.Selectable($"{site.Name}###site{site.DesignId}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                        {
                            _selectedSiteId = site.DesignId;
                            _selectedCrateId = -1;
                        }

                        string missing = site.MissingResources.Count == 0
                            ? ""
                            : string.Join(", ", site.MissingResources);
                        if (ImGui.IsItemHovered())
                        {
                            string tip = $"{site.Name}\nAssemble cost: {site.IndustryPointCosts:N0} installation points";
                            if (missing.Length > 0)
                                tip += $"\nMissing: {missing}";
                            ImGui.SetTooltip(tip);
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text($"{site.IndustryPointCosts:N0}");

                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Text, missing.Length > 0 ? Styles.BadColor : Styles.DescriptiveColor);
                        ImGui.Text(missing.Length > 0 ? missing : "—");
                        ImGui.PopStyleColor();
                    }
                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var colonyIds = new List<int>(construction.ColoniesInRange.Count);
            foreach (var colony in construction.ColoniesInRange)
                colonyIds.Add(colony.EntityId);
            int colonyId = construction.IsShip ? PickedColony(entityId, colonyIds) : 0;
            bool hasColony = !construction.IsShip || colonyId != 0;
            bool hasSelection = (selectedCrate != null || selectedSite != null) && hasColony;
            if (!hasSelection)
                ImGui.BeginDisabled();

            var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, 30);
            ImGui.PushStyleColor(ImGuiCol.Button, Styles.GoodColor * new Vector4(0.6f, 0.6f, 0.6f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Styles.GoodColor * new Vector4(0.8f, 0.8f, 0.8f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Styles.GoodColor * new Vector4(0.5f, 0.5f, 0.5f, 1f));

            if (ImGui.Button("Add to Queue", buttonSize) && hasSelection)
            {
                if (selectedCrate != null)
                {
                    uiState.GameClient?.SubmitCommandAsync(construction.IsShip
                        ? new AddToConstructionQueueCommand(entityId, selectedCrate.DesignId, selectedCrate.ComponentId, colonyId)
                        : new AddToConstructionQueueCommand(entityId, selectedCrate.DesignId, selectedCrate.ComponentId));
                }
                else if (selectedSite != null)
                {
                    uiState.GameClient?.SubmitCommandAsync(construction.IsShip
                        ? new AddToConstructionQueueCommand(entityId, selectedSite.DesignId, null, colonyId)
                        : new AddToConstructionQueueCommand(entityId, selectedSite.DesignId));
                }
            }

            ImGui.PopStyleColor(3);

            if (!hasSelection)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip(hasColony
                        ? "Select a crate or a site-only design"
                        : "No colony in transfer range.");
            }
            else if (ImGui.IsItemHovered())
            {
                string name = selectedCrate != null ? selectedCrate.Name : selectedSite!.Name;
                ImGui.SetTooltip($"Add {name} to the build queue");
            }
        }
    }
}

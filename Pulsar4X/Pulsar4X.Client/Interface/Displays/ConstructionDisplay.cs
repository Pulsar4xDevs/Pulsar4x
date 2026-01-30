using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Components;
using Pulsar4X.Factions;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Client
{
    public sealed class ConstructionDisplay
    {
        private static ConstructionDisplay? instance = null;
        private static readonly object padlock = new object();

        public EntityState? EntityState { get; private set; }
        private Entity? Entity;
        private LocalConstructionDB? _constructionDB;
        private FactionInfoDB? _factionInfoDB;
        private int _selectedDesignIndex = -1;
        private int _selectedQueueIndex = -1;
        private List<ComponentDesign> _availableDesigns = new();

        private ConstructionDisplay() { }

        internal static ConstructionDisplay GetInstance(EntityState state)
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new ConstructionDisplay();
                }
                instance.SetEntity(state);
            }

            return instance;
        }

        public void SetEntity(EntityState state)
        {
            if (state == EntityState) return;

            EntityState = state;
            Entity = state?.Entity;
            _selectedDesignIndex = -1;
            _selectedQueueIndex = -1;
        }

        private void Update()
        {
            if (_factionInfoDB == null) return;

            // Get all component designs that can be constructed locally
            // Filter to only show planet installation designs
            _availableDesigns = _factionInfoDB.ComponentDesigns.Values
                .Where(d => d.IsValid && (d.ComponentMountType & ComponentMountType.PlanetInstallation) != 0)
                .OrderBy(d => d.Name)
                .ToList();
        }

        public void Display(GlobalUIState state)
        {
            Entity = EntityState?.Entity;
            if (Entity == null) return;

            if (!Entity.TryGetDataBlob<LocalConstructionDB>(out _constructionDB) ||
                !state.Faction.TryGetDataBlob<FactionInfoDB>(out _factionInfoDB))
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

            Update();

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();

            // Header with construction points
            DisplayConstructionHeader();

            // Two column layout: Queue on left, Available designs on right
            if (ImGui.BeginChild("ConstructionQueue", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y - 50), ImGuiChildFlags.Borders))
            {
                DisplayQueue(state);
            }
            ImGui.EndChild();

            ImGui.SameLine();

            if (ImGui.BeginChild("AvailableDesigns", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y - 50), ImGuiChildFlags.Borders))
            {
                DisplayAvailableDesigns(state);
            }
            ImGui.EndChild();
        }

        private void DisplayConstructionHeader()
        {
            if (_constructionDB == null) return;

            Vector2 topSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ConstructionHeader", new Vector2(topSize.X, 35f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.Indent(8);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6);

                ImGui.Text("Construction Points Per Day:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(_constructionDB.PointsPerDay.ToString(Styles.IntFormat));
                ImGui.PopStyleColor();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Amount of construction progress applied to the build queue each day.");
                }

                ImGui.SameLine(ImGui.GetWindowWidth() - 200);

                int queueCount = _constructionDB.BuildQueue.Count;
                ImGui.Text("Items in Queue:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, queueCount > 0 ? Styles.GoodColor : Styles.DescriptiveColor);
                ImGui.Text(queueCount.ToString());
                ImGui.PopStyleColor();

                ImGui.Unindent(8);
            }
            ImGui.EndChild();
        }

        private void DisplayQueue(GlobalUIState state)
        {
            if (_constructionDB == null) return;

            DisplayHelpers.Header("Build Queue");

            if (_constructionDB.BuildQueue.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextWrapped("\nQueue is empty. Select a design from the right panel to add it to the queue.");
                ImGui.PopStyleColor();
                return;
            }

            var queueList = _constructionDB.BuildQueue.ToList();
            int queueCount = queueList.Count;
            int i = 0;
            long pointsAccumulatedBefore = 0;

            foreach (var job in queueList)
            {
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
                        var order = MoveUpInConstructionQueueOrder.Create(Entity, job);
                        state.Game.OrderHandler.HandleOrder(order);
                    }
                    if (!canMoveUp) ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(canMoveUp ? "Move up in queue" : "Already at top");

                    if (!canMoveDown) ImGui.BeginDisabled();
                    if (ImGui.ArrowButton("down", ImGuiDir.Down))
                    {
                        var order = MoveDownInConstructionQueueOrder.Create(Entity, job);
                        state.Game.OrderHandler.HandleOrder(order);
                    }
                    if (!canMoveDown) ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(canMoveDown ? "Move down in queue" : "Already at bottom");

                    // Content column
                    ImGui.TableNextColumn();

                    // Item name
                    ImGui.Text(job.Design.Name);

                    // Component type
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.Text(job.Design.ComponentType);
                    ImGui.PopStyleColor();

                    // Progress bar
                    float progress = (float)job.CurrentItemProgress;
                    string progressText = $"{job.PointsAccumulated:N0} / {job.Design.IndustryPointCosts:N0}";

                    ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.4f, 0.8f, 0.4f, 0.8f));
                    ImGui.ProgressBar(progress, new Vector2(-1, 0), progressText);
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Progress: {progress * 100:F1}%\n{job.PointsAccumulated:N0} of {job.Design.IndustryPointCosts:N0} construction points");
                    }

                    // Estimated completion time
                    if (_constructionDB.PointsPerDay > 0)
                    {
                        long pointsRemaining = job.Design.IndustryPointCosts - job.PointsAccumulated;
                        double totalDays = (double)(pointsAccumulatedBefore + pointsRemaining) / _constructionDB.PointsPerDay;

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
                        var order = RemoveFromConstructionQueueOrder.Create(Entity, job);
                        state.Game.OrderHandler.HandleOrder(order);
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

                pointsAccumulatedBefore += job.Design.IndustryPointCosts - job.PointsAccumulated;
                i++;
            }
        }

        private void DisplayAvailableDesigns(GlobalUIState state)
        {
            if (_factionInfoDB == null || Entity == null) return;

            DisplayHelpers.Header("Available Component Designs");

            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextWrapped("Select a design and click 'Add to Queue' to begin construction.");
            ImGui.PopStyleColor();
            ImGui.Spacing();

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

                    for (int i = 0; i < _availableDesigns.Count; i++)
                    {
                        var design = _availableDesigns[i];
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

            bool hasSelection = _selectedDesignIndex >= 0 && _selectedDesignIndex < _availableDesigns.Count;

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
                var design = _availableDesigns[_selectedDesignIndex];
                var order = AddToConstructionQueueOrder.Create(Entity, design);
                state.Game.OrderHandler.HandleOrder(order);
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
                ImGui.SetTooltip($"Add {_availableDesigns[_selectedDesignIndex].Name} to the build queue");
            }
        }
    }
}

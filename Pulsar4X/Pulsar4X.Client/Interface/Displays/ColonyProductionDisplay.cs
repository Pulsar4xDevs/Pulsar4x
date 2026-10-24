using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// Snapshot-based production-lines UI (the API-layer port of the old engine-backed IndustryDisplay):
    /// renders an entity's <see cref="IndustryView"/> and submits industry commands.
    /// </summary>
    public sealed class ColonyProductionDisplay
    {
        private string? _selectedProdLine;
        private int _newJobDesignIndex = 0;
        private int _newJobBatchCount = 1;
        private bool _newJobRepeat = false;
        private bool _newJobAutoInstall = true;
        private bool _rightPaneIsRefit;
        private string? _refitLineId;
        private int _refitShipIndex;
        private int _refitShipId;
        private int _refitTargetIndex = -1;
        private string? _refitAppliedKey;
        private readonly HashSet<int> _refitRemove = new();
        private readonly HashSet<int> _refitMount = new();

        internal ColonyProductionDisplay() { }

        public void Display(int entityId, IndustryView? industry, GlobalUIState uiState)
        {
            if (industry == null)
            {
                Vector2 topSize = ImGui.GetContentRegionAvail();
                if (ImGui.BeginChild("NoProductionAvailable", new Vector2(topSize.X, 56f), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                    ImGui.Text("You need an installation capable of production. Consider importing one.\n\nExamples: Factory, Shipyard or Refinery");
                    ImGui.PopStyleColor();
                }
                ImGui.EndChild();
                return;
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            ProductionLineDisplay(entityId, industry, uiState);
            ImGui.SameLine();

            var selectedLine = industry.ProductionLines.FirstOrDefault(l => l.Id == _selectedProdLine);
            if (selectedLine == null)
                return;

            if (ImGui.BeginChild("JobDescriptionPane", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y), ImGuiChildFlags.Borders))
            {
                if (_rightPaneIsRefit && selectedLine.IsShipyard)
                {
                    DisplayHelpers.Header("New refit for: " + selectedLine.Name);
                    RefitSection(entityId, selectedLine, uiState);
                }
                else
                {
                    DisplayHelpers.Header("Create a new job for: " + selectedLine.Name);
                    NewJobDisplay(entityId, selectedLine, uiState);
                }
            }
            ImGui.EndChild();
        }

        private void ProductionLineDisplay(int entityId, IndustryView industry, GlobalUIState uiState)
        {
            if (industry.ProductionLines.Count == 0)
            {
                ImGui.Text("No capacity for construction at this colony.");
                return;
            }

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("ColonyProductionLines", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Production Lines");

                foreach (var line in industry.ProductionLines)
                {
                    string headerTitle = line.Name;
                    if (line.Jobs.Count == 0)
                        headerTitle += " (Idle)";
                    ImGui.PushID(line.Id);

                    var pop = false;
                    if (_selectedProdLine == line.Id)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Header, Styles.DescriptiveColor);
                        pop = true;
                    }
                    if (ImGui.CollapsingHeader(headerTitle, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        if (ImGui.Button("+ New Job"))
                        {
                            _selectedProdLine = line.Id;
                            _rightPaneIsRefit = false;
                            _newJobDesignIndex = 0;
                            _newJobBatchCount = 1;
                        }

                        if (line.IsShipyard)
                        {
                            ImGui.SameLine();
                            if (ImGui.Button("+ New Refit"))
                            {
                                _selectedProdLine = line.Id;
                                _rightPaneIsRefit = true;
                            }
                        }

                        ImGui.SameLine();

                        ImGui.BeginDisabled();
                        if (ImGui.Button("Upgrade " + line.Name))
                        {
                            // TODO: add upgrade functionality
                        }
                        ImGui.EndDisabled();

                        if (line.Jobs.Count > 0)
                        {
                            ImGui.SameLine();
                            ImGui.Text("Progress per day:");
                            ImGui.SameLine();
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                            ImGui.Text(line.CurrentRatePerDay.ToString());
                            ImGui.PopStyleColor();
                            if (ImGui.IsItemHovered())
                                ImGui.SetTooltip("Assuming all resources needed are available.");

                            JobsTable(entityId, line, uiState);
                        }

                        if (line.IsShipyard)
                        {
                            YardList(line);
                            RefitQueue(line);
                        }
                    }
                    if (pop)
                    {
                        ImGui.PopStyleColor();
                    }
                    ImGui.PopID();
                }
            }
            ImGui.EndChild();
        }

        private void JobsTable(int entityId, ProductionLineView line, GlobalUIState uiState)
        {
            if (ImGui.BeginTable(line.Name, 4, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableSetupColumn("Batch", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableHeadersRow();

                for (int jobIndex = 0; jobIndex < line.Jobs.Count; jobIndex++)
                {
                    var job = line.Jobs[jobIndex];

                    ImGui.TableNextColumn();
                    ImGui.Text(job.Name);

                    ImGui.TableNextColumn();
                    ImGui.Text(job.NumberCompleted + "/" + job.NumberOrdered);

                    if (job.Repeat)
                    {
                        ImGui.SameLine();
                        ImGui.Image(uiState.Img_Repeat().ToTextureRef(), new Vector2(16, 16));
                    }

                    ImGui.TableNextColumn();
                    var color = job.MissingResources ? Styles.BadColor : Styles.GoodColor;

                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    if (job.Status == "Processing")
                        ImGui.Text("Processing (" + job.PercentComplete.ToString("0.#") + "%%)");
                    else
                        ImGui.Text(job.Status);
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0f);
                        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0f);
                        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.1f, 0.1f, 0.1f, 1f));
                        ImGui.BeginTooltip();
                        if (ImGui.BeginTable(job.JobId, 2, ImGuiTableFlags.Borders))
                        {
                            ImGui.TableSetupColumn("Resource Required");
                            ImGui.TableSetupColumn("Quantity Needed");
                            ImGui.TableHeadersRow();
                            ImGui.TableNextColumn();
                            ImGui.Text("Industry Points");
                            ImGui.TableNextColumn();
                            ImGui.Text(job.ProductionPointsLeft.ToString());

                            foreach (var requirement in job.RemainingRequirements)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text(requirement.Name);
                                ImGui.TableNextColumn();
                                ImGui.Text(requirement.Amount.ToString());
                            }
                            ImGui.EndTable();
                        }
                        ImGui.EndTooltip();
                        ImGui.PopStyleColor();
                        ImGui.PopStyleVar(2);
                    }
                    ImGui.TableNextColumn();
                    ActionButtons(entityId, line, job.JobId, jobIndex, uiState);
                    ImGui.TableNextRow();
                }
                ImGui.EndTable();
            }
        }

        private void ActionButtons(int entityId, ProductionLineView line, string jobId, int jobIndex, GlobalUIState uiState)
        {
            var invisButtonSize = new Vector2(15, 15);
            ImGui.PushID(jobId);
            if (jobIndex > 0)
            {
                if (ImGui.SmallButton("^"))
                {
                    uiState.GameClient?.SubmitCommandAsync(
                        new ChangeIndustryJobPriorityCommand(entityId, line.Id, jobId, -1));
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Move up in the produciton queue.");
            }
            else
            {
                ImGui.InvisibleButton("invis1", invisButtonSize);
            }
            ImGui.SameLine();

            if (jobIndex < line.Jobs.Count - 1)
            {
                if (ImGui.SmallButton("v"))
                {
                    uiState.GameClient?.SubmitCommandAsync(
                        new ChangeIndustryJobPriorityCommand(entityId, line.Id, jobId, 1));
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Move down in the produciton queue.");
            }
            else
            {
                ImGui.InvisibleButton("invis2", invisButtonSize);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("x"))
            {
                uiState.GameClient?.SubmitCommandAsync(
                    new CancelIndustryJobCommand(entityId, line.Id, jobId));
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Cancel the job.");
            ImGui.PopID();
        }

        private void NewJobDisplay(int entityId, ProductionLineView line, GlobalUIState uiState)
        {
            if (line.Constructibles.Count == 0)
            {
                ImGui.Text("This production line can't build anything the faction knows how to make.");
                return;
            }

            if (_newJobDesignIndex >= line.Constructibles.Count)
                _newJobDesignIndex = 0;

            ImGui.NewLine();
            ImGui.Text("Select a design:");
            var selectedDesign = line.Constructibles[_newJobDesignIndex];
            if (ImGui.BeginCombo("###newjobselection", selectedDesign.Name))
            {
                for (int i = 0; i < line.Constructibles.Count; i++)
                {
                    var item = line.Constructibles[i];
                    if (item.OverSlip)
                        ImGui.BeginDisabled();
                    if (ImGui.Selectable(item.Name + "##" + item.DesignId, i == _newJobDesignIndex))
                        _newJobDesignIndex = i;
                    if (item.OverSlip && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && item.SlipReason.Length > 0)
                        ImGui.SetTooltip(item.SlipReason);
                    if (item.OverSlip)
                        ImGui.EndDisabled();
                }
                ImGui.EndCombo();
            }
            selectedDesign = line.Constructibles[_newJobDesignIndex];

            ImGui.NewLine();
            ImGui.Text("Enter the quantity:");
            if (ImGui.InputInt("##batchcount", ref _newJobBatchCount))
            {
                if (_newJobBatchCount < 1)
                    _newJobBatchCount = 1;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("The production line will move to the next job in the queue\nafter finishing the number of items requested.");

            CostsDisplay(selectedDesign);

            ImGui.Columns(1);
            ImGui.NewLine();
            ImGui.Checkbox("##repeat", ref _newJobRepeat);
            ImGui.SameLine();
            ImGui.Text("Repeat this job?");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("A repeat job will run until cancelled.");

            bool canQueue = selectedDesign.CanAutoInstall;
            bool queueConstruction = canQueue && _newJobAutoInstall;
            if (!canQueue)
                ImGui.BeginDisabled();
            if (ImGui.Checkbox("##queueconstruction", ref queueConstruction))
                _newJobAutoInstall = queueConstruction;
            bool queueHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
            ImGui.SameLine();
            ImGui.Text("Queue construction on completion?");
            queueHovered |= ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
            if (!canQueue)
                ImGui.EndDisabled();
            if (queueHovered && selectedDesign.AutoInstallReason.Length > 0)
                ImGui.SetTooltip(selectedDesign.AutoInstallReason);

            ImGui.NewLine();

            if (selectedDesign.OverSlip)
                ImGui.BeginDisabled();
            if (ImGui.Button("Queue the job to " + line.Name))
            {
                uiState.GameClient?.SubmitCommandAsync(new QueueIndustryJobCommand(
                    entityId, line.Id, selectedDesign.DesignId, _newJobBatchCount,
                    _newJobRepeat, selectedDesign.CanAutoInstall && _newJobAutoInstall));
            }
            if (selectedDesign.OverSlip && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && selectedDesign.SlipReason.Length > 0)
                ImGui.SetTooltip(selectedDesign.SlipReason);
            if (selectedDesign.OverSlip)
                ImGui.EndDisabled();
        }

        private void RefitSection(int entityId, ProductionLineView line, GlobalUIState uiState)
        {
            if (_refitLineId != line.Id)
            {
                _refitLineId = line.Id;
                _refitShipIndex = 0;
                _refitShipId = 0;
                _refitTargetIndex = -1;
                _refitAppliedKey = null;
                _refitRemove.Clear();
                _refitMount.Clear();
            }

            if (line.RefitPointsPerDay <= 0)
            {
                ImGui.Text("This yard has no refit rate.");
                return;
            }

            if (line.RefitShips.Count == 0)
            {
                ImGui.Text("Ships in range");
                ImGui.BulletText("None");
                return;
            }

            if (_refitShipIndex >= line.RefitShips.Count)
                _refitShipIndex = 0;
            var selected = line.RefitShips[_refitShipIndex];
            if (_refitShipId != selected.EntityId)
            {
                _refitShipId = selected.EntityId;
                _refitTargetIndex = -1;
                _refitAppliedKey = null;
                _refitRemove.Clear();
                _refitMount.Clear();
            }
            if (_refitTargetIndex >= line.RefitTargets.Count)
            {
                _refitTargetIndex = -1;
                _refitAppliedKey = null;
            }

            ImGui.Text("Ship");
            if (ImGui.BeginCombo("###refit-ship", selected.Name + "  " + selected.Mass))
            {
                for (int i = 0; i < line.RefitShips.Count; i++)
                {
                    var ship = line.RefitShips[i];
                    if (ship.OverSlip)
                        ImGui.BeginDisabled();
                    if (ImGui.Selectable(ship.Name + "  " + ship.Mass + "##refit" + ship.EntityId, i == _refitShipIndex))
                        _refitShipIndex = i;
                    if (ship.OverSlip && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ship.SlipReason.Length > 0)
                        ImGui.SetTooltip(ship.SlipReason);
                    if (ship.OverSlip)
                        ImGui.EndDisabled();
                }
                ImGui.EndCombo();
            }
            selected = line.RefitShips[_refitShipIndex];

            string designLabel = _refitTargetIndex >= 0
                ? line.RefitTargets[_refitTargetIndex].Name
                : "None";
            ImGui.Text("Refit to");
            if (ImGui.BeginCombo("###refit-design", designLabel))
            {
                if (ImGui.Selectable("None##refit-design-none", _refitTargetIndex < 0))
                {
                    _refitTargetIndex = -1;
                    _refitAppliedKey = null;
                    _refitRemove.Clear();
                    _refitMount.Clear();
                }
                for (int i = 0; i < line.RefitTargets.Count; i++)
                {
                    var design = line.RefitTargets[i];
                    if (ImGui.Selectable(design.Name + "##refit-design" + design.DesignId, i == _refitTargetIndex))
                        _refitTargetIndex = i;
                }
                ImGui.EndCombo();
            }

            RefitTargetView? target = _refitTargetIndex >= 0 ? line.RefitTargets[_refitTargetIndex] : null;
            RefitDesignDiffResult? diff = target == null
                ? null
                : RefitDesignDiff.Compute(target, selected.Parts, line.RefitCargo);
            if (target != null && diff != null)
            {
                string key = selected.EntityId + "|" + target.DesignId + "|"
                    + string.Join(",", selected.Parts.Select(part => part.ComponentId)) + "|"
                    + string.Join(",", line.RefitCargo.Select(part => part.ComponentId));
                if (_refitAppliedKey != key)
                {
                    _refitRemove.Clear();
                    _refitMount.Clear();
                    foreach (var id in diff.RemoveInstanceIds)
                        _refitRemove.Add(id);
                    foreach (var id in diff.MountInstanceIds)
                        _refitMount.Add(id);
                    _refitAppliedKey = key;
                }
            }

            ImGui.Text("On the ship");
            if (selected.Parts.Count == 0)
                ImGui.BulletText("None");
            foreach (var part in selected.Parts)
            {
                bool on = _refitRemove.Contains(part.ComponentId);
                if (ImGui.Checkbox(part.Name + "  " + part.Mass + "##rem" + part.ComponentId, ref on))
                {
                    if (on)
                        _refitRemove.Add(part.ComponentId);
                    else
                        _refitRemove.Remove(part.ComponentId);
                }
            }

            ImGui.Text("In the warehouse");
            if (line.RefitCargo.Count == 0)
                ImGui.BulletText("None");
            foreach (var part in line.RefitCargo)
            {
                bool on = _refitMount.Contains(part.ComponentId);
                if (ImGui.Checkbox(part.Name + "  " + part.Mass + "##mnt" + part.ComponentId, ref on))
                {
                    if (on)
                        _refitMount.Add(part.ComponentId);
                    else
                        _refitMount.Remove(part.ComponentId);
                }
            }

            long cost = 0;
            var removeIds = new List<int>();
            var mountIds = new List<int>();
            foreach (var part in selected.Parts)
            {
                if (!_refitRemove.Contains(part.ComponentId))
                    continue;
                cost += part.Cost;
                removeIds.Add(part.ComponentId);
            }
            foreach (var part in line.RefitCargo)
            {
                if (!_refitMount.Contains(part.ComponentId))
                    continue;
                cost += part.Cost;
                mountIds.Add(part.ComponentId);
            }

            if (diff != null && diff.Missing.Count > 0)
            {
                ImGui.Text("To build");
                var buildable = new List<RefitMissingPartView>();
                foreach (var missing in diff.Missing)
                {
                    bool canBuild = line.Constructibles.Any(item => item.DesignId == missing.DesignId);
                    if (canBuild)
                    {
                        buildable.Add(missing);
                        ImGui.BulletText(missing.Name + "  x" + missing.Count);
                    }
                    else
                        ImGui.BulletText(missing.Name + "  x" + missing.Count + "  this line cannot build it");
                }
                if (buildable.Count == 0)
                    ImGui.BeginDisabled();
                if (ImGui.Button("Queue missing parts"))
                {
                    foreach (var missing in buildable)
                    {
                        uiState.GameClient?.SubmitCommandAsync(new QueueIndustryJobCommand(
                            entityId, line.Id, missing.DesignId, missing.Count, false, false));
                    }
                }
                if (buildable.Count == 0)
                    ImGui.EndDisabled();
            }

            ImGui.Text("Cost " + cost);
            bool blocked = cost <= 0 || selected.OverSlip;
            if (blocked)
                ImGui.BeginDisabled();
            if (ImGui.Button("Queue refit"))
            {
                uiState.GameClient?.SubmitCommandAsync(new QueueShipRefitCommand(
                    entityId, line.Id, selected.EntityId, removeIds, mountIds, target?.DesignId));
                _refitRemove.Clear();
                _refitMount.Clear();
            }
            if (blocked)
                ImGui.EndDisabled();
            if (blocked && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                string reason = "";
                if (selected.OverSlip)
                {
                    reason = selected.SlipReason.Length > 0
                        ? selected.SlipReason
                        : "That ship is over this slip.";
                    reason += " Ship " + selected.Mass + " kg, slip " + line.SlipSize.ToString("0") + " kg.";
                }
                if (cost <= 0)
                    reason = reason.Length == 0 ? "Nothing to refit." : reason + "\nNothing to refit.";
                ImGui.SetTooltip(reason);
            }
        }

        private static void RefitQueue(ProductionLineView line)
        {
            ImGui.Text("Refit " + line.RefitPointsPerDay.ToString("0") + " / day");
            if (line.RefitJobs.Count == 0)
            {
                ImGui.BulletText("None");
                return;
            }

            foreach (var job in line.RefitJobs)
                ImGui.BulletText(job.ShipName + "  " + job.PointsAccumulated + " / " + job.PointCost);
        }

        private static void YardList(ProductionLineView line)
        {
            if (!double.IsPositiveInfinity(line.SlipSize))
                ImGui.Text("Slip " + line.SlipSize.ToString("0"));

            ImGui.Text("In yard");
            if (line.Hulls.Count == 0)
            {
                ImGui.BulletText("None");
                return;
            }

            foreach (var hull in line.Hulls)
            {
                string row = hull.DesignName + "  " + hull.Name + "  " + hull.Mass;
                if (hull.Reason.Length > 0)
                    row += "  " + hull.Reason;
                ImGui.BulletText(row);
            }
        }

        private void CostsDisplay(ConstructibleItemView design)
        {
            int quantity = _newJobBatchCount;

            ImGui.NewLine();
            ImGui.Text("Inputs Needed:");
            if (ImGui.BeginTable("JobCostsTables", 4, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.4f);
                ImGui.TableSetupColumn("Cost Per Quantity", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableSetupColumn("Total Cost", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableSetupColumn("Available", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("");
                ImGui.SameLine();
                ImGui.Text("Industry Points");
                ImGui.TableNextColumn();
                ImGui.Text(design.IndustryPointsPerUnit.ToString());
                ImGui.TableNextColumn();
                ImGui.Text((design.IndustryPointsPerUnit * quantity).ToString());
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Total Cost = Cost Per Quantity * Quantity Ordered");
                ImGui.TableNextColumn();
                ImGui.Text("-");
                ImGui.TableNextRow();

                foreach (var cost in design.Costs)
                {
                    var totalCost = quantity * cost.PerUnit;

                    ImGui.TableNextColumn();
                    ImGui.Text("");
                    ImGui.SameLine();
                    ImGui.Text(cost.Name);
                    ImGui.TableNextColumn();
                    ImGui.Text(cost.PerUnit.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(totalCost.ToString());
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Total Cost = Cost Per Output * Quantity Ordered\n" + totalCost + " = " + cost.PerUnit + " * " + quantity);
                    ImGui.TableNextColumn();

                    bool short_ = cost.Available < totalCost;
                    if (short_)
                        ImGui.PushStyleColor(ImGuiCol.Text, cost.CanProduce ? Styles.BadColor : Styles.TerribleColor);

                    ImGui.Text(Stringify.Quantity(cost.Available));

                    if (short_)
                    {
                        if (ImGui.IsItemHovered())
                        {
                            if (cost.CanProduce)
                                ImGui.SetTooltip("Not enough " + cost.Name + " available on this colony.\nImport or produce some!");
                            else
                                ImGui.SetTooltip("Not enough " + cost.Name + " available on this colony.\nAnd we can't build this item!");
                        }

                        ImGui.PopStyleColor();
                    }
                    ImGui.TableNextRow();
                }

                ImGui.EndTable();
            }

            ImGui.NewLine();
            ImGui.Text("Outputs:");

            if (ImGui.BeginTable("JobOutputsTables", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.4f);
                ImGui.TableSetupColumn("Amount Per Quantity", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.None, 0.3f);
                ImGui.TableHeadersRow();

                ImGui.TableNextColumn();
                ImGui.Text("");
                ImGui.SameLine();
                ImGui.Text(design.Name);
                ImGui.TableNextColumn();
                ImGui.Text(design.OutputAmount.ToString());
                ImGui.TableNextColumn();
                ImGui.Text((design.OutputAmount * quantity).ToString());

                ImGui.EndTable();
            }
        }
    }
}

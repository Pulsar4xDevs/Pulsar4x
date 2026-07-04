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
                DisplayHelpers.Header("Create a new job for: " + selectedLine.Name);
                NewJobDisplay(entityId, selectedLine, uiState);
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
                            _newJobDesignIndex = 0;
                            _newJobBatchCount = 1;
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Upgrade " + line.Name))
                        {
                            // TODO: add upgrade functionality
                        }

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

            var constructableNames = line.Constructibles.Select(c => c.Name).ToArray();

            ImGui.NewLine();
            ImGui.Text("Select a design:");
            int curItemIndex = _newJobDesignIndex;
            if (ImGui.Combo("###newjobselection", ref curItemIndex, constructableNames, constructableNames.Length))
            {
                _newJobDesignIndex = curItemIndex;
            }

            var selectedDesign = line.Constructibles[_newJobDesignIndex];

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

            if (selectedDesign.CanAutoInstall)
            {
                ImGui.Checkbox("##autoinstall", ref _newJobAutoInstall);
                ImGui.SameLine();
                ImGui.Text("Auto-install on completion?");
            }

            ImGui.NewLine();

            if (ImGui.Button("Queue the job to " + line.Name))
            {
                uiState.GameClient?.SubmitCommandAsync(new QueueIndustryJobCommand(
                    entityId, line.Id, selectedDesign.DesignId, _newJobBatchCount,
                    _newJobRepeat, selectedDesign.CanAutoInstall && _newJobAutoInstall));
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

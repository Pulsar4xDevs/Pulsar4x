using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class ResearchWindow : UniquePulsarGuiWindow<ResearchWindow>
    {
        private readonly Vector2 invisButtonSize = new (15, 15);

        // The lab is selected by entity id and re-resolved each frame: labs are entities in the
        // active system's snapshot, which is replaced wholesale by server pushes.
        private int? _selectedLabId = null;

        private int selectCategoryFilterIndex = 0;
        private int _showAssignmentModal = -1;

        // Derived lookups, rebuilt only when a new ResearchSnapshot is pushed (reference change).
        private ResearchSnapshot? _research;
        private string[] _categoryNames = Array.Empty<string>();
        private string[] _categoryIds = Array.Empty<string>();
        private Dictionary<string, TechSnapshot> _techsById = new ();
        private List<TechSnapshot> _researchableTechs = new ();

        private ResearchWindow()
        {
        }

        internal static ResearchWindow GetInstance()
        {
            ResearchWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ResearchWindow)))
            {
                thisitem = new ResearchWindow();
            }
            thisitem = (ResearchWindow)_uiState.LoadedWindows[typeof(ResearchWindow)];

            return thisitem;
        }

        private void RefreshDerivedData(ResearchSnapshot research)
        {
            _research = research;

            _categoryNames = new string[research.Categories.Count + 1];
            _categoryIds = new string[research.Categories.Count + 1];
            _categoryNames[0] = "All";
            _categoryIds[0] = "";
            for (int i = 0; i < research.Categories.Count; i++)
            {
                _categoryNames[i + 1] = research.Categories[i].Name;
                _categoryIds[i + 1] = research.Categories[i].Id;
            }

            if (selectCategoryFilterIndex >= _categoryIds.Length)
                selectCategoryFilterIndex = 0;

            _techsById = research.Techs.ToDictionary(t => t.Id);
            RefreshTechs();
        }

        private void RefreshTechs()
        {
            if (_research == null)
                return;

            string categoryId = _categoryIds[selectCategoryFilterIndex];
            _researchableTechs = _research.Techs
                .Where(t => t.IsResearchable && (categoryId.Length == 0 || t.CategoryId.Equals(categoryId)))
                .OrderBy(t => t.Name)
                .ToList();
        }

        internal override void Display()
        {
            if(!IsActive)
                return;

            var galaxy = _uiState.GameClient?.Galaxy;
            var research = galaxy?.Research;

            if (Window.Begin("Research and Development", ref IsActive, _flags))
            {
                if(galaxy != null && research != null)
                {
                    if (!ReferenceEquals(research, _research))
                        RefreshDerivedData(research);

                    // Labs are the faction's researcher entities in the viewed system (the server only
                    // projects ResearcherView for the owning faction).
                    var system = galaxy.GetSystem(_uiState.SelectedStarSystemId);
                    var labs = system == null
                        ? new List<EntitySnapshot>()
                        : system.Entities.Where(e => e.HasView<ResearcherView>()).ToList();

                    // Keep the selection valid, defaulting to the first lab so the
                    // window is immediately usable without an extra click.
                    EntitySnapshot? selectedLab = null;
                    if(labs.Count > 0)
                    {
                        selectedLab = labs.FirstOrDefault(l => l.Id == _selectedLabId) ?? labs[0];
                    }
                    _selectedLabId = selectedLab?.Id;

                    Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                    var labListSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
                    var detailSize = new Vector2(windowContentSize.X - Styles.LeftColumnWidthLg - 8, windowContentSize.Y);

                    if(ImGui.BeginChild("LabList", labListSize, ImGuiChildFlags.Borders))
                    {
                        DisplayHelpers.Header("Research Labs", "Select a lab to manage its research queue");
                        DisplayLabList(labs);
                    }
                    ImGui.EndChild();

                    ImGui.SameLine();
                    if(ImGui.BeginChild("LabDetail", detailSize, ImGuiChildFlags.Borders))
                    {
                        if(selectedLab != null)
                            DisplayLabDetail(selectedLab, research);
                        else
                            ImGui.TextColored(Styles.DescriptiveColor, "No research labs in this system.");
                    }
                    ImGui.EndChild();
                }
            }
            Window.End();
        }

        private void DisplayLabList(List<EntitySnapshot> labs)
        {
            foreach(var lab in labs)
            {
                var researcher = lab.GetView<ResearcherView>();
                if(researcher == null)
                    continue;

                ImGui.PushID(lab.Id);

                if(ImGui.Selectable(researcher.DesignName + $"###{lab.Id}", _selectedLabId == lab.Id))
                {
                    _selectedLabId = lab.Id;
                }
                if(ImGui.IsItemHovered() && researcher.DesignTemplateName.Length > 0)
                {
                    DisplayHelpers.DescriptiveTooltip(
                        researcher.DesignName,
                        researcher.DesignTemplateName,
                        researcher.DesignDescription);
                }

                ImGui.TextColored(Styles.DescriptiveColor, researcher.LocationName.Length > 0 ? researcher.LocationName : "Unknown");

                var currentTechId = researcher.TechQueue.FirstOrDefault();
                if(currentTechId != null && _techsById.TryGetValue(currentTechId, out var tech) && tech.IsResearchable)
                {
                    float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                    ImGui.ProgressBar(frac, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()), tech.Name);
                    DisplayHelpers.TechTooltip(tech);
                }
                else
                {
                    ImGui.TextColored(Styles.OkColor, "Idle");
                }

                ImGui.Separator();
                ImGui.PopID();
            }
        }

        private void DisplayLabDetail(EntitySnapshot lab, ResearchSnapshot research)
        {
            var researcher = lab.GetView<ResearcherView>();
            if(researcher == null)
                return;

            DisplayHelpers.Header(researcher.DesignName);

            // Lab stats in an aligned label/value grid, two pairs per row
            if(ImGui.BeginTable("LabSummary", 4, ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.13f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.37f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.13f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None, 0.37f);

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Location");
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(researcher.LocationName.Length > 0 ? researcher.LocationName : "Unknown");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Scientist");
                ImGui.TableNextColumn();
                var nameDisplay = researcher.ScientistName ?? "Assign Scientist###assignbtn" + lab.Id;
                if(ImGui.Button(nameDisplay))
                {
                    _showAssignmentModal = lab.Id;
                }

                if(_showAssignmentModal > 0 && _showAssignmentModal == lab.Id)
                {
                    ResultModal.GetInstance().DisplayCustomButtons(
                        "Assign Scientist",
                        () => _showAssignmentModal = -1, // onClose
                        (closeModal) => // Custom render with close action
                        {
                            int currentScientistId = researcher.ScientistId ?? -1;
                            int selectedId = DisplayHelpers.PeopleChooser(
                                _uiState,
                                research.Scientists,
                                currentScientistId,
                                $"lab_{lab.Id}",
                                closeModal); // Pass close action as cancel

                            if (selectedId != currentScientistId)
                            {
                                if (selectedId == -1)
                                {
                                    // Unassign the scientist, the player selected "None"
                                    SubmitCommand(new UnassignScientistCommand(lab.Id, currentScientistId));
                                }
                                else if (selectedId > 0)
                                {
                                    // Assign the new scientist
                                    SubmitCommand(new AssignScientistCommand(lab.Id, selectedId));
                                }
                                closeModal();
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Cost per Day");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(researcher.CostPerDay.Value.ToString("C0", CultureInfo.CurrentCulture));
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Cost per Day",
                        "",
                        $"{researcher.CostPerDay.BaseValue.ToString("C0", CultureInfo.CurrentCulture)} Base Value",
                        delegate {
                            foreach(var modifier in researcher.CostPerDay.Modifiers)
                            {
                                ImGui.TextUnformatted($"{modifier.Delta.ToString("C0", CultureInfo.CurrentCulture)} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Progress per Day");
                ImGui.TableNextColumn();
                ImGui.Text(researcher.PointsPerDay.Value.ToString());
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Progress per Day",
                        "",
                        $"{researcher.PointsPerDay.BaseValue} Base Value",
                        delegate {
                            foreach(var modifier in researcher.PointsPerDay.Modifiers)
                            {
                                ImGui.TextUnformatted($"{modifier.Delta} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Funding");
                ImGui.TableNextColumn();
                int funding = researcher.FundingLevel;
                string label = researcher.FundingLevel switch
                {
                    0 => "No Funding",
                    1 => "Standard",
                    2 => "Enhanced",
                    3 => "Robust",
                    4 => "Generous",
                    5 => "Spared No Expense",
                    _ => ""
                };
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if(ImGui.SliderInt($"###{lab.Id}-funding", ref funding, 0, 5, label))
                {
                    SubmitCommand(new SetResearchFundingCommand(lab.Id, funding));
                }
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                ImGui.EndTable();
            }

            // Current research as a prominent full-width bar
            ImGui.Spacing();
            var currentTechId = researcher.TechQueue.FirstOrDefault();
            var barSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + 10);
            if(currentTechId != null && _techsById.TryGetValue(currentTechId, out var currentTech) && currentTech.IsResearchable)
            {
                float frac = (float)currentTech.ResearchProgress / currentTech.ResearchCost;
                ImGui.ProgressBar(frac, barSize, $"{currentTech.Name}  {currentTech.ResearchProgress}/{currentTech.ResearchCost}  ({frac:P0})");
                DisplayHelpers.TechTooltip(currentTech);
            }
            else
            {
                ImGui.ProgressBar(0f, barSize, "Idle — double click a tech to begin research");
            }
            ImGui.Spacing();

            var contentSize = ImGui.GetContentRegionAvail();
            var queueSize = new Vector2(contentSize.X - Styles.LeftColumnWidthLg - 8, contentSize.Y);
            var techsSize = new Vector2(Styles.LeftColumnWidthLg, contentSize.Y);

            if(ImGui.BeginChild("TechQueue", queueSize, ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Tech Queue");
                DisplayQueue(lab.Id, researcher);
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("AvailableTechs", techsSize, ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Available Techs", "Double click a tech to add it to this lab's queue");

                var availableSize = ImGui.GetContentRegionAvail();
                ImGui.SetNextItemWidth(availableSize.X);
                if(ImGui.Combo("###template-filter", ref selectCategoryFilterIndex, _categoryNames, _categoryNames.Length))
                {
                    RefreshTechs();
                }
                DisplayTechs(lab.Id);
            }
            ImGui.EndChild();
        }

        private void DisplayQueue(int labId, ResearcherView researcher)
        {
            if(researcher.TechQueue.Count == 0)
            {
                ImGui.TextColored(Styles.DescriptiveColor, "Queue is empty. Double click a tech on the right to add it.");
                return;
            }

            if(ImGui.BeginTable("TechQueue", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.05f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableHeadersRow();

                int index = 0;
                foreach(var techId in researcher.TechQueue)
                {
                    if(!_techsById.TryGetValue(techId, out var tech))
                        continue;

                    ImGui.TableNextColumn();
                    ImGui.Text($"{index + 1}");
                    ImGui.TableNextColumn();
                    ImGui.Text(tech.Name);
                    DisplayHelpers.TechTooltip(tech);
                    ImGui.TableNextColumn();
                    Buttons(labId, researcher, techId, ref index);
                    index++;
                }

                ImGui.EndTable();
            }
        }

        private void DisplayTechs(int labId)
        {
            if(ImGui.BeginTable("ResearchableTechs", 1, ImGuiTableFlags.BordersInnerV))
            {
                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    var tech = _researchableTechs[i];
                    if (tech.ResearchCost > 0) //could happen if bad json data?
                    {
                        ImGui.TableNextColumn();

                        float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                        var size = ImGui.GetContentRegionAvail();
                        var height = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new Vector2(size.X, height), "");
                        if (ImGui.IsItemHovered())
                        {
                            string metaInfo = "";
                            if(tech.NextLevelUnlocks.Count > 0)
                            {
                                metaInfo += "Unlocks:\n";
                                foreach(var unlockName in tech.NextLevelUnlocks)
                                {
                                    metaInfo += unlockName + "\n";
                                }
                            }
                            if(tech.MaxLevel > 1)
                            {
                                metaInfo += "\nMaximum: " + tech.MaxLevelName;
                            }

                            DisplayHelpers.DescriptiveTooltip(
                                tech.DisplayName,
                                tech.CategoryName,
                                tech.Description,
                                () => ImGui.Text(metaInfo));
                        }
                        ImGui.SetCursorPos(new Vector2(pos.X + 2f, pos.Y));
                        ImGui.Text(tech.DisplayName);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            SubmitCommand(new AddTechToQueueCommand(labId, tech.Id));
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        void Buttons(int labId, ResearcherView researcher, string techID, ref int i)
        {
            ImGui.BeginGroup();

            if (i > 0)
            {
                if(ImGui.SmallButton("^" + "##" + i))
                {
                    SubmitCommand(new MoveTechInQueueCommand(labId, techID, MoveUp: true));
                }
            }
            else
            {
                ImGui.InvisibleButton("invis2", invisButtonSize);
            }
            ImGui.SameLine();

            if (i < researcher.TechQueue.Count - 1)
            {
                if(ImGui.SmallButton("v" + "##" + i))
                {
                    SubmitCommand(new MoveTechInQueueCommand(labId, techID, MoveUp: false));
                }
            }
            else
            {
                ImGui.InvisibleButton("invis3", invisButtonSize);
            }
            ImGui.SameLine();

            if (ImGui.SmallButton("x" + "##" + i))
            {
                SubmitCommand(new RemoveTechFromQueueCommand(labId, techID));
                i--;
            }

            ImGui.EndGroup();
        }

        private void SubmitCommand(GameCommand command) => _uiState.GameClient?.SubmitCommandAsync(command);
    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using System.Linq;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Factions;
using Pulsar4X.Technology;
using System.Globalization;
using Pulsar4X.Extensions;
using Pulsar4X.People;
using Pulsar4X.Components;

namespace Pulsar4X.Client
{
    public class ResearchWindow : PulsarGuiWindow
    {
        private readonly Vector2 invisButtonSize = new (15, 15);
        private FactionDataStore? _factionData;
        private List<Tech> _researchableTechs = new();
        private Dictionary<string, Tech>? _researchableTechsByGuid;
        private EntityState? _selectedLab = null;

        private string[]? techCategoryNames;
        private string[]? techCategoryIds;
        private int selectCategoryFilterIndex = 0;
        int _showAssignmentModal = -1;

        private ResearchWindow()
        {
            OnFactionChange();
            if(_uiState.Game != null)
                _uiState.Game.TimePulse.GameGlobalDateChangedEvent += GameLoopOnGameGlobalDateChangedEvent;
        }

        private void GameLoopOnGameGlobalDateChangedEvent(DateTime newdate)
        {
            if (IsActive)
            {
                RefreshTechs();
            }
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

        private void OnFactionChange()
        {
            if(_uiState.Faction == null || _uiState.Game == null)
                return;

            _factionData = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data;

            selectCategoryFilterIndex = 0;

            var categories = _uiState.Game.TechCategories.Select(g => g.Value).ToList();
            categories.Sort((a, b) => a.Name.CompareTo(b.Name));

            var categoryNamesArray = categories.Select(c => c.Name).ToArray();
            var categoryIdsArray = categories.Select(c => c.UniqueID).ToArray();

            techCategoryNames = new string[_uiState.Game.TechCategories.Count + 1];
            techCategoryNames[0] = "All";
            Array.Copy(categoryNamesArray, 0, techCategoryNames, 1, categoryNamesArray.Length);

            techCategoryIds = new string[techCategoryNames.Length];
            techCategoryIds[0] = "";
            Array.Copy(categoryIdsArray, 0, techCategoryIds, 1, categoryIdsArray.Length);

            RefreshTechs();
        }

        private void RefreshTechs()
        {
            if(_factionData == null || techCategoryIds == null)
                return;

            if(selectCategoryFilterIndex == 0)
            {
                _researchableTechs = _factionData.Techs.Select(kvp => kvp.Value).Where(t => _factionData.IsResearchable(t.UniqueID)).ToList();
                _researchableTechs.Sort((a,b) => a.Name.CompareTo(b.Name));
            }
            else
            {
                var id = techCategoryIds[selectCategoryFilterIndex];
                _researchableTechs = _factionData.Techs.Select(kvp => kvp.Value).Where(t => _factionData.IsResearchable(t.UniqueID) && t.Category.Equals(id)).ToList();
                _researchableTechs.Sort((a,b) => a.Name.CompareTo(b.Name));
            }

            _researchableTechsByGuid = new (_factionData.Techs);
        }

        internal override void Display()
        {
            if(!IsActive
                || techCategoryNames == null)
                return;

            if (Window.Begin("Research and Development", ref IsActive, _flags))
            {
                if(_factionData != null
                    && _researchableTechsByGuid != null
                    && _uiState.Faction != null
                    && _uiState.Game != null)
                {
                    var labs = _uiState.SelectedSystemState.GetFilteredEntities(
                                    DataStructures.EntityFilter.Friendly,
                                    _uiState.Faction.Id,
                                    typeof(ResearcherDB));

                    // Keep the selection valid, defaulting to the first lab so the
                    // window is immediately usable without an extra click.
                    if(labs.Count == 0)
                        _selectedLab = null;
                    else if(_selectedLab == null)
                        _selectedLab = labs[0];
                    else
                        _selectedLab = labs.FirstOrDefault(l => l.Id == _selectedLab.Id) ?? labs[0];

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
                        if(_selectedLab != null)
                            DisplayLabDetail();
                        else
                            ImGui.TextColored(Styles.DescriptiveColor, "No research labs in this system.");
                    }
                    ImGui.EndChild();
                }
            }
            Window.End();
        }

        private void DisplayLabList(List<EntityState> labs)
        {
            if(_factionData == null || _researchableTechsByGuid == null)
                return;

            foreach(var lab in labs)
            {
                if(!lab.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                    continue;

                ImGui.PushID(lab.Id);

                if(ImGui.Selectable(researcherDB.Design.Name + $"###{lab.Id}", _selectedLab?.Id == lab.Id))
                {
                    _selectedLab = lab;
                }
                if(ImGui.IsItemHovered() && researcherDB.Design is ComponentDesign design)
                {
                    DisplayHelpers.DescriptiveTooltip(
                        researcherDB.Design.Name,
                        design.TemplateName,
                        design.Description);
                }

                var location = _uiState.SelectedSystemState.GetEntityById(researcherDB.LocationId);
                ImGui.TextColored(Styles.DescriptiveColor, location?.Name ?? "Unknown");

                researcherDB.TechQueue.TryPeek(out var techId);
                if(techId != null && _factionData.IsResearchable(techId))
                {
                    var tech = _researchableTechsByGuid[techId];
                    float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                    ImGui.ProgressBar(frac, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight()), tech.Name);
                    DisplayHelpers.TechTooltip(tech, _uiState);
                }
                else
                {
                    ImGui.TextColored(Styles.OkColor, "Idle");
                }

                ImGui.Separator();
                ImGui.PopID();
            }
        }

        private void DisplayLabDetail()
        {
            if(_selectedLab == null
                || _factionData == null
                || _researchableTechsByGuid == null
                || _uiState.Faction == null
                || _uiState.Game == null
                || techCategoryNames == null
                || !_selectedLab.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                return;

            DisplayHelpers.Header(researcherDB.Design.Name);

            var location = _uiState.SelectedSystemState.GetEntityById(researcherDB.LocationId);

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
                ImGui.Text(location?.Name ?? "Unknown");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Scientist");
                ImGui.TableNextColumn();
                var nameDisplay = "Assign Scientist###assignbtn" + _selectedLab.Id;
                if(researcherDB.ScientistId >= 0)
                {
                    var commander = _uiState.Game.GlobalManager.GetGlobalEntityById(researcherDB.ScientistId);
                    nameDisplay = commander.GetName(_uiState.Faction.Id);
                }
                if(ImGui.Button(nameDisplay))
                {
                    _showAssignmentModal = _selectedLab.Id;
                }

                if(_showAssignmentModal > 0 && _showAssignmentModal == _selectedLab.Id)
                {
                    ResultModal.GetInstance().DisplayCustomButtons(
                        "Assign Scientist",
                        () => _showAssignmentModal = -1, // onClose
                        (closeModal) => // Custom render with close action
                        {
                            int selectedId = DisplayHelpers.PeopleChooser(
                                _uiState,
                                researcherDB.ScientistId,
                                DataStructures.CommanderTypes.Scientist,
                                $"lab_{_selectedLab.Id}",
                                closeModal); // Pass close action as cancel

                            if (selectedId != researcherDB.ScientistId)
                            {
                                if (selectedId == -1)
                                {
                                    // Unassign the scientist, the player selected "None"
                                    var unassignOrder = UnassignScientistOrder.Create(_selectedLab.Entity, researcherDB.ScientistId);
                                    _uiState.Game.OrderHandler.HandleOrder(unassignOrder);
                                }
                                else if (selectedId > 0)
                                {
                                    // Assign the new scientist
                                    var assignmentOrder = AssignScientistOrder.Create(_selectedLab.Entity, selectedId);
                                    _uiState.Game.OrderHandler.HandleOrder(assignmentOrder);
                                }
                                closeModal();
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Cost per Day");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(researcherDB.CostPerDay.GetValue().ToString("C0", CultureInfo.CurrentCulture));
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Cost per Day",
                        "",
                        $"{researcherDB.CostPerDay.BaseValue.ToString("C0", CultureInfo.CurrentCulture)} Base Value",
                        delegate {
                            foreach(var modifier in researcherDB.CostPerDay.GetModifiers())
                            {
                                ImGui.TextUnformatted($"{(modifier.After - modifier.Before).ToString("C0", CultureInfo.CurrentCulture)} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.TextColored(Styles.DescriptiveColor, "Progress per Day");
                ImGui.TableNextColumn();
                ImGui.Text(researcherDB.PointsPerDay.GetValue().ToString());
                if(ImGui.IsItemHovered())
                {
                    DisplayHelpers.DescriptiveTooltip(
                        "Progress per Day",
                        "",
                        $"{researcherDB.PointsPerDay.BaseValue} Base Value",
                        delegate {
                            foreach(var modifier in researcherDB.PointsPerDay.GetModifiers())
                            {
                                ImGui.TextUnformatted($"{modifier.After - modifier.Before} {modifier.Name}");
                            }
                        });
                }

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(Styles.DescriptiveColor, "Funding");
                ImGui.TableNextColumn();
                int funding = researcherDB.FundingLevel;
                string label = researcherDB.FundingLevel switch
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
                if(ImGui.SliderInt($"###{_selectedLab.Id}-funding", ref funding, 0, 5, label))
                {
                    var changeOrder = FundingChangedOrder.Create(_selectedLab.Entity, (byte)funding);
                    _uiState.Game.OrderHandler.HandleOrder(changeOrder);
                }
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();

                ImGui.EndTable();
            }

            // Current research as a prominent full-width bar
            ImGui.Spacing();
            researcherDB.TechQueue.TryPeek(out var currentTechId);
            var barSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + 10);
            if(currentTechId != null && _factionData.IsResearchable(currentTechId))
            {
                var tech = _researchableTechsByGuid[currentTechId];

                float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                ImGui.ProgressBar(frac, barSize, $"{tech.Name}  {tech.ResearchProgress}/{tech.ResearchCost}  ({frac:P0})");
                DisplayHelpers.TechTooltip(tech, _uiState);
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
                DisplayQueue(researcherDB);
            }
            ImGui.EndChild();

            ImGui.SameLine();
            if(ImGui.BeginChild("AvailableTechs", techsSize, ImGuiChildFlags.Borders))
            {
                DisplayHelpers.Header("Available Techs", "Double click a tech to add it to this lab's queue");

                var availableSize = ImGui.GetContentRegionAvail();
                ImGui.SetNextItemWidth(availableSize.X);
                if(ImGui.Combo("###template-filter", ref selectCategoryFilterIndex, techCategoryNames, techCategoryNames.Length))
                {
                    RefreshTechs();
                }
                DisplayTechs();
            }
            ImGui.EndChild();
        }

        private void DisplayQueue(ResearcherDB researcherDB)
        {
            if(_researchableTechsByGuid == null)
                return;

            if(researcherDB.TechQueue.Count == 0)
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
                foreach(var techId in researcherDB.TechQueue.ToList())
                {
                    Tech tech = _researchableTechsByGuid[techId];
                    ImGui.TableNextColumn();
                    ImGui.Text($"{index + 1}");
                    ImGui.TableNextColumn();
                    ImGui.Text(tech.Name);
                    DisplayHelpers.TechTooltip(tech, _uiState);
                    ImGui.TableNextColumn();
                    Buttons(researcherDB, techId, ref index);
                    index++;
                }

                ImGui.EndTable();
            }
        }

        private void DisplayTechs()
        {
            if(_factionData == null || _uiState.Game == null)
                return;

            if(ImGui.BeginTable("ResearchableTechs", 1, ImGuiTableFlags.BordersInnerV))
            {
                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    if (_researchableTechs[i].ResearchCost > 0) //could happen if bad json data?
                    {
                        ImGui.TableNextColumn();

                        float frac = (float)_researchableTechs[i].ResearchProgress / _researchableTechs[i].ResearchCost;
                        var size = ImGui.GetContentRegionAvail();
                        var height = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new Vector2(size.X, height), "");
                        if (ImGui.IsItemHovered())
                        {
                            string metaInfo = "";
                            if(_researchableTechs[i].Unlocks.ContainsKey(_researchableTechs[i].Level + 1))
                            {
                                metaInfo += "Unlocks:\n";
                                foreach(var item in _researchableTechs[i].Unlocks[_researchableTechs[i].Level + 1])
                                {
                                    metaInfo += _factionData.GetName(item) + "\n";
                                }
                            }
                            if(_researchableTechs[i].MaxLevel > 1)
                            {
                                metaInfo += "\nMaximum: " + _researchableTechs[i].MaxLevelName();
                            }

                            DisplayHelpers.DescriptiveTooltip(
                                _researchableTechs[i].DisplayName(),
                                _uiState.Game.TechCategories[_researchableTechs[i].Category].Name,
                                _researchableTechs[i].Description,
                                () => ImGui.Text(metaInfo));
                        }
                        ImGui.SetCursorPos(new Vector2(pos.X + 2f, pos.Y));
                        ImGui.Text(_researchableTechs[i].DisplayName());

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            if(_selectedLab != null && _selectedLab.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                            {
                                var addOrder = AddTechToQueueOrder.Create(_selectedLab.Entity, _researchableTechs[i].UniqueID);
                                _uiState.Game.OrderHandler.HandleOrder(addOrder);
                            }
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        void Buttons(ResearcherDB researcherDB, string techID, ref int i)
        {
            if(researcherDB.OwningEntity == null || _uiState.Game == null)
                return;

            ImGui.BeginGroup();

            if (i > 0)
            {
                if(ImGui.SmallButton("^" + "##" + i))
                {
                    var moveOrder = MoveUpInQueueOrder.Create(researcherDB.OwningEntity, techID);
                    _uiState.Game.OrderHandler.HandleOrder(moveOrder);
                }
            }
            else
            {
                ImGui.InvisibleButton("invis2", invisButtonSize);
            }
            ImGui.SameLine();

            if (i < researcherDB.TechQueue.Count - 1)
            {
                if(ImGui.SmallButton("v" + "##" + i))
                {
                    var moveOrder = MoveDownInQueueOrder.Create(researcherDB.OwningEntity, techID);
                    _uiState.Game.OrderHandler.HandleOrder(moveOrder);
                }
            }
            else
            {
                ImGui.InvisibleButton("invis3", invisButtonSize);
            }
            ImGui.SameLine();

            if (ImGui.SmallButton("x" + "##" + i))
            {

                var removeOrder = RemoveTechFromQueueOrder.Create(researcherDB.OwningEntity, techID);
                _uiState.Game.OrderHandler.HandleOrder(removeOrder);
                i--;
            }

            ImGui.EndGroup();
        }
    }
}

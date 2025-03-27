using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using System.Linq;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Factions;
using Pulsar4X.Names;
using Pulsar4X.Technology;
using System.Globalization;
using Pulsar4X.Extensions;
using Pulsar4X.People;

namespace Pulsar4X.Client
{
    public class ResearchWindow : PulsarGuiWindow
    {
        private readonly Vector2 invisButtonSize = new (15, 15);
        private FactionDataStore? _factionData;
        private FactionTechDB? _factionTechDB;
        private List<Tech> _researchableTechs = new();
        private Dictionary<string, Tech>? _researchableTechsByGuid;
        private List<(Scientist scientist, Entity atEntity)>? _scienceTeams;
        private int _selectedTeam = -1;
        private EntityState? _selectedLab = null;

        private string[]? techCategoryNames;
        private string[]? techCategoryIds;
        private int selectCategoryFilterIndex = 0;
        bool _showAssignmentModal = false;

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
            _factionTechDB = _uiState.Faction.GetDataBlob<FactionTechDB>();
            _scienceTeams = _factionTechDB.AllScientists;

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
                || techCategoryNames == null
                || _scienceTeams == null)
                return;

            if (Window.Begin("Research and Development", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X - Styles.LeftColumnWidthLg - 8, windowContentSize.Y);
                var secondChildSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);

                if(ImGui.BeginChild("Techs", secondChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayHelpers.Header("Available Techs", "Double click to add to research queue");

                    var availableSize = ImGui.GetContentRegionAvail();
                    ImGui.SetNextItemWidth(availableSize.X);
                    if(ImGui.Combo("###template-filter", ref selectCategoryFilterIndex, techCategoryNames, techCategoryNames.Length))
                    {
                        RefreshTechs();
                    }
                    DisplayTechs();
                }
                ImGui.EndChild();

                ImGui.SameLine();
                if(ImGui.BeginChild("Teams", firstChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayHelpers.Header("Research Labs");
                    //DisplayTeams();
                    DisplayLabs();
                }
                ImGui.EndChild();

                if (_selectedTeam == -1)
                {
                    if (_scienceTeams.Count > 0 && _scienceTeams != null)
                    {
                       _selectedTeam = 0;
                    }
                }
            }
            Window.End();
        }

        private void DisplayLabs()
        {
            if(_factionData == null
                || _researchableTechsByGuid == null
                || _uiState.Faction == null
                || _uiState.Game == null)
                return;

            if(ImGui.BeginTable("Research Labs", 7, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Lab", ImGuiTableColumnFlags.None, 0.15f);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 0.125f);
                ImGui.TableSetupColumn("Scientist", ImGuiTableColumnFlags.None, 0.125f);
                ImGui.TableSetupColumn("Cost/Day", ImGuiTableColumnFlags.None, 0.075f);
                ImGui.TableSetupColumn("Progress/Day", ImGuiTableColumnFlags.None, 0.075f);
                ImGui.TableSetupColumn("Researching", ImGuiTableColumnFlags.None, 0.20f);
                ImGui.TableSetupColumn("Funding", ImGuiTableColumnFlags.None, 0.15f);
                ImGui.TableHeadersRow();

                var labs = _uiState.SelectedSystemState.GetFilteredEntities(
                                DataStructures.EntityFilter.Friendly,
                                _uiState.Faction.Id,
                                typeof(ResearcherDB));

                foreach(var lab in labs)
                {
                    if(!lab.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                        continue;

                    researcherDB.TechQueue.TryPeek(out var techId);

                    ImGui.TableNextColumn();
                    if(ImGui.Selectable(researcherDB.Design.Name + $"###{lab.Id}", _selectedLab?.Id == lab.Id))
                    {
                        _selectedLab = lab;
                    }
                    ImGui.TableNextColumn();
                    var location = _uiState.SelectedSystemState.GetEntityById(researcherDB.LocationId);
                    ImGui.Text(location?.Name);
                    ImGui.TableNextColumn();

                    var nameDisplay = "Assign Scientist";
                    if(researcherDB.ScientistId >= 0)
                    {
                        var commander = _uiState.Game.GlobalManager.GetGlobalEntityById(researcherDB.ScientistId);
                        nameDisplay = commander.GetName(_uiState.Faction.Id);
                    }
                    if(ImGui.Button(nameDisplay))
                    {
                        _showAssignmentModal = true;
                    }

                    if(_showAssignmentModal)
                    {
                        ResultModal.GetInstance().Display(
                            "Assign Scientist",
                            () => // Ok
                            {
                                _showAssignmentModal = false;
                            }, () => // Cancel
                            {
                                _showAssignmentModal = false;
                            }, () => // Custom render
                            {
                                if(!_uiState.Faction.TryGetDatablob<FactionInfoDB>(out var factionInfoDB))
                                    return;

                                foreach(var commanderId in factionInfoDB.Commanders)
                                {
                                    if(commanderId == researcherDB.ScientistId)
                                        continue;

                                    // TODO: this is probably super slow and should be improved
                                    // TODO: remove the call into the game
                                    var commander = _uiState.Game.GlobalManager.GetGlobalEntityById(commanderId);

                                    if(!commander.TryGetDatablob<CommanderDB>(out var commanderDB))
                                        continue;

                                    if(commanderDB.Type != DataStructures.CommanderTypes.Civilian)
                                        continue;

                                    if(ImGui.Button(commander.GetName(_uiState.Faction.Id)))
                                    {
                                        var assignmentOrder = AssignScientistOrder.Create(lab.Entity, commanderId);
                                        _uiState.Game.OrderHandler.HandleOrder(assignmentOrder);
                                    }
                                }

                                if(researcherDB.ScientistId >= 0 && ImGui.Button("None"))
                                {
                                    var unassignOrder = UnassignScientistOrder.Create(lab.Entity, researcherDB.ScientistId);
                                    _uiState.Game.OrderHandler.HandleOrder(unassignOrder);
                                }
                            });
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(researcherDB.CalculatedCostPerDay.ToString("C0", CultureInfo.CurrentCulture));
                    ImGui.TableNextColumn();
                    ImGui.Text(researcherDB.CalculatedResearchPoints.ToString());
                    if(ImGui.IsItemHovered())
                    {
                        DisplayHelpers.DescriptiveTooltip(
                            "Progress per Day",
                            "",
                            $"Base: {researcherDB.BaseResearchPoints}\nBonus: {researcherDB.CalculatedResearchPoints - researcherDB.BaseResearchPoints}");
                    }
                    ImGui.TableNextColumn();
                    if(techId != null && _factionData.IsResearchable(techId))
                    {
                        var tech = _researchableTechsByGuid[techId];

                        float frac = (float)tech.ResearchProgress / tech.ResearchCost;
                        var size = ImGui.GetTextLineHeight();
                        var barWidth = ImGui.GetContentRegionAvail().X;
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new Vector2(barWidth, size + 4), $"{tech.Name} {tech.ResearchProgress}/{tech.ResearchCost}");
                        //ImGui.SetCursorPos(pos);
                        //ImGui.Text();

                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltip(
                                tech.Name,
                                _uiState.Game.TechCategories[tech.Category].Name,
                                $"{tech.Description}\n\nProgress: {tech.ResearchProgress}/{tech.ResearchCost}");
                        }
                    }
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
                    var width = ImGui.GetContentRegionAvail().X;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.SliderInt($"###{lab.Id}-funding", ref funding, 0, 5, label))
                    {
                        var changeOrder = FundingChangedOrder.Create(lab.Entity, (byte)funding);
                        _uiState.Game.OrderHandler.HandleOrder(changeOrder);
                    }
                }

                ImGui.EndTable();
            }

            if(_selectedLab == null)
                return;

            ImGui.NewLine();
            DisplayHelpers.Header("Tech Queue for Selected Lab");

            if(ImGui.BeginTable("TechQueue", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.05f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableHeadersRow();

                if(_selectedLab.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                {
                    int index = 0;
                    foreach(var tech in researcherDB.TechQueue.ToList())
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{index + 1}");
                        ImGui.TableNextColumn();
                        ImGui.Text(_researchableTechsByGuid[tech].Name);
                        ImGui.TableNextColumn();
                        Buttons(researcherDB, tech, ref index);
                        index++;
                    }
                }

                ImGui.EndTable();
            }
        }

        private void DisplayTeams()
        {
            if(_scienceTeams == null
                || _factionData == null
                || _researchableTechsByGuid == null
                || _uiState.Faction == null)
                return;

            if(ImGui.BeginTable("Teams", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Scientist", ImGuiTableColumnFlags.None, 0.35f);
                ImGui.TableSetupColumn("Labs", ImGuiTableColumnFlags.None, 0.1f);
                ImGui.TableSetupColumn("Current Project", ImGuiTableColumnFlags.None, 0.35f);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 0.2f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < _scienceTeams.Count; i++)
                {
                    bool isSelected = _selectedTeam == i;

                    Scientist scientist = _scienceTeams[i].scientist;
                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(_scienceTeams[i].Item1.Name, isSelected))
                    {
                        _selectedTeam = i;
                    }

                    ImGui.TableNextColumn();
                    int allfacs = 0;
                    int facsAssigned = scientist.AssignedLabs;
                    if (_scienceTeams[i].atEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<ResearchPointsAtbDB>(out var foo))
                    {
                        allfacs = foo.Count;
                    }
                    ImGui.Text(facsAssigned.ToString() + "/" + allfacs.ToString());
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Assigned / Total");
                    ImGui.SameLine();

                    //Checks if more labs can be assigned
                    if (facsAssigned < allfacs)
                    {
                        if (ImGui.SmallButton("+"))//If so allow the user to add more labs
                        {
                            ResearchProcessor.AddLabs(scientist, 1);
                        }
                    }
                    else// Otherwise create an invisible button for spacing
                    {
                        ImGui.InvisibleButton("###invis-add", invisButtonSize);
                    }

                    if(facsAssigned > 0)
                    {
                        ImGui.SameLine();
                        if (ImGui.SmallButton("-"))
                        {
                            if (facsAssigned == 0)//If there are no labs to remove
                                ResearchProcessor.AddLabs(scientist, allfacs);//Roll over to max number of labs
                            else
                                ResearchProcessor.AddLabs(scientist, -1);//Otherwise remove a lab
                        }
                    }

                    ImGui.TableNextColumn();
                    if (scientist.ProjectQueue.Count > 0 && _factionData.IsResearchable(scientist.ProjectQueue[0].techID))
                    {
                        var proj = _researchableTechsByGuid[scientist.ProjectQueue[0].techID];

                        float frac = (float)proj.ResearchProgress / proj.ResearchCost;
                        var size = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new System.Numerics.Vector2(245, size), "");
                        ImGui.SetCursorPos(pos);
                        ImGui.Text(proj.Name);
                        if (ImGui.IsItemHovered())
                        {
                            string queue = "";
                            foreach (var queueItem in _scienceTeams[i].scientist.ProjectQueue)
                            {
                                queue += _researchableTechsByGuid[queueItem.techID].Name + "\n";
                            }
                            ImGui.SetTooltip(queue);
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(_scienceTeams[i].atEntity.GetDataBlob<NameDB>().GetName(_uiState.Faction));
                }

                ImGui.EndTable();
            }

            ImGui.NewLine();
            DisplayHelpers.Header("Tech Queue");

            if (_selectedTeam > -1)
            {
                SelectedSci(_selectedTeam);
            }
        }
        private void DisplayTechs()
        {
            if(_factionData == null || _scienceTeams == null || _uiState.Game == null)
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

        private void SelectedSci(int selected)
        {
            if(_scienceTeams == null || _researchableTechsByGuid == null)
                return;

            if(ImGui.BeginTable("TechQueue", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableHeadersRow();

                Scientist scientist = _scienceTeams[selected].scientist;
                int index = 0;
                foreach(var (techID, cycle) in scientist.ProjectQueue)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(_researchableTechsByGuid[techID].Name);
                    ImGui.TableNextColumn();
                    //Buttons(scientist, (techID, cycle), ref index);
                    index++;
                }

                ImGui.EndTable();
            }
        }

        void Buttons(ResearcherDB researcherDB, string techID, ref int i)
        {
            if(researcherDB.OwningEntity == null || _uiState.Game == null)
                return;

            ImGui.BeginGroup();

            // if(_researchableTechsByGuid != null &&_researchableTechsByGuid[queueItem.techID].MaxLevel > 1)
            // {
            //     string cyclestr = queueItem.cycle ? "O": "*";
            //     if (ImGui.SmallButton(cyclestr + "##" + i))
            //     {
            //         scientist.ProjectQueue[i] = (queueItem.techID, !queueItem.cycle);
            //     }

            //     if (ImGui.IsItemHovered())
            //         ImGui.SetTooltip("Requeue Project");
            // }
            // else
            // {
            //     ImGui.InvisibleButton("invis1", invisButtonSize);
            // }
            // ImGui.SameLine();

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
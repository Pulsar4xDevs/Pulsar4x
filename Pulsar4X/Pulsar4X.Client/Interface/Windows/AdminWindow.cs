using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using System.Linq;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Factions;
using Pulsar4X.Technology;
using Pulsar4X.People.Orders;
using GameEngine.People;
using Pulsar4X.Extensions;
using Pulsar4X.Engine;
using Pulsar4X.Colonies;

namespace Pulsar4X.Client
{
    public class AdminWindow : PulsarGuiWindow
    {
        private readonly Vector2 invisButtonSize = new (15, 15);
        private FactionDataStore? _factionData;
        private AdminSpaceAbilityState? _selectedAdminComponent = null;
        private Dictionary<int, Entity> _commanders;

        bool _showAssignmentModal = false;

        private AdminWindow()
        {
            OnFactionChange();
            if(_uiState.Game != null)
                _uiState.Game.TimePulse.GameGlobalDateChangedEvent += GameLoopOnGameGlobalDateChangedEvent;
        }

        private void GameLoopOnGameGlobalDateChangedEvent(DateTime newdate)
        {
            if (IsActive)
            {
                RefreshCommandPosts();
            }
        }

        internal static AdminWindow GetInstance()
        {
            AdminWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(AdminWindow)))
            {
                thisitem = new AdminWindow();
            }
            thisitem = (AdminWindow)_uiState.LoadedWindows[typeof(AdminWindow)];

            return thisitem;
        }

        private void OnFactionChange()
        {
            if(_uiState.Faction == null || _uiState.Game == null)
                return;

            _factionData = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data;



            RefreshCommandPosts();
        }

        private void RefreshCommandPosts()
        {

            //var _uiState.Game.GlobalManager.GetAllDataBlobsOfType<AdminSpaceDB>()
        }

        private void OpenColonyHexMap()
        {
            // Find the first colony entity for the current faction
            if (_uiState.Faction == null || _uiState.SelectedSystemState == null)
                return;

            var colonies = _uiState.SelectedSystemState.GetFilteredEntities(
                DataStructures.EntityFilter.Friendly,
                _uiState.Faction.Id,
                typeof(ColonyInfoDB));

            if (colonies.Any())
            {
                var firstColony = colonies.First().Entity;
                var hexMapWindow = ColonyHexMapWindow.GetInstance();
                hexMapWindow.SetSelectedColony(firstColony);
                hexMapWindow.ToggleActive();
            }
        }

        internal override void Display()
        {
            if(!IsActive)
                return;

            if (Window.Begin("Administration Posts", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X - Styles.LeftColumnWidthLg - 8, windowContentSize.Y);
                var secondChildSize = new Vector2(Styles.LeftColumnWidthLg, windowContentSize.Y);
/*
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
*/
                ImGui.SameLine();
                if(ImGui.BeginChild("Teams", firstChildSize, ImGuiChildFlags.Borders))
                {
                    DisplayHelpers.Header("Admin Posts");

                    // Add button to open hex map
                    if (ImGui.Button("Open Colony Hex Map"))
                    {
                        OpenColonyHexMap();
                    }
                    ImGui.Separator();

                    DisplayLabs();
                }
                ImGui.EndChild();
            }
            Window.End();
        }

        private void DisplayLabs()
        {
            if(_factionData == null
                || _uiState.Faction == null
                || _uiState.Game == null)
                return;

            if(ImGui.BeginTable("Admin Posts", 7, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Post", ImGuiTableColumnFlags.None, 0.15f);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 0.125f);
                ImGui.TableSetupColumn("Administrator", ImGuiTableColumnFlags.None, 0.125f);
                ImGui.TableSetupColumn("Cost/Day", ImGuiTableColumnFlags.None, 0.075f);
                ImGui.TableSetupColumn("Progress/Day", ImGuiTableColumnFlags.None, 0.075f);
                ImGui.TableSetupColumn("Researching", ImGuiTableColumnFlags.None, 0.20f);
                ImGui.TableSetupColumn("Funding", ImGuiTableColumnFlags.None, 0.15f);
                ImGui.TableHeadersRow();

                var entities = _uiState.SelectedSystemState.GetFilteredEntities(
                                DataStructures.EntityFilter.Friendly,
                                _uiState.Faction.Id,
                                typeof(AdminSpaceDB));


                foreach (var entityState in entities)
                {
                    if (!entityState.Entity.TryGetDataBlob<AdminSpaceDB>(out var adminSpaceDB))
                        continue;


                    foreach (var post in adminSpaceDB.CommanderSeats)
                    {
                        //adminSpaceDB.TechQueue.TryPeek(out var techId);

                        ImGui.TableNextColumn();

                        if (ImGui.Selectable(post.SeatType + $"###{entityState.Name}"))
                        {
                            _selectedAdminComponent = post;
                        }

                        if (ImGui.IsItemHovered() )
                        {

                        }

                        ImGui.TableNextColumn();
                        var location = _uiState.SelectedSystemState.GetEntityById(adminSpaceDB.OwningEntity.Id);
                        ImGui.Text(location?.Name);
                        ImGui.TableNextColumn();

                        var nameDisplay = "Assign Administrator";
                        if (post.TryGetCommander(out var commander))
                        {
                            nameDisplay = commander.OwningEntity.GetName(_uiState.Faction.Id);
                        }


                        if (ImGui.Button(nameDisplay))
                        {
                            _showAssignmentModal = true;
                        }

                        if (_showAssignmentModal)
                        {
                            ResultModal.GetInstance().DisplayCustomButtons(
                                "Assign Admin",
                                () => _showAssignmentModal = false, // onClose
                                (closeModal) => // Custom render with close action
                                {
                                    int selectedId = DisplayHelpers.PeopleChooser(
                                        _uiState,
                                        post.CommanderID,
                                        DataStructures.CommanderTypes.Civilian,
                                        $"admin_{entityState.Id}_{post.SeatType}",
                                        closeModal); // Pass close action as cancel

                                    if (selectedId != post.CommanderID)
                                    {
                                        if (selectedId == -1)
                                        {
                                            // Unassign the administrator, the player selected "None"
                                            var unassignOrder = UnassignAdministratorOrder.Create(entityState.Entity, post.CommanderID, post.ComponentName);
                                            _uiState.Game.OrderHandler.HandleOrder(unassignOrder);
                                        }
                                        else if (selectedId > 0)
                                        {
                                            // Assign the new administrator
                                            var assignmentOrder = AssignAdministratorOrder.Create(entityState.Entity, selectedId, post.ComponentName);
                                            _uiState.Game.OrderHandler.HandleOrder(assignmentOrder);
                                        }
                                        closeModal();
                                    }
                                });
                        }

                        ImGui.TableNextColumn();
                        /*
                        ImGui.Text(adminSpaceDB.CostPerDay.GetValue().ToString("C0", CultureInfo.CurrentCulture));
                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltip("Cost per Day",
                                                              "",
                                                              $"{adminSpaceDB.CostPerDay.BaseValue.ToString("C0", CultureInfo.CurrentCulture)} Base Value",
                                                              delegate
                                                              {
                                                                  foreach (var modifier in adminSpaceDB.CostPerDay.GetModifiers())
                                                                  {
                                                                      ImGui.Text($"{(modifier.After - modifier.Before).ToString("C0", CultureInfo.CurrentCulture)} {modifier.Name}");
                                                                  }
                                                              });
                        }
*/
                        ImGui.TableNextColumn();
                        /*
                        ImGui.Text(adminSpaceDB.PointsPerDay.GetValue().ToString());
                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltip("Progress per Day",
                                                              "",
                                                              $"{adminSpaceDB.PointsPerDay.BaseValue} Base Value",
                                                              delegate
                                                              {
                                                                  foreach (var modifier in adminSpaceDB.PointsPerDay.GetModifiers())
                                                                  {
                                                                      ImGui.TextUnformatted($"{modifier.After - modifier.Before} {modifier.Name}");
                                                                  }
                                                              });
                        }
*/
                        ImGui.TableNextColumn();
                        /*
                        if (techId != null && _factionData.IsResearchable(techId))
                        {

                        }

                        ImGui.TableNextColumn();
                        int funding = adminSpaceDB.FundingLevel;
                        string label = adminSpaceDB.FundingLevel switch
                        {
                            0 => "No Funding",
                            1 => "Standard",
                            2 => "Enhanced",
                            3 => "Robust",
                            4 => "Generous",
                            5 => "Spared No Expense",
                            _ => ""
                        };
                        */
                        var width = ImGui.GetContentRegionAvail().X;
                        ImGui.SetNextItemWidth(width);
                        /*
                        if (ImGui.SliderInt($"###{post.Id}-funding", ref funding, 0, 5, label))
                        {
                            var changeOrder = FundingChangedOrder.Create(post.ComponentInstance.ParentEntity, (byte)funding);
                            _uiState.Game.OrderHandler.HandleOrder(changeOrder);
                        }*/
                    }
                }

                ImGui.EndTable();
            }

            if(_selectedAdminComponent == null)
                return;

            ImGui.NewLine();
            DisplayHelpers.Header("Tech Queue for Selected Lab");

            if(ImGui.BeginTable("TechQueue", 3, Styles.TableFlags | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.05f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.5f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 0.45f);
                ImGui.TableHeadersRow();
/*
                if(_selectedAdminComponent.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                {
                    int index = 0;
                    foreach(var techId in researcherDB.TechQueue.ToList())
                    {

                        ImGui.TableNextColumn();
                        ImGui.Text($"{index + 1}");
                        ImGui.TableNextColumn();
                        ImGui.Text("foo");
                        //DisplayHelpers.TechTooltip(tech, _uiState);
                        ImGui.TableNextColumn();
                        Buttons(researcherDB, techId, ref index);
                        index++;
                    }
                }
*/
                ImGui.EndTable();
            }
        }

        private void DisplayTechs()
        {
            if(_factionData == null || _uiState.Game == null)
                return;

            if(ImGui.BeginTable("ResearchableTechs", 1, ImGuiTableFlags.BordersInnerV))
            {
                /*
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
                            if(_selectedAdminComponent != null && _selectedAdminComponent.TryGetDataBlob<ResearcherDB>(out var researcherDB))
                            {
                                var addOrder = AddTechToQueueOrder.Create(_selectedAdminComponent.Entity, _researchableTechs[i].UniqueID);
                                _uiState.Game.OrderHandler.HandleOrder(addOrder);
                            }
                        }
                    }
                }
*/
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
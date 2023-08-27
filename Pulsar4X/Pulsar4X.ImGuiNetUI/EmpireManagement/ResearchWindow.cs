using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class ResearchWindow : PulsarGuiWindow
    {
        private FactionTechDB _factionTechDB;
        private Dictionary<Guid, (TechSD tech, int amountDone, int amountMax)> _researchableTechsByGuid;
        private List<(TechSD tech, int amountDone, int amountMax)> _researchableTechs;
        private List<(Scientist scientist, Entity atEntity)> _scienceTeams;
        private int _selectedTeam = -1;

        private ResearchWindow()
        {
            OnFactionChange();
            _uiState.Game.GamePulse.GameGlobalDateChangedEvent += GameLoopOnGameGlobalDateChangedEvent; 
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
            _factionTechDB = _uiState.Faction.GetDataBlob<FactionTechDB>();
            _scienceTeams = _factionTechDB.AllScientists;
            RefreshTechs();
        }

        private void RefreshTechs()
        {
            _researchableTechs = _factionTechDB.GetResearchableTechs();
            _researchableTechsByGuid = _factionTechDB.GetResearchablesDic();

            _researchableTechs.Sort((a,b) => a.tech.Name.CompareTo(b.tech.Name));
        }

        internal override void Display()
        {

            if (IsActive && ImGui.Begin("Research and Development", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.75f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.245f, windowContentSize.Y);

                if(ImGui.BeginChild("Teams", firstChildSize, true))
                {
                    DisplayHelpers.Header("Teams");

                    float width = ImGui.GetContentRegionAvail().X;
                    float height = ImGui.GetTextLineHeightWithSpacing() * (_scienceTeams.Count + 2);
                    DisplayTeams(width, height);
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if(ImGui.BeginChild("Techs", secondChildSize, true))
                {
                    DisplayHelpers.Header("Available Techs");

                    DisplayTechs();
                    ImGui.EndChild();
                }

                if (_selectedTeam == -1)
                {
                    if (_scienceTeams.Count > 0 && _scienceTeams != null)
                    {
                       _selectedTeam = 0;
                    }
                }

            }
        }

        private int hoveredi = -1;

        private void DisplayTeams(float width, float height)
        {
            if(ImGui.BeginTable("Teams", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersInnerH))
            {
                ImGui.TableSetupColumn("Scientist", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Labs", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableSetupColumn("Current Project", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.None, 0.75f);
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
                        System.Numerics.Vector2 buttonsize = new System.Numerics.Vector2(15, 0);
                        ImGui.InvisibleButton(" ", buttonsize);
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
                    if (scientist.ProjectQueue.Count > 0 && _factionTechDB.IsResearchable(scientist.ProjectQueue[0].techID))
                    {
                        var proj = _researchableTechsByGuid[scientist.ProjectQueue[0].techID];

                        float frac = (float)proj.amountDone / proj.amountMax;
                        var size = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new System.Numerics.Vector2(245, size), "");
                        ImGui.SetCursorPos(pos);
                        ImGui.Text(proj.tech.Name);
                        if (ImGui.IsItemHovered())
                        {
                            string queue = "";
                            foreach (var queueItem in _scienceTeams[i].scientist.ProjectQueue)
                            {
                                queue += _researchableTechsByGuid[queueItem.techID].tech.Name + "\n";
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
            if(ImGui.BeginTable("ResearchableTechs", 2, ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    if (_researchableTechs[i].amountMax > 0) //could happen if bad json data?
                    {
                        ImGui.TableNextColumn();

                        float frac = (float)_researchableTechs[i].amountDone / _researchableTechs[i].amountMax;
                        var size = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new System.Numerics.Vector2(245, size), "");
                        ImGui.SetCursorPos(pos);
                        ImGui.Text(_researchableTechs[i].tech.Name);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            if (_selectedTeam > -1)
                                ResearchProcessor.AssignProject(_scienceTeams[_selectedTeam].scientist, _researchableTechs[i].tech.ID);
                        }
                        if (ImGui.IsItemHovered() && !_researchableTechs[i].tech.Description.IsNullOrEmpty())
                        {
                            ImGui.SetTooltip(_researchableTechs[i].tech.Description);
                        }
                        ImGui.TableNextColumn();
                        if(_researchableTechs[i].tech.MaxLevel > 1)
                        {
                            ImGui.Text(_factionTechDB.GetLevelforTech(_researchableTechs[i].tech).ToString());
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
        }

        private void SelectedSci(int selected)
        {
            ImGui.BeginChild("SelectedSci");
            Scientist scientist = _scienceTeams[selected].scientist;

            //ImGui.Columns(2);
            //ImGui.SetColumnWidth(0, 300);
            //ImGui.SetColumnWidth(1, 150);

            int loopto = scientist.ProjectQueue.Count;
            if (hoveredi >= scientist.ProjectQueue.Count)
                hoveredi = -1;
            if (hoveredi > -1)
                loopto = hoveredi;

            var spacingH = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight();


            float heightt = ImGui.GetTextLineHeightWithSpacing() * loopto + spacingH * loopto;
            float hoverHeigt = ImGui.GetTextLineHeightWithSpacing() + spacingH * 3;
            float heightb = ImGui.GetTextLineHeightWithSpacing() * scientist.ProjectQueue.Count - loopto;
            float colomnWidth0 = 300;

            for (int i = 0; i < loopto; i++)
            {
                ImGui.BeginChild("Top", new System.Numerics.Vector2(400, heightt));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);
                (Guid techID, bool cycle) queueItem = _scienceTeams[selected].scientist.ProjectQueue[i];
                (TechSD tech, int amountDone, int amountMax) projItem = _researchableTechsByGuid[queueItem.techID];

                ImGui.BeginGroup();
                var cpos = ImGui.GetCursorPos();
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                ImGui.Button("##projItem.tech.Name", new System.Numerics.Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
                ImGui.PopStyleColor();
                ImGui.SetCursorPos(cpos);
                ImGui.Text(projItem.tech.Name);
                ImGui.EndGroup();

                if (ImGui.IsItemHovered())
                {
                    hoveredi = i;
                }
                ImGui.NextColumn();
                ImGui.NextColumn();
                ImGui.EndChild();
            }

            if (hoveredi > -1)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0.5f);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2f);
                ImGui.BeginChild("Buttons", new System.Numerics.Vector2(400, hoverHeigt), true);
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);

                (Guid techID, bool cycle) queueItem = _scienceTeams[selected].scientist.ProjectQueue[hoveredi];
                (TechSD tech, int amountDone, int amountMax) projItem = _researchableTechsByGuid[queueItem.techID];


                ImGui.BeginGroup();
                ImGui.Text(projItem.tech.Name);
                ImGui.EndGroup();

                ImGui.NextColumn();

                Buttons(scientist, queueItem, hoveredi);

                ImGui.NextColumn();

                ImGui.EndChild();
                ImGui.PopStyleVar(2);


                for (int i = hoveredi + 1; i < scientist.ProjectQueue.Count; i++)
                {
                    ImGui.BeginChild("Bottom");
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 300);
                    (Guid techID, bool cycle) queueItem1 = _scienceTeams[selected].scientist.ProjectQueue[i];
                    (TechSD tech, int amountDone, int amountMax) projItem1 = _researchableTechsByGuid[queueItem1.techID];

                    ImGui.BeginGroup();
                    var cpos = ImGui.GetCursorPos();
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));
                    ImGui.Button("##projItem1.tech.Name", new System.Numerics.Vector2(colomnWidth0 - spacingH, ImGui.GetTextLineHeightWithSpacing()));
                    ImGui.PopStyleColor();
                    ImGui.SetCursorPos(cpos);
                    ImGui.Text(projItem1.tech.Name);
                    ImGui.EndGroup();

                    if (ImGui.IsItemHovered())
                    {
                        hoveredi = i;
                    }

                    ImGui.NextColumn();
                    ImGui.NextColumn();

                    ImGui.EndChild();
                }
            }

            ImGui.EndChild();

        }

        void Buttons(Scientist scientist, (Guid techID, bool cycle) queueItem, int i)
        {
            ImGui.BeginGroup();
            string cyclestr = "*";
            if (queueItem.cycle)
                cyclestr = "O";
            if (ImGui.SmallButton(cyclestr + "##" + i))
            {
                scientist.ProjectQueue[i] = (queueItem.techID, !queueItem.cycle);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Requeue Project");

            ImGui.SameLine();
            if (ImGui.SmallButton("^" + "##" + i) && i > 0)
            {
                scientist.ProjectQueue.RemoveAt(i);
                scientist.ProjectQueue.Insert(i - 1, queueItem);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("v" + "##" + i) && i < scientist.ProjectQueue.Count - 1)
            {

                scientist.ProjectQueue.RemoveAt(i);
                scientist.ProjectQueue.Insert(i + 1, queueItem);
            }

            ImGui.SameLine();
            if (ImGui.SmallButton("x" + "##" + i))
            {
                scientist.ProjectQueue.RemoveAt(i);
            }

            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                hoveredi = i;
            }
        }
    }
}
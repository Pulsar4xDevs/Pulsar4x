using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using System.Linq;

namespace Pulsar4X.SDL2UI
{
    public class ResearchWindow : PulsarGuiWindow
    {
        private readonly Vector2 invisButtonSize = new (15, 15);
        private FactionDataStore _factionData;
        private FactionTechDB _factionTechDB;
        private List<Tech> _researchableTechs;
        private Dictionary<string, Tech> _researchableTechsByGuid;
        private List<(Scientist scientist, Entity atEntity)> _scienceTeams;
        private int _selectedTeam = -1;

        private ResearchWindow()
        {
            OnFactionChange();
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
            _factionData = _uiState.Faction.GetDataBlob<FactionInfoDB>().Data;
            _factionTechDB = _uiState.Faction.GetDataBlob<FactionTechDB>();
            _scienceTeams = _factionTechDB.AllScientists;
            RefreshTechs();
        }

        private void RefreshTechs()
        {
            _researchableTechs = _factionData.Techs.Select(kvp => kvp.Value).Where(t => _factionTechDB.IsResearchable(t.UniqueID)).ToList();
            _researchableTechs.Sort((a,b) => a.Name.CompareTo(b.Name));

            _researchableTechsByGuid = new (_factionData.Techs);
        }

        internal override void Display()
        {

            if (IsActive && ImGui.Begin("Research and Development", ref IsActive, _flags))
            {
                Vector2 windowContentSize = ImGui.GetContentRegionAvail();
                var firstChildSize = new Vector2(windowContentSize.X * 0.75f, windowContentSize.Y);
                var secondChildSize = new Vector2(windowContentSize.X * 0.245f, windowContentSize.Y);

                if(ImGui.BeginChild("Techs", secondChildSize, true))
                {
                    DisplayHelpers.Header("Available Techs", "Double click to add to research queue");

                    DisplayTechs();
                    ImGui.EndChild();
                }
                ImGui.SameLine();
                if(ImGui.BeginChild("Teams", firstChildSize, true))
                {
                    DisplayHelpers.Header("Teams");

                    DisplayTeams();
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

        private void DisplayTeams()
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
            if(ImGui.BeginTable("ResearchableTechs", 2, ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1.5f);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableHeadersRow();

                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    if (_researchableTechs[i].ResearchCost > 0) //could happen if bad json data?
                    {
                        ImGui.TableNextColumn();

                        float frac = (float)_researchableTechs[i].ResearchProgress / _researchableTechs[i].ResearchCost;
                        var size = ImGui.GetTextLineHeight();
                        var pos = ImGui.GetCursorPos();
                        ImGui.ProgressBar(frac, new Vector2(245, size), "");
                        if (ImGui.IsItemHovered())
                        {
                            DisplayHelpers.DescriptiveTooltip(_researchableTechs[i].Name, _researchableTechs[i].Category.ToString(), _researchableTechs[i].Description);
                        }
                        ImGui.SetCursorPos(new Vector2(pos.X + 2f, pos.Y));
                        ImGui.Text(_researchableTechs[i].Name);

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            if (_selectedTeam > -1)
                                ResearchProcessor.AssignProject(_scienceTeams[_selectedTeam].scientist, _researchableTechs[i].UniqueID);
                        }
                        ImGui.TableNextColumn();
                        if(_researchableTechs[i].MaxLevel > 1)
                        {
                            ImGui.Text(_researchableTechs[i].Level.ToString());
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }
                }

                ImGui.EndTable();
            }
        }

        private void SelectedSci(int selected)
        {
            if(ImGui.BeginTable("TechQueue", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("Options", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                Scientist scientist = _scienceTeams[selected].scientist;
                int index = 0;
                foreach(var (techID, cycle) in scientist.ProjectQueue.ToArray())
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(_researchableTechsByGuid[techID].Name);
                    ImGui.TableNextColumn();
                    Buttons(scientist, (techID, cycle), index);
                    index++;
                }

                ImGui.EndTable();
            }

            //ImGui.BeginChild("SelectedSci");
            

            //ImGui.Columns(2);
            //ImGui.SetColumnWidth(0, 300);
            //ImGui.SetColumnWidth(1, 150);
            /*
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
            */
        }

        void Buttons(Scientist scientist, (string techID, bool cycle) queueItem, int i)
        {
            ImGui.BeginGroup();

            if(_researchableTechsByGuid[scientist.ProjectQueue[i].techID].MaxLevel > 1)
            {
                string cyclestr = queueItem.cycle ? "O": "*";
                if (ImGui.SmallButton(cyclestr + "##" + i))
                {
                    scientist.ProjectQueue[i] = (queueItem.techID, !queueItem.cycle);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Requeue Project");
            }
            else
            {
                ImGui.InvisibleButton("invis", invisButtonSize);
            }
            ImGui.SameLine();

            if (i > 0)
            {
                if(ImGui.SmallButton("^" + "##" + i))
                {
                    scientist.ProjectQueue.RemoveAt(i);
                    scientist.ProjectQueue.Insert(i - 1, queueItem);
                }
            }
            else
            {
                ImGui.InvisibleButton("invis", invisButtonSize);
            }
            ImGui.SameLine();

            if (i < scientist.ProjectQueue.Count - 1)
            {
                if(ImGui.SmallButton("v" + "##" + i))
                {
                    scientist.ProjectQueue.RemoveAt(i);
                    scientist.ProjectQueue.Insert(i + 1, queueItem);
                }
            }
            else
            {
                ImGui.InvisibleButton("invis", invisButtonSize);
            }
            ImGui.SameLine();

            if (ImGui.SmallButton("x" + "##" + i))
            {
                scientist.ProjectQueue.RemoveAt(i);
            }

            ImGui.EndGroup();
        }
    }
}
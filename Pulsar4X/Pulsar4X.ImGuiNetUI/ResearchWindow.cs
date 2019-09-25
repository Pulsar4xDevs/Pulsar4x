

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class ResearchWindow : PulsarGuiWindow
    {
        private FactionTechDB _factionTechDB;
        private Dictionary<Guid, (TechSD tech, int amountDone, int amountMax)> _researchableTechsByGuid;
        private List<(TechSD tech, int amountDone, int amountMax)> _researchableTechs;
        
        private EntityState _currentEntity;
        private TeamsHousedDB _teamsHousedDB;
        private List<(Scientist scientist, Entity atEntity)> _scienceTeams;
        private int _selectedTeam = -1;
       
        private ResearchWindow()
        {
            OnFactionChange();
            _state.Game.GameLoop.GameGlobalDateChangedEvent += GameLoopOnGameGlobalDateChangedEvent; 
        }

        private void GameLoopOnGameGlobalDateChangedEvent(DateTime newdate)
        {
            if (IsActive)
            {
                _researchableTechs = _factionTechDB.GetResearchableTechs();
                foreach (var item in _researchableTechs)
                {
                    _researchableTechsByGuid[item.tech.ID] = item;
                }
            }
        }


        internal static ResearchWindow GetInstance()
        {
            ResearchWindow thisitem;
            if (!_state.LoadedWindows.ContainsKey(typeof(ResearchWindow)))
            {
                thisitem = new ResearchWindow();
            }
            thisitem = (ResearchWindow)_state.LoadedWindows[typeof(ResearchWindow)];
            if (_state.LastClickedEntity != thisitem._currentEntity)
            {
                if (_state.LastClickedEntity.Entity.HasDataBlob<TeamsHousedDB>())
                {
                    thisitem.OnEntityChange(_state.LastClickedEntity);
                }
            }


            return thisitem;
        }


        private void OnFactionChange()
        {
            _factionTechDB = _state.Faction.GetDataBlob<FactionTechDB>();
            
            List<(TechSD tech, int amountDone, int amountMax)> researchableTechs = new List<(TechSD, int, int)>();
            Dictionary<Guid, (TechSD tech, int amountDone, int amountMax)> byGuid = new Dictionary<Guid, (TechSD tech, int amountDone, int amountMax)>();
            foreach (var item in _factionTechDB.GetResearchableTechs())
            {
                int max = ResearchProcessor.CostFormula(_factionTechDB, item.tech);
                var tuple = (item.tech, item.pointsResearched, max);
                researchableTechs.Add(tuple);
                byGuid.Add(item.tech.ID, tuple);
            }
            _researchableTechs = researchableTechs;
            _researchableTechsByGuid = byGuid;
            _scienceTeams = _factionTechDB.AllScientists;
        }

 

        private void OnEntityChange(EntityState entityState)
        {
            _currentEntity = entityState;

            _teamsHousedDB = (TeamsHousedDB)entityState.DataBlobs[typeof(TeamsHousedDB)];
        }


        internal override void Display()
        {
            if (IsActive && ImGui.Begin("Research and Development", ref IsActive, _flags))
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 300);
                ImGui.Text("Projects");
                ImGui.NextColumn();
                ImGui.Text("Science Teams");
                ImGui.NextColumn();
                ImGui.Separator();
                
                ImGui.BeginChild("ResearchablesHeader", new Vector2(300, ImGui.GetFontSize() + 5));
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, 250);
                ImGui.Text("Tech");
                ImGui.NextColumn();
                ImGui.Text("Level");
                ImGui.NextColumn();
                ImGui.Separator();
                ImGui.EndChild();
                
                ImGui.BeginChild("techlist");
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0,250);
                
                for (int i = 0; i < _researchableTechs.Count; i++)
                {
                    if (_researchableTechs[i].amountMax > 0) //could happen if bad json data?
                    {
                        ImGui.Text(_researchableTechs[i].tech.Name);
                        
                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        {
                            if(_selectedTeam > -1)
                                ResearchProcessor.AssignProject(_scienceTeams[_selectedTeam].scientist, _researchableTechs[i].tech.ID);
                        }
                        if(ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(_researchableTechs[i].tech.Description);
                        }
                        ImGui.NextColumn();
                        ImGui.Text(_factionTechDB.LevelforTech(_researchableTechs[i].tech).ToString());
                        
                        ImGui.NextColumn();
                        float frac = (float)_researchableTechs[i].amountDone / _researchableTechs[i].amountMax;
                        ImGui.ProgressBar(frac, new Vector2(248, 2));
                        if(ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(_researchableTechs[i].amountDone + "/" + _researchableTechs[i].amountMax );
                        }
                        
                    }
                }
                ImGui.EndChild();
                
                ImGui.NextColumn();

                ImGui.BeginChild("Teams");
                
                ImGui.Columns(4);
                ImGui.Text("Scientist");
                ImGui.NextColumn();
                ImGui.Text("Location");
                ImGui.NextColumn();
                ImGui.Text("Labs");
                ImGui.NextColumn();
                ImGui.Text("Current Project");
                ImGui.NextColumn();
                
                for (int i = 0; i < _scienceTeams.Count; i++)
                {

                    bool isSelected = _selectedTeam == i;
                    
                    Scientist scint = _scienceTeams[i].scientist;
                    if (ImGui.Selectable(_scienceTeams[i].Item1.Name, isSelected))
                    {
                        _selectedTeam = i;
                    }

                    ImGui.NextColumn();
                    ImGui.Text(_scienceTeams[i].atEntity.GetDataBlob<NameDB>().GetName(_state.Faction));
                    
                    ImGui.NextColumn();
                    int allfacs = 0;
                    int facsAssigned = scint.AssignedLabs;
                    //int facsFree = 0;
                    if(
                    _scienceTeams[i].atEntity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<ResearchPointsAtbDB>( out var foo ))
                    {
                        allfacs = foo.Count;
                        //facsFree = allfacs - facsAssigned;
                    }
                    ImGui.Text(facsAssigned.ToString() + "/" + allfacs.ToString());
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Assigned / Total");
                    ImGui.SameLine();
                    if (ImGui.SmallButton("+"))
                    {
                        ResearchProcessor.AddLabs(scint, 1);
                    }
                    ImGui.SameLine();
                    if (ImGui.SmallButton("-"))
                    {
                        ResearchProcessor.AddLabs(scint, -1);
                    }

                    ImGui.NextColumn();
                    if (scint.ProjectQueue.Count > 0 && _factionTechDB.IsResearchable(scint.ProjectQueue[0].techID))
                    {
                        var proj = _researchableTechsByGuid[scint.ProjectQueue[0].techID];
                        
                        ImGui.Text(proj.tech.Name);
                        if(ImGui.IsItemHovered())
                        {
                            string queue = "";
                            foreach (var queueItem in _scienceTeams[i].scientist.ProjectQueue)
                            {
                                queue += _researchableTechsByGuid[queueItem.techID].tech.Name + "\n";
                            }
                            ImGui.SetTooltip(queue);
                        }


                        float frac = (float)proj.amountDone / proj.amountMax;
                        ImGui.ProgressBar(frac, new Vector2(150, 10));

                    }
                }
                ImGui.EndChild();
            }
        }

        private void SelectedSci(int selected)
        {
            var scientist = _scienceTeams[selected].scientist;
            
            for (int i = 0; i < scientist.ProjectQueue.Count; i++)
            {
                var proj = _state.Game.StaticData.Techs[_scienceTeams[selected].scientist.ProjectQueue[0].techID];
                ImGui.Text(proj.Name);
                ImGui.Text(proj.Description);
            }

            if (ImGui.Button("Add Project"))
            {
                foreach (var rt in _researchableTechs)
                {
                    //ImGui.Text(rt.Key.Name);
                    //ImGui.Text(rt.Value.ToString());
                }
            }

        }
    }
}
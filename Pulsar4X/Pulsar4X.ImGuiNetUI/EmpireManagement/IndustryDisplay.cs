using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;

namespace Pulsar4X.SDL2UI
{
    public sealed class IndustryDisplay
    {
        private static IndustryDisplay instance = null;
        private static readonly object padlock = new object();

        public EntityState EntityState { get; private set; }

        private Guid _factionID;
        private FactionInfoDB _factionInfoDB;
        Dictionary<Guid, (Guid[] itemIDs, string[] itemNames) > _contructablesByPline = new Dictionary<Guid, (Guid[], string[])>();
        private IndustryJob _newConJob;
        private (Guid pline, int item) _newjobSelectionIndex = (Guid.Empty, 0);
        private int _newJobbatchCount = 1;
        private bool _newJobRepeat = false;
        private bool _newJobAutoInstall = true;
        private Dictionary<Guid,IndustryAbilityDB.ProductionLine> _prodLines;
        private Guid _selectedProdLine;
        private int _selectedExistingIndex = -1;
        private IndustryJob _lastClickedJob { get; set; }
        private IConstrucableDesign _lastClickedDesign;
        private Entity Entity;
        private IndustryAbilityDB _industryDB;
        private IndustryJob _selectedExistingConJob
        {
            get
            {
                if (_selectedProdLine != Guid.Empty 
                    && _selectedExistingIndex > -1
                    && _prodLines[_selectedProdLine].Jobs.Count > _selectedExistingIndex)
                {
                    return _prodLines[_selectedProdLine].Jobs[_selectedExistingIndex];
                }

                return null;
            }
        }

        Guid SelectedConstrucableID
        {

            get
            {
                if(_contructablesByPline.Count != _industryDB.ProductionLines.Count)
                    Update();
                return _contructablesByPline[_newjobSelectionIndex.pline].itemIDs[_newjobSelectionIndex.item];
            }
        }

        private IndustryDisplay() { }

        internal static IndustryDisplay GetInstance(EntityState state) {
            lock(padlock)
            {
                if(instance == null)
                {
                    instance = new IndustryDisplay();
                }
                instance.SetEntity(state);
            }

            return instance;
        }

        public void SetEntity(EntityState state)
        {
            if(state == EntityState) return;

            EntityState = state;
            //Update();
        }

        private void Update()
        {
            Entity = EntityState.Entity;
            _industryDB = Entity.GetDataBlob<IndustryAbilityDB>();
            //_job = job;

            _prodLines = _industryDB.ProductionLines;

            int count = _factionInfoDB.IndustryDesigns.Count;
            //_constructableDesigns = new IConstrucableDesign[count];
            var constructablesNames = new string[count];
            var constructablesIDs = new Guid[count];

            int i = 0;
            Dictionary<Guid, List<int>> _constructablesIndexesByType = new Dictionary<Guid, List<int>>();
            foreach (var kvp in _factionInfoDB.IndustryDesigns)
            {
                //_constructableDesigns[i] = kvp.Value;
                constructablesNames[i] = kvp.Value.Name;
                constructablesIDs[i] = kvp.Key;
                Guid typeID = kvp.Value.IndustryTypeID;

                if(!_constructablesIndexesByType.ContainsKey(typeID))
                    _constructablesIndexesByType.Add(typeID, new List<int>());
                _constructablesIndexesByType[typeID].Add(i);
                i++;
            }

            foreach (var plineKVP in _prodLines)
            {
                Guid componentID = plineKVP.Key;
                var pline = plineKVP.Value;
                List<Guid> itemIDs = new List<Guid>();
                List<string> itemNames = new List<string>();
                foreach (var typeID in pline.IndustryTypeRates.Keys)
                {
                    if(_constructablesIndexesByType.ContainsKey(typeID))
                    {
                        foreach (var index in _constructablesIndexesByType[typeID])
                        {
                            itemIDs.Add(constructablesIDs[index]);
                            itemNames.Add(constructablesNames[index]);
                        }
                    }
                }
                _contructablesByPline[componentID] = (itemIDs.ToArray(), itemNames.ToArray());
            }
        }

        public void Display(GlobalUIState state)
        {
            _factionInfoDB = state.Faction.GetDataBlob<FactionInfoDB>();
            _factionID = state.Faction.Guid;
            Update();

            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            ProductionLineDisplay(state);
            ImGui.SameLine();

            ImGui.SameLine();
            if(ImGui.BeginChild("ColonySummary2", new Vector2(windowContentSize.X * 0.5f - 8f, windowContentSize.Y), true))
            {
                EditButtonsDisplay(state);
                NewJobDisplay(state);
                if(_lastClickedJob != null)
                    CostsDisplay(_lastClickedJob, state);
                ImGui.EndChild();
            }
        }

        public void ProductionLineDisplay(GlobalUIState state)
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("ColonyProductionLines", new Vector2(windowContentSize.X * 0.5f, windowContentSize.Y), true))
            {
                foreach (var kvp in _prodLines)
                {
                    IndustryAbilityDB.ProductionLine ud = kvp.Value;
                    ImGui.PushID(kvp.Key.ToString());
                    //ImGui.Selectable()

                    if (ImGui.CollapsingHeader(ud.FacName))
                    {
                        ImGui.Columns(2);
                        ImGui.SetColumnWidth(0, 128);
                        Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
                        for (int ji = 0; ji < ud.Jobs.Count; ji++)
                        {
                            var cpos = ImGui.GetCursorPos();
                            var batchJob = ud.Jobs[ji];
                            string jobname = ud.Jobs[ji].Name;

                            bool selected = _selectedExistingIndex ==  ji;
                            float percent = 1 - (float)batchJob.ProductionPointsLeft / batchJob.ProductionPointsCost;
                            ImGui.ProgressBar(percent, progsize, "");
                            ImGui.SetCursorPos(cpos);
                            if (ImGui.Selectable(jobname, ref selected))
                            {
                                _selectedExistingIndex =  ji;
                                _lastClickedJob = _selectedExistingConJob;
                                _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                            }

                            ImGui.NextColumn();
                            ImGui.Text(batchJob.NumberCompleted + "/" + batchJob.NumberOrdered);

                            if (batchJob.Auto)
                            {
                                ImGui.SameLine();
                                ImGui.Image(state.Img_Repeat(), new Vector2(16, 16));
                            }

                            ImGui.NextColumn();
                        }
                        ImGui.Columns(1);
                    }

                    if (ImGui.IsItemClicked())
                    {
                        _selectedProdLine = kvp.Key;
                        _newjobSelectionIndex = (_selectedProdLine, 0);
                        _lastClickedJob = _selectedExistingConJob;
                        _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    }
                    ImGui.PopID();
                }
                ImGui.EndChild();
            }
        }

        void EditButtonsDisplay(GlobalUIState state)
        {
            //ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();

            if (ImGui.ImageButton(state.Img_Up(), new Vector2(16, 8)) && _selectedExistingConJob != null)
            {
                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID, -1);
                StaticRefLib.OrderHandler.HandleOrder(cmd); 
            }

            if (ImGui.ImageButton(state.Img_Down(), new Vector2(16, 8)) && _selectedExistingConJob != null)
            {
                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID, 1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(state.Img_Repeat(), new Vector2(16, 16)) && _selectedExistingConJob != null)
            {

                var jobcount = _selectedExistingConJob.NumberOrdered;
                var jobrepeat = _selectedExistingConJob.Auto;

                var cmd = IndustryOrder2.CreateEditJobOrder(_factionID, Entity, _selectedProdLine,_selectedExistingConJob.JobID, jobcount, !jobrepeat);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.SameLine();
            if (ImGui.ImageButton(state.Img_Cancel(), new Vector2(16, 16)) && _selectedExistingConJob != null)
            {
                //new ConstructCancelJob(_uiState.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, Entity, _selectedProdLine, _selectedExistingConJob.JobID);

                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (_lastClickedDesign != null)
            {
                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeInstalled)
                {
                    ImGui.Checkbox("Auto Install on colony", ref _newJobAutoInstall);
                    
                    if (_newJobAutoInstall)
                        _lastClickedJob.InstallOn = Entity;
                    else
                        _lastClickedJob.InstallOn = null;
                    
                }
                
                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeLaunched)
                {
                    if (Entity.HasDataBlob<ColonyInfoDB>())
                    {
                        var s = (ShipDesign)_lastClickedDesign;
                        var planet = Entity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                        var lowOrbit = planet.GetDataBlob<MassVolumeDB>().RadiusInM * 0.33333;
                    
                        var mass = s.MassPerUnit;
                    
                        var fuelCost = OrbitMath.FuelCostToLowOrbit(planet, mass);

                    
                        if (ImGui.Button("Launch to Low Orbit"))
                        {
                            LaunchShipCmd.CreateCommand(_factionID, Entity, _selectedProdLine, _lastClickedJob.JobID);
                        }
                        //ImGui.SameLine();


                        ImGui.Text("Fuel Cost: " + fuelCost);
                    
                    }
                }
            }

            //ImGui.EndGroup();
            
            //ImGui.Button("Install On Parent")
            
        }

        void NewJobDisplay(GlobalUIState state)
        {
            //ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);
            if(_newjobSelectionIndex.pline != Guid.Empty)
            {
                int curItemIndex = _newjobSelectionIndex.item;

                var constructableNames = _contructablesByPline[_selectedProdLine].itemNames;

                if (ImGui.Combo("NewJobSelection", ref curItemIndex, constructableNames, constructableNames.Length))
                {
                    _newjobSelectionIndex = (_newjobSelectionIndex.pline, curItemIndex);
                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                }

                ImGui.InputInt("Batch Count", ref _newJobbatchCount);

                ImGui.Checkbox("Repeat Job", ref _newJobRepeat);
                ImGui.SameLine();
                //if the selected item can be installed on a colony:
                

                if (ImGui.Button("Create New Job"))
                {

                    _newConJob = new IndustryJob(state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);

                    var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, Entity, _selectedProdLine, _newConJob);
                    _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);
                    if(_newJobAutoInstall)
                        _newConJob.InstallOn = Entity;
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    StaticRefLib.OrderHandler.HandleOrder(cmd);
                }

            }

            //ImGui.EndChild();
        }

        void CostsDisplay(JobBase selectedJob, GlobalUIState state)
        {
            ImGui.BeginChild("Resources Requred", new Vector2(294, 184 ), true, ImGuiWindowFlags.ChildWindow);
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 140);
            ImGui.SetColumnWidth(1, 48);
            ImGui.Text("Industry Points");
            ImGui.NextColumn();
            ImGui.Text(selectedJob.ProductionPointsLeft.ToString());
            ImGui.NextColumn();
            foreach (var item in selectedJob.ResourcesRequired)
            {
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(item.Key);
                if (cargoItem == null)
                    cargoItem = state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[item.Key];
                ImGui.Text(cargoItem.Name);
                ImGui.NextColumn();
                ImGui.Text(item.Value.ToString());
                ImGui.NextColumn();
            }
            
            ImGui.EndChild();
        }
    }
}
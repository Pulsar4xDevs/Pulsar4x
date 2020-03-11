using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Microsoft.Win32;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.SDL2UI;
using SDL2;
using Vector3 = Pulsar4X.ECSLib.Vector3;


namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    
    public class IndustryPannel2
    {
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

        private IndustryJob _lastClickedJob { get; set; }
        private IConstrucableDesign _lastClickedDesign;
        
        private Entity _selectedEntity;
        private IndustryAbilityDB _industryDB;
        private GlobalUIState _state;

     
        
        public IndustryPannel2(GlobalUIState state, Entity selectedEntity, IndustryAbilityDB industryDB)
        {
            
            _state = state;
            _selectedEntity = selectedEntity;
            _industryDB = industryDB;
            //_job = job;
            _factionInfoDB = state.Faction.GetDataBlob<FactionInfoDB>();
            _factionID = state.Faction.Guid;



            _selectedEntity.Manager.ManagerSubpulses.SystemDateChangedEvent += OnDatechange;
            OnUpdate();
        }

        void OnDatechange(DateTime newDate)
        {
            OnUpdate();
        }

        void OnUpdate()
        {
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



        Guid SelectedConstrucableID
        {

            get
            {
                if(_contructablesByPline.Count != _industryDB.ProductionLines.Count)
                    OnUpdate();
                return _contructablesByPline[_newjobSelectionIndex.pline].itemIDs[_newjobSelectionIndex.item];
            }
        }

        public void Display()
        {
  
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 285);
            ImGui.SetColumnWidth(1, 308);
            //ImGui.SetColumnWidth(1, 190);
            
            ProdLineDisplay();
            
            
            ImGui.NextColumn();
            ImGui.BeginGroup();
            EditButtonsDisplay();
            NewJobDisplay();
            if(_lastClickedJob != null)
                CostsDisplay(_lastClickedJob);
            ImGui.EndGroup();
            //ImGui.NextColumn();

            
            
                
        }

        public void ProdLineDisplay()
        {
            ImGui.BeginChild("prodline", new Vector2(280, 300), true, ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysAutoResize);


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
                            ImGui.Image(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16));
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

        void EditButtonsDisplay()
        {
            //ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();

            if (ImGui.ImageButton(_state.SDLImageDictionary["UpImg"], new Vector2(16, 8)))
            {

                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, _selectedEntity, _selectedProdLine, _selectedExistingConJob.JobID, -1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (ImGui.ImageButton(_state.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            {
                var cmd = IndustryOrder2.CreateChangePriorityOrder(_factionID, _selectedEntity, _selectedProdLine, _selectedExistingConJob.JobID, 1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
            {

                var jobcount = _selectedExistingConJob.NumberOrdered;
                var jobrepeat = _selectedExistingConJob.Auto;

                var cmd = IndustryOrder2.CreateEditJobOrder(_factionID, _selectedEntity, _selectedProdLine,_selectedExistingConJob.JobID, jobcount, !jobrepeat);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
            {
                //new ConstructCancelJob(_state.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                var cmd = IndustryOrder2.CreateCancelJobOrder(_factionID, _selectedEntity, _selectedProdLine, _selectedExistingConJob.JobID);

                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (_lastClickedDesign != null)
            {
                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeInstalled)
                {
                    ImGui.Checkbox("Auto Install on colony", ref _newJobAutoInstall);
                    
                    if (_newJobAutoInstall)
                        _lastClickedJob.InstallOn = _selectedEntity;
                    else
                        _lastClickedJob.InstallOn = null;
                    
                }
                
                if (_lastClickedDesign.GuiHints == ConstructableGuiHints.CanBeLaunched)
                {
                    if (_selectedEntity.HasDataBlob<ColonyInfoDB>())
                    {
                        var s = (ShipDesign)_lastClickedDesign;
                        var planet = _selectedEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                        var lowOrbit = planet.GetDataBlob<MassVolumeDB>().RadiusInM * 0.33333;
                    
                        var mass = s.Mass;
                    
                        var fuelCost = OrbitMath.FuelCostToLowOrbit(planet, mass);

                    
                        if (ImGui.Button("Launch to Low Orbit"))
                        {
                            LaunchShipCmd.CreateCommand(_factionID, _selectedEntity, _selectedProdLine, _lastClickedJob.JobID);
                        }
                        //ImGui.SameLine();


                        ImGui.Text("Fuel Cost: " + fuelCost);
                    
                    }
                }
            }

            ImGui.EndGroup();
            
            //ImGui.Button("Install On Parent")
            
        }

        void NewJobDisplay()
        {
            //ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);
            if(_newjobSelectionIndex.pline != Guid.Empty)
            {
                int curItemIndex = _newjobSelectionIndex.item;

                var constructableNames = _contructablesByPline[_selectedProdLine].itemNames;

                if (ImGui.Combo("NewJobSelection", ref curItemIndex, constructableNames, constructableNames.Length))
                {
                    _newjobSelectionIndex = (_newjobSelectionIndex.pline, curItemIndex);
                    _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                }

                ImGui.InputInt("Batch Count", ref _newJobbatchCount);

                ImGui.Checkbox("Repeat Job", ref _newJobRepeat);
                ImGui.SameLine();
                //if the selected item can be installed on a colony:
                

                if (ImGui.Button("Create New Job"))
                {

                    _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);

                    var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, _selectedEntity, _selectedProdLine, _newConJob);
                    _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);
                    if(_newJobAutoInstall)
                        _newConJob.InstallOn = _selectedEntity;
                    _lastClickedJob = _newConJob;
                    _lastClickedDesign = _factionInfoDB.IndustryDesigns[SelectedConstrucableID];
                    StaticRefLib.OrderHandler.HandleOrder(cmd);
                }

            }

            //ImGui.EndChild();
        }
        
        void CostsDisplay(JobBase selectedJob)
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
                    cargoItem = _state.Faction.GetDataBlob<FactionInfoDB>().ComponentDesigns[item.Key];
                ImGui.Text(cargoItem.Name);
                ImGui.NextColumn();
                ImGui.Text(item.Value.ToString());
                ImGui.NextColumn();
            }
            
            ImGui.EndChild();
        }

    }
}

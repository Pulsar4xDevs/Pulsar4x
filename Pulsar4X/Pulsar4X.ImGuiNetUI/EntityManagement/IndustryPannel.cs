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
        private JobBase _selectedExistingConJob
        {
            get
            {
                if (_selectedProdLine != null && _prodLines.Count > 0)
                {
                    return _prodLines[_selectedProdLine].Jobs[_selectedExistingIndex];
                }

                return null;
            }
        }

        private JobBase _lastClickedJob;
        
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
            get { return _contructablesByPline[_newjobSelectionIndex.pline].itemIDs[_newjobSelectionIndex.item]; }
        }

        public void Display()
        {
  
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 285);
            ImGui.SetColumnWidth(1, 200);
            
            ProdLineDisplay();
            
            
            ImGui.NextColumn();
            ImGui.BeginGroup();
            EditButtonsDisplay();
            NewJobDisplay();
            ImGui.EndGroup();
            
            
                
        }

        public void ProdLineDisplay()
        {
            ImGui.BeginChild("prodline", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow );
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 128);

            foreach (var kvp in _prodLines)
            {
                IndustryAbilityDB.ProductionLine ud = kvp.Value;
                ImGui.PushID(kvp.Key.ToString());
                //ImGui.Selectable()
                if (ImGui.CollapsingHeader(ud.FacName))
                {
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
                }
                if (ImGui.IsItemClicked())
                {
                    _selectedProdLine = kvp.Key;
                    _newjobSelectionIndex = (_selectedProdLine, 0);
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

            ImGui.EndGroup();
            
        }

        void NewJobDisplay()
        {
            //ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);
            if(_newjobSelectionIndex.pline != Guid.Empty)
            {
                int curItem = _newjobSelectionIndex.item;

                var constructableNames = _contructablesByPline[_selectedProdLine].itemNames;

                if (ImGui.Combo("NewJobSelection", ref curItem, constructableNames, constructableNames.Length))
                {
                    _newjobSelectionIndex = (_selectedProdLine, curItem);
                    _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    _lastClickedJob = _newConJob;
                }

                ImGui.InputInt("Batch Count", ref _newJobbatchCount);

                ImGui.Checkbox("Repeat Job", ref _newJobRepeat);
                ImGui.SameLine();
                //if the selected item can be installed on a colony:
                

                if (ImGui.Button("Create New Job"))
                {
                    if (_newConJob == null) //make sure that a job has been created. 
                    {
                        _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                    }

                    var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, _selectedEntity, _selectedProdLine, _newConJob);
                    _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);

                    StaticRefLib.OrderHandler.HandleOrder(cmd);
                }

            }

            //ImGui.EndChild();
        }

    }
}



                    /*
                    
                    //ImGui.Text(ud.FacName + " points: " + ud.MaxPoints);
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 416);
                    ImGui.SetColumnWidth(1, 200);

                    ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
                    //ImGui.Text("Industry Output:" + _constrDB.ConstructionPoints);
                    ImGui.BeginChild("Current Jobs", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
                    Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
                    ImGui.Columns(2);
                    ImGui.SetColumnWidth(0, 128);

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

                    ImGui.EndChild();


                    ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);

                    int curItem = _newjobSelectionIndex;

                    if (ImGui.Combo("NewJobSelection", ref curItem, _constructablesNames, _constructablesNames.Length))
                    {
                        _newjobSelectionIndex = curItem;
                        _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                        _lastClickedJob = _newConJob;
                    }

                    ImGui.InputInt("Batch Count", ref _newJobbatchCount);

                    ImGui.Checkbox("Repeat Job", ref _newJobRepeat);
                    ImGui.SameLine();
                    //if the selected item can be installed on a colony:





                    if (ImGui.Button("Create New Job"))
                    {
                        if (_newConJob == null) //make sure that a job has been created. 
                        {
                            _newConJob = new IndustryJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                        }

                        var cmd = IndustryOrder2.CreateNewJobOrder(_factionID, _selectedEntity, _selectedProdLine, _newConJob);
                        _newConJob.InitialiseJob((ushort)_newJobbatchCount, _newJobRepeat);

                        StaticRefLib.OrderHandler.HandleOrder(cmd);
                    }



                    ImGui.EndChild();
                    */



                /*
                ImGui.SameLine();

                ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
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

                ImGui.EndGroup();
                */
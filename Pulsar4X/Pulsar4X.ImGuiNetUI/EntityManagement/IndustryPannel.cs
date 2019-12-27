using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Microsoft.Win32;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.SDL2UI;
using Vector3 = Pulsar4X.ECSLib.Vector3;


namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class IndustryPannel<T>where T: BaseDataBlob, IIndustryDB
    {
        private Guid _factionID;
        private ICargoable[] _constructableDesigns;
        string[] _constructablesNames;
        Guid[] _constructablesIDs;
        private JobBase _newConJob;
        private int _newjobSelectionIndex = 0;
        private int _newJobbatchCount = 1;
        private bool _newJobRepeat = false;
        private bool _newJobAutoInstall = true;

        private JobBase[] _existingJobList;
        private int _selectedExistingIndex = 0;
        private JobBase _selectedExistingConJob
        {
            get
            {
                if (_existingJobList.Length > 0)
                    return _existingJobList[_selectedExistingIndex];
                return null;
            }
        }

        private JobBase _lastClickedJob;
        
        private Entity _selectedEntity;
        private IIndustryDB _industryDB;
        private GlobalUIState _state;
        //private JobBase _job;
        public IndustryPannel(GlobalUIState state, Entity selectedEntity, T industryDB, JobBase job)
        {
            _state = state;
            _selectedEntity = selectedEntity;
            _industryDB = industryDB;
            //_job = job;
            var factionInfoDB = state.Faction.GetDataBlob<FactionInfoDB>();
            _factionID = state.Faction.Guid;
            var jobItems = _industryDB.GetJobItems(factionInfoDB);
            _constructablesNames = new string[jobItems.Count];
            _constructableDesigns = new ICargoable[jobItems.Count];
            _constructablesIDs = new Guid[jobItems.Count];
            for (int i = 0; i < jobItems.Count; i++)
            {
                _constructableDesigns[i] = jobItems[i];
                _constructablesIDs[i] = jobItems[i].ID;
                _constructablesNames[i] = jobItems[i].Name;
            }
            _existingJobList = _industryDB.JobBatchList.ToArray();
        }
        
        Guid SelectedConstrucableID
        {
            get { return _constructablesIDs[_newjobSelectionIndex]; }
        }

        public void Display()
        {
            
            ImGui.PushID(typeof(T).ToString());

            ImGui.BeginChild("Industry", new Vector2(600, 200));
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 416);
            ImGui.SetColumnWidth(1, 200);
            
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            //ImGui.Text("Industry Output:" + _constrDB.ConstructionPoints);
            ImGui.BeginChild("Current Jobs", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
            Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 128);
            _existingJobList = _industryDB.JobBatchList.ToArray(); //should maybe cache this and update on datechange
            for (int i = 0; i < _existingJobList.Length; i++)
            {
                var cpos = ImGui.GetCursorPos();
                var batchJob = _existingJobList[i];
                string jobname = _existingJobList[i].Name;
                
                bool selected = _selectedExistingIndex == i;
                float percent = 1 - (float)batchJob.ProductionPointsLeft / batchJob.ProductionPointsCost;
                ImGui.ProgressBar(percent, progsize, "");
                ImGui.SetCursorPos(cpos);
                if (ImGui.Selectable(jobname, ref selected))
                {
                    _selectedExistingIndex = i;
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
            ImGui.SameLine();

            ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();
            if (ImGui.ImageButton(_state.SDLImageDictionary["UpImg"], new Vector2(16, 8)))
            {

                var cmd = IndustryOrder<T>.CreateChangePriorityOrder
                    (_factionID, _selectedEntity, _selectedExistingConJob.JobID, -1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (ImGui.ImageButton(_state.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            {
                var cmd = IndustryOrder<T>.CreateChangePriorityOrder
                    (_factionID, _selectedEntity, _selectedExistingConJob.JobID, 1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
            {
                
                var jobcount = _selectedExistingConJob.NumberOrdered;
                var jobrepeat = _selectedExistingConJob.Auto;

                var cmd = IndustryOrder<T>.CreateEditJobOrder
                    (_factionID, _selectedEntity, _selectedExistingConJob.JobID, jobcount, !jobrepeat);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
            {
                //new ConstructCancelJob(_state.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedExistingConJob.JobID);
                var cmd = IndustryOrder<T>.CreateCancelJobOrder
                    (_factionID, _selectedEntity, _selectedExistingConJob.JobID);

                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            switch (_selectedExistingConJob)
            {
                case RefineingJob r:
                    RefinarySpecificExisting(r);
                    break;
                case ConstructJob c:
                    ComponentSpecificExisting(c);
                    break;
                case ShipYardJob s:
                    ShipyardSpecificExisting(s);
                    break;
            }


            ImGui.EndGroup();

            ImGui.EndChild();

            ImGui.BeginChild("InitialiseJob", new Vector2(404, 84), true, ImGuiWindowFlags.ChildWindow);

            int curItem = _newjobSelectionIndex;

            if (ImGui.Combo("NewJobSelection", ref curItem, _constructablesNames, _constructablesNames.Length))
            {
                _newjobSelectionIndex = curItem;
                _lastClickedJob = _newConJob;
                switch (_industryDB)
                {
                    case RefineAbilityDB r:
                        _newConJob = new RefineingJob();
                        break;
                    case ConstructAbilityDB c:
                        _newConJob = new ConstructJob(_state.Faction.GetDataBlob<FactionInfoDB>(), SelectedConstrucableID);
                        break;
                    case ShipYardAbilityDB s:
                        _newConJob = new ShipYardJob();
                        break;
                }
            }

            ImGui.InputInt("Batch Count", ref _newJobbatchCount);
            
            ImGui.Checkbox("Repeat Job", ref _newJobRepeat);
            ImGui.SameLine();
            //if the selected item can be installed on a colony:

            switch (_industryDB)
            {
                case RefineAbilityDB r:
                    RefinarySpecificNew(r);
                    break;
                case ConstructAbilityDB c:
                    ComponentSpecificNew(c);
                    break;
                case ShipYardAbilityDB s:
                    ShipSpecificNew(s);
                    break;
            }
            

            
            if (ImGui.Button("Create New Job"))
            {
                if (_newConJob == null) //make sure that a job has been created. 
                {
                    switch (_industryDB)
                    {
                        case RefineAbilityDB r:
                            _newConJob = new RefineingJob();
                            break;
                        case ConstructAbilityDB c:
                            _newConJob = new ConstructJob();
                            break;
                        case ShipYardAbilityDB s:
                            _newConJob = new ShipYardJob();
                            break;
                    }
                }

                var cmd = IndustryOrder<T>.CreateNewJobOrder(_state.Faction.Guid, _selectedEntity, _newConJob);
                _newConJob.InitialiseJob(_state.Faction.GetDataBlob<FactionInfoDB>(), _selectedEntity, SelectedConstrucableID, (ushort)_newJobbatchCount, _newJobRepeat );
                
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            
            

            ImGui.EndChild();
            
            ImGui.NextColumn();
            if(_lastClickedJob != null)
            {
                CostsDisplay(_lastClickedJob);
                ImGui.NextColumn();
            }
            
            
            
            
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopID();
            
        }

        void RefinarySpecificNew(RefineAbilityDB r)
        {
        }

        void ComponentSpecificNew(ConstructAbilityDB c)
        {
            if(_newConJob != null)
            {
                ConstructJob job = (ConstructJob)_newConJob;
                if (job.ConstructionType.HasFlag(ConstructionType.Installations))
                {
                    ImGui.Checkbox("Auto Install on colony", ref _newJobAutoInstall);
                    if (_newJobAutoInstall)
                        job.InstallOn = _selectedEntity;
                    else
                        job.InstallOn = null;
                    ImGui.SameLine();
                }
            }
        }

        void ShipSpecificNew(ShipYardAbilityDB s)
        {

        }
        
        void RefinarySpecificExisting(RefineingJob r)
        {
        }
        void ComponentSpecificExisting(ConstructJob c)
        {
        }
        void ShipyardSpecificExisting(ShipYardJob s)
        {

            if(s.ProductionPointsLeft == 0)
            {
                if (_selectedEntity.HasDataBlob<ColonyInfoDB>())
                {
                    var planet = _selectedEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
                    var lowOrbit = planet.GetDataBlob<MassVolumeDB>().RadiusInM * 0.33333;
                    
                    var mass = s.ShipDesign.Mass;
                    var exaustVelocity = 275;
                    var sgp = OrbitMath.CalculateStandardGravityParameter(s.ShipDesign.Mass, planet.GetDataBlob<MassVolumeDB>().Mass);
                    Vector3 pos = new Vector3(lowOrbit, 0, 0);
                    double alt = lowOrbit - planet.GetDataBlob<MassVolumeDB>().RadiusInM;
                    var vel = OrbitMath.ObjectLocalVelocityPolar(sgp, pos, lowOrbit, 0, 0, 0);
                    var fuelCost = OrbitMath.TsiolkovskyFuelCost(mass, exaustVelocity, vel.speed);

                    
                    if (ImGui.Button("Launch to Low Orbit"))
                    {


                    }
                    //ImGui.SameLine();


                    ImGui.Text("Fuel Cost: " + fuelCost);
                    
                }
            }
            
        }

        void CostsDisplay(JobBase selectedJob)
        {
            ImGui.BeginChild("Resources Requred", new Vector2(180, 184 ), true, ImGuiWindowFlags.ChildWindow);
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 140);
            ImGui.SetColumnWidth(1, 40);
            foreach (var item in selectedJob.ResourcesRequired)
            {
                ICargoable cargoItem = StaticRefLib.StaticData.CargoGoods.GetAny(item.Key);

                ImGui.Text(cargoItem.Name);
                ImGui.NextColumn();
                ImGui.Text(item.Value.ToString());
                ImGui.NextColumn();
            }
            
            ImGui.EndChild();
        }
        
    }
}
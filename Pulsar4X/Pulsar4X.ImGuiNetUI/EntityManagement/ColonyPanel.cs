using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.ImGuiNetUI.EntityManagement;
using Vector2 = System.Numerics.Vector2;


namespace Pulsar4X.SDL2UI
{
    public class ColonyPanel : PulsarGuiWindow
    {
        EntityState _selectedEntity;
        CargoStorageVM _storeVM;
        RefiningVM _refineryVM;
        private RefineAbilityDB _refineDB;
        private ConstructAbilityDB _constrDB;
        private ShipYardAbilityDB _shipYardDB;
        private FactionInfoDB _factionInfoDB;

        private IndustryPannel<RefineAbilityDB, RefineingJob> _refinaryIndustryPannel;
        private IndustryPannel<ConstructAbilityDB, ConstructJob> _construcIndustryPannel;
        private IndustryPannel<ShipYardAbilityDB, ShipYardJob> _shipYardIndustryPannel;

        CargoListPannelSimple _cargoList;
        StaticDataStore _staticData;
        private ColonyPanel(StaticDataStore staticData, EntityState selectedEntity)
        {
            _selectedEntity = selectedEntity;
            _cargoList = new CargoListPannelSimple(staticData, selectedEntity);
            _staticData = staticData;
        }

        public static ColonyPanel GetInstance(StaticDataStore staticData, EntityState selectedEntity)
        {
            ColonyPanel instance;
            if (selectedEntity.CmdRef == null)
               selectedEntity.CmdRef = CommandReferences.CreateForEntity(_state.Game, selectedEntity.Entity);
            if (!_state.LoadedWindows.ContainsKey(typeof(ColonyPanel)))
            {
                instance = new ColonyPanel(staticData, selectedEntity);
                instance.HardRefresh();
                return instance;
            }
            instance = (ColonyPanel)_state.LoadedWindows[typeof(ColonyPanel)];
            if (instance._selectedEntity != selectedEntity)
            {
                instance._selectedEntity = selectedEntity;
                instance.HardRefresh();


            }


            return instance;
        }

        internal override void Display()
        {

            //resources list include refined or seperate?
            //factories/installations list - expandable to show health and disable/enable specific installations
            //mining stats pannel. 
            //refinary panel, expandable?
            //construction pannel, expandable?
            //constructed but not installed components. 
            //installation pannel (install constructed components
            if (IsActive)
            {
                if (ImGui.Begin("Cargo", ref IsActive, _flags))
                {

                    if (_storeVM != null)
                    {
                        /*
                        ImGui.BeginGroup();
                        foreach (var storetype in _storeVM.CargoResourceStores)
                        {
                            if (ImGui.CollapsingHeader(storetype.HeaderText + "###" + storetype.StorageTypeName, ImGuiTreeNodeFlags.CollapsingHeader))
                            {
                                foreach (var item in storetype.CargoItems)
                                {
                                    ImGui.Text(item.ItemName);
                                    ImGui.SameLine();
                                    ImGui.Text(item.ItemWeightPerUnit);
                                    ImGui.SameLine();
                                    ImGui.Text(item.NumberOfItems);
                                    ImGui.SameLine();
                                    ImGui.Text(item.TotalWeight);

                                }

                            }
                        }
                        ImGui.EndGroup();
                        */
                        _cargoList.Display();                       
                    }


                    if (_refinaryIndustryPannel != null && ImGui.CollapsingHeader("Refinary Points: " + _refineDB.PointsPerTick))
                    {
                        _refinaryIndustryPannel.Display();
                    }

                    if (_construcIndustryPannel != null && ImGui.CollapsingHeader("Construction Points: " + _constrDB.PointsPerTick))
                    {
                        _construcIndustryPannel.Display();
                    }
                    
                    if (_shipYardIndustryPannel != null && ImGui.CollapsingHeader("Construction Points: " + _shipYardDB.PointsPerTick))
                    {
                        _shipYardIndustryPannel.Display();
                    }
                }
                ImGui.End();
            }
        }

        
        
        /*
        void RefinaryDisplay()
        {
            ImGui.PushID("refinary");
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            ImGui.BeginChild("Current Jobs", new System.Numerics.Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
            Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
            foreach (var job in _refineryVM.CurrentJobs.ToArray())
            {

                bool selected = false;
                if (job == _refineryVM.CurrentJobSelectedItem)
                    selected = true;

                float percent = 1 - (float)job.ProductionPointsLeft / job.ProductionPointsCost;
                var cpos = ImGui.GetCursorPos();
                ImGui.ProgressBar(percent, progsize, "");
                ImGui.SetCursorPos(cpos);
                if (ImGui.Selectable(job.SingleLineText, ref selected))
                {
                    _refineryVM.CurrentJobSelectedItem = job;
                }

                if (job.Repeat)
                {
                    ImGui.SameLine();
                    ImGui.Image(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16));
                }
            }
            
            ImGui.EndChild();
            ImGui.SameLine();

            ImGui.BeginChild("Buttons", new Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
            ImGui.BeginGroup();
            if (ImGui.ImageButton(_state.SDLImageDictionary["UpImg"], new Vector2(16, 8)))
            { _refineryVM.CurrentJobSelectedItem.ChangePriority(-1); }
            if (ImGui.ImageButton(_state.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            { _refineryVM.CurrentJobSelectedItem.ChangePriority(1); }
            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
                _refineryVM.CurrentJobSelectedItem.ChangeRepeat(!_refineryVM.CurrentJobSelectedItem.Repeat);
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
                _refineryVM.CurrentJobSelectedItem.CancelJob();



            ImGui.EndGroup();

            ImGui.EndChild();

            ImGui.BeginChild("InitialiseJob", new Vector2(0, 84), true, ImGuiWindowFlags.ChildWindow);

            int curItem = _refineryVM.NewJobSelectedIndex;
            if (ImGui.Combo("NewJobSelection", ref curItem, _refineryVM.ItemDictionary.DisplayList.ToArray(), _refineryVM.ItemDictionary.Count))
            {
                _refineryVM.ItemDictionary.SelectedIndex = curItem;
            }
            int batchCount = _refineryVM.NewJobBatchCount;
            if (ImGui.InputInt("Batch Count", ref batchCount))
                _refineryVM.NewJobBatchCount = (ushort)batchCount;
            bool repeatJob = _refineryVM.NewJobRepeat;
            if (ImGui.Checkbox("Repeat Job", ref repeatJob))
            {
                _refineryVM.NewJobRepeat = repeatJob;
            }
            ImGui.SameLine();
            if (ImGui.Button("Create New Job"))
            {
                _refineryVM.OnNewBatchJob();
            }

            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopID();
                        
        }


        private ComponentDesign[] _constructableDesigns;
        string[] _constructablesNames;
        Guid[] _constructablesIDs;
        private int _selectedIndex = 0;
        private ConstructJob _selectedConJob;
        private int _newjobSelectionIndex = 0;
        private int _newbatchCount = 1;
        private bool _newbatchRepeat = false;
        private bool _autoInstall = true;
        void ConstructionDisplay()
        {
            ImGui.PushID("construction");
            
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            //ImGui.Text("Industry Output:" + _constrDB.PointsPerTick);
            ImGui.BeginChild("Current Jobs", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
            Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 128);
            var joblist = _constrDB.JobBatchList.ToArray();
            for (int i = 0; i < joblist.Length; i++)
            {
                var cpos = ImGui.GetCursorPos();
                var batchJob = joblist[i];
                string jobname = joblist[i].Name;
                
                bool selected = _selectedIndex == i;
                float percent = 1 - (float)batchJob.ProductionPointsLeft / batchJob.ProductionPointsCost;
                ImGui.ProgressBar(percent, progsize, "");
                ImGui.SetCursorPos(cpos);
                if (ImGui.Selectable(jobname, ref selected))
                {
                    _selectedConJob = (ConstructJob)joblist[i];
                    _selectedIndex = i;
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
                var cmd = new ConstructRePrioritizeCommand(_state.Faction.Guid, _selectedEntity.Entity.Guid, _selectedEntity.Entity.StarSysDateTime, _selectedConJob.JobID, -1);
                _selectedEntity.CmdRef.Handler.HandleOrder(cmd);
            }

            if (ImGui.ImageButton(_state.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            {
                var cmd = new ConstructRePrioritizeCommand(_state.Faction.Guid, _selectedEntity.Entity.Guid, _selectedEntity.Entity.StarSysDateTime, _selectedConJob.JobID, 1);
                _selectedEntity.CmdRef.Handler.HandleOrder(cmd);
            }
            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
            {
                var cmd = new ConstructChangeRepeatJob(_state.Faction.Guid, _selectedEntity.Entity.Guid, _selectedEntity.Entity.StarSysDateTime, _selectedConJob.JobID, !_selectedConJob.Auto);
                _selectedEntity.CmdRef.Handler.HandleOrder(cmd);
            }
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
            {
                var cmd = new ConstructCancelJob(_state.Faction.Guid, _selectedEntity.Entity.Guid, _selectedEntity.Entity.StarSysDateTime, _selectedConJob.JobID);
                _selectedEntity.CmdRef.Handler.HandleOrder(cmd);
            }



            ImGui.EndGroup();

            ImGui.EndChild();

            ImGui.BeginChild("InitialiseJob", new Vector2(0, 84), true, ImGuiWindowFlags.ChildWindow);

            int curItem = _newjobSelectionIndex;

            if (ImGui.Combo("NewJobSelection", ref curItem, _constructablesNames, _constructablesNames.Length))
            {
                _newjobSelectionIndex = curItem;
            }

            ImGui.InputInt("Batch Count", ref _newbatchCount);
            
            ImGui.Checkbox("Repeat Job", ref _newbatchRepeat);
            ImGui.SameLine();
            //if the selected item can be installed on a colony:
            if (_constructableDesigns[_newjobSelectionIndex].ConstructionType.HasFlag(ConstructionType.Installations))
            {
                ImGui.Checkbox("Auto Install on colony", ref _autoInstall);
                ImGui.SameLine();
            }
            if (ImGui.Button("Create New Job"))
            {
                var cmd = new ConstructItemCommand
                (
                    _state.Faction.Guid, 
                    _selectedEntity.Entity.Guid, 
                    _selectedEntity.Entity.StarSysDateTime, 
                    _constructablesIDs[_newjobSelectionIndex], 
                    (ushort)_newbatchCount, 
                    _newbatchRepeat
                    
                );
                _selectedEntity.CmdRef.Handler.HandleOrder(cmd);
            }

            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopID();
        }

        
        
        
        
        */
        
        
        
        
        internal void HardRefresh()
        {
            _factionInfoDB = _state.Faction.GetDataBlob<FactionInfoDB>();
            if (_selectedEntity.Entity.HasDataBlob<CargoStorageDB>())
            {
                var storeDB = _selectedEntity.Entity.GetDataBlob<CargoStorageDB>();
                _storeVM = new CargoStorageVM(_state.Game.StaticData, storeDB);
                _storeVM.SetUpdateListner(_selectedEntity.Entity.Manager.ManagerSubpulses);

            }

            if (_selectedEntity.Entity.HasDataBlob<RefineAbilityDB>())
            {
                _refineDB = _selectedEntity.Entity.GetDataBlob<RefineAbilityDB>();
                RefineingJob rjob = new RefineingJob();
                _refinaryIndustryPannel = new IndustryPannel<RefineAbilityDB, RefineingJob>(_state, _selectedEntity.Entity, _refineDB, rjob);
            }
            else
            {
                _refineDB = null;
                _refinaryIndustryPannel = null;
            }
            if(_selectedEntity.Entity.HasDataBlob<ConstructAbilityDB>())
            {
                _constrDB = _selectedEntity.Entity.GetDataBlob<ConstructAbilityDB>();
                ConstructJob cjob = new ConstructJob();
                _construcIndustryPannel = new IndustryPannel<ConstructAbilityDB, ConstructJob>(_state, _selectedEntity.Entity, _constrDB, cjob);
                
            }
            else
            {
                _constrDB = null;
                _construcIndustryPannel = null;
            }
            if(_selectedEntity.Entity.HasDataBlob<ShipYardAbilityDB>())
            {
                _shipYardDB = _selectedEntity.Entity.GetDataBlob<ShipYardAbilityDB>();
                ShipYardJob sjob = new ShipYardJob();
                _shipYardIndustryPannel = new IndustryPannel<ShipYardAbilityDB, ShipYardJob>(_state, _selectedEntity.Entity, _shipYardDB, sjob);
                
            }
            else
            {
                _shipYardDB = null;
                _shipYardIndustryPannel = null;
            }
            _cargoList = new CargoListPannelSimple(_staticData, _selectedEntity);

            /*
            lock (_factionInfoDB.ComponentDesigns)
            {
                int num = _factionInfoDB.ComponentDesigns.Count;
                _constructableDesigns = new ComponentDesign[num];
                _constructablesNames = new string[num]; 
                _constructablesIDs = new Guid[num];
                int i = 0;
                foreach (var design in _factionInfoDB.ComponentDesigns)
                {
                    _constructableDesigns[i] = design.Value;
                    _constructablesIDs[i] = design.Key;
                    _constructablesNames[i] = design.Value.Name;
                    i++;
                }

                
            }*/

        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }
}

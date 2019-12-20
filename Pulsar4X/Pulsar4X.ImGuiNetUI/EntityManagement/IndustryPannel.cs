using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.SDL2UI;


namespace Pulsar4X.ImGuiNetUI.EntityManagement
{
    public class IndustryPannel<T,U>where T: BaseDataBlob, IIndustryDB where U: JobBase
    {
        
        private ICargoable[] _constructableDesigns;
        string[] _constructablesNames;
        Guid[] _constructablesIDs;
        private int _selectedIndex = 0;
        private JobBase _selectedConJob;
        private int _newjobSelectionIndex = 0;
        private int _newbatchCount = 1;
        private bool _newbatchRepeat = false;
        private bool _autoInstall = true;
        private Entity _selectedEntity;
        private IIndustryDB _industryDB;
        private GlobalUIState _state;
        private U _job;
        public IndustryPannel(GlobalUIState state, Entity selectedEntity, T industryDB, U job)
        {
            _state = state;
            _selectedEntity = selectedEntity;
            _industryDB = industryDB;
            _job = job;
            var factionInfoDB = state.Faction.GetDataBlob<FactionInfoDB>();
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
        }
        
        Guid SelectedConstrucableID
        {
            get { return _constructablesIDs[_newjobSelectionIndex]; }
        }

        public void Display()
        {
            
            ImGui.PushID(_job.ToString());
            
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            //ImGui.Text("Industry Output:" + _constrDB.PointsPerTick);
            ImGui.BeginChild("Current Jobs", new Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);
            Vector2 progsize = new Vector2(128, ImGui.GetTextLineHeight());
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 128);
            var joblist = _industryDB.JobBatchList.ToArray();
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
                    _selectedConJob = joblist[i];
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

                var cmd = IndustryOrder<T,U>.CreateChangePriorityOrder
                    (_state.Faction.Guid, _selectedEntity, _selectedConJob.JobID, -1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }

            if (ImGui.ImageButton(_state.SDLImageDictionary["DnImg"], new Vector2(16, 8)))
            {
                var cmd = IndustryOrder<T,U>.CreateChangePriorityOrder
                    (_state.Faction.Guid, _selectedEntity, _selectedConJob.JobID, 1);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            ImGui.EndGroup();
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["RepeatImg"], new Vector2(16, 16)))
            {
                var cmd = new ConstructChangeRepeatJob(_state.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedConJob.JobID, !_selectedConJob.Auto);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            ImGui.SameLine();
            if (ImGui.ImageButton(_state.SDLImageDictionary["CancelImg"], new Vector2(16, 16)))
            {
                var cmd = new ConstructCancelJob(_state.Faction.Guid, _selectedEntity.Guid, _selectedEntity.StarSysDateTime, _selectedConJob.JobID);
                StaticRefLib.OrderHandler.HandleOrder(cmd);
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

            switch (_constructableDesigns[_newjobSelectionIndex])
            {
                case RefineingJob j:
                    RefinarySpecific(j);
                    break;
                case ConstructJob c:
                    ComponentSpecific(c);
                    break;
                case ShipYardJob s:
                    ShipSpecific(s);
                    break;
            }
            

            
            if (ImGui.Button("Create New Job"))
            {
                var cmd = IndustryOrder<T, U>.CreateNewJobOrder(_state.Faction.Guid, _selectedEntity, _job);
                _job.InitialiseJob(_state.Faction.GetDataBlob<FactionInfoDB>(), _selectedEntity, SelectedConstrucableID, (ushort)_newbatchCount, _newbatchRepeat );
                
                StaticRefLib.OrderHandler.HandleOrder(cmd);
            }
            
            

            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopID();
        }

        void RefinarySpecific(RefineingJob j)
        {
        }

        void ComponentSpecific(ConstructJob c)
        {
            if (c.ConstructionType.HasFlag(ConstructionType.Installations))
            {
                ImGui.Checkbox("Auto Install on colony", ref _autoInstall);
                ImGui.SameLine();
            }
        }

        void ShipSpecific(ShipYardJob s)
        {
        }
    }
}
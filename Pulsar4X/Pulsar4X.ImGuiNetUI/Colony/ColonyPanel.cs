using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;


namespace Pulsar4X.SDL2UI
{
    public class ColonyPanel : PulsarGuiWindow
    {
        EntityState _selectedEntity;
        CargoStorageVM _storeVM;
        RefiningVM _refineryVM;
        private ColonyPanel(EntityState selectedEntity)
        {
            _selectedEntity = selectedEntity;
        }

        public static ColonyPanel GetInstance(EntityState selectedEntity)
        {
            ColonyPanel instance;
            if (selectedEntity.CmdRef == null)
                CommandReferences.CreateForEntity(_state.Game, selectedEntity.Entity);
            if (!_state.LoadedWindows.ContainsKey(typeof(ColonyPanel)))
            {
                instance = new ColonyPanel(selectedEntity);
                instance.HardRefresh();
                return instance;
            }
            instance = (ColonyPanel)_state.LoadedWindows[typeof(ColonyPanel)];
            instance._selectedEntity = selectedEntity;

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
            if (ImGui.Begin("Cargo", ref IsActive, _flags))
            {

                if (_storeVM != null)
                {
                    ImGui.BeginGroup();
                    foreach (var storetype in _storeVM.CargoResourceStores)
                    {
                        if(ImGui.CollapsingHeader(storetype.HeaderText + "###" + storetype.StorageTypeName, ImGuiTreeNodeFlags.CollapsingHeader))
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
                }


                if(_refineryVM != null)
                {

                    if(ImGui.CollapsingHeader("Refinary"))
                    {
                        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
                        ImGui.BeginChild("Current Jobs", new System.Numerics.Vector2(0, 100), true, ImGuiWindowFlags.ChildWindow);
                        foreach (var job in _refineryVM.CurrentJobs)
                        {
                            ImGui.Text(job.Item);
                            ImGui.SameLine();
                            ImGui.Text(job.BatchQuantity.ToString());
                            ImGui.Text(job.ItemPercentRemaining.ToString());

                        }
                        ImGui.EndChild();

                        ImGui.BeginChild("CreateJob", new System.Numerics.Vector2(0,84), true, ImGuiWindowFlags.ChildWindow);

                        int curItem = _refineryVM.NewJobSelectedIndex;
                        if(ImGui.Combo("NewJobSelection", ref curItem, _refineryVM.ItemDictionary.DisplayList.ToArray(), _refineryVM.ItemDictionary.Count))
                        {
                            _refineryVM.ItemDictionary.SelectedIndex = curItem;
                        }
                        int batchCount = _refineryVM.NewJobBatchCount;
                        if (ImGui.InputInt("Batch Count", ref batchCount))
                            _refineryVM.NewJobBatchCount = (ushort)batchCount;
                        bool repeatJob = _refineryVM.NewJobRepeat;
                        if(ImGui.Checkbox("Repeat Job", ref repeatJob))
                        {
                            _refineryVM.NewJobRepeat = repeatJob;
                        }
                        ImGui.SameLine();
                        if(ImGui.Button("Create New Job"))
                        {
                            _refineryVM.OnNewBatchJob();
                        }

                        ImGui.EndChild();
                        ImGui.PopStyleVar();
                    }

                }
            }
            ImGui.End();
        }

        internal void HardRefresh()
        {
            if (_selectedEntity.Entity.HasDataBlob<CargoStorageDB>())
            {
                var storeDB = _selectedEntity.Entity.GetDataBlob<CargoStorageDB>();
                _storeVM = new CargoStorageVM(_state.Game.StaticData, storeDB);
                _storeVM.SetUpdateListner(_selectedEntity.Entity.Manager.ManagerSubpulses);

            }
            if(_selectedEntity.Entity.HasDataBlob<RefiningDB>())
            {
                var refinaryDB = _selectedEntity.Entity.GetDataBlob<RefiningDB>();
                _refineryVM = new RefiningVM(_state.Game, _selectedEntity.CmdRef, refinaryDB);
                _refineryVM.SetUpdateListner(_selectedEntity.Entity.Manager.ManagerSubpulses);
            }
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }
}

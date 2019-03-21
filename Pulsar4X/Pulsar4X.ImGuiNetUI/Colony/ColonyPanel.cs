using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;


namespace Pulsar4X.SDL2UI
{
    public class ColonyPanel : PulsarGuiWindow
    {
        EntityState _selectedEntity;
        CargoStorageVM _storeVM;
        RefiningVM _refineryVM;
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


                    if (_refineryVM != null)
                    {

                        if (ImGui.CollapsingHeader("Refinary"))
                        {
                            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
                            ImGui.BeginChild("Current Jobs", new System.Numerics.Vector2(280, 100), true, ImGuiWindowFlags.ChildWindow);

                            foreach (var job in _refineryVM.CurrentJobs.ToArray())
                            {

                                bool selected = false;
                                if (job == _refineryVM.CurrentJobSelectedItem)
                                    selected = true;

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

                            ImGui.BeginChild("Buttons", new System.Numerics.Vector2(116, 100), true, ImGuiWindowFlags.ChildWindow);
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

                            ImGui.BeginChild("CreateJob", new System.Numerics.Vector2(0, 84), true, ImGuiWindowFlags.ChildWindow);

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
                        }

                    }
                }
                ImGui.End();
            }
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
            _cargoList = new CargoListPannelSimple(_staticData, _selectedEntity);
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }
}

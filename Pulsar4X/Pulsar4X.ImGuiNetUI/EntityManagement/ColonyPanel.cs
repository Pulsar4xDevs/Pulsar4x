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
        private FactionInfoDB _factionInfoDB;

        private IndustryAbilityDB _industryDB;
        private IndustryPannel2 _industryPannel;

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
                _flags = ImGuiWindowFlags.AlwaysAutoResize;
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

                    if (_industryPannel != null)// && ImGui.CollapsingHeader("Refinary Points: " + _industryDB.ConstructionPoints))
                    {
                        _industryPannel.Display();
                    }
                    /*

                    if (_refinaryIndustryPannel != null && ImGui.CollapsingHeader("Refinary Points: " + _refineDB.ConstructionPoints))
                    {
                        _refinaryIndustryPannel.Display();
                    }

                    if (_construcIndustryPannel != null && ImGui.CollapsingHeader("Construction Points: " + _constrDB.ConstructionPoints))
                    {
                        _construcIndustryPannel.Display();
                    }
                    
                    if (_shipYardIndustryPannel != null && ImGui.CollapsingHeader("Construction Points: " + _shipYardDB.ConstructionPoints))
                    {
                        _shipYardIndustryPannel.Display();
                    }*/
                }
                ImGui.End();
            }
        }
        
        
        internal void HardRefresh()
        {
            _factionInfoDB = _state.Faction.GetDataBlob<FactionInfoDB>();
            if (_selectedEntity.Entity.HasDataBlob<CargoStorageDB>())
            {
                var storeDB = _selectedEntity.Entity.GetDataBlob<CargoStorageDB>();
                _storeVM = new CargoStorageVM(_state.Game.StaticData, storeDB);
                _storeVM.SetUpdateListner(_selectedEntity.Entity.Manager.ManagerSubpulses);

            }

            if (_selectedEntity.Entity.HasDataBlob<IndustryAbilityDB>())
            {
                _industryDB = _selectedEntity.Entity.GetDataBlob<IndustryAbilityDB>();
                _industryPannel = new IndustryPannel2(_state, _selectedEntity.Entity, _industryDB);
            }
            else
            {
                _industryDB = null;
                _industryPannel = null;
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

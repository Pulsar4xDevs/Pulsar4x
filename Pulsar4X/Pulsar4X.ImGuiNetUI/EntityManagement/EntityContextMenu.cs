using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.SDL2UI;

namespace Pulsar4X.SDL2UI
{

    public class EntityContextMenu
    {

        GlobalUIState _state;
        internal Entity ActiveEntity; //interacting with/ordering this entity
        Vector2 buttonSize = new Vector2(100, 12);

        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, Guid entityGuid)
        {
            _state = state;
            //_state.OpenWindows.Add(this);
            //IsActive = true;
            _entityState = state.StarSystemStates[state.SelectedStarSysGuid].EntityStatesWithNames[entityGuid];

        }

        internal void Display()
        {
            ActiveEntity = _entityState.Entity;
            ImGui.BeginGroup();

            
            if(EntityUIWindows.checkIfCanOpenWindow<PinCameraBlankMenuHelper>( _entityState)){
            
                EntityUIWindows.openUIWindow<PinCameraBlankMenuHelper>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(PinCameraBlankMenuHelper)]), _entityState, _state, true);
            }
            if (EntityUIWindows.checkIfCanOpenWindow<GotoSystemBlankMenuHelper>(_entityState))
            {
                EntityUIWindows.openUIWindow<GotoSystemBlankMenuHelper>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(GotoSystemBlankMenuHelper)]), _entityState, _state, true);
            }
            if (EntityUIWindows.checkIfCanOpenWindow<OrbitOrderWindow>(_entityState)){
            
                EntityUIWindows.openUIWindow<OrbitOrderWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]), _entityState, _state, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<ChangeCurrentOrbitWindow>(_entityState)){
            
                EntityUIWindows.openUIWindow<ChangeCurrentOrbitWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]), _entityState, _state, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<WeaponTargetingControl>(_entityState)){
            
                EntityUIWindows.openUIWindow<WeaponTargetingControl>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]), _entityState, _state, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<RenameWindow>( _entityState)){
            
                EntityUIWindows.openUIWindow<RenameWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(RenameWindow)]), _entityState, _state, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<CargoTransfer>(_entityState)){
            
                EntityUIWindows.openUIWindow<CargoTransfer>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(CargoTransfer)]), _entityState, _state, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<ColonyPanel>( _entityState)){
            
                EntityUIWindows.openUIWindow<ColonyPanel>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ColonyPanel)]), _entityState, _state, true);
            }
            /*
            if (ImGui.SmallButton("Pin Camera"))
            {
                _state.Camera.PinToEntity(_entityState.Entity);
                ImGui.CloseCurrentPopup();
            }

            //if entity can warp
            if (_entityState.Entity.HasDataBlob<WarpAbilityDB>())
            {
                if (ImGui.SmallButton("Translate to a new orbit"))
                {
                    OrbitOrderWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = OrbitOrderWindow.GetInstance(_entityState);
                }
            }
            //if entity can move
            if (_entityState.Entity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                if (ImGui.SmallButton("Change current orbit"))
                {
                    ChangeCurrentOrbitWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = ChangeCurrentOrbitWindow.GetInstance(_entityState);
                }
            }
            //if entity can fire?
            if (_entityState.Entity.HasDataBlob<FireControlAbilityDB>())
            {
                if (ImGui.SmallButton("Fire Control"))
                {
                    var instance = WeaponTargetingControl.GetInstance(_entityState);
                    instance.SetOrderEntity(_entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
            //if entity can be renamed?
            if (ImGui.SmallButton("Rename"))
            {
                RenameWindow.GetInstance(_entityState).IsActive = true;
                _state.ActiveWindow = RenameWindow.GetInstance(_entityState);
                ImGui.CloseCurrentPopup();
            }
            //if entity can target

            if (_entityState.Entity.HasDataBlob<CargoStorageDB>())
            {
                if (ImGui.SmallButton("Cargo"))
                {
                    var instance = CargoTransfer.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                }
            }
            //if entity can mine || refine || build
            //econOrderwindow

            if (_entityState.Entity.HasDataBlob<ColonyInfoDB>()) 
            { 
                if (ImGui.SmallButton("Econ"))
                {
                    var instance = ColonyPanel.GetInstance(_state.Game.StaticData, _entityState);
                    instance.IsActive = true;
                    _state.ActiveWindow = instance;
                } 
            }*/

            ImGui.EndGroup();

        }
    }
}

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
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(PinCameraBlankMenuHelper)]);
                EntityUIWindows.openUIWindow(typeof(PinCameraBlankMenuHelper), _entityState, _state, buttonresult ,true);
            }
            if (EntityUIWindows.checkIfCanOpenWindow<GotoSystemBlankMenuHelper>(_entityState))
            {
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(GotoSystemBlankMenuHelper)]);
                EntityUIWindows.openUIWindow(typeof(GotoSystemBlankMenuHelper), _entityState, _state, buttonresult, true);
            }
            if (EntityUIWindows.checkIfCanOpenWindow<SelectPrimaryBlankMenuHelper>(_entityState))
            {
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(SelectPrimaryBlankMenuHelper)]);
                EntityUIWindows.openUIWindow(typeof(SelectPrimaryBlankMenuHelper), _entityState, _state, buttonresult, true); 
            }
            if (EntityUIWindows.checkIfCanOpenWindow<OrbitOrderWindow>(_entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]);
                EntityUIWindows.openUIWindow(typeof(OrbitOrderWindow), _entityState, _state, buttonresult, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<ChangeCurrentOrbitWindow>(_entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]);
                EntityUIWindows.openUIWindow(typeof(ChangeCurrentOrbitWindow), _entityState, _state, buttonresult, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<WeaponTargetingControl>(_entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]);
                EntityUIWindows.openUIWindow(typeof(WeaponTargetingControl), _entityState, _state, buttonresult, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<RenameWindow>( _entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(RenameWindow)]);
                EntityUIWindows.openUIWindow(typeof(RenameWindow), _entityState, _state, buttonresult, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<CargoTransfer>(_entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(CargoTransfer)]);
                EntityUIWindows.openUIWindow(typeof(CargoTransfer), _entityState, _state, buttonresult, true);
            }
            if(EntityUIWindows.checkIfCanOpenWindow<ColonyPanel>( _entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ColonyPanel)]);
                EntityUIWindows.openUIWindow(typeof(ColonyPanel), _entityState, _state, buttonresult, true);
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

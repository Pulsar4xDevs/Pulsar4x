using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{
    public class EntityUIWindowSelector : PulsarGuiWindow
    {
        private EntityUIWindowSelector()
        {
            _flags =  ImGuiWindowFlags.NoCollapse;

        }

        internal static EntityUIWindowSelector GetInstance()
        {

            EntityUIWindowSelector thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(EntityUIWindowSelector)))
            {
                thisItem = new EntityUIWindowSelector();
            }
            else
            {
                thisItem = (EntityUIWindowSelector)_state.LoadedWindows[typeof(EntityUIWindowSelector)];
            }


            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
            ImGui.SetNextWindowSize(new Vector2(150, 200));
            if (ImGui.Begin("Actions", _flags))
            {
                //check if ANY entity has been clicked
                //if true, display all possible toolbar menu icons for it
                if (_state.LastClickedEntity != null)
                {

                    var _entityState = _state.LastClickedEntity;


                    if (EntityUIWindows.checkIfCanOpenWindow<PinCameraBlankMenuHelper>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<PinCameraBlankMenuHelper>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(PinCameraBlankMenuHelper)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(PinCameraBlankMenuHelper)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<OrbitOrderWindow>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<OrbitOrderWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(OrbitOrderWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<ChangeCurrentOrbitWindow>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<ChangeCurrentOrbitWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(ChangeCurrentOrbitWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<WeaponTargetingControl>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<WeaponTargetingControl>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(WeaponTargetingControl)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<RenameWindow>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<RenameWindow>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(RenameWindow)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(RenameWindow)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<CargoTransfer>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<CargoTransfer>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(CargoTransfer)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(CargoTransfer)]);
                    }
                    if (EntityUIWindows.checkIfCanOpenWindow<ColonyPanel>(_entityState))
                    {

                        EntityUIWindows.openUIWindow<ColonyPanel>(ImGui.SmallButton(GlobalUIState.namesForMenus[typeof(ColonyPanel)]), _entityState, _state, false);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(GlobalUIState.namesForMenus[typeof(ColonyPanel)]);
                    }

                }
                ImGui.End();
            }
        }
    }
}

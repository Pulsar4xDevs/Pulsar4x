using System;
using ImGuiNET;
using Pulsar4X.ImGuiNetUI;
using Pulsar4X.ImGuiNetUI.EntityManagement;

namespace Pulsar4X.SDL2UI
{

    public class EntityContextMenu
    {
        GlobalUIState _state;
        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, int entityGuid)
        {
            _state = state;
            //_uiState.OpenWindows.Add(this);
            //IsActive = true;
            _entityState = state.StarSystemStates[state.SelectedStarSysGuid].EntityStatesWithNames[entityGuid];

        }

        internal void Display()
        {
            ImGui.BeginGroup();

            void ContextButton(Type T)
            {
                //Creates a context button if it is valid
                if(EntityUIWindows.CheckIfCanOpenWindow(T, _entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.NamesForMenus[T]);
                    {
                        EntityUIWindows.OpenUIWindow(T, _entityState, _state, buttonresult ,true);
                    }
                }
            }

            //Creates all the context buttons
            ContextButton(typeof(SelectPrimaryBlankMenuHelper));
            ContextButton(typeof(PinCameraBlankMenuHelper));
            ContextButton(typeof(RenameWindow));
            ContextButton(typeof(FireControl));
            ContextButton(typeof(CargoTransfer));
            ContextButton(typeof(ColonyPanel));
            ContextButton(typeof(PlanetaryWindow));
            ContextButton(typeof(GotoSystemBlankMenuHelper));
            ContextButton(typeof(WarpOrderWindow));
            ContextButton(typeof(ChangeCurrentOrbitWindow));
            ContextButton(typeof(NavWindow));
            ContextButton(typeof(OrdersListUI));
            ImGui.EndGroup();

        }
    }
}

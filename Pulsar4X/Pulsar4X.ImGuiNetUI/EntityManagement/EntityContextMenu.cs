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
            //_uiState.OpenWindows.Add(this);
            //IsActive = true;
            _entityState = state.StarSystemStates[state.SelectedStarSysGuid].EntityStatesWithNames[entityGuid];

        }



        internal void Display()
        {
            ActiveEntity = _entityState.Entity;
            ImGui.BeginGroup();

            void ContextButton(Type T)
            {
                //Creates a context button if it is valid
                if(EntityUIWindows.checkIfCanOpenWindow(T, _entityState)){
                bool buttonresult = ImGui.SmallButton(GlobalUIState.namesForMenus[T]);
                    {

                        EntityUIWindows.openUIWindow(T, _entityState, _state, buttonresult ,true);

                    }
                
                }
            }
            
            //Creates all the context buttons
            ContextButton(typeof(SelectPrimaryBlankMenuHelper));
            ContextButton(typeof(PinCameraBlankMenuHelper));
            ContextButton(typeof(RenameWindow));
            ContextButton(typeof(WeaponTargetingControl));
            ContextButton(typeof(CargoTransfer));
            ContextButton(typeof(ColonyPanel));
            ContextButton(typeof(PlanetaryWindow));
            ContextButton(typeof(GotoSystemBlankMenuHelper));
            ContextButton(typeof(WarpOrderWindow));
            ContextButton(typeof(ChangeCurrentOrbitWindow));

            ImGui.EndGroup();

        }
    }
}

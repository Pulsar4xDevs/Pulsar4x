using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public struct EntityState
    {
        public Entity Entity;
        public string Name;

        public NameIcon NameIcon;
        public OrbitIcon OrbitIcon;

    }

    public class EntityContextMenu
    {

        GlobalUIState _state;
        internal Entity ActiveEntity; //interacting with/ordering this entity


        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, Guid entityGuid)
        {
            _state = state;
            //_state.OpenWindows.Add(this);
            //IsActive = true;
            _entityState = state.MapRendering.IconEntityStates[entityGuid];

        }

        internal void Display()
        {
            ActiveEntity = _entityState.Entity;
            ImGui.BeginGroup();
            if (ImGui.SmallButton("Pin Camera"))
            {
                _state.Camera.PinToEntity(_entityState.Entity);
                ImGui.CloseCurrentPopup();
            }

            //if entity can move
            if (_entityState.Entity.HasDataBlob<PropulsionDB>())
            {
                if (ImGui.SmallButton("Orbit"))
                {
                    OrbitOrderWindow.GetInstance(_entityState).IsActive = true;
                    _state.ActiveWindow = OrbitOrderWindow.GetInstance(_entityState);
                }
            }
            if (ImGui.SmallButton("Fire Control"))
            {
                var instance = WeaponTargetingControl.GetInstance(_entityState);
                instance.SetOrderEntity(_entityState);
                instance.IsActive = true;
                _state.ActiveWindow = instance;
            }

            //if entity can target


            //if entity can mine || refine || build
            //econOrderwindow

            ImGui.EndGroup();

        }
    }
}

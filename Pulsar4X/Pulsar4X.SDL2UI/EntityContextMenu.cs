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

    public interface IOrderWindow
    {
        void TargetEntity(EntityState entity);
    }

    public class EntityContextMenu : PulsarGuiWindow
    {


        internal Entity ActiveEntity; //interacting with/ordering this entity


        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, Guid entityGuid)
        {
            _state = state;
            _state.OpenWindows.Add(this);
            IsActive = true;
            _entityState = state.MapRendering.IconEntityStates[entityGuid];

        }

        protected override void DisplayActual()
        {
            ActiveEntity = _entityState.Entity;
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
                    _state.ActiveWindow = new OrbitOrderWindow(_state, _entityState);

                }
            }

            //if entity can target


            //if entity can mine || refine || build
            //econOrderwindow



        }
    }
}

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

    public class OrbitOrderWindow : PulsarGuiWindow, IOrderWindow
    {
        EntityState OrderingEntity;
        EntityState TargetEntity;
        GlobalUIState _state;
        string _displayText;
        public OrbitOrderWindow(GlobalUIState state, EntityState entity)
        {
            _state = state;
            OrderingEntity = entity;
            _state.OpenWindows.Add(this);
            _displayText = "Order: " + OrderingEntity.Name;
        }
        
        internal override void Display()
        {
            ImVec2 size = new ImVec2(200, 100);
            ImVec2 pos = new ImVec2(_state.MainWinSize.x / 2 - size.x / 2, _state.MainWinSize.y / 2 - size.y / 2);

            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(pos, ImGuiCond.FirstUseEver);

            ImGui.Begin(_displayText, ref IsActive, _flags);
            ImGui.Text("Target: ");
            ImGui.SameLine();
            if (TargetEntity.Entity == null)
                ImGui.Text("Select Body To Orbit");
            else
            {
                ImGui.Text(TargetEntity.Name);
                ImGui.SetTooltip("Point Of Orbital Insertion, This will be the Apoapsis");
                ImGui.Text("Apoapsis: ");

                ImGui.Text("Periapsis: ");
                ImGui.SameLine();
                ImGui.Text("Km");

            }



            ImGui.End();
        }

        void IOrderWindow.TargetEntity(EntityState entity)
        {
            TargetEntity = entity;
        }
    }

    public class EntityContextMenu : PulsarGuiWindow
    {


        internal Entity ActiveEntity; //interacting with/ordering this entity

        GlobalUIState _state;
        EntityState _entityState;

        public EntityContextMenu(GlobalUIState state, Guid entityGuid)
        {
            _state = state;
            _entityState = state.MapRendering.IconEntityStates[entityGuid];

        }

        internal override void Display()
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
                    _state.ActiveOrderWidow = new OrbitOrderWindow(_state, _entityState);

                }
            }

            //if entity can target


            //if entity can mine || refine || build
            //econOrderwindow



        }
    }
}

using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SMPannel : PulsarGuiWindow
    {
        private static SMPannel instance = null;
        public static bool Exsists;

        EntityState _selectedEntity;

        private SMPannel() 
        {

            Exsists = true;
            instance = this;
            IsActive = false;
            _state.SpaceMasterVM = new SpaceMasterVM();

        }
        //TODO auth of some kind. 
        public static SMPannel GetInstance()
        {
            if (instance != null)
                return instance;
            else
            {       
                return new SMPannel();
            }
        }

        internal override void Display()
        {
            //selectedEntityData
            if (ImGui.Begin("SM", ref IsActive, _flags))
            {
                if (_selectedEntity.Entity != null)
                {
                    Entity entity = _selectedEntity.Entity;
                    var datablobs = entity.DataBlobs;
                    ImGui.Text(_selectedEntity.Name);
                    foreach (var datablob in datablobs)
                    {
                        ImGui.Text(datablob.GetType().Name);
                    }
                    if (ImGui.Button("AddOrbit"))
                    {
                        //_state.LoadedWindows.Add(new OrbitOrderWindow(_state, _selectedEntity, true));
                    }

                }
                ImGui.End();
            }
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }
}

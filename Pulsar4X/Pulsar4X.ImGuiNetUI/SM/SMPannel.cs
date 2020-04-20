using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class SMPannel : PulsarGuiWindow
    {


        EntityState _selectedEntity;

        private SMPannel() 
        {
            _uiState.SpaceMasterVM = new SpaceMasterVM();
        }
        //TODO auth of some kind. 
        public static SMPannel GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(SMPannel)))
            {
                return new SMPannel();
            }
            return (SMPannel)_uiState.LoadedWindows[typeof(SMPannel)];
        }

        internal override void Display()
        {
            //selectedEntityData
            if (ImGui.Begin("SM", ref IsActive, _flags))
            {
                if (_selectedEntity != null && _selectedEntity.Entity != null)
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
                        var pannel = WarpOrderWindow.GetInstance(_selectedEntity, true);
                        pannel.SetActive();
                        _uiState.ActiveWindow = pannel;
                    }

                }

            }
            ImGui.End();
        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }
}
